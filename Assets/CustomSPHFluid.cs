
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
        public Color color;
        public float colorWeight;
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
    public Transform drainPoint;            
    public float rimYOffset = 0.4f;         
    public float bucketHeight = 1.4f;       
    public float topRadius = 0.5f;          
    public float bottomRadius = 0.35f;      
    public float drainHoleRadius = 0.08f;   
    public bool debugDrainDirection = false;
    public bool debugEmitConditions = false;

    [Header("Stream Control")]
    public float emissionRate = 120f;        
    public float exitVelocity = 3f;         
    private float emissionAccumulator = 0f; 
    [Tooltip("Spacing between consecutively emitted particles along the exit direction (meters)")]
    public float emissionSpacing = 0.00025f;
    [Tooltip("Distance threshold for extra drain-position emissions (meters)")]
    public float drainDistanceEmissionThreshold = 0.05f;

    [Header("Floor Painting")]
    public MeshRenderer floorRenderer;      
    public int textureResolution = 1024;    
    public int brushRadius = 7;             

    [Header("Unified Color Synchronization")]
    public Color paintColor = Color.blue;
    public Color[] paintColors = new Color[] { Color.blue };
    [Tooltip("كم لون تريد أن تختار من القائمة")]
    public int paintColorCount = 1;
    [Tooltip("اختر هل يتم اختيار لون الجسيم بشكل عشوائي من الألوان المتاحة")]
    public bool randomizePaintColors = true;
    public Material paintMaterial;
    public float particleBlendStrength = 0.05f;

    [Header("Canvas Fixes")]
    public bool invertX = false;
    public bool invertZ = false;

    private List<FluidParticle> particles = new List<FluidParticle>();
    private Vector3 lastBucketPosition;
    private Vector3 lastDrainPosition;
    private Texture2D dynamicCanvasTexture; 
    private MaterialPropertyBlock particlePropertyBlock;
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
        if (paintColors == null || paintColors.Length == 0)
        {
            paintColors = new Color[] { paintColor };
        }
        paintColorCount = Mathf.Clamp(paintColorCount, 1, paintColors.Length);
        paintColor32 = paintColor;
        currentPaintReserve = totalPaintReserve;
        lastBucketPosition = bucketTransform.position;
        lastDrainPosition = drainPoint != null ? drainPoint.position : bucketTransform.TransformPoint(new Vector3(0f, -bucketHeight, 0f));
        particlePropertyBlock = new MaterialPropertyBlock();

        InitializeCanvas();
        InitializeRuntimeMaterials();
        CreateProceduralLiquidObject();
        SpawnFluidParticles();
    }

    void OnValidate()
    {
        rimYOffset = Mathf.Max(0f, rimYOffset);
        bucketHeight = Mathf.Max(rimYOffset + 0.01f, bucketHeight);
        drainHoleRadius = Mathf.Max(0f, drainHoleRadius);

        if (paintColors == null || paintColors.Length == 0)
        {
            paintColors = new Color[] { paintColor };
        }
        paintColorCount = Mathf.Clamp(paintColorCount, 1, Mathf.Max(1, paintColors.Length));
    }

    Color GetPaintColor()
    {
        if (paintColors == null || paintColors.Length == 0)
            return paintColor;

        int count = Mathf.Clamp(paintColorCount, 1, paintColors.Length);
        if (randomizePaintColors && count > 1)
            return paintColors[Random.Range(0, count)];

        return paintColors[0];
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
        p.color = GetPaintColor();
        p.colorWeight = 1f;
        
        if (p.meshRenderer != null)
        {
            p.meshRenderer.enabled = false;
            UpdateParticleVisualColor(p);
        }
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

        if (debugDrainDirection && bucketTransform != null)
        {
            Vector3 debugStart = drainPoint != null ? drainPoint.position : bucketTransform.position;
            Vector3 debugDir = drainPoint != null ? drainPoint.forward : -bucketTransform.up;
            Debug.DrawRay(debugStart, debugDir * 2f, Color.red, 0f, false);
        }

        CalculateDensityAndPressure();
        CalculateForces();
        
        UpdateParticlesAndCollisions(dt, bucketVelocity, allowedEmissionsThisFrame, ref emittedThisFrame);
        UpdateProceduralLiquidMesh();

        emissionAccumulator -= emittedThisFrame * timeBetweenEmissions;
        if (emissionAccumulator < 0f) emissionAccumulator = 0f;

        RefreshParticleVisuals();
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

                    if (distSq < smoothingRadiusSq * 0.25f)
                    {
                        float dist = Mathf.Sqrt(distSq);
                        float influence = Mathf.Clamp01(1f - (dist / (smoothingRadius * 0.5f)));
                        float blendAmount = Mathf.Clamp01(influence * 0.08f);
                        if (blendAmount > 0f)
                        {
                            float totalColorWeight = Mathf.Max(0.0001f, p1.colorWeight + particles[j].colorWeight);
                            Color neighborMix = Color.Lerp(p1.color, particles[j].color, particles[j].colorWeight / totalColorWeight);
                            Color inverseMix = Color.Lerp(particles[j].color, p1.color, p1.colorWeight / totalColorWeight);
                            p1.color = Color.Lerp(p1.color, neighborMix, blendAmount);
                            particles[j].color = Color.Lerp(particles[j].color, inverseMix, blendAmount);
                            float avgWeight = Mathf.Lerp(p1.colorWeight, particles[j].colorWeight, 0.5f);
                            p1.colorWeight = avgWeight;
                            particles[j].colorWeight = avgWeight;
                        }
                    }
                }
            }
        }
    }

    Vector3 p2Position(Vector3 p2, Vector3 p1) { return p2 - p1; }

    void UpdateParticlesAndCollisions(float dt, Vector3 bucketVelocity, int allowedEmissions, ref int emittedCount)
    {
        Vector3 bPos = bucketTransform.position;
        Quaternion bRot = bucketTransform.rotation;
        Vector3 currentDrainPosition = drainPoint != null ? drainPoint.position : bucketTransform.TransformPoint(new Vector3(0f, -bucketHeight, 0f));
        float distanceMoved = Vector3.Distance(currentDrainPosition, lastDrainPosition);
        int extraIntermediateEmissions = 0;
        if (distanceMoved > drainDistanceEmissionThreshold && drainDistanceEmissionThreshold > 0f)
        {
            extraIntermediateEmissions = Mathf.FloorToInt(distanceMoved / drainDistanceEmissionThreshold);
        }

        Vector3 drainLocalPosition = Vector3.zero;
        if (drainPoint != null)
            drainLocalPosition = Quaternion.Inverse(bRot) * (drainPoint.position - bPos);

        List<FluidParticle> eligibleParticles = new List<FluidParticle>();
        for (int i = particles.Count - 1; i >= 0; i--)
        {
            FluidParticle p = particles[i];
            p.previousPosition = p.position;

            p.velocity += p.force * dt;
            p.position += p.velocity * dt;

            if (floorRenderer != null && p.position.y <= floorRenderer.transform.position.y)
            {
                if (p.isEmitted) PaintLineOnCanvas(p.previousPosition, p.position, p.velocity.magnitude, p.color, p.colorWeight);

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

                Vector3 worldDrainPos = drainPoint != null
                    ? drainPoint.position
                    : bucketTransform.TransformPoint(new Vector3(0f, -bucketHeight, 0f));
                Vector3 drainLocalPos = bucketTransform.InverseTransformPoint(worldDrainPos);
                float rimLocalY = drainLocalPos.y + (bucketHeight - rimYOffset);
                float bottomLocalY = drainLocalPos.y;

                bool wasAboveRim = localPos.y > rimLocalY;
                if (wasAboveRim) localPos.y = rimLocalY;

                float totalSubHeight = bucketHeight - rimYOffset;
                float currentYInsideBucket = rimLocalY - localPos.y;
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
                // التأكد من أن القطرات تخرج فقط من الثقب وباتجاه الميلان الصحيح
                Vector3 worldDrainPointPos = drainPoint != null ? drainPoint.position : bucketTransform.TransformPoint(new Vector3(0f, -bucketHeight, 0f));
                Vector3 drainPointLocalPos = bucketTransform.InverseTransformPoint(worldDrainPointPos);
                bool canEmitSpatial = drainPoint != null;
                bool canEmitFromDrain = currentPaintReserve > 0f && !wasAboveRim && canEmitSpatial;

                if (drainPoint != null)
                {
                    Vector2 localParticleXZ = new Vector2(localPos.x, localPos.z);
                    Vector2 localDrainXZ = new Vector2(drainPointLocalPos.x, drainPointLocalPos.z);
                    float distToDrain = Vector2.Distance(localParticleXZ, localDrainXZ);
                    bool insideRadius = distToDrain <= drainHoleRadius * 4f; // relax horizontal tolerance
                    bool atDrainHeight = Mathf.Abs(localPos.y - drainPointLocalPos.y) <= 0.35f; // relax vertical tolerance
                    canEmitFromDrain &= insideRadius && atDrainHeight;

                    // if particle is horizontally within drain area but not yet at drain height,
                    // apply a small pull toward the drain so it can reach the emission zone
                    if (insideRadius && !atDrainHeight)
                    {
                        Vector3 pullDir = (worldDrainPointPos - p.position).normalized;
                        float pullStrength = 2.0f; // tuned gentle pull
                        p.velocity += pullDir * pullStrength * dt;
                        if (debugEmitConditions && i == 0 && Time.frameCount % 40 == 0)
                        {
                            Debug.Log($"[Drain Pull] applied pull towards drain, distToDrain={distToDrain:F3}, heightDiff={localPos.y - drainPointLocalPos.y:F3}");
                        }
                    }

                    if (debugEmitConditions && i == 0 && Time.frameCount % 40 == 0)
                    {
                        Debug.Log($"[Drain Debug] localPos={localPos}, drainLocalPos={drainLocalPos}, distToDrain={distToDrain:F3}, insideRadius={insideRadius}, atDrainHeight={atDrainHeight}, wasAboveRim={wasAboveRim}, canEmit={canEmitFromDrain}");
                    }
                }

                if (canEmitFromDrain)
                {
                    // Collect eligible particle for randomized emission after the update pass
                    eligibleParticles.Add(p);
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

            if (!p.isEmitted)
            {
                p.position = (bRot * localPos) + bPos;
            }
            p.visualTransform.position = p.position;
        }

        // --- Emit a randomized selection of eligible particles up to the allowed emissions this frame ---
        int remainingToEmit = Mathf.Max(0, allowedEmissions - emittedCount);
        int emitBudget = remainingToEmit + extraIntermediateEmissions;
        if (eligibleParticles.Count > 0 && emitBudget > 0)
        {
            // If not enough strictly-eligible particles, widen the search to nearby non-emitted particles
            if (eligibleParticles.Count < emitBudget)
            {
                Vector3 worldDrainPointPosFill = drainPoint != null ? drainPoint.position : bucketTransform.TransformPoint(new Vector3(0f, -bucketHeight, 0f));
                Vector3 drainLocalPosFill = bucketTransform.InverseTransformPoint(worldDrainPointPosFill);
                for (int pi = 0; pi < particles.Count && eligibleParticles.Count < remainingToEmit; pi++)
                {
                    var cand = particles[pi];
                    if (cand == null || cand.isEmitted) continue;
                    if (eligibleParticles.Contains(cand)) continue;
                    Vector3 candLocal = bucketTransform.InverseTransformPoint(cand.position);
                    float dist = Vector2.Distance(new Vector2(candLocal.x, candLocal.z), new Vector2(drainLocalPosFill.x, drainLocalPosFill.z));
                    if (dist <= drainHoleRadius * 6f && candLocal.y <= drainLocalPosFill.y + 0.5f)
                    {
                        eligibleParticles.Add(cand);
                    }
                }
            }
            // shuffle eligible list (Fisher-Yates)
            for (int s = 0; s < eligibleParticles.Count; s++)
            {
                int k = Random.Range(s, eligibleParticles.Count);
                var tmp = eligibleParticles[s];
                eligibleParticles[s] = eligibleParticles[k];
                eligibleParticles[k] = tmp;
            }

            int emits = Mathf.Min(emitBudget, eligibleParticles.Count);
            for (int e = 0; e < emits; e++)
            {
                FluidParticle p = eligibleParticles[e];
                if (p == null || p.isEmitted || currentPaintReserve <= 0f) continue;

                Vector3 worldDrainPos = currentDrainPosition;
                if (extraIntermediateEmissions > 0 && e < extraIntermediateEmissions)
                {
                    worldDrainPos = Vector3.Lerp(lastDrainPosition, currentDrainPosition, (float)(e + 1) / extraIntermediateEmissions);
                }

                p.isEmitted = true;
                if (p.meshRenderer != null) p.meshRenderer.enabled = true;

                Vector3 worldExitDirection = Vector3.down;
                if (drainPoint != null)
                {
                    Vector3 candidateForward = drainPoint.TransformDirection(Vector3.forward).normalized;
                    Vector3 candidateUp = drainPoint.TransformDirection(Vector3.up).normalized;
                    Vector3 candidateDown = drainPoint.TransformDirection(Vector3.down).normalized;

                    if (Vector3.Dot(candidateForward, Vector3.down) > 0.5f)
                        worldExitDirection = candidateForward;
                    else if (Vector3.Dot(candidateDown, Vector3.down) > 0.5f)
                        worldExitDirection = candidateDown;
                    else if (Vector3.Dot(candidateUp, Vector3.down) > 0.5f)
                        worldExitDirection = candidateUp;
                    else
                        worldExitDirection = -bucketTransform.up;
                }

                Vector3 tangent = Vector3.Cross(worldExitDirection, Vector3.up);
                if (tangent.sqrMagnitude < 0.001f)
                    tangent = Vector3.Cross(worldExitDirection, bucketTransform.right);
                tangent.Normalize();
                Vector3 bitangent = Vector3.Cross(worldExitDirection, tangent).normalized;

                float u = Random.value;
                float r = drainHoleRadius * 0.5f * Mathf.Sqrt(u);
                float theta = Random.value * Mathf.PI * 2f;
                Vector3 randomPlaneOffset = tangent * (Mathf.Cos(theta) * r) + bitangent * (Mathf.Sin(theta) * r);

                p.velocity = (worldExitDirection * exitVelocity) + bucketVelocity;
                currentPaintReserve -= 1f;
                emittedCount++;
                float spacing = emissionSpacing;
                p.position = worldDrainPos + randomPlaneOffset + worldExitDirection * (0.05f + e * spacing);

                if (p.visualTransform != null) p.visualTransform.position = p.position;
                if (p.trailRenderer != null) { p.trailRenderer.enabled = true; p.trailRenderer.Clear(); }
            }
        }

        lastDrainPosition = currentDrainPosition;
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

    void PaintLineOnCanvas(Vector3 startWorldPos, Vector3 endWorldPos, float impactSpeed, Color particleColor, float colorWeight)
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

        float intensity = Mathf.Clamp01(colorWeight * particleBlendStrength + impactSpeed * 0.03f);

        for (int i = 0; i <= steps; i++)
        {
            float lerpRatio = (float)i / steps;
            int centerX = Mathf.RoundToInt(Mathf.Lerp(x1, x2, lerpRatio));
            int centerY = Mathf.RoundToInt(Mathf.Lerp(y1, y2, lerpRatio));

            ApplyBrushStamp(centerX, centerY, dynamicRadius, splatterChance, particleColor, intensity);
        }

        isTextureDirty = true;
    }

    void ApplyBrushStamp(int centerX, int centerY, int radius, float splatterChance, Color brushColor, float intensity)
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
                        int pixelIndex = py * textureResolution + px;
                        Color existingColor = canvasPixels[pixelIndex];
                        Color blendedColor = Color.Lerp(existingColor, brushColor, intensity);
                        canvasPixels[pixelIndex] = blendedColor;
                    }
                }
            }
        }
    }

    void UpdateParticleVisualColor(FluidParticle p)
    {
        if (p.meshRenderer == null) return;
        particlePropertyBlock.Clear();
        particlePropertyBlock.SetColor("_Color", p.color);
        p.meshRenderer.SetPropertyBlock(particlePropertyBlock);
        if (p.trailRenderer != null)
        {
            p.trailRenderer.material.color = p.color;
        }
    }

    void RefreshParticleVisuals()
    {
        for (int i = 0; i < particles.Count; i++)
        {
            if (particles[i].meshRenderer != null)
            {
                UpdateParticleVisualColor(particles[i]);
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

        if (drainPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(drainPoint.position, 0.03f);
            Gizmos.DrawLine(drainPoint.position, drainPoint.position + drainPoint.forward * 0.3f);
        }
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

