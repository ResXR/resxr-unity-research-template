using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EFadeType
{
    None,
    FadeInOut,
    FadeIn,
    FadeOut,
}

public static class FadeTypeExtensions
{
    public static bool ShouldFadeIn(this EFadeType fadeType)
    {
        return fadeType is EFadeType.FadeIn or EFadeType.FadeInOut;
    }

    public static bool ShouldFadeOut(this EFadeType fadeType)
    {
        return fadeType is EFadeType.FadeOut or EFadeType.FadeInOut;
    }
}