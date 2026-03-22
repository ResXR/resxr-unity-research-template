using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

//TODO: refactor so if works with regular transforms and not rect transforms
public class UIShaker : MonoBehaviour
{
    [SerializeField] private float _duration;
    [SerializeField] private float _strength;
    private bool _shaking;
    private RectTransform _rectTransform;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    public void Shake()
    {
        if (_shaking) return;
        _shaking = true;
        Tween shakeTween = _rectTransform.DOShakeAnchorPos(_duration, _strength, fadeOut: false);
        shakeTween.onComplete = () => _shaking = false;
    }
}