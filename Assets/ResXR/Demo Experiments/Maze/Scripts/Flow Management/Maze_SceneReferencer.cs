using UnityEngine;

public class Maze_SceneReferencer : ResXRSingleton<Maze_SceneReferencer>
{

    [Header("References")]
    public PlayerPositionMark startingPositionMark;
    public InstructionsPanelWithConfirmation generalInstructions;
    public InstructionsPanel endInstructions;
    public InstructionsPanel trialStartPanel;
    public Coin coin;
    public Maze maze;

    [Header("Settings")]
    public float trialNumberPanelVisibleDuration = 3f;

}
