using NaughtyAttributes;
using UnityEngine;

public class HorizontalObjectLayoutGroup : MonoBehaviour
{
    [Header("General Settings")]
    public bool showDebugLogs = false;
    [SerializeField]
    private bool useGroupLayout = true; // If false, it will not update the layout automatically

    public enum Alignment { Left, Center, Right }
    public enum Arrangement { Linear, Circular }

    [Header("Arrangement Settings")]
    public Arrangement arrangement = Arrangement.Linear;
    public Alignment alignment = Alignment.Left;
    // Circular arrangement properties
    [ShowIf("arrangement", Arrangement.Circular)] public float radius = 5f;
    [Tooltip("Arc length for circular arrangement in degrees. 360 means a full circle.")]
    [Range(0f, 360f)]
    [ShowIf("arrangement", Arrangement.Circular)] public float arcLength = 360f;
    [ShowIf("arrangement", Arrangement.Circular)] public bool rotateToFaceCenter = true;
    // Linear arrangement properties
    [ShowIf("arrangement", Arrangement.Linear)] public float elementWidth = 1f;
    [ShowIf("arrangement", Arrangement.Linear)] public float elementSpacing = 1f;

    private HorizontalObjectLayoutGroupMember[] groupMembers;



    public void UpdateLayout()
    {
        if (!useGroupLayout) return;
        if (showDebugLogs) Debug.Log($"[ObjectLayoutGroup]Updating layout");

        // Always refresh the groupMembers array to catch changes in the hierarchy
        groupMembers = GetComponentsInChildren<HorizontalObjectLayoutGroupMember>();

        if (groupMembers == null) return;

        HorizontalObjectLayoutGroupMember[] activeMembers = System.Array.FindAll(groupMembers, m => m.gameObject.activeSelf);

        switch (arrangement)
        {
            case Arrangement.Linear:
                LinearLayout(activeMembers);
                break;
            case Arrangement.Circular:
                CircularLayout(activeMembers);
                break;
        }
    }

    private void CircularLayout(HorizontalObjectLayoutGroupMember[] activeMembers)
    {
        int count = activeMembers.Length;
        if (count == 0) return;

        float angleStep = (count > 1) ? arcLength / (count - 1) : 0f;

        // Determine starting angle based on alignment
        float startAngle = 90f; // default front

        switch (alignment)
        {
            case Alignment.Left:
                startAngle = 90f; // fan opens to the left
                break;
            case Alignment.Center:
                startAngle = 90f - arcLength / 2f; // center the arc
                break;
            case Alignment.Right:
                startAngle = 90f - arcLength; // fan opens to the right
                break;
        }

        for (int i = 0; i < count; i++)
        {
            float angle = startAngle + i * angleStep;
            float radian = angle * Mathf.Deg2Rad;
            Vector3 position = new Vector3(Mathf.Cos(radian) * radius, 0, Mathf.Sin(radian) * radius);
            activeMembers[i].transform.localPosition = position;

            if (rotateToFaceCenter)
            {
                Quaternion rotation = Quaternion.LookRotation(position.normalized, Vector3.up);
                activeMembers[i].transform.rotation = rotation;
                if (showDebugLogs) Debug.Log($"[ObjectLayoutGroup]Set rotation of {activeMembers[i].name} to face center: {rotation}");
            }
            if (showDebugLogs) Debug.Log($"[ObjectLayoutGroup]Set the position of {activeMembers[i].name} to {position}");
        }
        if (showDebugLogs) Debug.Log($"[ObjectLayoutGroup]Circular layout applied with radius {radius} and arc length {arcLength}");
    }

    private void LinearLayout(HorizontalObjectLayoutGroupMember[] activeMembers)
    {
        int count = activeMembers.Length;
        if (count == 0) return;

        float totalWidth = count * elementWidth + (count - 1) * elementSpacing;
        float startX = 0f;
        switch (alignment)
        {
            case Alignment.Left:
                startX = 0f;
                break;
            case Alignment.Center:
                startX = -totalWidth / 2f + elementWidth / 2f;
                break;
            case Alignment.Right:
                startX = -totalWidth + elementWidth;
                break;
        }
        for (int i = 0; i < count; i++)
        {
            Vector3 position = new Vector3(startX + i * (elementWidth + elementSpacing), 0, 0);
            activeMembers[i].transform.localPosition = position;
            if (showDebugLogs) Debug.Log($"[ObjectLayoutGroup]Set the position of {activeMembers[i].name} to {position}");
        }
        if (showDebugLogs) Debug.Log($"[ObjectLayoutGroup]Linear layout applied with alignment {alignment} and element width {elementWidth} and spacing {elementSpacing}");
    }

    public void SetUseGroupLayout(bool value)
    {
        useGroupLayout = value;
        if (value)
        {
            UpdateLayout();
        }
        else
        {
            // If we are disabling group layout, reset positions to original
            ResetLayout();
        }
        if (showDebugLogs) Debug.Log($"[ObjectLayoutGroup]Set useGroupLayout to {value}");
    }



    [Button("Reset Layout (to original positions)")]
    private void ResetLayout()
    {
        if (showDebugLogs) Debug.Log($"[ObjectLayoutGroup]Resetting layout to original positions");
        if (originalPositions != null && originalPositions.Length > 0)
        {
            for (int i = 0; i < groupMembers.Length; i++)
            {
                groupMembers[i].transform.localPosition = originalPositions[i];
                groupMembers[i].transform.localEulerAngles = originalRotations[i];
                if (showDebugLogs) Debug.Log($"[ObjectLayoutGroup]Reset position of {groupMembers[i].name} to original position: {originalPositions[i]}");
            }
        }
    }

    [Space(20)]
    [InfoBox("Careful: This will overwrite stored positions!", EInfoBoxType.Warning)]
    [SerializeField, ReadOnly]
    private Vector3[] originalPositions;
    [SerializeField, ReadOnly]
    private Vector3[] originalRotations;
    [Tooltip("This is the original position of each member in the group. It is used to reset positions when this script is disabled.")]
    [Button("Store Original Positions")]
    public void StoreOriginalPositions()
    {
        groupMembers = GetComponentsInChildren<HorizontalObjectLayoutGroupMember>();

        if (groupMembers == null) return;
        originalPositions = new Vector3[groupMembers.Length];
        originalRotations = new Vector3[groupMembers.Length];
        for (int i = 0; i < groupMembers.Length; i++)
        {
            originalPositions[i] = groupMembers[i].transform.localPosition;
            originalRotations[i] = groupMembers[i].transform.localEulerAngles;
            if (showDebugLogs) Debug.Log($"[ObjectLayoutGroup]Stored original position of {groupMembers[i].name}: {originalPositions[i]}");
        }

    }


#if UNITY_EDITOR
    private void OnValidate()
    {
        UpdateLayout();
        if (!useGroupLayout)
        {
            ResetLayout();
        }
    }
#endif
}
