using System;
using UnityEngine;

public class SliderCollider : MonoBehaviour
{
    private bool isTouching = false;
    private Collider toucher;

    private Vector3 currentToucherPosition;
    public Vector3 CurrentToucherPosition
    {
        get { return currentToucherPosition; }
    }
    public bool showDebugLogs = false;
    public event Action<Vector3> OnToucherPositionChanged;

    private void Awake()
    {
        OnToucherPositionChanged = _ => { }; //empty delegate initialization
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Toucher") && !isTouching)
        {
            isTouching = true;
            toucher = other;
            if (showDebugLogs)
            {
                Debug.Log("[SliderCollider] Toucher entered: " + other.name);
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!isTouching)
        {
            return;
        }
        if (other == toucher)
        {
            currentToucherPosition = other.transform.position;
            OnToucherPositionChanged.Invoke(currentToucherPosition);
            if (showDebugLogs)
            {
                Debug.Log("[SliderCollider] Toucher position updated: " + currentToucherPosition);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (isTouching && toucher == other)
        {
            isTouching = false;
            toucher = null;
            if (showDebugLogs)
            {
                Debug.Log("[SliderCollider] Toucher exited: " + other.name);
            }
        }
    }

}
