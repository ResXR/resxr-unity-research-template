using DG.Tweening;
using UnityEngine;
using TMPro;

public static class ComponentAnimator
{

    public static Tween TransformTween(Transform sourceTransform, Transform targetTransform, float transitionDuration, bool isLocal = false)
    {
        Vector3 originPosition = isLocal ? sourceTransform.localPosition : sourceTransform.position;
        Quaternion originRotation = isLocal ? sourceTransform.localRotation : sourceTransform.rotation;
        Vector3 originScale = sourceTransform.localScale;

        Tween tween;
        tween = DOVirtual.Float(0f, 1f, transitionDuration, t =>
        {
            if (isLocal)
            {
                sourceTransform.localPosition = Vector3.Lerp(originPosition, targetTransform.localPosition, t);
                sourceTransform.localRotation = Quaternion.Lerp(originRotation, targetTransform.localRotation, t);
            }
            else
            {
                sourceTransform.position = Vector3.Lerp(originPosition, targetTransform.position, t);
                sourceTransform.rotation = Quaternion.Lerp(originRotation, targetTransform.rotation, t);
            }

            sourceTransform.localScale = Vector3.Lerp(originScale, targetTransform.localScale, t);
        });

        return tween;
    }

    /*
     * - Not supporting Dashed Rectangle yet.
     */
    public static Tween RectangleTween(Rectangle sourceRectangle, Rectangle targetRectangle, float transitionDuration)
    {
        Tween tween;

        // Immediately set non-animated properties
        sourceRectangle.BlendMode = targetRectangle.BlendMode;
        sourceRectangle.ScaleMode = targetRectangle.ScaleMode;
        sourceRectangle.CornerRadiusMode = targetRectangle.CornerRadiusMode;
        sourceRectangle.ThicknessSpace = targetRectangle.ThicknessSpace;
        sourceRectangle.UseFill = targetRectangle.UseFill;
        sourceRectangle.FillSpace = targetRectangle.FillSpace;
        sourceRectangle.FillType = targetRectangle.FillType;
        sourceRectangle.Pivot = targetRectangle.Pivot;
        sourceRectangle.Type = targetRectangle.Type;

        // Size
        float sourceWidth = sourceRectangle.Width;
        float sourceHeight = sourceRectangle.Height;

        // Thickness
        float sourceThickness = sourceRectangle.Thickness;

        // Corner Radius
        float sourceCornerRadius = sourceRectangle.CornerRadius;

        // Color
        Color sourceColor = sourceRectangle.Color;

        // Fill
        float sourceFillRadius = sourceRectangle.FillRadialRadius;
        Vector3 sourceRadialOrigin = sourceRectangle.FillRadialOrigin;
        Vector3 sourceLinearStart = sourceRectangle.FillLinearStart;
        Vector3 sourceLinearEnd = sourceRectangle.FillLinearEnd;
        Color sourceStart = sourceRectangle.FillColorStart;
        Color sourceEnd = sourceRectangle.FillColorEnd;

        tween = DOVirtual.Float(0f, 1f, transitionDuration, t =>
        {
            sourceRectangle.FillColorStart = Color.Lerp(sourceStart, targetRectangle.FillColorStart, t);
            sourceRectangle.FillColorEnd = Color.Lerp(sourceEnd, targetRectangle.FillColorEnd, t);

            sourceRectangle.Width = Mathf.Lerp(sourceWidth, targetRectangle.Width, t);
            sourceRectangle.Height = Mathf.Lerp(sourceHeight, targetRectangle.Height, t);
            sourceRectangle.Thickness = Mathf.Lerp(sourceThickness, targetRectangle.Thickness, t);
            sourceRectangle.CornerRadius = Mathf.Lerp(sourceCornerRadius, targetRectangle.CornerRadius, t);
            sourceRectangle.Color = Color.Lerp(sourceColor, targetRectangle.Color, t);


            sourceRectangle.FillRadialRadius = Mathf.Lerp(sourceFillRadius, targetRectangle.FillRadialRadius, t);
            sourceRectangle.FillRadialOrigin = Vector3.Lerp(sourceRadialOrigin, targetRectangle.FillRadialOrigin, t);

            sourceRectangle.FillLinearStart = Vector3.Lerp(sourceLinearStart, targetRectangle.FillLinearStart, t);
            sourceRectangle.FillLinearEnd = Vector3.Lerp(sourceLinearEnd, targetRectangle.FillLinearEnd, t);

        });

        return tween;
    }

    public static Tween TextMeshProTween(TextMeshPro sourceText, TextMeshPro targetText, float transitionDuration)
    {
        sourceText.textStyle = targetText.textStyle;
        sourceText.fontStyle = targetText.fontStyle;

        // font size
        float fontSize = sourceText.fontSize;

        // color
        Color textColor = sourceText.color;

        Tween tween;
        tween = DOVirtual.Float(0f, 1f, transitionDuration, t =>
        {
            sourceText.fontSize = Mathf.Lerp(fontSize, targetText.fontSize, t);
            sourceText.color = Color.Lerp(textColor, targetText.color, t);

        });

        return tween;
    }
}
