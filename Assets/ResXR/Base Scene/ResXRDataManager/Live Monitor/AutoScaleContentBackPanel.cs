using TMPro;
using UnityEngine;


[RequireComponent(typeof(TMPBackPanelResizer))]
/// <summary>
/// This class supports runtime-generated UI by collecting all TextMeshPro components under a specified parent, 
/// registering them with a TMPBackPanelResizer, and resizing their background panels after the text content is populated. 
public class AutoScaleContentBackPanels : MonoBehaviour
{
    public Transform textParent; // Parent transform containing TextMeshPro components
    private TMPBackPanelResizer backPanelResizer;

    private void Start()
    {
        backPanelResizer = GetComponent<TMPBackPanelResizer>();

        if (textParent == null)
        {
            Debug.LogError("[AutoScaleContentBackPanels] textParent is not assigned. runtime auto-scaling will not work.");
            return;
        }

        // Find all TextMeshPro components under the specified parent
        TextMeshPro[] textComponents = textParent.GetComponentsInChildren<TMPro.TextMeshPro>(true);

        // Register each TextMeshPro component with the TMPBackPanelResizer
        foreach (TextMeshPro textComp in textComponents)
        {
            backPanelResizer.RegisterTextComponent(textComp);
        }

        // Resize the back panel to fit the newly registered text components
        backPanelResizer.ResizeBackPanel();

    }
}
