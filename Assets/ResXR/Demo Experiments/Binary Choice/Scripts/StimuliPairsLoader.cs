using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class StimuliPairsLoader
{

    private List<StimuliPair> stimuliPairs = new List<StimuliPair>();
    private int currentPairIndex = 0;

    #region constructor and initialization
    public StimuliPairsLoader(string folderPath, StimuliOrder order)
    {
        LoadPairs(folderPath, order);
    }

    private void LoadPairs(string folderPath, StimuliOrder order)
    {
        // Validate folderPath
        if (folderPath == null || folderPath == "")
        {
            Debug.LogError("[StimuliPairsDispatcher] LoadPairs: folderPath is null or empty.");
            return;
        }

        // clear any previously loaded pairs
        stimuliPairs.Clear();

        Debug.Log($"[StimuliPairsDispatcher] Loading sprites from folder: {folderPath} with order: {order}");

        // Load all sprites from the specified folder
        List<Sprite> sprites = Resources.LoadAll<Sprite>(folderPath).ToList<Sprite>();

        switch (order)
        {
            case StimuliOrder.RandomOrder:
                {
                    // shuffle the list of sprites (using the ResXR ListExtensions)
                    sprites.Shuffle();
                    break;
                }
            case StimuliOrder.FixedOrder:
                {
                    // sort the sprites by name to ensure fixed order
                    sprites = sprites.OrderBy(sprite => sprite.name).ToList();
                    break;
                }
        }

        // create pairs from the ordered sprites list
        for (int i = 0; i < sprites.Count - 1; i += 2)
        {
            StimuliPair pair = new StimuliPair(sprites[i], sprites[i + 1]);
            stimuliPairs.Add(pair);
        }

        Debug.Log($"[StimuliPairsDispatcher] Created {stimuliPairs.Count} stimuli pairs from {sprites.Count} sprites.");
    }
    #endregion

    #region public methods
    public List<StimuliPair> GetAllPairs()
    {
        return stimuliPairs;
    }

    public bool HasMorePairs()
    {
        return currentPairIndex < stimuliPairs.Count;
    }

    public StimuliPair GetNextPair()
    {
        return stimuliPairs[currentPairIndex++];
    }

    #endregion
}



public class StimuliPair
///<summary>
///a class representing a pair of stimuli sprites for convinience.
{
    public Sprite stimulusASprite;
    public Sprite stimulusBSprite;

    public StimuliPair(Sprite StimulusASprite, Sprite StimulusBSprite)
    {
        this.stimulusASprite = StimulusASprite;
        this.stimulusBSprite = StimulusBSprite;
    }
}
