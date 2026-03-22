using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public abstract class APinchable : MonoBehaviour, IComparable<APinchable>
{
    public PinchManager PinchingHandPinchManager { get; set; }
    public Collider Collider { get; private set; }

    public virtual float PinchExitThreshold => 0.97f;

    // public virtual int Priority => 0;
    public int Priority;

    protected int _numberOfPinchersInside = 0;
    protected AHoverEffect _hoverEffect;


    private void Awake()
    {
        Collider = GetComponent<Collider>();
        _hoverEffect = GetComponent<AHoverEffect>();
        DoOnAwake();
    }

    protected virtual void DoOnAwake()
    {
    }

    public virtual void OnHoverEnter(PinchManager pinchManager)
    {
        _numberOfPinchersInside++;
    }

    public virtual void OnHoverStay(PinchManager pinchManager)
    {
        UpdateHoverEffectState(pinchManager, CanBePinched(pinchManager));
    }

    protected void UpdateHoverEffectState(PinchManager pinchManager, bool shouldEffectBeActive)
    {
        if (_hoverEffect != null)
        {
            _hoverEffect.UpdateHoverEffectState(pinchManager, shouldEffectBeActive);
        }
    }

    public virtual void OnHoverExit(PinchManager pinchManager)
    {
        _numberOfPinchersInside--;
        UpdateHoverEffectState(pinchManager, _numberOfPinchersInside > 0);
    }

    public virtual bool CanBePinched(PinchManager pinchManager)
    {
        return true;
    }

    public virtual void OnPinchEnter(PinchManager pinchManager)
    {
    }

    public virtual void OnPinchExit()
    {
    }

    public int CompareTo(APinchable other)
    {
        if (ReferenceEquals(other, this))
        {
            //Duplicate object
            return 0;
        }

        //return 1 if other is greater, 0 if same, -1 if smaller
        int result = other.Priority.CompareTo(Priority);

        //Allow different instances with the same priority
        if (result == 0)
        {
            return GetHashCode().CompareTo(
                other.GetHashCode());
        }

        return result;
    }

    protected bool IsOtherHand(PinchManager pinchManager)
    {
        return PinchingHandPinchManager != null && PinchingHandPinchManager != pinchManager;
    }

    protected virtual void OnDestroy()
    {
        PinchingHandPinchManager?.RemovePinchableInRange(this);
    }

    private void OnValidate()
    {
        if (!GetComponent<Collider>().isTrigger)
        {
            Debug.LogWarning("Please make sure the collider of the pinchable object is set to trigger");
        }
    }
}