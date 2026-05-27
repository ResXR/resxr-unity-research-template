using UnityEngine;

[ExecuteAlways]
public class HorizontalObjectLayoutGroupMember : MonoBehaviour
{
    private void OnEnable()
    {
        NotifyParent();
    }

    private void OnDisable()
    {
        NotifyParent();
    }

    private void NotifyParent()
    {
        if (transform.parent != null)
        {
            var layout = transform.parent.GetComponent<HorizontalObjectLayoutGroup>();
            if (layout != null)
            {
                layout.UpdateLayout();
            }
        }
    }
}