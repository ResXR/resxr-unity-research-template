using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ADetector : MonoBehaviour
{
    public Action InteractionStarted;
    public Action InteractionEnded;
}