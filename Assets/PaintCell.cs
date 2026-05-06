using UnityEngine;

public class PaintCell
{
    public Vector3 worldPosition;
    public float paintAmount;
    public Color color;
    public bool painted;

    public GameObject cellObject; // 🔴 هذا السطر مهم جداً
}