using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grabbable : APinchable
{
    //Change to sorted set
    private List<PlacementPoint> _nearbyPlacementPoints = new();
    private PlacementPoint _placementPointThatContainsObject;

    public override void OnPinchEnter(PinchManager pinchManager)
    {
        transform.parent = pinchManager.Pincher.transform;
        transform.position = pinchManager.Pincher.transform.position;
        if (_placementPointThatContainsObject)
        {
            _placementPointThatContainsObject.RemoveObject();
            _placementPointThatContainsObject = null;
        }
    }

    public override void OnPinchExit()
    {
        //TODO: get highest priority placement point

        PlacementPoint placementPoint = GetFirstAvailablePlacementPoint();
        if (placementPoint != null)
        {
            _placementPointThatContainsObject = placementPoint;
            placementPoint.PlaceObject(transform);
        }
    }

    private PlacementPoint GetFirstAvailablePlacementPoint()
    {
        foreach (PlacementPoint placementPoint in _nearbyPlacementPoints)
        {
            if (!placementPoint.ContainsObject)
            {
                return placementPoint;
            }
        }

        return null;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlacementPoint>())
        {
            _nearbyPlacementPoints.Add(other.GetComponent<PlacementPoint>());
            Debug.Log(_nearbyPlacementPoints.Count);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlacementPoint>())
        {
            _nearbyPlacementPoints.Remove(other.GetComponent<PlacementPoint>());
        }
    }
}