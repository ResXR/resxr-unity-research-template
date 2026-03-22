using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

public class InstructionsPanel : MonoBehaviour
{
    [Header("references")]
    public GameObject backPanel;
    public TextMeshPro title;
    public TextMeshPro text;
    public TMPBackPanelResizer backPanelResizer;

    [Header("settings")]
    public bool hideOnAwake = true;
    public bool useAnimations = true;
    public bool collectEyeGaze = true;

    private Vector3 initialScale;
    private Collider eyeGazeCollider;

    protected virtual void Awake()
    {
        initialScale = transform.localScale;

        // setup eye gaze collider
        eyeGazeCollider = backPanel.GetComponent<Collider>();

        if (collectEyeGaze && eyeGazeCollider == null)
        {
            Debug.LogWarning("[InstructionsPanel] No collider found on backPanel for eye gaze collection. Adding a BoxCollider. Please check its dimensions or add your own.");
            eyeGazeCollider = backPanel.AddComponent<BoxCollider>();
        }
        else if (!collectEyeGaze && eyeGazeCollider != null)
        {
            Destroy(eyeGazeCollider);
        }

        if (hideOnAwake)
        {
            HideInstant();
        }

    }

    public virtual async UniTask Show(bool doResizeBackPanel = true)
    {
        // Ensure geometry exists and active BEFORE measuring (else TMP may not update correctly)
        ShowInstant();

        if (doResizeBackPanel)
        {
            // Make sure we're not at zero scale while measuring (animation may have set it to zero)
            Vector3 prevScale = transform.localScale;
            transform.localScale = initialScale;

            // Let TMP update once (helps in some cases)
            await UniTask.Yield();
            title.ForceMeshUpdate();
            text.ForceMeshUpdate();

            backPanelResizer.ResizeBackPanel();

            transform.localScale = prevScale;
        }

        if (useAnimations)
        {
            await AnimateShow();
        }
        else
        {
            ShowInstant();
        }


        Debug.Log($"[InstructionsPanel] {gameObject.name} was shown.");
    }

    public virtual async UniTask Hide()
    {
        if (useAnimations)
        {
            await AnimateHide();
        }
        else
        {
            HideInstant();
        }
        Debug.Log($"[InstructionsPanel] {gameObject.name} was hidden.");
    }


    public void SetTitle(string newTitle)
    {
        title.text = newTitle;
    }

    public void SetText(string newText)
    {
        text.text = newText;
    }

    public async UniTask ShowForSeconds(float seconds, bool doResizeBackPanel = true)
    {
        await Show(doResizeBackPanel);
        await UniTask.Delay(System.TimeSpan.FromSeconds(seconds));
        await Hide();
    }

    protected virtual void ShowInstant()
    {
        backPanel.SetActive(true);
        title.gameObject.SetActive(true);
        text.gameObject.SetActive(true);
    }

    protected virtual void HideInstant()
    {
        backPanel.SetActive(false);
        title.gameObject.SetActive(false);
        text.gameObject.SetActive(false);
    }

    private async UniTask AnimateShow(float duration = 0.25f)
    {
        // Enable objects first (but make scale zero)
        transform.localScale = Vector3.zero;
        ShowInstant();
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float normalized = t / duration;

            // smooth easing 
            float ease = 1f - Mathf.Pow(1f - normalized, 3f);

            transform.localScale = initialScale * ease;
            await UniTask.Yield();
        }

        transform.localScale = initialScale;
    }

    private async UniTask AnimateHide(float duration = 0.25f)
    {
        // Make sure it's visible before shrinking
        ShowInstant();

        float t = 0f;
        Vector3 startScale = initialScale;

        while (t < duration)
        {
            t += Time.deltaTime;
            float normalized = Mathf.Clamp01(t / duration);

            // ease-in (mirrors the ease-out of AnimateShow)
            float ease = Mathf.Pow(normalized, 3);

            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, ease);

            await UniTask.Yield();
        }

        transform.localScale = Vector3.zero;

        // now hide objects
        HideInstant();
    }


}
