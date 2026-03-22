using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System;
using NaughtyAttributes;


/// <summary>
/// Resizes a background mesh (e.g., Quad) to match the combined bounds of one or more 3D TextMeshPro components.
/// Also draws gizmos showing the detected bounds.
/// </summary>

public class TMPBackPanelResizer : MonoBehaviour
{
    [Header("References")]
    [Tooltip("3D TextMeshPro components (NOT TextMeshProUGUI).")]
    public List<TextMeshPro> tmps = new List<TextMeshPro>();

    [Tooltip("Background mesh (Quad/Plane) positioned behind the text.")]
    public Transform backPanel;

    [Header("Padding (local units)")]
    [Tooltip("Adds extra space around the text bounds. X = horizontal, Y = vertical.")]
    public Vector2 padding = new Vector2(0.1f, 0.05f);

    [Header("Options")]
    public bool resizeOnAwake = true;
    public bool resizeEveryFrame = false;

    // Cached last computed bounds (in THIS component's local space) for drawing gizmos
    private Bounds lastBounds;
    private bool hasLastBounds = false;

    private void Reset()
    {
        // Auto-fill: grab TMPs on self + children
        tmps.Clear();
        GetComponentsInChildren(includeInactive: true, result: tmps);

        if (backPanel == null && transform.childCount > 0)
            backPanel = transform.GetChild(0);
    }

    private void Awake()
    {
        if (resizeOnAwake)
            ResizeBackPanel();
    }

    private void Update()
    {
        if (resizeEveryFrame)
            ResizeBackPanel();
    }

    [Button]
    public void ResizeBackPanel()
    {
        if (backPanel == null || tmps == null || tmps.Count == 0)
            return;

        // Compute combined bounds in THIS object's local space
        if (!TryGetCombinedLocalBounds(out Bounds combinedLocalBounds))
        {
            hasLastBounds = false;
            return;
        }

        lastBounds = combinedLocalBounds;
        hasLastBounds = true;

        Vector3 size = combinedLocalBounds.size;
        Vector3 center = combinedLocalBounds.center;

        float width = size.x + padding.x * 2f;
        float height = size.y + padding.y * 2f;

        // Scale back panel (assumes 1x1 quad centered at (0,0) in its own local)
        Vector3 scale = backPanel.localScale;
        scale.x = width;
        scale.y = height;
        backPanel.localScale = scale;

        // Position behind combined center (Z unchanged)
        Vector3 pos = backPanel.localPosition;
        pos.x = center.x;
        pos.y = center.y;
        backPanel.localPosition = pos;
    }

    private bool TryGetCombinedLocalBounds(out Bounds combined)
    {
        combined = default;
        bool any = false;

        // We want bounds in THIS object's local space, so we:
        // TMP local bounds -> TMP world -> THIS local.
        Matrix4x4 worldToThisLocal = transform.worldToLocalMatrix;

        for (int i = 0; i < tmps.Count; i++)
        {
            TextMeshPro t = tmps[i];
            if (t == null) continue;

            t.ForceMeshUpdate();

            Bounds tmpLocalBounds = t.bounds; // local to TMP transform

            // Convert the 8 corners to THIS local space and encapsulate
            Matrix4x4 tmpLocalToWorld = t.transform.localToWorldMatrix;

            Vector3 c = tmpLocalBounds.center;
            Vector3 e = tmpLocalBounds.extents;

            Vector3[] corners = new Vector3[8]
            {
                c + new Vector3(-e.x, -e.y, -e.z),
                c + new Vector3( e.x, -e.y, -e.z),
                c + new Vector3( e.x,  e.y, -e.z),
                c + new Vector3(-e.x,  e.y, -e.z),
                c + new Vector3(-e.x, -e.y,  e.z),
                c + new Vector3( e.x, -e.y,  e.z),
                c + new Vector3( e.x,  e.y,  e.z),
                c + new Vector3(-e.x,  e.y,  e.z),
            };

            for (int k = 0; k < corners.Length; k++)
            {
                Vector3 world = tmpLocalToWorld.MultiplyPoint3x4(corners[k]);
                Vector3 localToThis = worldToThisLocal.MultiplyPoint3x4(world);

                if (!any)
                {
                    combined = new Bounds(localToThis, Vector3.zero);
                    any = true;
                }
                else
                {
                    combined.Encapsulate(localToThis);
                }
            }
        }

        return any;
    }

    private void OnDrawGizmos()
    {
        if (!hasLastBounds) return;

        Gizmos.color = Color.magenta;

        // lastBounds is in THIS object's local space -> convert to world for drawing
        Matrix4x4 localToWorld = transform.localToWorldMatrix;

        Vector3 c = lastBounds.center;
        Vector3 s = lastBounds.size;
        Vector3 ext = s * 0.5f;

        Vector3[] corners = new Vector3[8];
        corners[0] = localToWorld.MultiplyPoint3x4(c + new Vector3(-ext.x, -ext.y, -ext.z));
        corners[1] = localToWorld.MultiplyPoint3x4(c + new Vector3(ext.x, -ext.y, -ext.z));
        corners[2] = localToWorld.MultiplyPoint3x4(c + new Vector3(ext.x, ext.y, -ext.z));
        corners[3] = localToWorld.MultiplyPoint3x4(c + new Vector3(-ext.x, ext.y, -ext.z));
        corners[4] = localToWorld.MultiplyPoint3x4(c + new Vector3(-ext.x, -ext.y, ext.z));
        corners[5] = localToWorld.MultiplyPoint3x4(c + new Vector3(ext.x, -ext.y, ext.z));
        corners[6] = localToWorld.MultiplyPoint3x4(c + new Vector3(ext.x, ext.y, ext.z));
        corners[7] = localToWorld.MultiplyPoint3x4(c + new Vector3(-ext.x, ext.y, ext.z));

        DrawBoxEdge(0, 1, corners); DrawBoxEdge(1, 2, corners); DrawBoxEdge(2, 3, corners); DrawBoxEdge(3, 0, corners);
        DrawBoxEdge(4, 5, corners); DrawBoxEdge(5, 6, corners); DrawBoxEdge(6, 7, corners); DrawBoxEdge(7, 4, corners);
        DrawBoxEdge(0, 4, corners); DrawBoxEdge(1, 5, corners); DrawBoxEdge(2, 6, corners); DrawBoxEdge(3, 7, corners);
    }

    private void DrawBoxEdge(int a, int b, Vector3[] corners)
    {
        Gizmos.DrawLine(corners[a], corners[b]);
    }

    internal void RegisterTextComponent(TextMeshPro textComp)
    {
        tmps.Add(textComp);
    }
}
