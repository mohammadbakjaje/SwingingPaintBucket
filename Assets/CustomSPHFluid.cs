
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// محاكي سائل SPH فائق الأداء - مع فيزياء الأسطح ومظهر المواد الحقيقي.
/// تم حل مشكلة توقف التدفق عند ميلان الدلو، خروج الطلاء من الجوانب، وتجمع القطرات.
/// </summary>
public class CustomSPHFluid : MonoBehaviour
{
    public enum CanvasMaterialType 
    { 
        Cloth, 
        Wood,  
        Metal  
    }

    private class FluidParticle
    {
        public Vector3 position;
        public Vector3 previousPosition; 
        public Vector3 velocity;
        public Vector3 force;
        public float density;
        public float pressure;
        public Transform visualTransform;
        public MeshRenderer meshRenderer; 
        public TrailRenderer trailRenderer; 
        public bool isEmitted; 
    }

    [Header("🎨 Canvas Material Physics & Look")]
    [Tooltip("اختر نوع المادة للوحة")]
    public CanvasMaterialType surfaceMaterial = CanvasMaterialType.Cloth;
    
    [Tooltip("ضع هنا صورة القماش")]
    public Texture2D clothTexture;
    [Tooltip("ضع هنا صورة الخشب")]
    public Texture2D woodTexture;
    [Tooltip("ضع هنا صورة المعدن")]
    public Texture2D metalTexture;

    [Header("Fluid Properties")]
    public int maxParticles = 250;          
    public float smoothingRadius = 0.5f;    
    public float targetDensity = 150f;      
    public float pressureStiffness = 100f;  
    public float viscosity = 0.2f;          
    public Vector3 gravity = new Vector3(0f, -9.81f, 0f);

    [Header("Paint Volume Control")]
    public float totalPaintReserve = 1000f; 
    private float currentPaintReserve;      

    [Header("Bucket Dimensions")]
    public Transform bucketTransform;       
    public float rimYOffset = 0.4f;         
    public float bucketHeight = 1.4f;       
    public float topRadius = 0.5f;          
    public float bottomRadius = 0.35f;      
    public float drainHoleRadius = 0.08f;    

    [Header("Stream Control")]
    public float emissionRate = 60f;        
    public float exitVelocity = 3f;         
    private float emissionAccumulator = 0f; 

    [Header("Floor Painting")]
    public MeshRenderer floorRenderer;      
    public int textureResolution = 1024;    
    public int brushRadius = 7;             

    [Header("Unified Color Synchronization")]
    public Color paintColor = Color.blue;   
    public Material paintMaterial;          

    [Header("Canvas Fixes")]
    public bool invertX = false;
    public bool invertZ = false;

    private List<FluidParticle> particles = new List<FluidParticle>();
    private Vector3 lastBucketPosition; 
    private Texture2D dynamicCanvasTexture; 
    private Color32[] canvasPixels; 
    private Color32 paintColor32;
    private bool isTextureDirty = false;    

    private Material runtimeLiquidMaterial;
    private Material runtimeParticleMaterial;

    private MeshFilter liquidMeshFilter;
    private Mesh liquidMesh;
    private Vector3[] liquidVertices;
    private int meshSegments = 24; 

    private float smoothingRadiusSq;

    void Start()
    {
        if (bucketTransform == null) return;
        
        smoothingRadiusSq = smoothingRadius * smoothingRadius;
        paintColor32 = paintColor;
        currentPaintReserve = totalPaintReserve;
        lastBucketPosition = bucketTransform.position; 

        InitializeCanvas();
        InitializeRuntimeMaterials();
        CreateProceduralLiquidObject();
        SpawnFluidParticles();
    }

