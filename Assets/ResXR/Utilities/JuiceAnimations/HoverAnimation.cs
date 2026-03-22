using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class HoverAnimation : MonoBehaviour
{
    [SerializeField] private Vector3 _distance;
    [SerializeField] private float _loopDuration;

    private void Start()
    {
        Hover();
    }

    private void Hover()
    {
        Tween hoverTween = DOTween.To(() => transform.localPosition, x => transform.localPosition = x, transform.localPosition + _distance,
                _loopDuration)
            .SetEase(Ease.InOutSine);
        hoverTween.SetLoops(-1, LoopType.Yoyo);
    }
}