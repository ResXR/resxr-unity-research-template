using Cysharp.Threading.Tasks;
using UnityEngine;

public class InstructionsPanelWithConfirmation : InstructionsPanel
{
    [SerializeField]
    private GameButton confirmButton;


    protected override void Awake()
    {
        base.Awake();
        if (this.confirmButton == null)
        {
            Debug.LogError($"[InstructionsPanelWithConfirmation] {gameObject.name}: confirmButton reference is not set in the inspector.");
        }

        if (hideOnAwake)
        {
            confirmButton.gameObject.SetActive(false);
        }
    }


    protected override void ShowInstant()
    {
        base.ShowInstant();
        confirmButton.gameObject.SetActive(true);
    }

    protected override void HideInstant()
    {
        base.HideInstant();
        confirmButton.gameObject.SetActive(false);
    }

    public async UniTask ShowAndWaitForConfirmation(bool doResizeBackPanel = true)
    {
        await Show(doResizeBackPanel);
        await confirmButton.WaitForButtonPress();

        Debug.Log($"[InstructionsPanelWithConfirmation] {gameObject.name} confirmed by user.");
        await Hide();
    }


}
