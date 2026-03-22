using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

//TODO: Separate Pinch events from script
public class PinchManager
{
    public Action<PinchManager> PinchEnter, PinchExit;
    private readonly SortedSet<APinchable> _pinchablesInRange = new();
    [HideInInspector] public APinchable PinchedObject;

    private bool _isPinching = false;
    private bool _wasPinchingLastFrame = false;
    private bool _pinchOccuredThisFrame => !_wasPinchingLastFrame && _isPinching;

    private float _timeSinceLastPinch;

    public PinchingConfiguration Configuration => _configuration;
    private PinchingConfiguration _configuration;
    public Pincher Pincher => _pincher;
    private readonly Pincher _pincher;
    public HandType HandType => _handType;
    private readonly HandType _handType;

    private float _pinchablesInRangeRadius;
    private List<APinchable> _pinchablesOutOfRange;

    public PinchManager(ResXRHand hand, PinchingConfiguration pinchingConfiguration)
    {
        _handType = hand.HandType;
        _pincher = hand.Pincher;
        _configuration = pinchingConfiguration;
        _timeSinceLastPinch = _configuration.MinimumTimeBetweenPinches;
        _pinchablesOutOfRange = new List<APinchable>();
        _pinchablesInRangeRadius = _pincher.GetComponent<SphereCollider>().radius * _pincher.transform.localScale.x;
    }

    public void HandlePinching()
    {
        _timeSinceLastPinch += Time.deltaTime;

        RemovePinchablesOutOfRange();

        _pincher.UpdatePincher();

        HandlePinchEvents();

        foreach (APinchable pinchable in _pinchablesInRange)
        {
            pinchable.OnHoverStay(this);
        }

        bool pinchingObject = PinchedObject != null;
        if (pinchingObject && _pincher.Strength <= PinchedObject.PinchExitThreshold)
        {
            ReleaseObject();
        }

        if (!_pinchOccuredThisFrame || pinchingObject) return;

        APinchable objectToPinch = ChooseObjectToPinch();
        if (objectToPinch != null)
        {
            PinchObject(objectToPinch);
        }
    }

    private void RemovePinchablesOutOfRange()
    {
        _pinchablesOutOfRange.Clear();

        foreach (APinchable pinchable in _pinchablesInRange)
        {
            //The problem is that we compare to the middle of the second object.
            bool pinchableOutOfRange =
                Vector3.Distance(pinchable.Collider.ClosestPoint(_pincher.transform.position),
                    _pincher.transform.position) > _pinchablesInRangeRadius;
            if (pinchableOutOfRange) _pinchablesOutOfRange.Add(pinchable);
        }

        foreach (APinchable pinchable in _pinchablesOutOfRange)
        {
            RemovePinchableInRange(pinchable);
        }
    }

    private void HandlePinchEvents()
    {
        _wasPinchingLastFrame = _isPinching;
        if (!_isPinching)
        {
            bool nextPinchReady = _timeSinceLastPinch >= _configuration.MinimumTimeBetweenPinches;
            if (_pincher.Strength > _configuration.PinchEnterThreshold && nextPinchReady)
            {
                _timeSinceLastPinch = 0;
                _isPinching = true;
                PinchEnter?.Invoke(this);
            }
        }
        else
        {
            if (_pincher.Strength < _configuration.PinchExitThreshold)
            {
                _isPinching = false;
                PinchExit?.Invoke(this);
            }
        }
    }

    public APinchable ChooseObjectToPinch()
    {
        foreach (APinchable pinchable in _pinchablesInRange)
        {
            if (pinchable.CanBePinched(this))
            {
                return pinchable;
            }
        }

        return null;
    }

    public APinchable ChooseInteractablePinchable()
    {
        foreach (APinchable pinchable in _pinchablesInRange)
        {
            if (pinchable.CanBePinched(this))
            {
                return pinchable;
            }
        }

        return null;
    }

    private void PinchObject(APinchable objectToPinch)
    {
        PinchedObject = objectToPinch;
        objectToPinch.PinchingHandPinchManager = this;
        objectToPinch.OnPinchEnter(this);
    }

    private void ReleaseObject()
    {
        PinchedObject.OnPinchExit();
        PinchedObject.PinchingHandPinchManager = null;
        PinchedObject = null;
    }

    public void AddPinchableInRange(APinchable pinchable)
    {
        _pinchablesInRange.Add(pinchable);
        pinchable.OnHoverEnter(this);
    }

    public void RemovePinchableInRange(APinchable pinchable)
    {
        _pinchablesInRange.Remove(pinchable);
        pinchable.OnHoverExit(this);
    }

    //Called from pinchables, that want to know if a certain other pinchable is in range.
    public APinchable FindPinchableByType(Type pinchableType)
    {
        foreach (APinchable pinchable in _pinchablesInRange)
        {
            if (pinchable.GetType() == pinchableType)
            {
                return pinchable;
            }
        }

        return null;
    }

    public bool IsHandPinchingThisFrame()
    {
        return _isPinching;
    }
}