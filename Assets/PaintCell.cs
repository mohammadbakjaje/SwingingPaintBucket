using UnityEngine;

/// <summary>
/// Lightweight data container for a grid cell. Not a MonoBehaviour for allocation control.
/// </summary>
public class PaintCell
{
    public Vector3 worldPosition;
    public float paintAmount = 0f;
    public Color color = Color.white;
    public bool painted = false;

    public GameObject cellObject;

    public void Reset()
    {
        paintAmount = 0f;
        color = Color.white;
        painted = false;
        cellObject = null;
    }
}