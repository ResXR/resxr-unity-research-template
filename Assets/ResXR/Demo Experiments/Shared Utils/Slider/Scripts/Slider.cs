#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using NaughtyAttributes;


[ExecuteAlways]
public class Slider : MonoBehaviour
{
    [InfoBox("A VR Slider allowing users to select a value by moving a handle along a line between two endpoints. Supports discrete steps or continuous values, with visual step marks and optional confirmation button.\nTo resize the slider, move the 'Start Point' and 'End Point' transforms.")]

    [Header("Settings")]
    [Tooltip("number of intervals. minimum 1")]
    [Min(1)]
    public int NumOfSteps = 10;
    public float MinValue = 0f;
    public float MaxValue = 1f;

    [SerializeField, ReadOnly] private float StepSize => (MaxValue - MinValue) / NumOfSteps;
    [SerializeField] private string valueFormat = "F2";
    [SerializeField] private string valueUnitSymbol = "$";
    [Range(0f, 1f)]
    [SerializeField] private float currentNormalized = 0f;
    [ReadOnly] public float CurrentValue = 0f;

    public bool ShowStepMarks = true;
    public bool AllowContinuousValues = false;

    [SerializeField] private bool hideOnAwake = false;
    [Tooltip("If true, the user must touch/move the slider before being able to confirm the value.")]
    [SerializeField] private bool requireSliderTouchBeforeConfirm = false;
    private bool _hasBeenTouched = false;

    [Header("References")]
    [SerializeField] private SimpleButton confirmButton;
    [SerializeField] private GameObject stepMarkPrefab;
    [SerializeField] private Transform stepMarkParent;
    [SerializeField] private Transform minPoint;
    [SerializeField] private Transform maxPoint;
    [SerializeField] private TextMeshPro minPointText; // Optional: for displaying min value
    [SerializeField] private TextMeshPro maxPointText; // Optional: for displaying max value
    [SerializeField] private Transform valueMark; //or: slider handle
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private TextMeshPro valueText;
    [SerializeField] private BoxCollider lineCollider;
    [SerializeField] private float colliderHeight = 0.02f;
    [SerializeField] private float colliderDepth = 0.02f;

    [SerializeField, HideInInspector]
    private List<GameObject> stepMarks = new List<GameObject>();

    private bool _isRebuilding = false;
    // Track last-known endpoint positions to detect motion in edit/play
    private Vector3 _prevMinPos;
    private Vector3 _prevMaxPos;
    private const float _posEpsilonSqr = 1e-8f;
    private float initialValue;

    private UniTaskCompletionSource tcs;

    public event Action sliderWasTouched;

    #region Unity LifeCycle
    private void Awake()
    {
        if (hideOnAwake)
        {
            gameObject.SetActive(false);
        }
    }


    private void Start()
    {
        if (stepMarkPrefab == null || minPoint == null || maxPoint == null || valueMark == null || lineRenderer == null)
        {
            Debug.LogError("Slider is not properly configured. Please assign all required references.");
            return;
        }

        SliderCollider sliderCollider = lineCollider.gameObject.GetComponent<SliderCollider>();
        if (sliderCollider == null)
        {
            sliderCollider = lineCollider.gameObject.AddComponent<SliderCollider>();
        }

        sliderCollider.OnToucherPositionChanged += SetValueFromColliderPosition;

        DrawLine();
        UpdateCollider();
        UpdateStepMarks();
        SetValue(CurrentValue);

        if (stepMarkParent == null)
        {
            stepMarkParent = transform; // Use the slider's transform if no parent is assigned
        }

        initialValue = CurrentValue;

        if (requireSliderTouchBeforeConfirm)
        {
            if (confirmButton == null)
                Debug.LogError("[Slider] requireSliderTouchBeforeConfirm is enabled but confirmButton is not assigned.");

            confirmButton.SetButtonEnabled(false);
            _hasBeenTouched = false;
        }
        else
        {
            // optional: if you want confirm always enabled when rule is off
            confirmButton?.SetButtonEnabled(true);
        }

    }

