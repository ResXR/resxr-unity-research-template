using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// The only script communicating directly with the headset sdk, exposing its functionality to other in project classes.
// Created: 28.01.2024 - Tal. Updated: 28.01.2024 - Tal.
public class ResXRHeadsetServices : ResXRSingleton<ResXRHeadsetServices>
{
    [SerializeField] private OVRManager _ovrManager;

    private void Awake()
    {
        if(_ovrManager == null)
        {
            Debug.LogError("No OVRManager found in ResXR Headset Services. Make sure to drag it in Base Scene");
        }        
    }

    public void SetPassthrough(bool state)
    {
        _ovrManager.isInsightPassthroughEnabled = state;
    }
}
