using Cysharp.Threading.Tasks;
using ResXRData;
using UnityEngine;

public struct ChoiceResult
{
    public string chosenImageName;
    public string chosenOption;
    public string handUsed;
    public float reactionTime;
    public float displayTime;
    public float choiceTime;
}

public class ChoicesManager : MonoBehaviour
{
    public Choice choiceA;
    public Choice choiceB;

    public bool hideOnAwake = true;

    private void Awake()
    {
        if (hideOnAwake)
        {
            HideChoices();
        }
    }

    private void HideChoices()
    {
        choiceA.gameObject.SetActive(false);
        choiceB.gameObject.SetActive(false);
    }

    private void ShowChoices()
    {
        choiceA.gameObject.SetActive(true);
        choiceB.gameObject.SetActive(true);
    }

    private static bool _stimulusBoundsLogged;

    public void LogStimulusBoundsOnce()
    {
        if (_stimulusBoundsLogged) return;
        _stimulusBoundsLogged = true;

        float t = Time.realtimeSinceStartup;

        LogBoundsForChoice(choiceA, "A", t);
        LogBoundsForChoice(choiceB, "B", t);
    }

    private void LogBoundsForChoice(Choice choice, string choiceId, float timeSinceStart)
    {
        bool wasActive = choice.gameObject.activeSelf;
        if (!wasActive)
            choice.gameObject.SetActive(true);

        var renderer = choice.GetComponent<Renderer>();
        var collider = choice.GetComponent<Collider>();

        float rcX = float.NaN, rcY = float.NaN, rcZ = float.NaN;
        float rsX = float.NaN, rsY = float.NaN, rsZ = float.NaN;
        float ccX = float.NaN, ccY = float.NaN, ccZ = float.NaN;
        float csX = float.NaN, csY = float.NaN, csZ = float.NaN;

        if (renderer != null)
        {
            var b = renderer.bounds;
            rcX = b.center.x; rcY = b.center.y; rcZ = b.center.z;
            rsX = b.size.x; rsY = b.size.y; rsZ = b.size.z;
        }

        if (collider != null)
        {
            var b = collider.bounds;
            ccX = b.center.x; ccY = b.center.y; ccZ = b.center.z;
            csX = b.size.x; csY = b.size.y; csZ = b.size.z;
        }

        if (!wasActive)
            choice.gameObject.SetActive(false);

        var data = new StimulusBounds(timeSinceStart, choiceId, rcX, rcY, rcZ, rsX, rsY, rsZ, ccX, ccY, ccZ, csX, csY, csZ);
        ResXRDataManager.Instance.LogCustom(data);
    }

    public async UniTask<ChoiceResult> SetImagesAndWaitForChoice(StimuliPair pair)
    {
        choiceA.SetImage(pair.stimulusASprite);
        choiceB.SetImage(pair.stimulusBSprite);

        ShowChoices();
        float displayTime = Time.realtimeSinceStartup;

        Debug.Log($"[ChoicesManager] Choice displayed between {choiceA.GetCurrentImageName()} and {choiceB.GetCurrentImageName()}, waiting for user...");

        // Wait for either choice to be touched and get which one finished first
        (int chosenIndex, Collider colliderA, Collider colliderB) = await UniTask.WhenAny(
            choiceA.WaitForTouch(),
            choiceB.WaitForTouch()
        );

        float choiceTime = Time.realtimeSinceStartup;
        float reactionTime = choiceTime - displayTime;

        string chosenImageName = chosenIndex == 0 ? choiceA.GetCurrentImageName() : choiceB.GetCurrentImageName();
        string chosenOption = chosenIndex == 0 ? "A" : "B";

        Collider triggerCollider = chosenIndex == 0 ? colliderA : colliderB;
        string handUsed = "";
        if (triggerCollider != null)
        {
            var handCollider = triggerCollider.GetComponentInParent<HandCollider>();
            if (handCollider != null)
            {
                handUsed = handCollider.HandT.ToString();
            }
        }

        HideChoices();

        Debug.Log($"[ChoicesManager] Chosen Image: {chosenImageName}, Reaction Time: {reactionTime} seconds.");

        return new ChoiceResult
        {
            chosenImageName = chosenImageName,
            chosenOption = chosenOption,
            handUsed = handUsed,
            reactionTime = reactionTime,
            displayTime = displayTime,
            choiceTime = choiceTime
        };
    }
}
