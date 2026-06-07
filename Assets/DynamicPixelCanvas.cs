using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynamicPixelCanvas : MonoBehaviour
{
    public enum SurfaceType { Metal, Wood, Paper, Cloth }

    public class ProjectileParticle
    {
        public Vector3 origin;
        public Vector3 velocity;
        public float viscosity;
        public float creationTime;

        public ProjectileParticle(Vector3 o, Vector3 v, float visc)
        {
            this.origin = o;
            this.velocity = v;
            this.viscosity = visc;
            this.creationTime = Time.time;
        }
    }

    [Header("Canvas Settings")]
    public int textureWidth = 512;
    public int textureHeight = 512;
    public Color canvasBackgroundColor = Color.white;
    public SurfaceType surfaceType = SurfaceType.Paper;

    [Header("Paint Configuration")]
    public Color paintColor = Color.red;

    private Texture2D dynamicTexture;
    private Color32[] pixelBuffer;
    private List<ProjectileParticle> activeProjectiles = new List<ProjectileParticle>();
    private Vector3 gravity = new Vector3(0, -9.81f, 0);

    // Analytical Plane representation data
    private Vector3 planeNormal;
    private Vector3 planePoint;
    private Vector3 planeRight;
    private Vector3 planeUp;
    private Vector2 canvasWorldSize;

    void Start()
    {
        EnforceNoColliders();
        InitializeTexture();
        CachePlaneGeometry();
    }

    void EnforceNoColliders()
    {
        Collider[] colliders = GetComponents<Collider>();
        foreach (var col in colliders)
        {
            DestroyImmediate(col);
        }
    }

    void InitializeTexture()
    {
        dynamicTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        pixelBuffer = new Color32[textureWidth * textureHeight];
        
        Color32 bg32 = canvasBackgroundColor;
        for (int i = 0; i < pixelBuffer.Length; i++)
        {
            pixelBuffer[i] = bg32;
        }
        
        dynamicTexture.SetPixels32(pixelBuffer);
        dynamicTexture.Apply();

        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.material.mainTexture = dynamicTexture;
        }
    }

    void CachePlaneGeometry()
    {
        planeNormal = transform.forward;
        planePoint = transform.position;
        planeRight = transform.right;
        planeUp = transform.up;

        // Extracting size dimensions from the local mesh bounds scales
        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf != null && mf.mesh != null)
        {
            Vector3 meshSize = mf.mesh.bounds.size;
            canvasWorldSize = new Vector2(meshSize.x * transform.localScale.x, meshSize.y * transform.localScale.y);
        }
        else
        {
            canvasWorldSize = new Vector2(transform.localScale.x, transform.localScale.y);
        }
    }

    public void RegisterProjectile(Vector3 origin, Vector3 initialVelocity, float viscosity)
    {
        activeProjectiles.Add(new ProjectileParticle(origin, initialVelocity, viscosity));
    }

    void Update()
    {
        bool textureNeedsApply = false;
        float dt = Time.deltaTime;

        // Performance Guard: Cache analytical geometric variables since the canvas might move/rotate via physics or VR hands
        CachePlaneGeometry();

        for (int i = activeProjectiles.Count - 1; i >= 0; i--)
        {
            ProjectileParticle p = activeProjectiles[i];
            float tCurrent = Time.time - p.creationTime;
            float tNext = tCurrent + dt;

            // Compute current and next steps mathematically
            Vector3 pCurr = p.origin + p.velocity * tCurrent + 0.5f * gravity * (tCurrent * tCurrent);
            Vector3 pNext = p.origin + p.velocity * tNext + 0.5f * gravity * (tNext * tNext);

            // Check analytical intersection line segment between pCurr and pNext against the infinite plane equation
            float denom = Vector3.Dot(pNext - pCurr, planeNormal);
            if (Mathf.Abs(denom) > 0.0001f)
            {
                float tPlane = Vector3.Dot(planePoint - pCurr, planeNormal) / denom;
                
                // If intersection occurs exactly within this frame execution segment
                if (tPlane >= 0f && tPlane <= 1f)
                {
                    Vector3 hitWorldPos = pCurr + tPlane * (pNext - pCurr);
                    Vector3 velocityAtImpact = p.velocity + gravity * (tCurrent + tPlane * dt);

                    if (TryMapWorldToCanvasPixels(hitWorldPos, out int pixelX, out int pixelY))
                    {
                        // Trigger Advanced Particle Splatter Algorithm
                        ApplySplatterToBuffer(pixelX, pixelY, velocityAtImpact, p.viscosity);
                        textureNeedsApply = true;
                    }

                    activeProjectiles.RemoveAt(i);
                    continue;
                }
            }

            // Out of bounds cleanup (safety fallback if particle drops past -50y without hitting anything)
            if (pNext.y < -50f)
            {
                activeProjectiles.RemoveAt(i);
            }
        }

        // Optimization Strategy: Invoke Apply exactly once per frame execution maximum
        if (textureNeedsApply)
        {
            dynamicTexture.SetPixels32(pixelBuffer);
            dynamicTexture.Apply();
        }
    }

    bool TryMapWorldToCanvasPixels(Vector3 worldPos, out int pixelX, out int pixelY)
    {
        pixelX = 0;
        pixelY = 0;

        Vector3 localVector = worldPos - planePoint;

        // Project world coordinates onto the 2D plane coordinate axes systems
        float xWorld = Vector3.Dot(localVector, planeRight);
        float yWorld = Vector3.Dot(localVector, planeUp);

        // Map from localized centered origin to UV layout coordinates [0, 1]
        float u = (xWorld / canvasWorldSize.x) + 0.5f;
        float v = (yWorld / canvasWorldSize.y) + 0.5f;

        if (u >= 0f && u <= 1f && v >= 0f && v <= 1f)
        {
            pixelX = Mathf.FloorToInt(u * (textureWidth - 1));
            pixelY = Mathf.FloorToInt(v * (textureHeight - 1));
            return true;
        }

        return false;
    }

    // --- رابعاً: خوارزمية التلوين وتناثر الرذاذ (Particle Splatter Algorithm) ---
    void ApplySplatterToBuffer(int centerX, int centerY, Vector3 impactVelocity, float viscosity)
    {
        float speed = impactVelocity.magnitude;
        
        // Base dynamic radius bound calculations determined by momentum vs thickness parameters
        float baseRadius = (speed * 2.0f) / (viscosity + 0.5f);
        
        // Surface Porosity Modifiers adjustment values
        float spreadingFactor = 1.0f;
        float roughnessNoise = 0.0f;

        switch (surfaceType)
        {
            case SurfaceType.Metal:
                spreadingFactor = 1.6f; // High splash spread, flat surface
                roughnessNoise = 0.1f;
                break;
            case SurfaceType.Wood:
                spreadingFactor = 1.1f;
                roughnessNoise = 0.4f; // Noticeable grain absorption deviation
                break;
            case SurfaceType.Paper:
                spreadingFactor = 1.0f;
                roughnessNoise = 0.2f;
                break;
            case SurfaceType.Cloth:
                spreadingFactor = 0.7f; // Highly absorbent structural capillary fibers restrict massive splashes
                roughnessNoise = 0.7f; // High dynamic scattering bleeding edges
                break;
        }

        int radius = Mathf.Clamp(Mathf.CeilToInt(baseRadius * spreadingFactor), 2, 24);

        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                int targetX = centerX + x;
                int targetY = centerY + y;

                if (targetX >= 0 && targetX < textureWidth && targetY >= 0 && targetY < textureHeight)
                {
                    float distanceSq = x * x + y * y;
                    float currentRadiusSq = radius * radius;

                    if (distanceSq <= currentRadiusSq)
                    {
                        float distNorm = Mathf.Sqrt(distanceSq) / radius;

                        // Procedural Perlin Distortion Engine to disrupt perfectly round vector shapes
                        float noise = Mathf.PerlinNoise(targetX * 0.15f, targetY * 0.15f) * roughnessNoise;
                        float modifiedDist = distNorm + noise;

                        if (modifiedDist <= 1.0f)
                        {
                            // Gaussian Falloff Equation Modeling
                            float alphaIntensity = Mathf.Exp(-4f * modifiedDist * modifiedDist);
                            
                            // Splatter scattering droplet generation on extreme outer thresholds
                            if (distNorm > 0.6f)
                            {
                                float splashProbability = Random.value * speed;
                                if (splashProbability < 1.8f) continue; // Leaves isolated droplet pixels blank
                            }

                            int index = targetY * textureWidth + targetX;
                            Color32 currentPixel = pixelBuffer[index];

                            // Direct Alpha Blending computation without allocations
                            float blendAlpha = alphaIntensity;
                            pixelBuffer[index].r = (byte)Mathf.Lerp(currentPixel.r, paintColor.r * 255, blendAlpha);
                            pixelBuffer[index].g = (byte)Mathf.Lerp(currentPixel.g, paintColor.g * 255, blendAlpha);
                            pixelBuffer[index].b = (byte)Mathf.Lerp(currentPixel.b, paintColor.b * 255, blendAlpha);
                            pixelBuffer[index].a = (byte)Mathf.Max(currentPixel.a, (byte)(blendAlpha * 255));
                        }
                    }
                }
            }
        }

        // ========================================================
        // التعديل المصيري والإنقاذي للوحة (قمنا بإضافته هنا في نهاية الدالة):
        // ========================================================
        
        // 1. تأكد من استدعاء كائن الإكساء الفعلي لديك (غالبا اسمه dynamicTexture أو m_Texture)
        // إذا كان اسم المتغير مختلفاً لديك في أعلى السكربت، استبدل "dynamicTexture" باسمه الحقيقي.
        if (dynamicTexture != null)
        {
            // تطبيق المصفوفة المحدثة بالكامل على الـ Texture الخاص باللوحة
            dynamicTexture.SetPixels32(pixelBuffer);
            
            // أمر الفرملة ودفع الألوان لكرت الشاشة لتعرض اللوحة الألوان فوراً
            dynamicTexture.Apply();
        }
    }
    // --- دالة استقبال الجسيمات وتحويلها إلى مقذوفات رياضية ---
    // --- دالة استقبال الجسيمات وتحويلها الذكي إلى بكسلات ---
