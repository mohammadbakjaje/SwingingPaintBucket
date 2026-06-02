using UnityEngine;

/// <summary>
/// PaintGrid يستقبل أحداث رش الطلاء من الفيزياء ويحسب الرشّة رياضياً على مستوى اللوحة.
/// لا يستخدم أي Raycast أو Collider، فقط Projection على مستوٍ أفقي وصنف خلية خفيف.
/// </summary>
public class PaintGrid : MonoBehaviour
{
    [Header("Grid")]
    public int rows = 20;
    public int cols = 20;
    public float cellSize = 0.2f;

    [Header("Canvas Plane Settings")]
    [SerializeField] private float canvasYHeight = 0.0f;
    [SerializeField] private float canvasWidth = 10f;
    [SerializeField] private float canvasHeight = 10f;

    [Header("Splatter Tuning")]
    [SerializeField] private float baseRadiusFactor = 0.05f;

    [Header("Visuals")]
    [SerializeField] private Color defaultCellColor = Color.white;

    private PaintCell[,] grid;
    private MaterialPropertyBlock mpb;

    private void Awake()
    {
        mpb = new MaterialPropertyBlock();
        GenerateGrid();
    }

    private void OnEnable()
    {
        PhysicsEvents.OnPaintSplatter += HandlePaintSplatter;
    }

    private void OnDisable()
    {
        PhysicsEvents.OnPaintSplatter -= HandlePaintSplatter;
    }

    public PaintCell GetCellFromWorldPosition(Vector3 worldPos)
    {
        Vector3 localPos = worldPos - transform.position;

        int x = Mathf.FloorToInt(localPos.x / cellSize);
        int y = Mathf.FloorToInt(localPos.z / cellSize);

        if (x < 0 || x >= cols || y < 0 || y >= rows)
            return null;

        return grid[x, y];
    }

    public void PaintAtPoint(Vector3 worldPos, float radius, float currentSpeed, Color paintColor)
    {
        Vector3 localPos = worldPos - transform.position;
        Vector3 localCenter = new Vector3(localPos.x + canvasWidth * 0.5f, 0f, localPos.z + canvasHeight * 0.5f);

        int centerX = Mathf.FloorToInt(localCenter.x / cellSize);
        int centerY = Mathf.FloorToInt(localCenter.z / cellSize);

        int range = Mathf.CeilToInt(radius / cellSize);

        for (int x = centerX - range; x <= centerX + range; x++)
        {
            for (int y = centerY - range; y <= centerY + range; y++)
            {
                if (x < 0 || x >= cols || y < 0 || y >= rows)
                    continue;

                PaintCell cell = grid[x, y];

                float cellCenterX = (x + 0.5f) * cellSize;
                float cellCenterZ = (y + 0.5f) * cellSize;
                float dx = cellCenterX - localCenter.x;
                float dz = cellCenterZ - localCenter.z;
                float dist = Mathf.Sqrt(dx * dx + dz * dz);

                if (dist <= range * cellSize)
                {
                    float strength = 1f - (dist / (range * cellSize));
                    ApplyPaint(cell, strength, currentSpeed, paintColor);
                }
            }
        }
    }

    private void ApplyPaint(PaintCell cell, float strength, float intensity, Color color)
    {
        float addedPaint = strength * Mathf.Clamp01(intensity * 0.1f) * Time.deltaTime * 10f;
        cell.paintAmount = Mathf.Clamp01(cell.paintAmount + addedPaint);
        cell.color = Color.Lerp(defaultCellColor, color, cell.paintAmount);
        cell.painted = cell.paintAmount > 0f;

        if (cell.cellObject == null)
            return;

        var renderer = cell.cellObject.GetComponent<Renderer>();
        if (renderer == null)
            return;

        mpb.SetColor("_Color", cell.color);
        renderer.SetPropertyBlock(mpb);
    }

    private void GenerateGrid()
    {
        grid = new PaintCell[cols, rows];
        float startX = -canvasWidth * 0.5f + cellSize * 0.5f;
        float startZ = -canvasHeight * 0.5f + cellSize * 0.5f;

        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                Vector3 cellPosition = new Vector3(
                    startX + x * cellSize,
                    canvasYHeight,
                    startZ + y * cellSize
                );

                GameObject cellObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
                cellObject.transform.position = transform.position + cellPosition;
                cellObject.transform.parent = transform;
                cellObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                cellObject.transform.localScale = Vector3.one * cellSize;

                var col = cellObject.GetComponent<Collider>();
                if (col != null)
                    Destroy(col);

                PaintCell cell = new PaintCell();
                cell.worldPosition = cellObject.transform.position;
                cell.paintAmount = 0f;
                cell.color = defaultCellColor;
                cell.painted = false;
                cell.cellObject = cellObject;

                var renderer = cellObject.GetComponent<Renderer>();
                if (renderer != null)
                {
                    mpb.SetColor("_Color", defaultCellColor);
                    renderer.SetPropertyBlock(mpb);
                }

                grid[x, y] = cell;
            }
        }
    }

    private void HandlePaintSplatter(Vector3 bucketPosition, Color paintColor, float currentSpeed, float viscosity)
    {
        Vector3 impactPoint = new Vector3(bucketPosition.x, canvasYHeight, bucketPosition.z);
        Vector3 local = impactPoint - transform.position;

        float halfW = canvasWidth * 0.5f;
        float halfH = canvasHeight * 0.5f;

        if (local.x < -halfW || local.x > halfW || local.z < -halfH || local.z > halfH)
            return;

        float safeVisc = Mathf.Max(0.0001f, viscosity);
        float radius = (currentSpeed / safeVisc) * baseRadiusFactor;
        radius = Mathf.Clamp(radius, 0.01f, Mathf.Max(canvasWidth, canvasHeight));

        PaintAtPoint(impactPoint, radius, currentSpeed, paintColor);
    }
}
