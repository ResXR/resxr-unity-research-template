// CustomDataClassValidator.cs
// Editor-only: runs after every assembly reload (compile) and checks:
//   1. Every public field on every CustomDataClass implementation has a [ColumnInfo] annotation,
//      and any Levels entries use the required "value:description" format.
//   2. Any column name shared across non-[BuiltInTable] tables has identical [ColumnInfo]
//      metadata in all tables — because the Python pipeline merges all custom tables into one
//      BIDS events table and every shared column name must describe the same data.
//
// Catches annotation problems immediately in the Unity Editor — no need to run a session.
// The runtime counterpart in ResXRDataManager.ValidateColumnAnnotations() remains for
// on-device builds where the Editor is not available (writes to both Debug and ResXRDebugLogs.csv).
//
// Uses TypeCache (Unity 2019.3+) — fast, no full assembly scan needed.

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
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

            // ── Check 1: per-field annotation + levels format ────────────────────────────
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
                            $"The field name will be used as a placeholder description in {type.Name}'s sidecar entry — " +
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

            // ── Check 2: cross-table column conflict detection ───────────────────────────
            // The Python pipeline merges all non-[BuiltInTable] custom tables into one BIDS events
            // table. Any column name shared across tables MUST have identical [ColumnInfo] metadata
            // (description, units, format, levels, min, max). Mismatches corrupt the merged sidecar.
            var firstSeen = new Dictionary<string, (System.Type type, FieldInfo field, ColumnInfoAttribute attr)>(StringComparer.Ordinal);

            foreach (System.Type type in types)
            {
                if (type.IsAbstract || type.IsInterface) continue;

                // Skip built-in tables — they are not part of the merged events table.
                if (type.GetCustomAttribute<BuiltInTableAttribute>() != null) continue;

                foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    ColumnInfoAttribute attr = field.GetCustomAttribute<ColumnInfoAttribute>();
                    if (attr == null) continue; // missing annotation already caught in Check 1

                    if (!firstSeen.TryGetValue(field.Name, out var prior))
                    {
                        firstSeen[field.Name] = (type, field, attr);
                        continue;
                    }

                    // Same column name found in a second type — compare all metadata.
                    var conflicts = new System.Text.StringBuilder();
                    if (!string.Equals(prior.attr.Description, attr.Description, System.StringComparison.Ordinal))
                        conflicts.Append($"\n  Description: \"{prior.attr.Description}\" vs \"{attr.Description}\"");
                    if (!string.Equals(prior.attr.Units, attr.Units, System.StringComparison.Ordinal))
                        conflicts.Append($"\n  Units: \"{prior.attr.Units}\" vs \"{attr.Units}\"");
                    if (!string.Equals(prior.attr.Format, attr.Format, System.StringComparison.Ordinal))
                        conflicts.Append($"\n  Format: \"{prior.attr.Format}\" vs \"{attr.Format}\"");
                    if (!LevelsEqual(prior.attr.Levels, attr.Levels))
                        conflicts.Append($"\n  Levels differ");
                    if (!DoubleMetaEqual(prior.attr.Minimum, attr.Minimum))
                        conflicts.Append($"\n  Minimum: {prior.attr.Minimum} vs {attr.Minimum}");
                    if (!DoubleMetaEqual(prior.attr.Maximum, attr.Maximum))
                        conflicts.Append($"\n  Maximum: {prior.attr.Maximum} vs {attr.Maximum}");

                    if (conflicts.Length > 0)
                        Debug.LogError(
                            $"[ResXR: CustomDataClassValidator] Cross-table column conflict: column '{field.Name}' appears in " +
                            $"both '{prior.type.Name}' and '{type.Name}' with different [ColumnInfo] metadata. " +
                            $"The Python pipeline merges all custom tables into one BIDS events table — " +
                            $"every shared column name must have identical annotations across all tables. " +
                            $"Conflicting fields:{conflicts}");
                }
            }
        }

        private static bool LevelsEqual(string[] a, string[] b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
                if (!string.Equals(a[i], b[i], System.StringComparison.Ordinal)) return false;
            return true;
        }

        private static bool DoubleMetaEqual(double a, double b)
        {
            if (double.IsNaN(a) && double.IsNaN(b)) return true;
            return a == b;
        }
    }
}
#endif
