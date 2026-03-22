using UnityEngine;

// Determines the player's position and orientation in scenes.
public enum ERepositionType { FloorLevel, EyeLevel}
public class PlayerRepositioner : MonoBehaviour
{
    public ERepositionType Type;

    private void Awake()
    {
        // destroy the reference objects when loading the scene
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
    }
}