// --- دالة استقبال الجسيمات وتحويلها الذكي إلى بكسلات ---
    public void RegisterIncomingParticle(Vector3 spawnPosition, Vector3 initialVelocity, float particleViscosity)
    {
        Vector3 gravity = new Vector3(0, -9.81f, 0);
        
        // 1. حساب زمن السقوط حتى مستوى ارتفاع سطح اللوحة الحالي تماماً
        float t = 0f;
        float yInitial = spawnPosition.y - transform.position.y; // الحساب بالنسبة لارتفاع اللوحة الحالي
        float vY = initialVelocity.y;
        
        float discriminant = vY * vY - 2f * gravity.y * yInitial;
        if (discriminant >= 0 && gravity.y != 0)
        {
            t = (-vY - Mathf.Sqrt(discriminant)) / gravity.y;
            if (t < 0) t = (-vY + Mathf.Sqrt(discriminant)) / gravity.y;
        }
        else
        {
            t = Mathf.Sqrt(Mathf.Abs(2f * yInitial / 9.81f));
        }

        // 2. حساب نقطة الاصطدام الدقيقة في الفضاء العالمي
        Vector3 impactPos = spawnPosition + initialVelocity * t + 0.5f * gravity * (t * t);

        // 3. السحر الرياضي: تحويل نقطة الاصطدام من الفضاء العالمي إلى الفضاء المحلي للوحة تماماً
        Vector3 localImpact = transform.InverseTransformPoint(impactPos);

        // في الفضاء المحلي لـ Quad أو Plane الافتراضي، السطح يمتد من -0.5 إلى +0.5 في محوري X و Y
        // نقوم بتحويل هذه النسبة من (-0.5 إلى 0.5) لتصبح نسبة مئوية نظيفة من (0 إلى 1)
        float normX = localImpact.x + 0.5f;
        float normY = localImpact.y + 0.5f; // بما أن اللوحة مدورة 90 درجة، المحور المحلي العمودي عليها يصبح Y محلي

        // 4. تحويل النسبة المئوية إلى بكسلات حقيقية داخل الإكساء (512x512)
        int textureWidth = 512;  
        int textureHeight = 512; 

        int pixelX = Mathf.FloorToInt(normX * textureWidth);
        int pixelY = Mathf.FloorToInt(normY * textureHeight);

        // 5. إذا وقع الاصطدام داخل حدود اللوحة المحلية، ارسم فوراً!
        if (pixelX >= 0 && pixelX < textureWidth && pixelY >= 0 && pixelY < textureHeight)
        {
            Vector3 impactVelocityAtSurface = initialVelocity + gravity * t;
            
            // استدعاء دالة الرسم والتشويه رابعاً الموجودة مسبقاً لديك
            ApplySplatterToBuffer(pixelX, pixelY, impactVelocityAtSurface, particleViscosity);
        }
    }
}