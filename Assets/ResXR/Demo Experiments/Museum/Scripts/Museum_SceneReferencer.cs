using UnityEngine;

public class Museum_SceneReferencer : ResXRSingleton<Museum_SceneReferencer>
{
    [Header("References")]
    public InstructionsPanelWithConfirmation welcomeInstructions;
    public InstructionsPanelWithConfirmation endInstructions;
    public InstructionsPanelWithConfirmation endOfExplorationInstructions;
    public InstructionsPanelWithConfirmation ratingInstructions;
    public PlayerPositionMark ratingTaskPlayerPositionMark;

    public ImagesRating imagesRating;

    [Header("Artwork Bounds")]
    [Tooltip("Assign all artwork Renderer components here. ArtworkBounds.csv is written once at session start.\n" +
             "IMPORTANT: Make sure each artwork GameObject has a meaningful, unique name — it becomes the ArtworkName column.")]
    public Renderer[] artworks;
}
