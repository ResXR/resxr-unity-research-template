using UnityEngine;
using UnityEngine.Events;

public class SimpleButton : MonoBehaviour
{
    public UnityEvent SimpleButtonPressed = new UnityEvent();
    public Material disabledMaterial;
    public Renderer buttonGraphics;
    private Material originalMaterial;

    private bool disabled = false;

    private void Awake()
    {
        originalMaterial = buttonGraphics.material;
    }

    public void SetButtonEnabled(bool enabled)
    {
        if (enabled)
        {
            buttonGraphics.material = originalMaterial;
            disabled = false;
        }
        else
        {
            buttonGraphics.material = disabledMaterial;
            disabled = true;
        }
    }


    private void OnTriggerEnter(Collider other)
    {
        if (disabled) return;
        if (other.gameObject.CompareTag("Toucher"))
        {
            SimpleButtonPressed.Invoke();
        }
    }
}
