using UnityEngine;
using System;

public class PaintManager : MonoBehaviour
{
    // ==========================================
    // 1. البنية الأساسية للأسطح (Enums & Structs)
    // ==========================================
    public enum SurfaceType { Metal, Wood, Paper, Cloth }

    [Serializable]
    public struct SurfaceProfile
    {
        public SurfaceType type;
        [Range(0f, 1f)] public float roughness;     // خشونة السطح
        [Range(0f, 1f)] public float porosity;      // المسامية (تتحكم في نعومة الأطراف)
        [Range(0.1f, 5f)] public float spreading;   // معامل انتشار الطلاء على السطح (الطاقة السطحية)
    }

    // ==========================================
    // 2. المتغيرات وإعدادات المفتش (Inspector)
    // ==========================================
    [Header("Dependencies")]
    [SerializeField] private Renderer targetRenderer; // السطح الذي سيتم الرسم عليه

    [Header("Color Management (Slots)")]
    [SerializeField]
    private Color32[] colorSlots = new Color32[5]
    {
        new Color32(255, 0, 0, 255),    // أحمر
        new Color32(0, 0, 255, 255),    // أزرق
        new Color32(255, 255, 0, 255),  // أصفر
        new Color32(0, 255, 0, 255),    // أخضر
        new Color32(255, 255, 255, 255) // أبيض
    };
    private int currentColorIndex = 0;

    [Header("Physics & Calibration Constants")]
    [SerializeField] private float alpha = 1.2f; // أس سرعة الارتطام
    [SerializeField] private float beta = -0.5f; // أس اللزوجة (علاقة عكسية غالباً)

    [Header("Surface Profiles Configuration")]
    [SerializeField]
    private SurfaceProfile[] surfaceProfiles = new SurfaceProfile[4]
    {
        new SurfaceProfile { type = SurfaceType.Metal, roughness = 0.1f, porosity = 0.0f, spreading = 1.5f },
        new SurfaceProfile { type = SurfaceType.Wood,  roughness = 0.4f, porosity = 0.3f, spreading = 1.0f },
        new SurfaceProfile { type = SurfaceType.Paper, roughness = 0.6f, porosity = 0.7f, spreading = 0.7f },
        new SurfaceProfile { type = SurfaceType.Cloth, roughness = 0.8f, porosity = 0.9f, spreading = 0.5f }
    };

    // متغيرات إدارة البكسلات في الذاكرة (Optimization)
    private Texture2D m_Texture;
    private Color32[] m_PixelArray;
    private int m_TexWidth;
    private int m_TexHeight;
    private bool m_IsTextureDirty = false; // Flag للتحديث في نهاية الإطار