    void InitializeCanvas()
    {
        if (floorRenderer == null) return;
        
        dynamicCanvasTexture = new Texture2D(textureResolution, textureResolution, TextureFormat.RGBA32, false);
        int totalPixels = textureResolution * textureResolution;
        canvasPixels = new Color32[totalPixels];
        
        Texture2D selectedBaseTexture = null;
        switch (surfaceMaterial)
        {
            case CanvasMaterialType.Cloth: selectedBaseTexture = clothTexture; break;
            case CanvasMaterialType.Wood: selectedBaseTexture = woodTexture; break;
            case CanvasMaterialType.Metal: selectedBaseTexture = metalTexture; break;
        }

        if (selectedBaseTexture != null)
        {
            for (int y = 0; y < textureResolution; y++)
            {
                for (int x = 0; x < textureResolution; x++)
                {
                    float u = (float)x / textureResolution;
                    float v = (float)y / textureResolution;
                    canvasPixels[y * textureResolution + x] = selectedBaseTexture.GetPixelBilinear(u, v);
                }
            }
        }
        else 
        {
            Color32 defaultColor = new Color32(255, 255, 255, 255);
            for (int i = 0; i < totalPixels; i++) canvasPixels[i] = defaultColor;
        }
        
        dynamicCanvasTexture.SetPixels32(canvasPixels);
        dynamicCanvasTexture.Apply(false);
        floorRenderer.material.mainTexture = dynamicCanvasTexture;
    }

    void InitializeRuntimeMaterials()
    {
        if (paintMaterial != null)
        {
            runtimeLiquidMaterial = new Material(paintMaterial);
            runtimeParticleMaterial = new Material(paintMaterial);
        }
        else
        {
            Shader defaultShader = Shader.Find("Standard");
            runtimeLiquidMaterial = new Material(defaultShader);
            runtimeParticleMaterial = new Material(defaultShader);
        }
        runtimeLiquidMaterial.color = paintColor;
        runtimeParticleMaterial.color = paintColor;
    }

    void CreateProceduralLiquidObject()
    {
        GameObject liquidObj = new GameObject("Procedural_Conical_Liquid");
        liquidObj.transform.SetParent(bucketTransform);
        liquidObj.transform.localPosition = Vector3.zero;
        liquidObj.transform.localRotation = Quaternion.identity;
        liquidObj.transform.localScale = Vector3.one;

        liquidMeshFilter = liquidObj.AddComponent<MeshFilter>();
        MeshRenderer mr = liquidObj.AddComponent<MeshRenderer>();
        mr.sharedMaterial = runtimeLiquidMaterial;

        liquidMesh = new Mesh();
        liquidMesh.MarkDynamic(); 

        int totalVertices = meshSegments * 2 + 2; 
        liquidVertices = new Vector3[totalVertices];
        List<int> triangles = new List<int>();

        int botCenterIdx = 0;
        int topCenterIdx = 1;
        int botStart = 2;
        int topStart = 2 + meshSegments;

        for (int i = 0; i < meshSegments; i++)
        {
            int next = (i + 1) % meshSegments;
            triangles.Add(botCenterIdx);
            triangles.Add(botStart + next);
            triangles.Add(botStart + i);

            triangles.Add(topCenterIdx);
            triangles.Add(topStart + i);
            triangles.Add(topStart + next);

            triangles.Add(botStart + i);
            triangles.Add(topStart + i);
            triangles.Add(botStart + next);

            triangles.Add(botStart + next);
            triangles.Add(topStart + i);
            triangles.Add(topStart + next);
        }

        liquidMesh.vertices = liquidVertices;
        liquidMesh.triangles = triangles.ToArray();
        liquidMeshFilter.mesh = liquidMesh;
    }

