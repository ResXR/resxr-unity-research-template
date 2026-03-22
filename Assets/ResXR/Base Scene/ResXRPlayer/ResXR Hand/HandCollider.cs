using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandCollider : MonoBehaviour
{
    public HandType HandT;

    OVRSkeleton ovrSkeleton;
    public int fingerIndex;

    public void Init(OVRSkeleton skeleton)
    {
        ovrSkeleton = skeleton;
    }

    public void UpdateHandCollider()
    {
        TrackPosition();
    }

    private void TrackPosition()
    {
        transform.position = ovrSkeleton.Bones[fingerIndex].Transform.position;
        transform.rotation = ovrSkeleton.Bones[fingerIndex].Transform.rotation;
    }
}
