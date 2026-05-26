using UnityEngine;
using UnityEngine.Events;

public class SimpleButton : MonoBehaviour
{
    public UnityEvent SimpleButtonPressed = new UnityEvent();

    [SerializeField] private Material disabledMaterial;
    [SerializeField] private Renderer buttonGraphics;

    private Material originalMaterial;
    private bool disabled = false;

    private void Awake()
    {
        if (buttonGraphics == null)
        {
            Debug.LogWarning($"[SimpleButton] Missing buttonGraphics on {name}", this);
            return;
        }

        // Read the default material without instantiating a runtime copy in edit mode.
        originalMaterial = buttonGraphics.sharedMaterial;
    }

    public void SetButtonEnabled(bool enabled)
    {
        if (buttonGraphics == null)
            return;

        // Prevent editor-time scene mutation.
        if (!Application.isPlaying)
            return;

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
        if (!Application.isPlaying)
            return;

        if (disabled)
            return;

        if (other.gameObject.CompareTag("Toucher"))
        {
            SimpleButtonPressed.Invoke();
        }
    }
}
