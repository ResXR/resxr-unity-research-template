using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class ColorExtensions
{
    public static void SetAlpha<T>(this T graphic, float alpha) where T : Graphic
    {
        graphic.color = new Color(graphic.color.r, graphic.color.g, graphic.color.b, alpha);
    }

    public static void SetAlpha(this Rectangle rectangle, float alpha)
    {
        rectangle.Color = new Color(rectangle.Color.r, rectangle.Color.g, rectangle.Color.b, alpha);
    }
}