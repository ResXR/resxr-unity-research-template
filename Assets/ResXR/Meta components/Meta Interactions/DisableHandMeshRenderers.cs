using UnityEngine;
using System.Collections;

public class DisableHandMeshRenderers : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(DisableMeshRenderersAfterInitialization());
    }

    private IEnumerator DisableMeshRenderersAfterInitialization()
    {
        // Continuously check until the SkinnedMeshRenderer is available
        while (true)
        {
            if (ResXRPlayer.Instance != null)
            {
                if (ResXRPlayer.Instance.HandLeft != null && ResXRPlayer.Instance.HandLeft._handSMR != null)
                {
                    ResXRPlayer.Instance.HandLeft._handSMR.enabled = false;
                }
                if (ResXRPlayer.Instance.HandRight != null && ResXRPlayer.Instance.HandRight._handSMR != null)
                {
                    ResXRPlayer.Instance.HandRight._handSMR.enabled = false;
                }
                // Exit the loop once both renderers are disabled
                yield break;
            }
            // Wait for the next frame before checking again
            yield return null;
        }
    }
} 