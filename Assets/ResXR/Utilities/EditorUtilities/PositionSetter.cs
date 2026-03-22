#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PositionSetter : MonoBehaviour
{
    [SerializeField] private int _positionIndex;
    [SerializeField] private Vector3[] _positions;

    public void SetPosition()
    {
        transform.position = _positions[_positionIndex];
        _positionIndex = _positionIndex == _positions.Length - 1 ? 0 : _positionIndex + 1;
    }
}
#endif