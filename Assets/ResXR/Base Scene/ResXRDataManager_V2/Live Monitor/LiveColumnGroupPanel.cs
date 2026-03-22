using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using TMPro;
using ResXRData;
using UnityEngine;

public class LiveColumnGroupPanel : MonoBehaviour
{
    [Header("Live Stream Source")]
    public LiveStreamKind liveStreamKind;
    public LiveColumnGroupKind liveColumnGroupKind;
    [Header("Sub-grouping")]
    public LiveColumnSubGroupKind subGroupKind = LiveColumnSubGroupKind.All;


    [Header("References")]
    public Transform contentGridContainer;
    public GameObject textBlockPrefab;
    public GameObject Back;
    public TextMeshPro TitleText;

    [Header("Settings")]
    [Range(1, 60)]
    public float UIrefreshRateHz = 15f;
    public int entriesPerBlock = 70;
    public bool expandOnStart = false;


    private LiveMonitorService _tap;
    private ColumnIndex _schema;
    private LiveColumnGroup _groupSpec;
    private List<TextMeshPro> _blocks = new List<TextMeshPro>();
    private List<int> _displayColumns = new List<int>();
    private bool _IsFirstUpdate = true;
    // Debug: store per-block bounds for visualization
    private readonly Dictionary<TextMeshPro, Bounds> _debugBlockBounds = new();
    // Debug: aggregate bounds of all blocks
    private Bounds _debugTotalBounds;


    private float _nextRefreshTime;

    private void Start()
    {
        _tap = LiveMonitorService.Instance;
        if (_tap == null)
        {
            Debug.LogWarning("[LiveColumnGroupPanel] LiveMonitorService not found.");
            enabled = false;
            return;
        }
        ExpandPanel(expandOnStart);

        StartCoroutine(WaitForSchemaAndBuild());
    }

    private System.Collections.IEnumerator WaitForSchemaAndBuild()
    {
        while (_schema == null)
        {
            LiveRow row;
            bool ok = liveStreamKind == LiveStreamKind.Continuous
                ? _tap.TryGetLatestContinuous(out row)
                : _tap.TryGetLatestFace(out row);

            if (ok && row.IsValid)
            {
                _schema = row.schema;
                break;
            }
            yield return null;
        }

        if (_schema == null)
        {
            Debug.LogWarning("[LiveColumnGroupPanel] No schema available.");
            yield break;
        }

        // Build groups, pick our group
        var groups = LiveColumnGroups.BuildGroups(_schema, liveStreamKind);
        _groupSpec = groups.Find(g => g.Kind == liveColumnGroupKind);
        if (_groupSpec == null || _groupSpec.AllColumns.Count == 0)
        {
            Debug.LogWarning($"[LiveColumnGroupPanel] No columns for group {liveColumnGroupKind}.");
            yield break;
        }

        // Build the per-panel column list based on sub-group kind
        _displayColumns = BuildDisplayColumnList();
        if (_displayColumns == null || _displayColumns.Count == 0)
        {
            Debug.LogWarning($"[LiveColumnGroupPanel] No display columns after filtering for {subGroupKind} in group {liveColumnGroupKind}. Falling back to All.");
            _displayColumns = new List<int>(_groupSpec.AllColumns);
        }

        BuildBlocks();
        // --- Auto title update ---
        if (TitleText != null)
        {
            TitleText.text = $"({liveStreamKind}) {liveColumnGroupKind} / {subGroupKind}";
        }

    }

    private List<int> BuildDisplayColumnList()
    {
        // Base list
        var source = _groupSpec.AllColumns;

        if (subGroupKind == LiveColumnSubGroupKind.All)
            return new List<int>(source);

        List<int> result = new List<int>();

        for (int i = 0; i < source.Count; i++)
        {
            int colIdx = source[i];
            string colName = _schema[colIdx];

            bool isFlag = IsFlagColumnName(colName);
            bool isTime = IsTimeColumnName(colName);

            switch (subGroupKind)
            {
                case LiveColumnSubGroupKind.FlagsOnly:
                    if (isFlag)
                        result.Add(colIdx);
                    break;

                case LiveColumnSubGroupKind.ValuesOnly:
                    // values = not flags, not time
                    if (!isFlag && !isTime)
                        result.Add(colIdx);
                    break;

                case LiveColumnSubGroupKind.TimeOnly:
                    if (isTime)
                        result.Add(colIdx);
                    break;
            }
        }

        return result;
    }


    private static bool IsFlagColumnName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        // Explicit singletons
        if (name == "shouldRecenter" || name == "recenterEvent")
            return true;

