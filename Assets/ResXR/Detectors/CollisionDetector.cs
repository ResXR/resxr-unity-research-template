using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionDetector : ADetector
{
    [SerializeField] private bool _detectSpecificCollision;
    [SerializeField] private string _colliderTag;

    private void OnCollisionEnter(Collision other)
    {
        HandleCollisionInteraction(InteractionStarted, other.gameObject);
    }

    private void HandleCollisionInteraction(Action actionToInvoke, GameObject other)
    {
        bool detectedCollision = !_detectSpecificCollision || other.CompareTag(_colliderTag);
        if (detectedCollision) actionToInvoke?.Invoke();
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleCollisionInteraction(InteractionStarted, other.gameObject);
    }

    private void OnCollisionExit(Collision other)
    {
        HandleCollisionInteraction(InteractionEnded, other.gameObject);
    }

    private void OnTriggerExit(Collider other)
    {
        HandleCollisionInteraction(InteractionEnded, other.gameObject);
    }
}