    void SpawnFluidParticles()
    {
        for (int i = 0; i < maxParticles; i++)
        {
            FluidParticle p = new FluidParticle();
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "Paint_Drop_" + i;
            
            Collider builtInCollider = sphere.GetComponent<Collider>();
            if (builtInCollider != null) Destroy(builtInCollider);

            sphere.transform.localScale = Vector3.one * 0.10f; 
            p.meshRenderer = sphere.GetComponent<MeshRenderer>();
            p.meshRenderer.sharedMaterial = runtimeParticleMaterial;
            p.visualTransform = sphere.transform;

            p.trailRenderer = sphere.AddComponent<TrailRenderer>();
            p.trailRenderer.time = 0.12f; 
            p.trailRenderer.startWidth = 0.10f;
            p.trailRenderer.endWidth = 0.03f;
            p.trailRenderer.sharedMaterial = runtimeParticleMaterial;
            p.trailRenderer.numCornerVertices = 4;
            p.trailRenderer.numCapVertices = 4;
            p.trailRenderer.enabled = false;
            
            RespawnParticleInsideBucket(p);
            particles.Add(p);
        }
    }

    void RespawnParticleInsideBucket(FluidParticle p)
    {
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float h = Random.Range(-bucketHeight * 0.9f, -rimYOffset - 0.05f);
        
        float totalSubHeight = bucketHeight - rimYOffset;
        float currentYInside = -h - rimYOffset;
        float t = Mathf.Clamp01(currentYInside / totalSubHeight);
        float spawnRadius = Mathf.Lerp(topRadius, bottomRadius, t) * 0.8f;
        
        float r = Random.Range(0f, spawnRadius);
        Vector3 offset = bucketTransform.rotation * new Vector3(Mathf.Cos(angle) * r, h, Mathf.Sin(angle) * r);
        
        p.position = bucketTransform.position + offset;
        p.previousPosition = p.position;
        p.velocity = Vector3.zero;
        p.force = Vector3.zero;
        p.isEmitted = false; 
        
        if (p.meshRenderer != null) p.meshRenderer.enabled = false;
        if (p.trailRenderer != null) { p.trailRenderer.enabled = false; p.trailRenderer.Clear(); }
        if (p.visualTransform != null) p.visualTransform.position = p.position;
    }

    void Update()
    {
        if (isTextureDirty && dynamicCanvasTexture != null)
        {
            dynamicCanvasTexture.SetPixels32(canvasPixels);
            dynamicCanvasTexture.Apply(false);
            isTextureDirty = false;
        }
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        emissionAccumulator += dt;

        float timeBetweenEmissions = 1f / emissionRate;
        int allowedEmissionsThisFrame = Mathf.FloorToInt(emissionAccumulator / timeBetweenEmissions);
        int emittedThisFrame = 0;

        paintColor32 = paintColor;
        if (runtimeLiquidMaterial != null) runtimeLiquidMaterial.color = paintColor;
        if (runtimeParticleMaterial != null) runtimeParticleMaterial.color = paintColor;

        Vector3 bucketVelocity = Vector3.zero;
        if (dt > 0f && bucketTransform != null)
        {
            bucketVelocity = (bucketTransform.position - lastBucketPosition) / dt;
            lastBucketPosition = bucketTransform.position;
        }

        CalculateDensityAndPressure();
        CalculateForces();
        
        UpdateParticlesAndCollisions(dt, bucketVelocity, allowedEmissionsThisFrame, ref emittedThisFrame);
        UpdateProceduralLiquidMesh();

        emissionAccumulator -= emittedThisFrame * timeBetweenEmissions;
        if (emissionAccumulator < 0f) emissionAccumulator = 0f;
    }

    void CalculateDensityAndPressure()
    {
        int count = particles.Count;
        for (int i = 0; i < count; i++)
        {
            FluidParticle p1 = particles[i];
            if (p1.isEmitted) continue;

            p1.density = 0f;
            for (int j = 0; j < count; j++)
            {
                if (particles[j].isEmitted) continue;

                Vector3 diff = p1.position - particles[j].position;
                float distSq = diff.sqrMagnitude; 

                if (distSq < smoothingRadiusSq)
                {
                    float rSq = smoothingRadiusSq - distSq;
                    p1.density += rSq * rSq * rSq; 
                }
            }
            p1.pressure = pressureStiffness * (p1.density - targetDensity);
            if (p1.pressure < 0) p1.pressure = 0;
        }
    }

