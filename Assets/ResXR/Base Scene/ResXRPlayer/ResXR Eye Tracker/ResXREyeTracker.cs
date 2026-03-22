// ResXR Eye Tracker: runs gaze raycasts from the left and right eye transforms and 
// a combined (cyclopean) ray. Uses OVREyeGaze confidence to gate when each ray is valid.
//
// Per-eye: For each eye, if that eye's OVREyeGaze.Confidence >= threshold, a raycast is run
// from that eye's position along its forward. Results are exposed as Left/RightEyeGazeHitPosition,
// Left/RightFocusedObject, and HasLeftEyeHit / HasRightEyeHit.
//
// Combined (cyclopean): when both eyes have confidence >= threshold, a single raycast is run
// from the midpoint of the two eye positions, in the direction of the normalized sum of the two
// eye forwards. Results are exposed as FocusedObject and EyeGazeHitPosition (same API as before).
// If either eye is below threshold, the combined ray is not computed and those are cleared.
//
// for most use cases, the combined ray is sufficient. If you need to track the focused object for each eye separately, you can use the Per-eye properties.
//
// Layer masking: use "IgnoreEyeTracking" layer to exclude objects from eye tracking. (set via the layer property for any gameobject in the inspector)
//
// At most 3 raycasts per UpdateEyeTracker() (left, right, and combined when both confident).
// Init() caches OVREyeGaze from _rightEye and _leftEye; direction is always from the transforms'
// forward, not from OVREyeGaze.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResXREyeTracker : MonoBehaviour
{
    public Transform FocusedObject => _focusedObject;
    public Vector3 EyeGazeHitPosition => _eyeGazeHitPosition;
    public Transform RightEye => _rightEye;
    public Transform LeftEye => _leftEye;
    public Vector3 EyePosition => _eyePosition;

    // Per-eye gaze (separate raycasts)
    public Vector3 LeftEyeGazeHitPosition { get; private set; }
    public Vector3 RightEyeGazeHitPosition { get; private set; }
    public Transform LeftFocusedObject { get; private set; }
    public Transform RightFocusedObject { get; private set; }
    public bool HasLeftEyeHit { get; private set; }
    public bool HasRightEyeHit { get; private set; }

    /// <summary>When true, per-eye raycasts are run (up to 3 rays). When false, only the combined ray is run (1 ray). Set by ResXRDataManager_V2 from recordingOptions.includeSeparateEyesGaze.</summary>
    public bool EnableSeparateEyeRaycasts { get; set; }

    [SerializeField] private Transform _rightEye;
    [SerializeField] private Transform _leftEye;
    private Vector3 _eyePosition;
    private const float EYERAYMAXLENGTH = 1000;
    private const float EYETRACKINGCONFIDENCETHRESHOLD = .5f;
    private Vector3 NOTTRACKINGVECTORVALUE = new Vector3(-1f, -1f, -1f);
    private OVREyeGaze _ovrEyeR;
    private OVREyeGaze _ovrEyeL;
    private Transform _focusedObject;
    private Vector3 _eyeGazeHitPosition;
    LayerMask _eyeTrackingLayerMask = ~(1 << 7);

    public void Init()
    {
        if (_rightEye != null && _rightEye.TryGetComponent(out OVREyeGaze er))
            _ovrEyeR = er;
        if (_leftEye != null && _leftEye.TryGetComponent(out OVREyeGaze el))
            _ovrEyeL = el;

        _focusedObject = null;
        _eyeGazeHitPosition = NOTTRACKINGVECTORVALUE;
        LeftFocusedObject = null;
        RightFocusedObject = null;
        LeftEyeGazeHitPosition = NOTTRACKINGVECTORVALUE;
        RightEyeGazeHitPosition = NOTTRACKINGVECTORVALUE;
        HasLeftEyeHit = false;
        HasRightEyeHit = false;
    }

    public void UpdateEyeTracker()
    {
        if (!EnableSeparateEyeRaycasts)
        {
            // Combined-only mode: clear per-eye outputs, skip per-eye raycasts
            RightFocusedObject = null;
            RightEyeGazeHitPosition = NOTTRACKINGVECTORVALUE;
            HasRightEyeHit = false;
            LeftFocusedObject = null;
            LeftEyeGazeHitPosition = NOTTRACKINGVECTORVALUE;
            HasLeftEyeHit = false;
        }
        else
        {
            // Per-eye raycasts (each uses its own confidence)
            if (_ovrEyeR != null && _ovrEyeR.Confidence >= EYETRACKINGCONFIDENCETHRESHOLD)
            {
                RaycastHit hit;
                if (Physics.Raycast(_rightEye.position, _rightEye.forward, out hit, EYERAYMAXLENGTH, _eyeTrackingLayerMask))
                {
                    RightEyeGazeHitPosition = hit.point;
                    RightFocusedObject = hit.transform;
                    HasRightEyeHit = true;
                }
                else
                {
                    RightEyeGazeHitPosition = NOTTRACKINGVECTORVALUE;
                    RightFocusedObject = null;
                    HasRightEyeHit = false;
                }
            }
            else
            {
                RightFocusedObject = null;
                RightEyeGazeHitPosition = NOTTRACKINGVECTORVALUE;
                HasRightEyeHit = false;
            }

            if (_ovrEyeL != null && _ovrEyeL.Confidence >= EYETRACKINGCONFIDENCETHRESHOLD)
            {
                RaycastHit hit;
                if (Physics.Raycast(_leftEye.position, _leftEye.forward, out hit, EYERAYMAXLENGTH, _eyeTrackingLayerMask))
                {
                    LeftEyeGazeHitPosition = hit.point;
                    LeftFocusedObject = hit.transform;
                    HasLeftEyeHit = true;
                }
                else
                {
                    LeftEyeGazeHitPosition = NOTTRACKINGVECTORVALUE;
                    LeftFocusedObject = null;
                    HasLeftEyeHit = false;
                }
            }
            else
            {
                LeftFocusedObject = null;
                LeftEyeGazeHitPosition = NOTTRACKINGVECTORVALUE;
                HasLeftEyeHit = false;
            }
        }

        // Combined (cyclopean) ray: only when BOTH eyes confident
        bool bothConfident = _ovrEyeR != null && _ovrEyeL != null
            && _ovrEyeR.Confidence >= EYETRACKINGCONFIDENCETHRESHOLD
            && _ovrEyeL.Confidence >= EYETRACKINGCONFIDENCETHRESHOLD;

        if (bothConfident)
        {
            _eyePosition = (_rightEye.position + _leftEye.position) / 2f;
            Vector3 eyeForward = (_rightEye.forward + _leftEye.forward).normalized;

            if (eyeForward.sqrMagnitude < 1e-6f)
                eyeForward = _rightEye.forward;

            RaycastHit hit;
            if (Physics.Raycast(_eyePosition, eyeForward, out hit, EYERAYMAXLENGTH, _eyeTrackingLayerMask))
            {
                _focusedObject = hit.transform;
                _eyeGazeHitPosition = hit.point;
            }
            else
            {
                _focusedObject = null;
                _eyeGazeHitPosition = NOTTRACKINGVECTORVALUE;
            }

#if UNITY_EDITOR
            Debug.DrawRay(_eyePosition, eyeForward * 2f, Color.red);
#endif
        }
        else
        {
            _focusedObject = null;
            _eyeGazeHitPosition = NOTTRACKINGVECTORVALUE;
        }
    }
}
