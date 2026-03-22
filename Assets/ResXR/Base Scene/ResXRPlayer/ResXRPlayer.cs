using Cysharp.Threading.Tasks;
using System;
using Unity.VisualScripting;
using UnityEngine;
using System.Threading;

public class ResXRPlayer : ResXRSingleton<ResXRPlayer>
{
    [Header("Player Trackables")]
    [SerializeField]
    private Transform ovrRig;

    [SerializeField] private Transform playerHead;
    [SerializeField] private Transform rightHandAnchor;
    [SerializeField] private Transform leftHandAnchor;
    public Transform PlayerHead => playerHead;
    public Transform RightHand => rightHandAnchor;
    public Transform LeftHand => leftHandAnchor;

    [Header("Hand Tracking")] public ResXRHand HandLeft;
    public ResXRHand HandRight;

    [Header("Eye Tracking")] public ResXREyeTracker EyeTracker;
    public bool IsEyeTrackingEnabled;
    public Transform FocusedObject => EyeTracker.FocusedObject;
    public Vector3 EyeGazeHitPosition => EyeTracker.EyeGazeHitPosition;
    public Transform RightEye => EyeTracker.RightEye;
    public Transform LeftEye => EyeTracker.LeftEye;


    [Header("Face Tracking")]
    [SerializeField]
    public OVRFaceExpressions ovrFace;

    public OVRFaceExpressions OVRFace => ovrFace;
    public bool IsFaceTrackingEnabled;

    // Color overlay
    [SerializeField] private MeshRenderer colorOverlayMR;

    public ControllersInputManager ControllersInputManager;
    public PinchingInputManager PinchingInputManager;

    protected override void DoInAwake()
    {
        HandRight.Init();
        HandLeft.Init();

        ControllersInputManager = new ControllersInputManager();
        PinchingInputManager = new PinchingInputManager(HandLeft, HandRight);

        if (IsEyeTrackingEnabled)
            EyeTracker.Init();
        if (IsFaceTrackingEnabled)
        {
            ovrRig.AddComponent<OVRFaceExpressions>();
        }
    }


    void Update()
    {
        HandRight.UpdateHand();
        HandLeft.UpdateHand();

        if (IsEyeTrackingEnabled)
        {
            EyeTracker.UpdateEyeTracker();
        }
    }

    // covers player's view with color. 
    public async UniTask FadeViewToColor(Color targetColor, float duration)
    {
        if (duration == 0)
        {
            colorOverlayMR.material.color = targetColor;
            return;
        }

        Color currentColor = colorOverlayMR.material.color;
        if (currentColor == targetColor) return;

        float lerpTime = 0;
        while (lerpTime < duration)
        {
            lerpTime += Time.deltaTime;
            float t = lerpTime / duration;
            colorOverlayMR.material.color = Color.Lerp(currentColor, targetColor, t);

            await UniTask.Yield();
        }
    }

    public void RepositionPlayer(PlayerRepositioner repositioner)
    {
        RepositionAfterHeadsetLoaded(repositioner).Forget();
    }

    private async UniTask RepositionAfterHeadsetLoaded(PlayerRepositioner repositioner)
    {
        Vector3 position, forward;
        position = repositioner.transform.position;
        forward = repositioner.transform.forward;
        Vector3 initialHeadPosition = PlayerHead.position;

        await UniTask.WaitUntil(() => OVRManager.isHmdPresent && PlayerHead.position != initialHeadPosition);   // if head moved -> headset initiallzied

        // rotate
        transform.rotation = Quaternion.LookRotation(forward, Vector3.up);

        // move
        Vector3 headOffset = position - PlayerHead.position;
        if (repositioner.Type == ERepositionType.FloorLevel) headOffset.y = 0; // cancel height change.
        transform.position += headOffset;
    }

    public void RecenterView()
    {
        // IMPLEMENT
    }

    public void CalibrateFloorHeight()
    {
        // IMPLEMENT
    }

    public void SetPassthrough(bool state)
    {
        ResXRHeadsetServices.Instance.SetPassthrough(state);
    }

    #region Hands

    public Transform GetHandFingerCollider(HandType handType, FingerType fingerType)
    {
        switch (handType)
        {
            case HandType.Left:
                return HandLeft.GetFingerCollider(fingerType);
            case HandType.Right:
                return HandRight.GetFingerCollider(fingerType);
            case HandType.Any:
                return HandLeft.GetFingerCollider(fingerType);
            case HandType.None:
                return null;
            default: return null;
        }
    }

    #endregion
}