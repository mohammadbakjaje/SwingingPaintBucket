using UnityEngine;

public class PaintGrid : MonoBehaviour
{
    public int rows = 20;
    public int cols = 20;

    public float cellSize = 0.2f;
    public Color currentPaintColor = Color.red;
    private PaintCell[,] grid;

    void Start()
    {
        GenerateGrid();
    }
    public PaintCell GetCellFromWorldPosition(Vector3 worldPos)
    {
        // تحويل النقطة إلى local بالنسبة للوحة
        Vector3 localPos = worldPos - transform.position;

        int x = Mathf.FloorToInt(localPos.x / cellSize);
        int y = Mathf.FloorToInt(localPos.z / cellSize);

        // تحقق من الحدود
        if (x < 0 || x >= rows || y < 0 || y >= cols)
            return null;

        return grid[x, y];
    }


    public void PaintAtPoint(Vector3 worldPos, float radius, float currentSpeed)
    {
        Vector3 localPos = worldPos - transform.position;

        int centerX = Mathf.FloorToInt(localPos.x / cellSize);
        int centerY = Mathf.FloorToInt(localPos.z / cellSize);

        int range = Mathf.CeilToInt(radius / cellSize);

        for (int x = centerX - range; x <= centerX + range; x++)
        {
            for (int y = centerY - range; y <= centerY + range; y++)
            {
                if (x < 0 || x >= rows || y < 0 || y >= cols)
                    continue;

                PaintCell cell = grid[x, y];

                float dist = Vector2.Distance(
                    new Vector2(x, y),
                    new Vector2(centerX, centerY)
                );

                if (dist <= range)
                {
                    float strength = 1f - (dist / range);

                    ApplyPaint(cell, strength, currentSpeed);
                }
            }
        }
    }


    void ApplyPaint(PaintCell cell, float strength, float intensity)
    {
        // أضفنا عامل 'intensity' للتحكم بقوة الرشة (المركزية غامقة والطرطشة فاتحة)
        float addedPaint = strength * intensity * Time.deltaTime * 10.0f;

        cell.paintAmount += addedPaint;
        cell.paintAmount = Mathf.Clamp01(cell.paintAmount);

        Color finalColor = Color.Lerp(Color.white, currentPaintColor, cell.paintAmount);

        if (cell.cellObject != null)
        {
            cell.cellObject.GetComponent<Renderer>().material.color = finalColor;
        }
    }

    void GenerateGrid()
    {
        grid = new PaintCell[rows, cols];

        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < cols; y++)
            {
                Vector3 cellPosition = new Vector3(
                    x * cellSize,
                    0,
                    y * cellSize
                );
                GameObject cellObject = GameObject.CreatePrimitive(PrimitiveType.Quad);

                cellObject.transform.position = transform.position + cellPosition;
                cellObject.transform.parent = transform;
                cellObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                cellObject.transform.localScale = Vector3.one * cellSize;

                PaintCell cell = new PaintCell();
                cell.worldPosition = cellObject.transform.position;
                cell.paintAmount = 0f;
                cell.color = Color.white;
                cell.painted = false;
                cell.cellObject = cellObject;
                Renderer renderer = cellObject.GetComponent<Renderer>();
                renderer.material.color = Color.white;

                grid[x, y] = cell;
            }
        }
    }
}