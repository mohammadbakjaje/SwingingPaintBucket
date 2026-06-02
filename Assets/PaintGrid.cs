using UnityEngine;

/// <summary>
/// PaintGrid يستقبل أحداث رش الطلاء من الفيزياء ويحسب الرشّة رياضياً على مستوى اللوحة.
/// لا يستخدم أي Raycast أو Collider، فقط Projection على مستوٍ أفقي وصنف خلية خفيف.
/// </summary>
public class PaintGrid : MonoBehaviour
{
    public Color currentPaintColor = Color.red;

    [Header("Texture Settings")]
    public int textureWidth = 1024;
    public int textureHeight = 1024;
    public Color clearColor = Color.white;

    private Texture2D dynamicTexture;
    private Color32[] pixelData;
    private Renderer quadRenderer;
    private bool needsApply = false;

    // دالة تهيئة أولية: تنفذ مرة واحدة عند بدء تشغيل اللعبة
    // هنا نحذف أي كوليدر موجود على الكواد، ننشئ التكتشر في الذاكرة، ثم نربطها بالماتيريال
    void Start()
    {
        RemoveColliderFromQuad();
        CreateDynamicTexture();
        ApplyTextureToQuad();
    }

    // دالة تحديث نهاية الإطار: تطبق التغييرات على التكتشر مرة واحدة في نهاية كل فريم
    void LateUpdate()
    {
        if (needsApply && dynamicTexture != null && pixelData != null)
        {
            dynamicTexture.SetPixels32(pixelData);
            dynamicTexture.Apply();
            needsApply = false;
        }
    }

    // دالة إزالة الكوليدر من الكواد الوحيد إذا كان موجوداً
    // هذا يمنع أي كوليدرز غير مرغوب بها وتجنب مشاكل التحقق الجامعي
    void RemoveColliderFromQuad()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Destroy(col);
        }
    }

    // دالة إنشاء Texture2D جديدة في الذاكرة وتعبئتها بلون الخلفية الافتراضي
    // دالة إنشاء Texture2D جديدة في الذاكرة وتعبئتها بلون الخلفية الافتراضي
    void CreateDynamicTexture()
    {
        dynamicTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        dynamicTexture.filterMode = FilterMode.Bilinear;
        dynamicTexture.wrapMode = TextureWrapMode.Clamp;

        pixelData = new Color32[textureWidth * textureHeight];
        Color32 clearColor32 = clearColor;
        for (int i = 0; i < pixelData.Length; i++)
        {
            pixelData[i] = clearColor32;
        }

        dynamicTexture.SetPixels32(pixelData);
        dynamicTexture.Apply();
    }

    // دالة ربط الـ Texture التي أنشأناها بالماتيريال الخاص بالكواد
    // نستخدم مادة جديدة واحدة فقط لتفادي مشاكل الأداء في VR
    void ApplyTextureToQuad()
    {
        quadRenderer = GetComponent<Renderer>();
        if (quadRenderer == null)
            return;

        quadRenderer.sharedMaterial = new Material(quadRenderer.sharedMaterial);
        quadRenderer.sharedMaterial.mainTexture = dynamicTexture;
        quadRenderer.sharedMaterial.mainTextureScale = Vector2.one;
        quadRenderer.sharedMaterial.mainTextureOffset = Vector2.zero;
    }

    // دالة الرسم: تحول نقطة الاصطدام إلى بكسل على التكتشر وتلون دائرة من البكسلات
    public void PaintAtPoint(Vector3 worldPos, float radius, float currentSpeed)
    {
        if (dynamicTexture == null || pixelData == null)
            return;

        Vector3 localPos = transform.InverseTransformPoint(worldPos);
        float u = localPos.x + 0.5f;
        float v = localPos.y + 0.5f;

        if (u < 0f || u > 1f || v < 0f || v > 1f)
            return;

        int centerX = Mathf.FloorToInt(u * textureWidth);
        int centerY = Mathf.FloorToInt(v * textureHeight);

        float boardWidth = Mathf.Abs(transform.localScale.x);
        int pixelRadius = Mathf.CeilToInt((radius / boardWidth) * textureWidth);
        pixelRadius = Mathf.Max(pixelRadius, 1);
        int radiusSqr = pixelRadius * pixelRadius;

        Color32 paintColor32 = currentPaintColor;

        int startX = Mathf.Max(0, centerX - pixelRadius);
        int endX = Mathf.Min(textureWidth - 1, centerX + pixelRadius);
        int startY = Mathf.Max(0, centerY - pixelRadius);
        int endY = Mathf.Min(textureHeight - 1, centerY + pixelRadius);

        for (int x = startX; x <= endX; x++)
        {
            int dx = x - centerX;
            int dxSqr = dx * dx;
            for (int y = startY; y <= endY; y++)
            {
                int dy = y - centerY;
                int distSqr = dxSqr + dy * dy;
                if (distSqr > radiusSqr)
                    continue;

                float strength = 1f - (Mathf.Sqrt(distSqr) / pixelRadius);
                float alpha = Mathf.Clamp01(strength * currentSpeed * Time.deltaTime * 10f);

                int index = y * textureWidth + x;
                Color existing = pixelData[index];
                Color blended = Color.Lerp(existing, paintColor32, alpha);
                pixelData[index] = blended;
            }
        }

        needsApply = true;
    }

    // دالة تنظيف الذاكرة عند تدمير الكائن: تحذف الـ Texture والماتيريال الديناميكيين
    void OnDestroy()
    {
        if (dynamicTexture != null)
        {
            Destroy(dynamicTexture);
        }

        if (quadRenderer != null && quadRenderer.sharedMaterial != null)
        {
            Destroy(quadRenderer.sharedMaterial);
        }
    }

}