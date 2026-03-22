using NaughtyAttributes;
using UnityEngine;

public class PlaceInFrontOfPlayerHead : MonoBehaviour
{
    public enum PlacementMode
    {
        OneShotOnEnable,   // reposition once when enabled
        FollowContinuously // reposition every LateUpdate
    }

    [Header("Mode")]
    public PlacementMode mode = PlacementMode.OneShotOnEnable;

    [Header("Placement")]
    [Min(0.01f)]
    public float distance = 1.2f;
    public float verticalOffset = 0f;
    public bool useHorizontalForwardOnly = false; // ignore head pitch (keeps object level)

    [Header("Orientation")]
    public bool facePlayer = true;          // rotate to face the player head
    public bool keepUpright = true;         // lock rotation to world up
    public bool flipFacing180 = false;      // if object forward is "backwards", flip it


    private bool IsFollowMode() => mode == PlacementMode.FollowContinuously;
    [Header("Smoothing (optional, only for FollowContinuously mode)")]
    [ShowIf(nameof(IsFollowMode))] public bool smooth = false;
    [ShowIf(nameof(IsFollowMode)), Min(0f)] public float positionLerpSpeed = 2f;
    [ShowIf(nameof(IsFollowMode)), Min(0f)] public float rotationLerpSpeed = 2f;


    private Transform _head;

    private void Awake()
    {
        CacheHead();
    }

    private void OnEnable()
    {
        CacheHead();

        if (mode == PlacementMode.OneShotOnEnable)
            RepositionNow();
    }

    private void LateUpdate()
    {
        if (mode != PlacementMode.FollowContinuously)
            return;

        if (_head == null)
            CacheHead();

        if (_head != null)
            RepositionNow();
    }

    /// <summary>Call this manually if you're using OneShot mode and want to reposition on demand.</summary>
    [Button]
    public void RepositionNow()
    {
        if (_head == null)
            return;

        Vector3 forward = _head.forward;

        if (useHorizontalForwardOnly)
        {
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f) forward = _head.forward; // fallback
            forward.Normalize();
        }

        Vector3 targetPos = _head.position + forward * distance + Vector3.up * verticalOffset;

        Quaternion targetRot = transform.rotation;

        if (facePlayer)
        {
            // Look at the player (so the object's forward points towards the head)
            Vector3 toHead = (_head.position - targetPos);

            if (useHorizontalForwardOnly)
                toHead.y = 0f;

            if (toHead.sqrMagnitude > 0.0001f)
            {
                targetRot = Quaternion.LookRotation(toHead.normalized, Vector3.up);

                if (flipFacing180)
                    targetRot *= Quaternion.Euler(0f, 180f, 0f);

                if (keepUpright)
                {
                    Vector3 e = targetRot.eulerAngles;
                    targetRot = Quaternion.Euler(0f, e.y, 0f);
                }
            }
        }
        else if (keepUpright)
        {
            // If not facing player but want upright, zero out pitch/roll
            Vector3 e = transform.rotation.eulerAngles;
            targetRot = Quaternion.Euler(0f, e.y, 0f);
        }

        bool shouldSnap = (mode == PlacementMode.OneShotOnEnable) || !smooth;

        if (shouldSnap)
        {
            
            transform.SetPositionAndRotation(targetPos, targetRot);
            
        }
        else
        {
            float pT = 1f - Mathf.Exp(-positionLerpSpeed * Time.deltaTime);
            float rT = 1f - Mathf.Exp(-rotationLerpSpeed * Time.deltaTime);

            transform.position = Vector3.Lerp(transform.position, targetPos, pT);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rT);
        }


    }

    private void CacheHead()
    {
        if (ResXRPlayer.Instance != null)
            _head = ResXRPlayer.Instance.PlayerHead;
    }
}