    void CalculateForces()
    {
        int count = particles.Count;
        for (int i = 0; i < count; i++)
        {
            FluidParticle p1 = particles[i];
            p1.force = gravity;

            if (p1.isEmitted) continue;

            for (int j = 0; j < count; j++)
            {
                if (i == j || particles[j].isEmitted) continue;

                Vector3 diff = p2Position(particles[j].position, p1.position);
                float distSq = diff.sqrMagnitude;

                if (distSq < smoothingRadiusSq && distSq > 0.00001f)
                {
                    float rSq = smoothingRadiusSq - distSq;
                    float pressureForce = (p1.pressure + particles[j].pressure) * rSq;
                    p1.force -= diff * pressureForce * 0.5f;

                    Vector3 relativeVelocity = particles[j].velocity - p1.velocity;
                    p1.force += relativeVelocity * viscosity * rSq;
                }
            }
        }
    }

    Vector3 p2Position(Vector3 p2, Vector3 p1) { return p2 - p1; }

    void UpdateParticlesAndCollisions(float dt, Vector3 bucketVelocity, int allowedEmissions, ref int emittedCount)
    {
        Vector3 bPos = bucketTransform.position;
        Quaternion bRot = bucketTransform.rotation;

        for (int i = particles.Count - 1; i >= 0; i--)
        {
            FluidParticle p = particles[i];
            p.previousPosition = p.position;

            p.velocity += p.force * dt;
            p.position += p.velocity * dt;

            if (floorRenderer != null && p.position.y <= floorRenderer.transform.position.y)
            {
                if (p.isEmitted) PaintLineOnCanvas(p.previousPosition, p.position, p.velocity.magnitude);

                if (currentPaintReserve > 0f)
                {
                    RespawnParticleInsideBucket(p);
                    continue; 
                }
                else
                {
                    if (p.visualTransform != null) Destroy(p.visualTransform.gameObject);
                    particles.RemoveAt(i);
                    continue; 
                }
            }

            Vector3 localPos = Quaternion.Inverse(bRot) * (p.position - bPos);

            if (!p.isEmitted)
            {
                if (currentPaintReserve <= 0f)
                {
                    if (p.visualTransform != null) Destroy(p.visualTransform.gameObject);
                    particles.RemoveAt(i);
                    continue;
                }

                if (localPos.y > -rimYOffset) localPos.y = -rimYOffset;

                float totalSubHeight = bucketHeight - rimYOffset;
                float currentYInsideBucket = -localPos.y - rimYOffset; 
                float t = Mathf.Clamp01(currentYInsideBucket / totalSubHeight);
                float currentRadius = Mathf.Lerp(topRadius, bottomRadius, t);

                Vector2 horizontalPos = new Vector2(localPos.x, localPos.z);
                float distFromCenter = horizontalPos.magnitude;

                if (localPos.y < -bucketHeight * 0.4f)
                {
                    Vector3 pullToCenter = new Vector3(-localPos.x, 0f, -localPos.z) * 12f;
                    p.velocity += bRot * pullToCenter * dt;
                }

                if (distFromCenter > currentRadius)
                {
                    horizontalPos = horizontalPos.normalized * currentRadius;
                    localPos.x = horizontalPos.x;
                    localPos.z = horizontalPos.y;
                    
                    Vector3 localVel = Quaternion.Inverse(bRot) * p.velocity;
                    localVel.x *= -0.1f; 
                    localVel.z *= -0.1f;
                    p.velocity = bRot * localVel;
                }

                // --- التعديل الجوهري للقطرات ---
                // التأكد من أن القطرات تخرج فقط من الثقب وباتجاه ميلان الدلو
                if (currentPaintReserve > 0f && emittedCount < allowedEmissions)
                {
                    p.isEmitted = true;
                    if (p.meshRenderer != null) p.meshRenderer.enabled = true;

                    // إزاحة القطرة قليلاً للأسفل (-0.05) لتجنب التداخل مع مجسم الدلو
                    localPos.x = Random.Range(-drainHoleRadius, drainHoleRadius) * 0.5f;
                    localPos.z = Random.Range(-drainHoleRadius, drainHoleRadius) * 0.5f;
                    localPos.y = -bucketHeight - 0.05f; 

                    // استخدام (-bucketTransform.up) لضمان الخروج مع اتجاه الثقب بدلاً من الأسفل المطلق للعالم
                    Vector3 worldExitDirection = -bucketTransform.up; 
                    p.velocity = (worldExitDirection * exitVelocity) + bucketVelocity;

                    currentPaintReserve -= 1f;
                    emittedCount++; 

                    // تحديث المكان فوراً لكي لا يقوم الـ Trail برسم خط عشوائي
                    p.position = (bRot * localPos) + bPos;
                    if (p.visualTransform != null) p.visualTransform.position = p.position;

                    if (p.trailRenderer != null) { 
                        p.trailRenderer.enabled = true; 
                        p.trailRenderer.Clear(); 
                    }
                }
                else
                {
                    float bottomThreshold = -bucketHeight + 0.02f;
                    if (localPos.y <= bottomThreshold)
                    {
                        localPos.y = bottomThreshold;
                        Vector3 localVel = Quaternion.Inverse(bRot) * p.velocity;
                        localVel.y *= -0.05f; 
                        p.velocity = bRot * localVel;
                    }
                }
            }
            else
            {
                if (p.meshRenderer != null && !p.meshRenderer.enabled) p.meshRenderer.enabled = true;
                if (p.trailRenderer != null && !p.trailRenderer.enabled) p.trailRenderer.enabled = true;
            }

            p.position = (bRot * localPos) + bPos;
            p.visualTransform.position = p.position;
        }
    }