    private void Update()
    {
        if (minPoint == null || maxPoint == null) return;

        var a = minPoint.position;
        var b = maxPoint.position;

        if ((a - _prevMinPos).sqrMagnitude > _posEpsilonSqr ||
            (b - _prevMaxPos).sqrMagnitude > _posEpsilonSqr)
        {
            OnEndpointsMoved();
            _prevMinPos = a;
            _prevMaxPos = b;
        }
    }

    private void OnEnable()
    {
        if (minPoint != null) _prevMinPos = minPoint.position;
        if (maxPoint != null) _prevMaxPos = maxPoint.position;
    }

    private void OnValidate()
    {
        ApplyFromNormalized(currentNormalized);

        if (stepMarkParent == null) stepMarkParent = transform;
        if (minPoint == null || maxPoint == null || lineRenderer == null) return;

        DrawLine();
        UpdateCollider();
        UpdateEndValueTexts();

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            ScheduleRegenerateStepMarks();   //  queue, don’t destroy in OnValidate
            return;
        }
#endif

        // Play mode
        RegenerateStepMarksNow();
    }



    #endregion

    #region Update visual elements
    private void UpdateStepMarks()
    {
        RequestRegenerateStepMarks();
    }

    private void RequestRegenerateStepMarks()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            // Immediate in Edit Mode (no scheduling, no guards)
            RegenerateStepMarksNow();
            return;
        }
#endif
        // Play mode: do it immediately (uses Destroy)
        RegenerateStepMarksNow();
    }

    private bool StepMarksInvalid()
    {
        return stepMarks == null
            || stepMarks.Count != NumOfSteps    // if you want endpoints included use NumOfSteps+1 here
            || stepMarks.Exists(m => m == null);
    }


    private void RegenerateStepMarksNow()
    {
        if (_isRebuilding) return;
        _isRebuilding = true;

        if (stepMarkParent == null) stepMarkParent = transform;

        // If marks are already valid and ShowStepMarks didn't change, skip work
        // Comment this out if you prefer unconditional rebuilds on every inspector nudge
        if (ShowStepMarks && !StepMarksInvalid())
        {
            _isRebuilding = false;
            return;
        }

        ClearStepMarksNow();

        if (!ShowStepMarks)
        {
            _isRebuilding = false;
            return; // cleared & done
        }

        if (stepMarkPrefab == null || stepMarkParent == null || minPoint == null || maxPoint == null)
        {
            _isRebuilding = false;
            return;
        }

        for (int i = 0; i < NumOfSteps; i++)
        {
            float stepValue = MinValue + (i * StepSize);
            float t = (MaxValue - MinValue) > 0f ? (stepValue - MinValue) / (MaxValue - MinValue) : 0f;
            Vector3 pos = Vector3.Lerp(minPoint.position, maxPoint.position, t);

            GameObject inst;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                inst = (GameObject)PrefabUtility.InstantiatePrefab(stepMarkPrefab, stepMarkParent);
                inst.transform.position = pos;
            }
            else
#endif
            {
                inst = Instantiate(stepMarkPrefab, pos, Quaternion.identity, stepMarkParent);
            }

            inst.name = $"StepMark_{i}";
            stepMarks.Add(inst);
        }

        _isRebuilding = false;
    }


    private void ClearStepMarksNow()
    {
        if (stepMarks == null || stepMarks.Count == 0) return;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            for (int i = 0; i < stepMarks.Count; i++)
            {
                var go = stepMarks[i];
                if (!go) continue;
                if (go.scene.IsValid() && !PrefabUtility.IsPartOfPrefabAsset(go))
                    Undo.DestroyObjectImmediate(go);
            }
            stepMarks.RemoveAll(g => g == null || !g || !g.scene.IsValid());
            return;
        }
#endif

        foreach (var go in stepMarks)
            if (go) Destroy(go);
        stepMarks.Clear();
    }

