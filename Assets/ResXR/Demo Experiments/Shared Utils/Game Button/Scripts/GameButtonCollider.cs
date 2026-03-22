using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class GameButtonCollider : MonoBehaviour
{

    public event Action onPress;
    public event Action onRelease;

    private Collider coll;

    // touch counter to avoid detecting multiple fingers as different presses
    private int _touchCount = 0;


    private void Awake()
    {
        coll = GetComponent<Collider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        
        if (other.gameObject.tag == "Toucher")
        {
            if (_touchCount == 0)
            {
                onPress?.Invoke();
                
            }
            _touchCount += 1;
        }
    }


    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "Toucher")
        {
            _touchCount -= 1;
            if (_touchCount == 0)
            {
                onRelease?.Invoke();
            }
        }
    }

}
