
// Stores references for everything needer to refer to in the scene.
using UnityEngine;

public class BinaryChoice_SceneReferencer : ResXRSingleton<BinaryChoice_SceneReferencer>
{

    [Header("Configurations")]
    public float SecondsBetweenStimuli = 0.5f;
    public float instructionsDisplayTime = 3f;

    [Header("Objects")]
    public FixationCross fixationCross;
    public ChoicesManager choicesManager;
    public InstructionsPanelWithConfirmation generalInstructions;
    public InstructionsPanel endInstructions;
}
