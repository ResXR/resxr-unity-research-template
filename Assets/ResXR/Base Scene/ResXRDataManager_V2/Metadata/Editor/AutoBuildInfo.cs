
// Generates a unique build_id, writes StreamingAssets/build_info.json,
// and stamps version fields (bundleVersion + Android/iOS build numbers).

#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ResXRData
{

    public sealed class AutoBuildInfo : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        // ====== CONFIG (tweak if you like) ======
        // Append +<build_id> to PlayerSettings.bundleVersion (Application.version at runtime)
        private const bool AppendBuildIdToBundleVersion = true;

        // Auto-set platform build numbers (Android versionCode / iOS buildNumber)
        private const bool AutoSetPlatformBuildNumbers = true;

        // Optional: read a short git hash (written by CI) from this file, if present
        private const string GitShortHashFileRelative = "../.git_short_hash";

        // ====== ENTRYPOINT ======
        public void OnPreprocessBuild(BuildReport report)
        {
            // 1) Generate a unique build ID
            string buildId = GenerateBuildId(); // e.g. 20250903-121530-7F3A9C2B

            // 2) Compose build_info.json payload
            var info = new BuildInfo
            {
                build_id = buildId,
                utc_build_iso8601 = DateTime.UtcNow.ToString("o"),
                unity = Application.unityVersion,
                target = report.summary.platform.ToString(),
                git_commit = TryReadGitShortHash()
            };

            // 3) Write to StreamingAssets/build_info.json (unpacked into APK under assets/)
            WriteBuildInfoJson(info);

            // 4) Stamp version fields for traceability / install-over-install
            StampVersionFields(buildId, report.summary.platform);

            Debug.Log($"[AutoBuildInfo] build_id={buildId}  unity={info.unity}  target={info.target}  git={info.git_commit}");
        }

        // ====== HELPERS ======
        private static string GenerateBuildId()
        {
            // UTC timestamp + 8-char GUID for uniqueness (sortable & compact)
            return $"{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant()}";
        }

        [Serializable]
        private class BuildInfo
        {
            public string build_id;
            public string utc_build_iso8601;
            public string unity;
            public string target;
            public string git_commit; // optional (blank if not available)
        }

        private static string TryReadGitShortHash()
        {
            try
            {
                string path = Path.Combine(Application.dataPath, GitShortHashFileRelative);
                if (File.Exists(path))
                {
                    var txt = File.ReadAllText(path).Trim();
                    return string.IsNullOrEmpty(txt) ? "" : txt;
                }
            }
            catch
            {
                Debug.Log("[AutoBuildInfo] No git short hash file found.");
            }
            return "";
        }

        private static void WriteBuildInfoJson(BuildInfo info)
        {
            string streaming = Path.Combine(Application.dataPath, "StreamingAssets");
            if (!Directory.Exists(streaming)) Directory.CreateDirectory(streaming);

            string jsonPath = Path.Combine(streaming, "build_info.json");
            string json = JsonUtility.ToJson(info, prettyPrint: true);
            File.WriteAllText(jsonPath, json);
            AssetDatabase.ImportAsset(RelativeToProject(jsonPath));
        }

        private static string RelativeToProject(string absolutePath)
        {
            var proj = Directory.GetParent(Application.dataPath)!.FullName.Replace('\\', '/');
            return absolutePath.Replace('\\', '/').Replace(proj + "/", "");
        }

        private static void StampVersionFields(string buildId, BuildTarget target)
        {
            if (AppendBuildIdToBundleVersion)
            {
                // Keep your human-readable semantic version as-is, just append +build_id
                // Example: "1.4.3+20250903-121530-7F3A9C2B"
                string human = PlayerSettings.bundleVersion; // what you set in Player Settings
                if (!string.IsNullOrEmpty(buildId) && (human == null || !human.Contains("+")))
                {
                    PlayerSettings.bundleVersion = $"{human}+{buildId}";
                }
            }

            if (!AutoSetPlatformBuildNumbers) return;

            // Monotonic integer that increases every minute since 2000-01-01 (fits in int32)
            int monotonicCode = GenerateMonotonicCode();

            // Android: versionCode (must increase to install over an older build)
#if UNITY_ANDROID
            try
            {
                PlayerSettings.Android.bundleVersionCode = monotonicCode;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AutoBuildInfo] Failed to set Android.versionCode: {e.Message}");
            }
#endif

            // iOS: buildNumber is a string, but should be monotonically increasing
#if UNITY_IOS
        try
        {
            // Keep it compact but sortable, e.g., "20250903.1215"
            string iosBuild = $"{DateTime.UtcNow:yyyyMMdd.HHmm}";
            PlayerSettings.iOS.buildNumber = iosBuild;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AutoBuildInfo] Failed to set iOS.buildNumber: {e.Message}");
        }
#endif
        }

        private static int GenerateMonotonicCode()
        {
            var now = DateTime.UtcNow;
            int days = (int)(now - new DateTime(2000, 1, 1)).TotalDays;
            int minutes = now.Hour * 60 + now.Minute;
            return days * 10000 + minutes; // increases every minute
        }

        // ====== Convenience menu (optional) ======
        [MenuItem("Tools/Build Info/Preview build_info.json")]
        private static void PreviewBuildInfo()
        {
            string path = Path.Combine(Application.dataPath, "StreamingAssets/build_info.json");
            if (File.Exists(path))
                Debug.Log($"[AutoBuildInfo] {path}\n{File.ReadAllText(path)}");
            else
                Debug.LogWarning("[AutoBuildInfo] build_info.json not found. Trigger a build or reimport.");
        }
    }
#endif
}