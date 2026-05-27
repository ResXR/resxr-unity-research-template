using UnityEngine;

public class FollowEyeSphere : MonoBehaviour
{
    [SerializeField] private float lerpSpeed = 10;
    [SerializeField, ReadOnly] private Vector3 hitPoint;
    [SerializeField, ReadOnly] private string focusedObject;
    private ResXRPlayer player;

    private void Start()
    {
        player = ResXRPlayer.Instance;
    }

    private void FixedUpdate()
    {
        transform.position = Vector3.Lerp(transform.position, ResXRPlayer.Instance.EyeGazeHitPosition, lerpSpeed * Time.deltaTime);

        hitPoint = player.EyeGazeHitPosition;

        if (player.FocusedObject != null)
        {
            focusedObject = player.FocusedObject.name;
        }
        else
        {
            focusedObject = "null";
        }

    }
}