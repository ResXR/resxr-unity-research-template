// CustomDataClassValidator.cs
// Editor-only: runs after every assembly reload (compile) and checks that every
// public field on every CustomDataClass implementation has a [ColumnInfo] annotation,
// and that any Levels entries use the required "value:description" format.
//
// Catches annotation problems immediately in the Unity Editor — no need to run a session.
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
                    ColumnInfoAttribute attr = field.GetCustomAttribute<ColumnInfoAttribute>();

                    if (attr == null)
                    {
                        Debug.LogError(
                            $"[ResXR: CustomDataClassValidator] Field '{field.Name}' in '{type.Name}' has no [ColumnInfo] annotation. " +
                            $"The field name will be used as a placeholder description in custom_tables_sidecar.json — " +
                            $"the Python pipeline will have no verified metadata for this field. " +
                            $"Add [ColumnInfo(\"description\")] to provide accurate BIDS metadata.");
                        continue;
                    }

                    // Validate levels format: each entry must be "value:description".
                    // Value-only entries are not allowed — BIDS requires a description for every level.
                    // See https://bids-specification.readthedocs.io/en/stable/common-principles.html#levels
                    if (attr.Levels != null)
                    {
                        foreach (string level in attr.Levels)
                        {
                            string entry = level?.Trim() ?? "";

                            if (string.IsNullOrWhiteSpace(entry))
                            {
                                Debug.LogError(
                                    $"[ResXR: CustomDataClassValidator] Field '{field.Name}' in '{type.Name}': " +
                                    $"a level entry is empty or whitespace. All levels must use \"value:description\" format. " +
                                    $"See https://bids-specification.readthedocs.io/en/stable/common-principles.html#levels");
                                continue;
                            }

                            int colon = entry.IndexOf(':');

                            if (colon < 0)
                            {
                                Debug.LogError(
                                    $"[ResXR: CustomDataClassValidator] Field '{field.Name}' in '{type.Name}': " +
                                    $"level \"{entry}\" has no description. Value-only levels are not allowed — " +
                                    $"use \"value:description\" format (e.g. \"{entry}:{entry} description here\"). " +
                                    $"See https://bids-specification.readthedocs.io/en/stable/common-principles.html#levels");
                                continue;
                            }

                            string key  = entry.Substring(0, colon).Trim();
                            string desc = entry.Substring(colon + 1).Trim();

                            if (string.IsNullOrEmpty(key))
                                Debug.LogError(
                                    $"[ResXR: CustomDataClassValidator] Field '{field.Name}' in '{type.Name}': " +
                                    $"level \"{entry}\" has an empty value (nothing before the colon). " +
                                    $"Use \"value:description\" format. " +
                                    $"See https://bids-specification.readthedocs.io/en/stable/common-principles.html#levels");

                            if (string.IsNullOrEmpty(desc))
                                Debug.LogError(
                                    $"[ResXR: CustomDataClassValidator] Field '{field.Name}' in '{type.Name}': " +
                                    $"level \"{entry}\" has an empty description (nothing after the colon). " +
                                    $"Use \"value:description\" format. " +
                                    $"See https://bids-specification.readthedocs.io/en/stable/common-principles.html#levels");
                        }
                    }
                }
            }
        }
    }
}
#endif
