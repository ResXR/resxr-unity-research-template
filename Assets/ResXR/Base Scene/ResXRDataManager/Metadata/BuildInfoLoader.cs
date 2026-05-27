
// Runtime loader for StreamingAssets/build_info.

using Cysharp.Threading.Tasks; // <- UniTask
using System;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace ResXRData
{


    public sealed class BuildInfoLoader : ResXRSingleton<BuildInfoLoader>
    {
        [Serializable]
        public class BuildInfo
        {
            public string build_id = "";
            public string utc_build_iso8601 = "";
            public string unity = "";
            public string target = "";
            public string git_commit = "";
        }

        public BuildInfo Current { get; private set; } = new BuildInfo();
        public bool IsLoaded { get; private set; }
        public event Action<BuildInfo> OnLoaded;

        public static string StreamingBuildInfoPath =>
            Path.Combine(Application.streamingAssetsPath, "build_info.json");

        protected override void DoInAwake()
        {
            // Kick off async load on the main thread; no DontDestroyOnLoad required the template.
            LoadAsync().Forget(); // fire-and-forget (safe; we gate with IsLoaded)
        }

        /// <summary>
        /// Loads StreamingAssets/build_info.json on all platforms.
        /// UniTask version: no thread-pool hops; yields on PlayerLoop.
        /// </summary>
        public async UniTask LoadAsync()
        {
            if (IsLoaded) return;

            try
            {
                string json = null;

                if (RequiresWebRequest(Application.streamingAssetsPath)) //If Android/WebGL (jar:/http:) - uses UnityWebRequest and awaits it with UniTask.
                {
                    using var req = UnityWebRequest.Get(StreamingBuildInfoPath);
                    await req.SendWebRequest().ToUniTask(); // await UWR via UniTask

#if UNITY_2020_2_OR_NEWER
                    if (req.result != UnityWebRequest.Result.Success)
#else
                if (req.isHttpError || req.isNetworkError)
#endif
                    {
                        Debug.LogWarning($"[BuildInfoLoader] Failed to read build_info.json: {req.error}");
                    }
                    else
                    {
                        json = req.downloadHandler.text;
                    }
                }
                else //If desktop/iOS - direct file IO works, so File.ReadAllText.
                {
                    var path = StreamingBuildInfoPath;
                    if (File.Exists(path)) json = File.ReadAllText(path);
                    else Debug.LogWarning($"[BuildInfoLoader] build_info.json not found at {path}");
                }

                if (!string.IsNullOrEmpty(json))
                    Current = JsonUtility.FromJson<BuildInfo>(json) ?? new BuildInfo();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[BuildInfoLoader] Exception reading build_info.json: {e.Message}");
            }
            finally
            {
                IsLoaded = true;
                OnLoaded?.Invoke(Current);
            }
        }

        private static bool RequiresWebRequest(string streamingAssetsPath)
        {
            var p = streamingAssetsPath ?? "";
            return p.StartsWith("jar:", StringComparison.OrdinalIgnoreCase)
                || p.StartsWith("zip:", StringComparison.OrdinalIgnoreCase)
                || p.StartsWith("http", StringComparison.OrdinalIgnoreCase);
        }
    }
}