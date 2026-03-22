using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class ScaleEffect : MonoBehaviour
{
    [SerializeField] private float _scaleDuration;
    [SerializeField] private float _scaleAmount;
    private Vector3 _scaleUpTargetValue;
    private Vector3 _scaleDownTargetValue;


    void Start()
    {
        _scaleUpTargetValue = transform.localScale + Vector3.one * _scaleAmount;
        _scaleDownTargetValue = transform.localScale;
        Sequence sequence = DOTween.Sequence();
        sequence.Append(DOTween.To(() => transform.localScale, x => transform.localScale = x, _scaleUpTargetValue, _scaleDuration / 2));
        sequence.Append(
            DOTween.To(() => transform.localScale, x => transform.localScale = x, _scaleDownTargetValue, _scaleDuration / 2));
        sequence.SetLoops(-1, LoopType.Yoyo).SetEase(Ease.Linear);
    }
}