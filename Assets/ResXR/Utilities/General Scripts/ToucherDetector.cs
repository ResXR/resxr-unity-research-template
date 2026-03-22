using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public enum ButtonColliderType { Press, Hover }

// switch to work with unity events
public class ToucherDetector : MonoBehaviour
{
    [SerializeField] private bool _touchOnlyFromForward;
    public UnityEvent<Transform> ToucherEnter;
    public UnityEvent<Transform> ToucherExited;
    public UnityEvent HeadEnter;
    public UnityEvent HeadExit;

    private List<Transform> _touchersInside = new();

    // fast fix- when disabling a button after it is touched it cannot be touched again
    private void OnEnable()
    {
        _touchersInside.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        switch (other.tag)
        {
            case "Toucher":
                if (_touchersInside.Count == 0)
                {
                    if (IsValidToucherEnter(other))
                        ToucherEnter.Invoke(other.transform);
                }
                _touchersInside.Add(other.transform);
                break;
            case "PlayerHead":
                HeadEnter.Invoke();
                break;
            default: return;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        switch (other.tag)
        {
            case "Toucher":
                if (_touchersInside.Count == 1)
                {
                    ToucherExited.Invoke(other.transform);
                }
                _touchersInside.Remove(other.transform);
                break;
            case "PlayerHead":
                HeadExit.Invoke();
                break;
            default: return;
        }
    }

    // Call if collider becomes inactive while there are touchers inside
    public void ResetTouchersInside()
    {
        _touchersInside.Clear();
    }

    private bool IsValidToucherEnter(Collider toucher)
    {
        if (!_touchOnlyFromForward) return true;

        Vector3 toucherToButton = toucher.transform.position - transform.position;
        bool isToucherAboveButton = Vector3.Dot(transform.forward, toucherToButton) > 0;
        return isToucherAboveButton;
    }
}
