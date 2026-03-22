using UnityEngine;

public class MaterialFadeChecker : MonoBehaviour
{
    public enum ComparisonType
    {
        DisableWhenBelow,  // Disable renderer when value is less than threshold
        DisableWhenAbove   // Disable renderer when value is greater than threshold
    }

    public bool useFadeValue = true; // Toggle between using fade or alpha
    public bool useSharedMaterial = true; // Toggle between material and sharedMaterial
    public string fadePropertyName = "_mul"; // Property name to check in the shader
    public Renderer objectRenderer; // Reference to the Renderer component
    [Range(0.0f, 1.0f)]
    public float threshold = 0.01f; // Threshold for comparison
    public ComparisonType comparisonMode = ComparisonType.DisableWhenBelow; // How to compare the value
    private Material objectMaterial; // Material of the object
    private bool isRendererEnabled; // Tracks the current state of the MeshRenderer


    void Start()
    {
        // Get the material from the Renderer component
        if (objectRenderer == null)
        {
            objectRenderer = GetComponent<Renderer>();
        }

        if (objectRenderer != null)
        {
            // Use either shared material or instance material based on toggle
            objectMaterial = useSharedMaterial ? objectRenderer.sharedMaterial : objectRenderer.material;
            isRendererEnabled = objectRenderer.enabled; // Initialize the renderer state
        }
        else
        {
            Debug.LogError("Renderer component not found on this GameObject.");
        }
    }

    void OnEnable()
    {
        // Refresh material reference when component is enabled
        if (objectRenderer != null)
        {
            objectMaterial = useSharedMaterial ? objectRenderer.sharedMaterial : objectRenderer.material;
        }
    }

    void Update()
    {
        if (objectMaterial == null) return;

        // Get the value to check: either shader property or alpha of the color
        float checkValue = useFadeValue
            ? objectMaterial.GetFloat(fadePropertyName) // Use the configurable property name
            : objectMaterial.color.a;

        bool shouldDisable = false;
        
        // Determine if we should disable based on the comparison mode
        switch (comparisonMode)
        {
            case ComparisonType.DisableWhenBelow:
                shouldDisable = checkValue <= threshold;
                break;
            case ComparisonType.DisableWhenAbove:
                shouldDisable = checkValue >= threshold;
                break;
        }

        // Update MeshRenderer only if its state needs to change
        if (shouldDisable && isRendererEnabled)
        {
            objectRenderer.enabled = false;
            isRendererEnabled = false;
        }
        else if (!shouldDisable && !isRendererEnabled)
        {
            objectRenderer.enabled = true;
            isRendererEnabled = true;
        }
    }
}
