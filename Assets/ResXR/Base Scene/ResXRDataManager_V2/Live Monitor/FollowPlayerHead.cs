using UnityEngine;

public class FollowPlayerHead : MonoBehaviour
{
    Transform playerHead;
    void Start()
    {
        playerHead = ResXRPlayer.Instance.PlayerHead;
    }

    
    void Update()
    {
        transform.position = playerHead.position;
        transform.rotation = playerHead.rotation;
    }
}
