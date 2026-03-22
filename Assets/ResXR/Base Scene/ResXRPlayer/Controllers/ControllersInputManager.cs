using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System;

public class ControllersInputManager : AInputManager
{
    //Is left trigger held this frame
    public override bool IsLeftHeld()
    {
        return OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger) > .7f;
    }

    //Is right trigger held this frame
    public override bool IsRightHeld()
    {
        return OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger) > .7f;
    }

    protected override void DoWhileWaitingForHold(HandType handType, float holdingDuration, float duration)
    {
        //Updating vibration
        if (holdingDuration > 0)
        {
            SetControllersVibrationOnHold(handType, holdingDuration / duration);
        }
        else
        {
            ResetControllersVibration(handType);
        }
    }

    private void SetControllersVibrationOnHold(HandType handType, float vibrationStrength)
    {
        switch (handType)
        {
            case HandType.Left:
                OVRInput.SetControllerVibration(vibrationStrength, vibrationStrength, OVRInput.Controller.LTouch);
                break;
            case HandType.Right:
                OVRInput.SetControllerVibration(vibrationStrength, vibrationStrength, OVRInput.Controller.RTouch);
                break;
            default:
            {
                if (IsLeftHeld())
                {
                    OVRInput.SetControllerVibration(vibrationStrength, vibrationStrength, OVRInput.Controller.LTouch);
                }

                if (IsRightHeld())
                {
                    OVRInput.SetControllerVibration(vibrationStrength, vibrationStrength, OVRInput.Controller.RTouch);
                }

                break;
            }
        }
    }

    private static void ResetControllersVibration(HandType handType)
    {
        switch (handType)
        {
            case HandType.Left:
                OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.LTouch);
                break;
            case HandType.Right:
                OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.RTouch);
                break;
            default:
            {
                OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.RTouch);
                OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.LTouch);
                break;
            }
        }
    }

    protected override void DoOnHoldFinished(HandType handType)
    {
        ResetControllersVibration(handType);
    }


    //Utility method to set vibration even when there is no hold/press
    //TODO: add hand type
    public async UniTask SetControllersVibration(float vibrationStrength, float duration)
    {
        OVRInput.SetControllerVibration(vibrationStrength, vibrationStrength, OVRInput.Controller.RTouch);
        OVRInput.SetControllerVibration(vibrationStrength, vibrationStrength, OVRInput.Controller.LTouch);

        await UniTask.Delay(TimeSpan.FromSeconds(duration));

        OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.RTouch);
        OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.LTouch);
    }
}