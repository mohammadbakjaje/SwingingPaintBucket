using UnityEngine;

public class PaintGrid : MonoBehaviour
{
    public int rows = 20;
    public int cols = 20;

    public float cellSize = 0.2f;

    private PaintCell[,] grid;

    void Start()
    {
        GenerateGrid();
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

                Renderer renderer = cellObject.GetComponent<Renderer>();
                renderer.material.color = Color.white;

                grid[x, y] = cell;
            }
        }
    }
}