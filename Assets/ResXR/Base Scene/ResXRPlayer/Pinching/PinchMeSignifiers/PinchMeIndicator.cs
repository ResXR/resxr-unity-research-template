// Removed: using Shapes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PinchMeIndicator : MonoBehaviour
{
    [SerializeField] private HandType _handType;
    private PinchManager _pinchManager;
    private ResXRHand _hand;
    private Sphere _sphere;
    private Line _line;

    private APinchable _objectToPinch = null;

    public void Awake()
    {
        _sphere = GetComponentInChildren<Sphere>();
        _line = GetComponentInChildren<Line>();
    }

    private void Start()
    {
        Debug.Log(ResXRPlayer.Instance.HandLeft);
        _pinchManager = _handType == HandType.Left ? ResXRPlayer.Instance.HandLeft.PinchManager : ResXRPlayer.Instance.HandRight.PinchManager;
    }

    private void Update()
    {
        _objectToPinch = _pinchManager.ChooseInteractablePinchable();

        if (_objectToPinch != null)
        {
            // _objectToPinch.UpdatePinchMeIndicatorLineStartPosition();
            transform.eulerAngles = Vector3.zero;
            SetLineAndSphereActiveState(true);
            // _line.Start = _objectToPinch.PinchMeIndicatorLineStartPosition - transform.position;
        }
        else if (_sphere.gameObject.activeSelf)
        {
            SetLineAndSphereActiveState(false);
        }
    }

    private void SetLineAndSphereActiveState(bool newState)
    {
        _sphere.gameObject.SetActive(newState);
        _line.gameObject.SetActive(newState);
    }
}