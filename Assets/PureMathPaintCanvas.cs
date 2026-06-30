using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class PureMathPaintCanvas : MonoBehaviour
{
    [Header("Canvas Settings (إعدادات لوحة الرسم)")]
    public int textureSize = 1024; 
    public Color backgroundColor = Color.white; 

    private Texture2D dynamicTexture;
    private Renderer planeRenderer;
    private Vector3 planeScale;
    private Vector2Int? lastPixelPos = null;
    
    // متغير مراقبة التعديل لمنع استهلاك المعالج
    private bool isTextureDirty = false;

    void Awake()
    {
        planeRenderer = GetComponent<Renderer>();
        planeScale = transform.localScale;

        dynamicTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        
        Color[] pixels = new Color[textureSize * textureSize];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = backgroundColor;
        
        dynamicTexture.SetPixels(pixels);
        dynamicTexture.Apply();

        if (planeRenderer != null)
        {
            planeRenderer.material.mainTexture = dynamicTexture;
        }
    }

    // نقل عملية تطبيق التغييرات إلى الفريم الرندري بدلاً من الفريم الفيزيائي
    void Update()
    {
        if (isTextureDirty && dynamicTexture != null)
        {
            dynamicTexture.Apply(false);
            isTextureDirty = false;
        }
    }

    public void PaintAtWorldPosition(Vector3 worldPos, Color paintColor, float brushRadiusUnits)
    {
        float planeWidth = planeScale.x * 10f;
        float planeLength = planeScale.z * 10f;
        Vector3 planeCenter = transform.position;

        float minX = planeCenter.x - (planeWidth * 0.5f);
        float minZ = planeCenter.z - (planeLength * 0.5f);

        float u = (worldPos.x - minX) / planeWidth;
        float v = (worldPos.z - minZ) / planeLength;

        if (u >= 0f && u <= 1f && v >= 0f && v <= 1f)
        {
            int centerX = (int)(u * textureSize);
            int centerY = (int)(v * textureSize);

            int brushPixelRadius = (int)((brushRadiusUnits / planeWidth) * textureSize);
            if (brushPixelRadius < 1) brushPixelRadius = 1;

            if (lastPixelPos.HasValue)
            {
                DrawContinuousLine(lastPixelPos.Value.x, lastPixelPos.Value.y, centerX, centerY, brushPixelRadius, paintColor);
            }
            else
            {
                DrawCircleBrush(centerX, centerY, brushPixelRadius, paintColor);
            }

            // نكتفي برفع العلم هنا دون إيقاف المعالج
            isTextureDirty = true;
            lastPixelPos = new Vector2Int(centerX, centerY);
        }
        else
        {
            lastPixelPos = null;
        }
    }

    private void DrawCircleBrush(int cx, int cy, int radius, Color color)
    {
        float intensity = 0.65f;
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                if (x * x + y * y <= radius * radius)
                {
                    int targetX = cx + x;
                    int targetY = cy + y;

                    if (targetX >= 0 && targetX < textureSize && targetY >= 0 && targetY < textureSize)
                    {
                        BlendCanvasPixel(targetX, targetY, color, intensity);
                    }
                }
            }
        }
    }

    private void DrawContinuousLine(int x0, int y0, int x1, int y1, int radius, Color color)
    {
        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            DrawCircleBrush(x0, y0, radius, color);

            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }

    private void BlendCanvasPixel(int x, int y, Color newColor, float blendFactor)
    {
        Color current = dynamicTexture.GetPixel(x, y);
        Color finalColor = Color.Lerp(current, newColor, Mathf.Clamp01(blendFactor));
        dynamicTexture.SetPixel(x, y, finalColor);
    }

    public void ResetLastTransientPosition()
    {
        lastPixelPos = null;
    }
}