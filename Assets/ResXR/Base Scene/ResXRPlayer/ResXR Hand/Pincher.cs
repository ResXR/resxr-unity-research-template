using System;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;

public class Pincher : MonoBehaviour
{
    //public float Strength => _pinchStrength;
    public float Strength
    {
        get { return _pinchStrength; }
        set { _pinchStrength = value; }
    }

    private OVRSkeleton _ovrSkeleton;
    private const int INDEX_I = 10, THUMB_I = 5; // OpenXR hand bone indices: IndexTip=10, ThumbTip=5
    private float _pinchStrength;

    private float _pinchDistance;

    private PinchManager _pinchManager;

    public void Init(OVRSkeleton ovrSkeleton, PinchManager pinchManager)
    {
        _ovrSkeleton = ovrSkeleton;
        _pinchManager = pinchManager;
    }

    public void UpdatePincher()
    {
        // set pinch position
        Vector3 indexFingerPosition = _ovrSkeleton.Bones[INDEX_I].Transform.position;
        Vector3 thumbFingerPosition = _ovrSkeleton.Bones[THUMB_I].Transform.position;
        Vector3 pincherTargetPosition = (thumbFingerPosition + indexFingerPosition) / 2;
        transform.position = pincherTargetPosition;

        // set pinch strength based on finger distance
        _pinchDistance = (indexFingerPosition - thumbFingerPosition).sqrMagnitude;
        _pinchStrength = Mathf.InverseLerp(_pinchManager.Configuration.PinchMaxDistance, _pinchManager.Configuration.PinchMinDistance,
            _pinchDistance);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent(out APinchable pinchable))
        {
            return;
        }

        Debug.Log("Adding pinchable: " + pinchable.name);
        _pinchManager.AddPinchableInRange(pinchable);
    }
}