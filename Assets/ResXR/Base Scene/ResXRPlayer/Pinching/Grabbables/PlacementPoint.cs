using System;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class PlacementPoint : MonoBehaviour
{
    public bool ContainsObject { get; private set; }

    public void PlaceObject(Transform objectToPlace)
    {
        objectToPlace.parent = transform;
        float objectBottom = objectToPlace.GetComponent<Collider>().bounds.min.y;
        float objectYAddition = objectToPlace.transform.position.y - objectBottom;
        objectToPlace.position = new Vector3(transform.position.x, transform.position.y + objectYAddition, transform.position.z);
        objectToPlace.rotation = transform.rotation;
        ContainsObject = true;
    }

    public void RemoveObject()
    {
        ContainsObject = false;
    }
}