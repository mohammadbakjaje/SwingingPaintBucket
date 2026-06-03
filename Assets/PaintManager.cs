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
        [Range(0.1f, 5f)] public float spreading;   // معامل انتشار الطلاء على السطح
    }

    // ==========================================
    // 2. المتغيرات وإعدادات المفتش (Inspector)
    // ==========================================
    [Header("Dependencies")]
    [SerializeField] private Renderer targetRenderer; // السطح الذي سيتم الرسم عليه

    [Header("Color Management (Slots)")]
    [SerializeField] private Color32[] colorSlots = new Color32[5] 
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
    [SerializeField] private SurfaceProfile[] surfaceProfiles = new SurfaceProfile[4]
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
        // جلب خامة الكائن المستهدف وإنشاء نسخة فريدة من الـ Texture لتعديلها
        if (targetRenderer != null && targetRenderer.material.mainTexture != null)
        {
            Texture2D originalTex = targetRenderer.material.mainTexture as Texture2D;
            if (originalTex != null)
            {
                // إنشاء Texture جديدة مطابقة للأصل لعدم التعديل على الملف الخام في الهاردسك
                m_Texture = new Texture2D(originalTex.width, originalTex.height, TextureFormat.RGBA32, false);
                m_Texture.SetPixels32(originalTex.GetPixels32());
                m_Texture.Apply();
                
                targetRenderer.material.mainTexture = m_Texture;
                
                m_TexWidth = m_Texture.width;
                m_TexHeight = m_Texture.height;
                
                // جلب مصفوفة البكسلات الملوية إلى الذاكرة المؤقتة (RAM) لسرعة المعالجة
                m_PixelArray = m_Texture.GetPixels32();
                return;
            }
        }
        Debug.LogError("PaintManager: Target Renderer or Texture2D is missing!");
    }

    // ==========================================
    // 4. دالة استقبال الحدث الفيزيائي
    // ==========================================
    public void OnPhysicsImpact(Vector2 uvCoordinates, float impactVelocity, float viscosity, SurfaceType hitSurface)
    {
        if (m_PixelArray == null) return;

        // أ. جلب خصائص السطح واللون الحالي
        SurfaceProfile profile = GetProfileForSurface(hitSurface);
        Color32 paintColor = colorSlots[currentColorIndex];

        // ب. حساب نصف القطر الديناميكي بالأبعاد الأكاديمية: R ∝ Spreading * (v^alpha) * (viscosity^beta)
        float calculatedRadius = profile.spreading * Mathf.Pow(impactVelocity, alpha) * Mathf.Pow(viscosity, beta);
        
        // تحويل نصف القطر من وحدات العالم إلى وحدات البكسل بناءً على حجم التكستشر (تقريبي)
        int pixelRadius = Mathf.Clamp(Mathf.RoundToInt(calculatedRadius * Mathf.Max(m_TexWidth, m_TexHeight)), 2, 128);

        // ج. تحويل إحداثيات UV إلى إحداثيات بكسل مركزية (Center X, Y)
        int centerX = Mathf.FloorToInt(uvCoordinates.x * m_TexWidth);
        int centerY = Mathf.FloorToInt(uvCoordinates.y * m_TexHeight);

        // د. تطبيق دالة الرسم المحسنة باستخدام الـ Bounding Box
        ExecuteSplatter(centerX, centerY, pixelRadius, paintColor, profile);
    }

    // ==========================================
    // 5. خوارزمية الرسم وتأثير غاوس (Splatter Algorithm)
    // ==========================================
    private void ExecuteSplatter(int cx, int cy, int radius, Color32 color, SurfaceProfile profile)
    {
        // تحديد حدود الـ Bounding Box لتقنين ركود الحلقات التكرارية وضمان الـ Performance
        int minX = Mathf.Max(0, cx - radius);
        int maxX = Mathf.Min(m_TexWidth - 1, cx + radius);
        int minY = Mathf.Max(0, cy - radius);
        int maxY = Mathf.Min(m_TexHeight - 1, cy + radius);

        float radiusSqr = radius * radius;
        
        // تجهيز قيم غاوس مسبقاً إذا كان السطح مسامياً لتفادي الحساب المتكرر داخل الـ Loop
        // الانحراف المعياري (Sigma) يعتمد على مسامية السطح (Porosity)
        float porosityFactor = Mathf.Max(0.05f, profile.porosity); 
        float sigma = radius * (1.1f - porosityFactor); // كلما زادت المسامية، كبُر انتشار الضبابية
        float twoSigmaSqr = 2f * sigma * sigma;

        for (int y = minY; y <= maxY; y++)
        {
            int rowOffset = y * m_TexWidth; // تحسين الوصول للمصفوفة أحادية البعد
            
            for (int x = minX; x <= maxX; x++)
            {
                // حساب مربعات المسافة برمجياً لتجنب استدعاء دالة الجذر التربيعي (Mathf.Sqrt) البطيئة
                float dx = x - cx;
                float dy = y - cy;
                float distSqr = (dx * dx) + (dy * dy);

                if (distSqr <= radiusSqr)
                {
                    float finalAlpha = 1.0f;

                    // تطبيق خوارزمية الأطراف بناءً على نوع السطح والمسامية
                    if (profile.porosity > 0f)
                    {
                        // معادلة غاوس الأكاديمية للأطراف الناعمة: Alpha = e^(-d^2 / 2*sigma^2)
                        float dist = Mathf.Sqrt(distSqr);
                        finalAlpha = Mathf.Exp(-(dist * dist) / twoSigmaSqr);
                        
                        // تعديل أطراف التنعيم بناءً على الخشونة لإضافة تأثير تشتت بسيط
                        finalAlpha = Mathf.Clamp01(finalAlpha * (1.0f - (dist / radius) * profile.roughness));
                    }

                    // دمج اللون الجديد مع القديم في المصفوفة (Color Blending / Linear Interpolation)
                    int pixelIndex = x + rowOffset;
                    Color32 currentPixelColor = m_PixelArray[pixelIndex];

                    // معادلة الـ Blending اليدوية بدون فلاتر جاهزة:
                    // NewColor = CurrentColor * (1 - Alpha) + PaintColor * Alpha
                    m_PixelArray[pixelIndex].r = (byte)Mathf.Lerp(currentPixelColor.r, color.r, finalAlpha);
                    m_PixelArray[pixelIndex].g = (byte)Mathf.Lerp(currentPixelColor.g, color.g, finalAlpha);
                    m_PixelArray[pixelIndex].b = (byte)Mathf.Lerp(currentPixelColor.b, color.b, finalAlpha);
                    m_PixelArray[pixelIndex].a = (byte)Mathf.Max(currentPixelColor.a, (byte)(finalAlpha * 255));
                }
            }
        }

        // رفع الـ Flag لإعلام المحرك بوجوب تحديث الـ Texture في نهاية الإطار
        m_IsTextureDirty = true;
    }

    // ==========================================
    // 6. تحسين الأداء (LateUpdate Batching)
    // ==========================================
    private void LateUpdate()
    {
        // إذا حدث اصطدام أو أكثر خلال هذا الإطار، نقوم بتحديث الـ Texture "مرة واحدة فقط" هنا
        if (m_IsTextureDirty)
        {
            m_Texture.SetPixels32(m_PixelArray);
            m_Texture.Apply(false); // تمرير false لإيقاف الـ Mipmaps مؤقتاً لزيادة الأداء أثناء اللعب
            m_IsTextureDirty = false;
        }
    }

    // ==========================================
    // أدوات مساعدة (Helper Functions)
    // ==========================================
    private SurfaceProfile GetProfileForSurface(SurfaceType type)
    {
        for (int i = 0; i < surfaceProfiles.Length; i++)
        {
            if (surfaceProfiles[i].type == type) return surfaceProfiles[i];
        }
        return surfaceProfiles[0]; // الافتراضي Metal
    }

    public void SetColorSlot(int index)
    {
        if (index >= 0 && index < colorSlots.Length)
        {
            currentColorIndex = index;
        }
    }
}