    // ==========================================
    // 3. التجهيز والتهيئة (Initialization)
    // ==========================================
    private void Start()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<Renderer>();
        }

        InitializeTexture();
    }

    private void InitializeTexture()
    {
        if (targetRenderer != null && targetRenderer.material.mainTexture != null)
        {
            Texture2D originalTex = targetRenderer.material.mainTexture as Texture2D;
            if (originalTex != null)
            {
                m_Texture = new Texture2D(originalTex.width, originalTex.height, TextureFormat.RGBA32, false);
                m_Texture.SetPixels32(originalTex.GetPixels32());
                m_Texture.Apply();

                targetRenderer.material.mainTexture = m_Texture;

                m_TexWidth = m_Texture.width;
                m_TexHeight = m_Texture.height;

                m_PixelArray = m_Texture.GetPixels32();
                return;
            }
        }
        Debug.LogError("PaintManager: Target Renderer or Texture2D is missing!");
    }

    // ==========================================
    // 4. دالة استقبال الحدث الفيزيائي
    // ==========================================
    public void OnPhysicsImpact(Vector2 uvCoordinates, float impactVelocity, float viscosity, SurfaceType hitSurface, Color32 incomingColor)
    {
        if (m_PixelArray == null) return;

        SurfaceProfile profile = GetProfileForSurface(hitSurface);
        Color32 paintColor = incomingColor;

        float calculatedRadius = profile.spreading * Mathf.Pow(impactVelocity, alpha) * Mathf.Pow(viscosity, beta);
        int pixelRadius = Mathf.Clamp(Mathf.RoundToInt(calculatedRadius * Mathf.Max(m_TexWidth, m_TexHeight) * 0.05f), 3, 128);

        int centerX = Mathf.FloorToInt(uvCoordinates.x * m_TexWidth);
        int centerY = Mathf.FloorToInt(uvCoordinates.y * m_TexHeight);

        ExecuteSplatter(centerX, centerY, pixelRadius, paintColor, profile);
    }

    // ==========================================
    // 5. خوارزمية الرسم وتأثير غاوس والخشونة
    // ==========================================
    private void ExecuteSplatter(int cx, int cy, int radius, Color32 color, SurfaceProfile profile)
    {
        int minX = Mathf.Max(0, cx - radius);
        int maxX = Mathf.Min(m_TexWidth - 1, cx + radius);
        int minY = Mathf.Max(0, cy - radius);
        int maxY = Mathf.Min(m_TexHeight - 1, cy + radius);

        float radiusSqr = radius * radius;

        float porosityFactor = Mathf.Max(0.05f, profile.porosity);
        float sigma = radius * (1.1f - porosityFactor);
        float twoSigmaSqr = 2f * sigma * sigma;

        for (int y = minY; y <= maxY; y++)
        {
            int rowOffset = y * m_TexWidth;

            for (int x = minX; x <= maxX; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                float distSqr = (dx * dx) + (dy * dy);

                if (distSqr <= radiusSqr)
                {
                    float finalAlpha = 1.0f;

                    if (profile.porosity > 0f)
                    {
                        float dist = Mathf.Sqrt(distSqr);
                        finalAlpha = Mathf.Exp(-distSqr / twoSigmaSqr);

                        if (profile.roughness > 0f)
                        {
                            float angle = Mathf.Atan2(dy, dx);
                            float roughnessNoise = Mathf.Sin(angle * 12f) * profile.roughness * 0.15f;
                            finalAlpha = Mathf.Clamp01(finalAlpha + roughnessNoise);
                        }
                    }
                    else
                    {
                        if (profile.roughness > 0f)
                        {
                            float dist = Mathf.Sqrt(distSqr);
                            float angle = Mathf.Atan2(dy, dx);
                            float edgeNoise = Mathf.Sin(angle * 16f) * profile.roughness * radius * 0.1f;
                            if (dist > (radius + edgeNoise)) continue;
                        }
                    }

                    int pixelIndex = x + rowOffset;
                    Color32 currentPixelColor = m_PixelArray[pixelIndex];

                    float blendFactor = finalAlpha * (color.a / 255f);

                    m_PixelArray[pixelIndex].r = (byte)Mathf.Lerp(currentPixelColor.r, color.r, blendFactor);
                    m_PixelArray[pixelIndex].g = (byte)Mathf.Lerp(currentPixelColor.g, color.g, blendFactor);
                    m_PixelArray[pixelIndex].b = (byte)Mathf.Lerp(currentPixelColor.b, color.b, blendFactor);
                    m_PixelArray[pixelIndex].a = (byte)Mathf.Max(currentPixelColor.a, (byte)(blendFactor * 255));
                }
            }
        }

        m_IsTextureDirty = true;
    }

    // ==========================================
    // 6. تحسين الأداء (LateUpdate Batching)
    // ==========================================
    private void LateUpdate()
    {
        if (m_IsTextureDirty)
        {
            m_Texture.SetPixels32(m_PixelArray);
            m_Texture.Apply(false);
            m_IsTextureDirty = false;
        }
    }

    // ==========================================
    // 7. أدوات مساعدة واشتراك بالأحداث
    // ==========================================
    private SurfaceProfile GetProfileForSurface(SurfaceType type)
    {
        for (int i = 0; i < surfaceProfiles.Length; i++)
        {
            if (surfaceProfiles[i].type == type) return surfaceProfiles[i];
        }
        return surfaceProfiles[0];
    }

    public void SetColorSlot(int index)
    {
        if (index >= 0 && index < colorSlots.Length)
        {
            currentColorIndex = index;
        }
    }

    private void OnEnable()
    {
        PhysicsEvents.OnPaintSplatterSplatted += HandleExternalImpact;
    }

    private void OnDisable()
    {
        PhysicsEvents.OnPaintSplatterSplatted -= HandleExternalImpact;
    }

    private void HandleExternalImpact(Vector3 worldPos, Color color, Vector3 velocity, float viscosity)
    {
        if (targetRenderer == null) return;

        Bounds bounds = targetRenderer.bounds;

        float u = (worldPos.x - bounds.min.x) / bounds.size.x;

        float v = 0f;
        if (bounds.size.z > bounds.size.y)
        {
            v = (worldPos.z - bounds.min.z) / bounds.size.z;
        }
        else
        {
            v = (worldPos.y - bounds.min.y) / bounds.size.y;
        }

        Vector2 uv = new Vector2(Mathf.Clamp01(u), Mathf.Clamp01(v));

        // تم تعديلها هنا مباشرة إلى Metal لتختبر المعدن فوراً!
        SurfaceType currentSurface = SurfaceType.Cloth;

        OnPhysicsImpact(uv, velocity.magnitude, viscosity, currentSurface, color);
    }
}