using UnityEngine;

public class Line : MonoBehaviour
{
    public Vector3 Start { get; set; }
    public Vector3 End { get; set; }
    public float Thickness { get; set; }
    public Color Color { get; set; }

    public void Initialize(Vector3 start, Vector3 end, float thickness, Color color)
    {
        Start = start;
        End = end;
        Thickness = thickness;
        Color = color;
    }
} 