    void UpdateProceduralLiquidMesh()
    {
        if (liquidMeshFilter == null || totalPaintReserve <= 0f) return;

        float remainingRatio = Mathf.Clamp01(currentPaintReserve / totalPaintReserve);
        
        if (remainingRatio <= 0 && particles.Count == 0)
        {
            liquidMeshFilter.gameObject.SetActive(false);
            return;
        }
        else
        {
            if (!liquidMeshFilter.gameObject.activeSelf) liquidMeshFilter.gameObject.SetActive(true);
        }

        float totalHeight = bucketHeight - rimYOffset;
        float currentHeight = totalHeight * remainingRatio;
        float currentTopRadius = Mathf.Lerp(bottomRadius, topRadius, remainingRatio);

        liquidVertices[0] = new Vector3(0, -bucketHeight, 0); 
        liquidVertices[1] = new Vector3(0, -bucketHeight + currentHeight, 0); 

        int botStart = 2;
        int topStart = 2 + meshSegments;

        for (int i = 0; i < meshSegments; i++)
        {
            float angle = i * Mathf.PI * 2 / meshSegments;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            liquidVertices[botStart + i] = new Vector3(cos * bottomRadius, -bucketHeight, sin * bottomRadius);
            liquidVertices[topStart + i] = new Vector3(cos * currentTopRadius, -bucketHeight + currentHeight, sin * currentTopRadius);
        }

        liquidMesh.vertices = liquidVertices;
        liquidMesh.RecalculateNormals();
        liquidMesh.RecalculateBounds();
    }

