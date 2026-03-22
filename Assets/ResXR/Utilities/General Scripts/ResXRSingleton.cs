using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResXRSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;
    public static T Instance => instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this as T;
        }
        else
        {
            Debug.LogWarning($"Multiple instances of singleton of type {typeof(T)} found in scene. Destroying the current one.");
            Destroy(gameObject);
        }

        DoInAwake();
    }

    protected virtual void DoInAwake()
    {

    }
}
