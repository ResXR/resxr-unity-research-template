using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Cysharp.Threading.Tasks;


public class PinchingInputManager : AInputManager
{
    private readonly ResXRHand _handLeft;
    private readonly ResXRHand _handRight;

    public PinchingInputManager(ResXRHand handLeft, ResXRHand handRight)
    {
        _handLeft = handLeft;
        _handRight = handRight;
    }

    public override bool IsLeftHeld()
    {
        return _handLeft.PinchManager.IsHandPinchingThisFrame();
    }

    public override bool IsRightHeld()
    {
        return _handRight.PinchManager.IsHandPinchingThisFrame();
    }
}