        // Suffix-based / pattern-based flags
        if (name.EndsWith("_Flags", System.StringComparison.Ordinal))
            return true;
        if (name.EndsWith("_IsValid", System.StringComparison.Ordinal))
            return true;

        // Generic states
        if (name.Contains("Status", System.StringComparison.Ordinal))
            return true;
        if (name.Contains("Present", System.StringComparison.Ordinal))
            return true;
        if (name.Contains("Valid_", System.StringComparison.Ordinal))
            return true;
        if (name.Contains("Tracked_", System.StringComparison.Ordinal))
            return true;
        if (name.Contains("Conf", System.StringComparison.Ordinal))
            return true;

        // You can extend this with more heuristics if you want
        return false;
    }

    private static bool IsTimeColumnName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        // Explicitly NOT time: you asked to keep this in "other"
        if (name == "timeSinceStartup")
            return false;

        // Any column with "Time" in the name:
        // Node_*_Time, Eyes_Time, Body_Time, Face_Time, HeadNodeTime, etc.
        if (name.Contains("Time", System.StringComparison.Ordinal))
            return true;

        // Hand timestamps: RequestedTS / SampleTS
        if (name.EndsWith("TS", System.StringComparison.Ordinal) ||
            name.Contains("_TS", System.StringComparison.Ordinal))
            return true;

        return false;
    }



    private void BuildBlocks()
    {
        foreach (Transform child in contentGridContainer)
            Destroy(child.gameObject);
        _blocks.Clear();

        int total = _displayColumns.Count;

        int blockCount = Mathf.Max(1, (total + entriesPerBlock - 1) / entriesPerBlock);

        for (int i = 0; i < blockCount; i++)
        {
            TextMeshPro inst = Instantiate(textBlockPrefab, contentGridContainer).GetComponent<TextMeshPro>();
            inst.text = "";
            _blocks.Add(inst);
        }

    }




    private void Update()
    {
        if (_schema == null || _blocks.Count == 0) return;

        float now = Time.unscaledTime;
        if (now < _nextRefreshTime) return;
        _nextRefreshTime = now + (1f / Mathf.Max(1f, UIrefreshRateHz));

        LiveRow row;
        bool ok = liveStreamKind == LiveStreamKind.Continuous
            ? _tap.TryGetLatestContinuous(out row)
            : _tap.TryGetLatestFace(out row);

        if (!ok || !row.IsValid)
        {
            foreach (var b in _blocks) b.text = "(no data yet)";
            return;
        }

        UpdateBlocks(row);
    }

    private void UpdateBlocks(LiveRow row)
    {
        var schema = row.schema;
        var values = row.values;
        var mask = row.columnIsSetMask;

        int total = _displayColumns.Count;

        for (int blockIndex = 0; blockIndex < _blocks.Count; blockIndex++)
        {
            int start = blockIndex * entriesPerBlock;
            int end = Mathf.Min(start + entriesPerBlock, total);

            var sb = new StringBuilder(256);

            for (int i = start; i < end; i++)
            {
                int colIdx = _displayColumns[i];
                string name = schema[colIdx];

                sb.Append(name);
                sb.Append(": ");

                if (mask[colIdx] && values[colIdx] != null)
                    sb.Append(FormatValue(values[colIdx]));
                else
                    sb.Append("?");

                if (i < end - 1)
                    sb.AppendLine();
            }

            _blocks[blockIndex].text = sb.ToString();
        }

        if (_IsFirstUpdate)
        {
            //ResizeBackNextFrameAsync().Forget(); //auto option, TODO: need to debug, not working properly 
            SimpleResizeBackQuad();
            _IsFirstUpdate = false;
        }
    }
    #region Resize Back Quad
    private void ResizeBackQuadToBlocks()
    {
        if (Back == null || _blocks == null || _blocks.Count == 0)
            return;

        Vector3 min = new(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 max = new(float.MinValue, float.MinValue, float.MinValue);

        _debugBlockBounds.Clear();

        foreach (var tmp in _blocks)
        {
            if (!TryGetTightWorldBounds(tmp, out Bounds b))
                continue;

            _debugBlockBounds[tmp] = b;

            Debug.Log($"TMP {tmp.name} tight bounds: center={b.center} size={b.size}");

            min = Vector3.Min(min, b.min);
            max = Vector3.Max(max, b.max);
        }

        if (_debugBlockBounds.Count == 0)
            return;

        Vector3 sizeWorld = max - min;

        float margin = 0.01f;
        sizeWorld.x += margin * 2f;
        sizeWorld.y += margin * 2f;

        Vector3 centerWorld = (min + max) * 0.5f;

        // store for gizmos if you like
        _debugTotalBounds = new Bounds(centerWorld, sizeWorld);

        // convert world size to local scale, compensating for parent scale
        Transform t = Back.transform;
        Vector3 parentScale = t.parent != null ? t.parent.lossyScale : Vector3.one;

        Vector3 targetLocalScale = new Vector3(
            sizeWorld.x / Mathf.Max(parentScale.x, 1e-6f),
            sizeWorld.y / Mathf.Max(parentScale.y, 1e-6f),
            t.localScale.z
        );

        t.localScale = targetLocalScale;
        t.position = new Vector3(centerWorld.x, centerWorld.y, t.position.z);
    }

    private async UniTaskVoid ResizeBackNextFrameAsync()
    {
        await UniTask.DelayFrame(2);

        bool containerWasActive = contentGridContainer.gameObject.activeSelf;
        if (!containerWasActive)
            contentGridContainer.gameObject.SetActive(true);

        foreach (var b in _blocks)
            b.ForceMeshUpdate(forceTextReparsing: true);

        await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);

        ResizeBackQuadToBlocks();

        if (!containerWasActive)
            contentGridContainer.gameObject.SetActive(false);
    }

    private static bool TryGetTightWorldBounds(TextMeshPro tmp, out Bounds bounds)
    {
        bounds = new Bounds();
        if (tmp == null)
            return false;

        var textInfo = tmp.textInfo;
        if (textInfo == null || textInfo.characterCount == 0)
            return false;

        bool initialized = false;
        var t = tmp.transform;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            var ch = textInfo.characterInfo[i];
            if (!ch.isVisible)
                continue;

            // character quad in local space
            Vector3 bl = t.TransformPoint(ch.bottomLeft);
            Vector3 tl = t.TransformPoint(ch.topLeft);
            Vector3 tr = t.TransformPoint(ch.topRight);
            Vector3 br = t.TransformPoint(ch.bottomRight);

            if (!initialized)
            {
                initialized = true;
                bounds = new Bounds(bl, Vector3.zero);
            }

            bounds.Encapsulate(bl);
            bounds.Encapsulate(tl);
            bounds.Encapsulate(tr);
            bounds.Encapsulate(br);
        }

        return initialized;
    }

    private void SimpleResizeBackQuad()
    {

        float margin = 0.06f;
        if (Back == null)
            return;
        float height = (0.035f * Mathf.Min(_displayColumns.Count, entriesPerBlock)) + (2 * margin);
        float width = (0.68f * _blocks.Count) + (2 * margin);
        Back.transform.localScale = new Vector3(width, height, 1f);

        Vector3 position = contentGridContainer.position + new Vector3(0f, -(height / 2) + margin, 0.02f);
        Back.transform.position = position;
    }

    #endregion

    private static string FormatValue(object value)
    {
        if (value == null) return "null";
        switch (value)
        {
            case float f: return f.ToString("F3", System.Globalization.CultureInfo.InvariantCulture);
            case double d: return d.ToString("F3", System.Globalization.CultureInfo.InvariantCulture);
            case int i: return i.ToString(System.Globalization.CultureInfo.InvariantCulture);
            case bool b: return b ? "true" : "false";
            default: return value.ToString();
        }
    }

    public void ExpandPanel(bool expand)
    {
        contentGridContainer.gameObject.SetActive(expand);
        Back.SetActive(expand);
    }

    //private void OnDrawGizmosSelected()
    //{
    //    // draw per-block magenta boxes
    //    if (_debugBlockBounds != null && _debugBlockBounds.Count > 0)
    //    {
    //        Gizmos.color = Color.magenta;
    //        foreach (var kvp in _debugBlockBounds)
    //        {
    //            Bounds b = kvp.Value;
    //            Gizmos.DrawWireCube(b.center, b.size);
    //        }
    //    }

    //    // draw total yellow box
    //    if (_debugTotalBounds.size != Vector3.zero)
    //    {
    //        Gizmos.color = Color.yellow;
    //        Gizmos.DrawWireCube(_debugTotalBounds.center, _debugTotalBounds.size);
    //    }

    //    // draw back quad cyan
    //    if (Back != null)
    //    {
    //        var backRenderer = Back.GetComponent<Renderer>();
    //        if (backRenderer != null)
    //        {
    //            Gizmos.color = Color.cyan;
    //            Gizmos.DrawWireCube(backRenderer.bounds.center, backRenderer.bounds.size);
    //        }
    //    }
    //}




}


