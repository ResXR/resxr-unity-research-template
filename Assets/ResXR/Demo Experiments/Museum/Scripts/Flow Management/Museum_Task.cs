using NaughtyAttributes;
using System;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class Museum_Task
{
    [FormerlySerializedAs("roundType")]
    public Museum_TaskType taskType;

    public bool isFreeExploration => taskType == Museum_TaskType.FreeExploration;
    public float durationInSeconds;

}

public enum Museum_TaskType
{
    ImagesRating,
    FreeExploration
}
