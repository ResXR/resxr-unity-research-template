using Unity.Mathematics;
using UnityEngine;

public class Maze : MonoBehaviour
{
    private Quaternion originalOrientation;
    private bool isRotated = false;

    private void Awake()
    {
        originalOrientation = transform.rotation;
    }

    public void Rotate180Degrees()
    {
        isRotated = !isRotated;
        transform.rotation = Quaternion.AngleAxis(isRotated ? 180f : 0f, Vector3.up) * originalOrientation;
    }



}
