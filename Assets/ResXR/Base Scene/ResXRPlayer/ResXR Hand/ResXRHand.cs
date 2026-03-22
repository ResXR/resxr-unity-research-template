using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum HandType { Left, Right, None, Any }
public enum FingerType { Thumb = 5, Index = 10, Middle = 15, Ring = 20, Pinky = 25 } // OpenXR hand bone indices for tips

public class ResXRHand : MonoBehaviour
{
    public HandType HandType;

    private bool _isActive = true;

    private OVRSkeleton _ovrSkeleton;
    private List<HandCollider> _handColliders;

    private Pincher _pincher;

    private PinchManager _pinchManager;
    public Pincher Pincher => _pincher;
    public PinchManager PinchManager => _pinchManager;
    [SerializeField] private PinchingConfiguration _pinchingConfiguration;

    public SkinnedMeshRenderer _handSMR;
    private Tween _visibilityTween;
    private Sequence _pinchVisibilitySequence;
    private Sequence _pinchColorSequence;

    public void Init()
    {
        _ovrSkeleton = GetComponentInChildren<OVRSkeleton>();
        _pincher = GetComponentInChildren<Pincher>();

        _handColliders = GetComponentsInChildren<HandCollider>().ToList();
        _handSMR = GetComponentInChildren<SkinnedMeshRenderer>();
        foreach (HandCollider hc in _handColliders)
        {
            hc.Init(_ovrSkeleton);
        }

        _pinchManager = new PinchManager(this, _pinchingConfiguration);
        _pincher.Init(_ovrSkeleton, _pinchManager);
    }

    public void UpdateHand()
    {
        if (!_isActive || !_ovrSkeleton.IsDataHighConfidence) return;

        foreach (HandCollider hc in _handColliders)
        {
            hc.UpdateHandCollider();
        }

        _pinchManager.HandlePinching();
    }

    public Transform GetFingerCollider(FingerType fingerType)
    {
        foreach (HandCollider hc in _handColliders)
        {
            if (hc.fingerIndex == (int)fingerType)
            {
                return hc.transform;
            }
        }

        return null;
    }

    public void SetHandVisibility(bool state)
    {
        float targetAlpha = state ? 1 : 0;
        _visibilityTween.Kill();
        _visibilityTween = _handSMR.material.DOFloat(targetAlpha, "_Hand_Opacity", .25f);
        // if hand is disabled then enable invisible collider
        //_invisibleHandCollider.SetActive(!state);
    }

    public void SignifyPinch(bool state)
    {
        float targetPinchValue = state ? .35f : 0;
        _pinchVisibilitySequence.Kill();
        _pinchVisibilitySequence.Append(_handSMR.material.DOFloat(targetPinchValue, "_Index_Multiplier", .25f));
        _pinchVisibilitySequence.Join(_handSMR.material.DOFloat(targetPinchValue, "_Thumb_Multiplier", .25f));
    }

    public void SetPinchSignifyColor(Color color)
    {
        _pinchColorSequence.Kill();
        _pinchColorSequence.Append(_handSMR.material.DOColor(color, "_Index_Color", .25f));
        _pinchColorSequence.Join(_handSMR.material.DOColor(color, "_Thumb_Color", .25f));
    }

    public void SetFingerColor(FingerType finger, Color color)
    {
        switch (finger)
        {
            case FingerType.Thumb:
                _handSMR.material.DOColor(color, "_Thumb_Color", .25f); break;
            case FingerType.Index:
                _handSMR.material.DOColor(color, "_Index_Color", .25f); break;
            default: break;
        }
    }

    public void SetFingerSignifyStrength(FingerType finger, float strength)
    {
        switch (finger)
        {
            case FingerType.Thumb:
                _handSMR.material.DOFade(strength, "_Thumb_Multiplier", .25f); break;
            case FingerType.Index:
                _handSMR.material.DOFade(strength, "_Index_Multiplier", .25f); break;
            default: break;
        }
    }
}