    void PaintLineOnCanvas(Vector3 startWorldPos, Vector3 endWorldPos, float impactSpeed)
    {
        if (floorRenderer == null || canvasPixels == null) return;

        Vector3 localStart = floorRenderer.transform.InverseTransformPoint(startWorldPos);
        Vector3 localEnd = floorRenderer.transform.InverseTransformPoint(endWorldPos);

        float u1 = (localStart.x + 5f) / 10f;
        float v1 = (localStart.z + 5f) / 10f;
        float u2 = (localEnd.x + 5f) / 10f;
        float v2 = (localEnd.z + 5f) / 10f;

        if (invertX) { u1 = 1f - u1; u2 = 1f - u2; }
        if (invertZ) { v1 = 1f - v1; v2 = 1f - v2; }

        int x1 = (int)(u1 * textureResolution);
        int y1 = (int)(v1 * textureResolution);
        int x2 = (int)(u2 * textureResolution);
        int y2 = (int)(v2 * textureResolution);

        float pixelDist = Vector2.Distance(new Vector2(x1, y1), new Vector2(x2, y2));
        int steps = Mathf.Max(1, Mathf.RoundToInt(pixelDist / (brushRadius * 0.4f)));

        float spreadMultiplier = 1f;
        float splatterChance = 0f;

        switch (surfaceMaterial)
        {
            case CanvasMaterialType.Cloth:
                spreadMultiplier = 0.6f; 
                splatterChance = 0.1f;   
                break;
            case CanvasMaterialType.Wood:
                spreadMultiplier = 1.0f; 
                splatterChance = 0.4f;   
                break;
            case CanvasMaterialType.Metal:
                spreadMultiplier = 1.7f; 
                splatterChance = 0.8f;   
                break;
        }

        float speedFactor = Mathf.Clamp(impactSpeed * 0.15f, 0.7f, 2.2f);
        int dynamicRadius = Mathf.RoundToInt(brushRadius * speedFactor * spreadMultiplier);
        if (dynamicRadius < 2) dynamicRadius = 2;

        for (int i = 0; i <= steps; i++)
        {
            float lerpRatio = (float)i / steps;
            int centerX = Mathf.RoundToInt(Mathf.Lerp(x1, x2, lerpRatio));
            int centerY = Mathf.RoundToInt(Mathf.Lerp(y1, y2, lerpRatio));

            ApplyBrushStamp(centerX, centerY, dynamicRadius, splatterChance);
        }

        isTextureDirty = true;
    }

    void ApplyBrushStamp(int centerX, int centerY, int radius, float splatterChance)
    {
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                int distSq = x * x + y * y;
                int radiusSq = radius * radius;

                if (distSq <= radiusSq)
                {
                    if (distSq / (float)radiusSq > 0.65f)
                    {
                        if (Random.value < splatterChance) continue; 
                    }

                    int px = centerX + x;
                    int py = centerY + y;
                    if (px >= 0 && px < textureResolution && py >= 0 && py < textureResolution)
                    {
                        canvasPixels[py * textureResolution + px] = paintColor32;
                    }
                }
            }
        }
    }

    void OnDrawGizmos()
    {
        if (bucketTransform == null) return;
        Gizmos.color = Color.green;
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = bucketTransform.localToWorldMatrix;
        DrawWireCircle(new Vector3(0, -rimYOffset, 0), topRadius);
        DrawWireCircle(new Vector3(0, -bucketHeight, 0), bottomRadius);
        Gizmos.color = Color.red;
        DrawWireCircle(new Vector3(0, -bucketHeight, 0), drainHoleRadius);
        Gizmos.color = Color.green;
        int segments = 8;
        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2 / segments;
            Vector3 topPt = new Vector3(Mathf.Cos(angle) * topRadius, -rimYOffset, Mathf.Sin(angle) * topRadius);
            Vector3 botPt = new Vector3(Mathf.Cos(angle) * bottomRadius, -bucketHeight, Mathf.Sin(angle) * bottomRadius);
            Gizmos.DrawLine(topPt, botPt);
        }
        Gizmos.matrix = oldMatrix;
    }

    void DrawWireCircle(Vector3 center, float radius)
    {
        int segments = 20;
        Vector3 lastPt = center + new Vector3(radius, 0, 0);
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2 / segments;
            Vector3 nextPt = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(lastPt, nextPt);
            lastPt = nextPt;
        }
    }
}

