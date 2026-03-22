using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class DOTweenSafeModeInitializer : MonoBehaviour
{
    void Awake()
    {
        DOTween.Init(useSafeMode: true, logBehaviour: LogBehaviour.ErrorsOnly);
    }
}