#if UNITY_EDITOR
    private bool _regenQueued = false;

    private void ScheduleRegenerateStepMarks()
    {
        if (_regenQueued) return;
        _regenQueued = true;

        EditorApplication.delayCall += () =>
        {
            _regenQueued = false;
            if (this == null) return;

            RegenerateStepMarksNow();                // safe to DestroyImmediate here
            EditorUtility.SetDirty(this);
            if (gameObject && gameObject.scene.IsValid())
                EditorSceneManager.MarkSceneDirty(gameObject.scene);
        };
    }
#endif

    private void OnEndpointsMoved()
    {
        DrawLine();
        UpdateCollider();
        if (ShowStepMarks)
            RepositionStepMarksOrRebuild(); // cheap when possible

        // Keep handle/text in sync with new endpoints
        SetValue(CurrentValue);
    }

    private void RepositionStepMarksOrRebuild()
    {
        if (stepMarks == null) stepMarks = new List<GameObject>();

        // If the list is invalid (wrong count or nulls), rebuild via your safe path
        bool needsRebuild = stepMarks.Count != NumOfSteps || stepMarks.Exists(m => m == null);
        if (needsRebuild)
        {
            RequestRegenerateStepMarks();
            return;
        }

        // Fast path: just move them
        for (int i = 0; i < NumOfSteps; i++)
        {
            // Using value-space spacing; identical to your Create loop:
            float stepValue = MinValue + (i * StepSize);
            float t = (MaxValue - MinValue) > 0f ? (stepValue - MinValue) / (MaxValue - MinValue) : 0f;
            Vector3 pos = Vector3.Lerp(minPoint.position, maxPoint.position, t);
            var mark = stepMarks[i];
            if (mark != null) mark.transform.position = pos;
        }
    }



    private void UpdateValueText()
    {
        if (valueText != null)
        {
            valueText.text = CurrentValue.ToString(valueFormat) + valueUnitSymbol;
        }
    }

    private void DrawLine()
    {
        lineRenderer.positionCount = 2;
        if (minPoint != null && maxPoint != null)
        {
            lineRenderer.SetPosition(0, minPoint.position);
            lineRenderer.SetPosition(1, maxPoint.position);
        }
        else
        {
            Debug.LogError("Min or Max point is not assigned for the line renderer.");
        }
    }

    private void UpdateCollider()
    {
        if (lineCollider == null || minPoint == null || maxPoint == null || lineRenderer == null)
            return;

        // World-space endpoints
        Vector3 start = minPoint.position;
        Vector3 end = maxPoint.position;
        Vector3 dir = end - start;
        float lengthWorld = dir.magnitude;
        if (lengthWorld < 1e-6f) return;

        // Ensure collider is under the lineRenderer (axes match)
        Transform t = lineCollider.transform;
        if (t.parent != lineRenderer.transform)
            t.SetParent(lineRenderer.transform, false);

        // Reset local scale (only parent chain scale should apply)
        t.localScale = Vector3.one;

        // Place and orient the collider in world space
        t.position = (start + end) * 0.5f;
        t.rotation = Quaternion.FromToRotation(Vector3.right, dir.normalized);

        // Effective world scale per local axis (handles rotated and non-uniformly scaled parents)
        float sx = t.TransformVector(Vector3.right).magnitude;   // world units per 1 local X
        float sy = t.TransformVector(Vector3.up).magnitude;      // world units per 1 local Y
        float sz = t.TransformVector(Vector3.forward).magnitude; // world units per 1 local Z

        // Desired world thicknesses
        float heightWorld = colliderHeight;
        float depthWorld = colliderDepth;

        // Convert world sizes to local sizes for BoxCollider.size
        float localLen = lengthWorld / Mathf.Max(sx, 1e-6f);
        float localHeight = heightWorld / Mathf.Max(sy, 1e-6f);
        float localDepth = depthWorld / Mathf.Max(sz, 1e-6f);

        // Apply
        lineCollider.center = Vector3.zero;
        lineCollider.size = new Vector3(localLen, localHeight, localDepth);
    }


    private void UpdateValueMark()
    {
        Vector3 newPosition = Vector3.Lerp(minPoint.position, maxPoint.position, (CurrentValue - MinValue) / (MaxValue - MinValue));
        valueMark.position = newPosition;
    }


    private void UpdateEndValueTexts()
    {
        if (minPointText != null)
            minPointText.text = MinValue.ToString(valueFormat) + valueUnitSymbol;

        if (maxPointText != null)
            maxPointText.text = MaxValue.ToString(valueFormat) + valueUnitSymbol;
    }
    #endregion

    #region Apply Value

    private void ApplyFromNormalized(float t01)
    {
        t01 = Mathf.Clamp01(t01);
        float raw = Mathf.Lerp(MinValue, MaxValue, t01);
        ApplyValue(raw); // centralize snapping + visuals
    }

    private void ApplyValue(float newValue)
    {
        // clamp, then snap only if needed
        float v = Mathf.Clamp(newValue, MinValue, MaxValue);
        if (!AllowContinuousValues)
        {
            v = SnapToStep(v);
        }

        CurrentValue = v;

        // reflect result back to normalized so the Inspector shows where it landed
        currentNormalized = (MaxValue > MinValue)
            ? Mathf.InverseLerp(MinValue, MaxValue, CurrentValue)
            : 0f;

        UpdateValueMark();
        UpdateValueText();
    }

    private void SetValueFromColliderPosition(Vector3 worldPos)
    {
        //Debug.Log($"[Slider] SetValueFromColliderPosition: {worldPos}");
        Vector3 closestPoint = GetClosestPointOnSegment(worldPos);
        float t = GetTOnSegment(closestPoint);
        ApplyFromNormalized(t);
        if (requireSliderTouchBeforeConfirm && !_hasBeenTouched)
        {
            _hasBeenTouched = true;
            confirmButton.SetButtonEnabled(true);
        }
        sliderWasTouched?.Invoke();
    }
    #endregion

    #region Public API
    public void SetValue(float newValue)
    {
        ApplyValue(newValue);
    }

    public async UniTask<float> WaitForConfirm()
    {
        tcs = new UniTaskCompletionSource();
        await UniTask.Yield();
        confirmButton.SimpleButtonPressed.AddListener(OnConfirmPressed);

        await tcs.Task;

        confirmButton.SimpleButtonPressed.RemoveListener(OnConfirmPressed);

        return CurrentValue;
    }


    public async UniTask CancelWait()
    {
        await UniTask.Yield();
        if (tcs != null)
            tcs.TrySetCanceled();
    }

    public void ResetValue()
    {
        SetValue(initialValue);
        if (requireSliderTouchBeforeConfirm)
        {
            _hasBeenTouched = false;
            confirmButton.SetButtonEnabled(false);
        }
    }

    #endregion

    #region helpers

    private void OnConfirmPressed()
    {
        tcs.TrySetResult();
    }

    private float SnapToStep(float v)
    {
        if (MaxValue <= MinValue)
            return MinValue;

        v = Mathf.Clamp(v, MinValue, MaxValue);

        if (AllowContinuousValues || NumOfSteps <= 0)
            return v;

        // number of intervals, so each step is this size:
        float stepSize = (MaxValue - MinValue) / NumOfSteps;

        // find nearest step index
        int stepIndex = Mathf.RoundToInt((v - MinValue) / stepSize);

        return MinValue + stepIndex * stepSize;
    }

    // 0..1 along the segment [minPoint -> maxPoint], clamped
    private float GetTOnSegment(Vector3 worldPos)
    {
        Vector3 a = minPoint.position;
        Vector3 b = maxPoint.position;
        Vector3 ab = b - a;
        float lenSq = ab.sqrMagnitude;
        if (lenSq < 1e-8f) return 0f; // degenerate segment

        float t = Vector3.Dot(worldPos - a, ab) / lenSq; // projection scalar
        return Mathf.Clamp01(t);
    }

    // actual closest point in world space (optional, handy for snapping handle)
    private Vector3 GetClosestPointOnSegment(Vector3 worldPos)
    {
        float t = GetTOnSegment(worldPos);
        return Vector3.Lerp(minPoint.position, maxPoint.position, t);
    }
    #endregion
}
