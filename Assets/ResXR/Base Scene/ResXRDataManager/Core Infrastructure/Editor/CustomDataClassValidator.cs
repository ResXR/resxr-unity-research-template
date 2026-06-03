// CustomDataClassValidator.cs
// Editor-only: runs after every assembly reload (compile) and checks that every
// public field on every CustomDataClass implementation has a [ColumnInfo] annotation.
//
// Catches missing annotations immediately in the Unity Editor — no need to run a session.
// The runtime counterpart in ResXRDataManager.ValidateColumnAnnotations() remains for
// on-device builds where the Editor is not available (writes to both Debug and ResXRLogs.csv).
//
// Uses TypeCache (Unity 2019.3+) — fast, no full assembly scan needed.

#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ResXRData
{
    [InitializeOnLoad]
    public static class CustomDataClassValidator
    {
        static CustomDataClassValidator()
        {
            // TypeCache uses Unity's internal type index — much faster than AppDomain.GetAssemblies().
            // Returns all concrete types implementing CustomDataClass across the whole project.
            var types = TypeCache.GetTypesDerivedFrom<CustomDataClass>();

            foreach (System.Type type in types)
            {
                if (type.IsAbstract || type.IsInterface) continue;

                // onset and duration are interface PROPERTIES, not fields — GetFields skips them automatically.
                foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (field.GetCustomAttribute<ColumnInfoAttribute>() == null)
                    {
                        Debug.LogError(
                            $"[ResXR: CustomDataClassValidator] Field '{field.Name}' in '{type.Name}' has no [ColumnInfo] annotation. " +
                            $"The field name will be used as a placeholder description in custom_tables_sidecar.json — " +
                            $"the Python pipeline will have no verified metadata for this field. " +
                            $"Add [ColumnInfo(\"description\")] to provide accurate BIDS metadata.");
                    }
                }
            }
        }
    }
}
#endif
