using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AHoverEffect : MonoBehaviour
{
    [SerializeField] protected bool _isActive = false;

    public void UpdateHoverEffectState(PinchManager pinchManager, bool shouldEffectBeActive)
    {
        if (shouldEffectBeActive)
        {
            Activate(pinchManager);
        }
        else
        {
            Deactivate(pinchManager);
        }
    }

    public virtual void Activate(PinchManager pinchManager)
    {
        if (_isActive)
        {
            return;
        }

        _isActive = true;
        DoOnActivation(pinchManager);
    }

    protected virtual void DoOnActivation(PinchManager pinchManager)
    {
    }

    public virtual void Deactivate(PinchManager pinchManager)
    {
        if (!_isActive)
        {
            return;
        }

        _isActive = false;
        DoOnDeactivation(pinchManager);
    }

    protected virtual void DoOnDeactivation(PinchManager pinchManager)
    {
    }
}