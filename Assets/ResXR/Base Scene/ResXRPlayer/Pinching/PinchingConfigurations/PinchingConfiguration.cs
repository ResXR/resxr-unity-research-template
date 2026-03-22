using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PinchingConfiguration", menuName = "ScriptableObjects/Configurations/Pinching")]
public class PinchingConfiguration : ScriptableObject
{
    public float PinchExitThreshold = .97f;
    public float PinchEnterThreshold = .99f;
    public float MinimumTimeBetweenPinches = 0.2f;
    public float PinchMaxDistance = .01f;
    public float PinchMinDistance = .0006f;
}
