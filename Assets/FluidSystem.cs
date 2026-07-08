using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Standalone SPH fluid simulation for the 'bakjaje' scene.
///
/// Self-contained and decoupled — no painting, canvas, or drain logic. Particles are
/// simulated with Smoothed Particle Hydrodynamics under real world-down gravity and kept
/// strictly inside the volume of a cylinder (Bucket_Parent). Because the containment reads
/// the cylinder's own transform, scaling or rotating Bucket_Parent automatically scales and
/// tilts the physics volume, producing sloshing when you rotate it at runtime.
///
/// Setup: put this component anywhere in the scene and drag 'Bucket_Parent' (the transparent
/// cylinder) into the 'Container' field. Hit Play.
/// </summary>
public class FluidSystem : MonoBehaviour
{
    [Header("Container (Bucket_Parent)")]
    [Tooltip("The transparent cylinder. Its transform defines the fluid volume dynamically.")]
    public Transform container;
    [Tooltip("Radius of the container mesh in LOCAL space (Unity's default cylinder = 0.5).")]
    public float localRadius = 0.5f;
    [Tooltip("Half-height of the container mesh in LOCAL space (Unity's default cylinder = 1.0).")]
    public float localHalfHeight = 1.0f;

    [Header("Fluid Particles")]
    public int particleCount = 220;
    [Tooltip("Visual radius of each rendered paint droplet (world units).")]
    public float particleVisualRadius = 0.06f;

    [Header("SPH Parameters")]
    public float smoothingRadius = 0.25f;
    public float restDensity = 30f;
    public float pressureStiffness = 60f;
    public float nearPressureStiffness = 120f;
    public float viscosity = 0.06f;
    public float particleMass = 1f;

    [Header("Gravity & Movement")]
    [Tooltip("World-space gravity. Stays world-down so rotating the container makes fluid slosh.")]
    public Vector3 gravity = new Vector3(0f, -9.81f, 0f);
    [Tooltip("Fraction of velocity kept when a particle bounces off the container wall.")]
    [Range(0f, 1f)] public float wallBounciness = 0.15f;
    [Tooltip("Extra sub-steps per FixedUpdate for a more stable, less jittery fluid.")]
    [Range(1, 4)] public int subSteps = 2;

    [Header("Rendering")]
    public Color paintColor = new Color(0.15f, 0.45f, 0.95f, 1f);
    [Range(0f, 1f)] public float glossiness = 0.85f;
    [Range(0f, 1f)] public float metallic = 0.1f;

    // --- Internal state ---
    private class Particle
    {
        public Vector3 position;
        public Vector3 velocity;
        public float density;
        public float nearDensity;
        public float pressure;
        public float nearPressure;
        public Transform visual;
    }

    private readonly List<Particle> particles = new List<Particle>();
    private Material runtimeMaterial;
    private float smoothingRadiusSq;
    private Transform particleRoot;

    void Start()
    {
        if (container == null)
        {
            Debug.LogWarning("[FluidSystem] No container assigned — assign Bucket_Parent in the Inspector.");
            enabled = false;
            return;
        }

        smoothingRadiusSq = smoothingRadius * smoothingRadius;
        CreateRuntimeMaterial();
        SpawnParticles();
    }

    void CreateRuntimeMaterial()
    {
        // Try common pipelines so the paint looks right regardless of render pipeline.
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        runtimeMaterial = new Material(shader);
        runtimeMaterial.color = paintColor;
        if (runtimeMaterial.HasProperty("_BaseColor")) runtimeMaterial.SetColor("_BaseColor", paintColor);
        if (runtimeMaterial.HasProperty("_Glossiness")) runtimeMaterial.SetFloat("_Glossiness", glossiness);
        if (runtimeMaterial.HasProperty("_Smoothness")) runtimeMaterial.SetFloat("_Smoothness", glossiness);
        if (runtimeMaterial.HasProperty("_Metallic")) runtimeMaterial.SetFloat("_Metallic", metallic);
    }

    void SpawnParticles()
    {
        particleRoot = new GameObject("FluidParticles").transform;
        particleRoot.SetParent(transform, false);

        for (int i = 0; i < particleCount; i++)
        {
            Particle p = new Particle();

            // Random point inside the cylinder's local volume, then to world space.
            float angle = Random.value * Mathf.PI * 2f;
            float r = localRadius * Mathf.Sqrt(Random.value) * 0.9f;
            float y = Random.Range(-localHalfHeight * 0.9f, localHalfHeight * 0.9f);
            Vector3 local = new Vector3(Mathf.Cos(angle) * r, y, Mathf.Sin(angle) * r);
            p.position = container.TransformPoint(local);
            p.velocity = Vector3.zero;

            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "Droplet_" + i;
            Collider col = sphere.GetComponent<Collider>();
            if (col != null) Destroy(col);
            sphere.transform.SetParent(particleRoot, false);
            sphere.transform.localScale = Vector3.one * (particleVisualRadius * 2f);
            sphere.transform.position = p.position;
            sphere.GetComponent<MeshRenderer>().sharedMaterial = runtimeMaterial;
            p.visual = sphere.transform;

            particles.Add(p);
        }
    }

    void FixedUpdate()
    {
        if (particles.Count == 0) return;

        float dt = Time.fixedDeltaTime / Mathf.Max(1, subSteps);
        for (int s = 0; s < subSteps; s++)
        {
            ComputeDensityPressure();
            ComputeAndApplyForces(dt);
            Integrate(dt);
        }

        // Push final positions to the rendered spheres once per frame.
        for (int i = 0; i < particles.Count; i++)
            particles[i].visual.position = particles[i].position;
    }

    void ComputeDensityPressure()
    {
        int count = particles.Count;
        for (int i = 0; i < count; i++)
        {
            Particle pi = particles[i];
            pi.density = 0f;
            pi.nearDensity = 0f;

            for (int j = 0; j < count; j++)
            {
                Vector3 diff = pi.position - particles[j].position;
                float distSq = diff.sqrMagnitude;
                if (distSq < smoothingRadiusSq)
                {
                    float q = 1f - Mathf.Sqrt(distSq) / smoothingRadius; // 0..1 falloff
                    pi.density += particleMass * q * q;
                    pi.nearDensity += particleMass * q * q * q;
                }
            }

            pi.pressure = pressureStiffness * (pi.density - restDensity);
            pi.nearPressure = nearPressureStiffness * pi.nearDensity;
        }
    }

    void ComputeAndApplyForces(float dt)
    {
        int count = particles.Count;
        for (int i = 0; i < count; i++)
        {
            Particle pi = particles[i];
            Vector3 force = gravity * particleMass; // world-down gravity

            for (int j = 0; j < count; j++)
            {
                if (i == j) continue;
                Particle pj = particles[j];

                Vector3 diff = pi.position - pj.position;
                float distSq = diff.sqrMagnitude;
                if (distSq >= smoothingRadiusSq || distSq < 1e-8f) continue;

                float dist = Mathf.Sqrt(distSq);
                float q = 1f - dist / smoothingRadius;
                Vector3 dir = diff / dist;

                // Pressure + near-pressure repulsion (keeps the fluid from collapsing).
                float pressureTerm = (pi.pressure + pj.pressure) * 0.5f * q
                                   + (pi.nearPressure + pj.nearPressure) * 0.5f * q * q;
                force += dir * pressureTerm;

                // Viscosity — smooths relative motion.
                Vector3 relVel = pj.velocity - pi.velocity;
                force += relVel * viscosity * q;
            }

            pi.velocity += force / Mathf.Max(0.0001f, pi.density > 0f ? pi.density : particleMass) * dt;
        }
    }

    void Integrate(float dt)
    {
        for (int i = 0; i < particles.Count; i++)
        {
            Particle p = particles[i];
            p.position += p.velocity * dt;
            ContainInCylinder(p);
        }
    }

    /// <summary>
    /// Clamps a particle into the container's cylindrical volume. InverseTransformPoint /
    /// TransformPoint bake in the container's scale, rotation and position, so the volume
    /// tracks the cylinder automatically when you resize or rotate Bucket_Parent.
    /// </summary>
    void ContainInCylinder(Particle p)
    {
        Vector3 local = container.InverseTransformPoint(p.position);

        // Side wall (radial clamp).
        Vector2 xz = new Vector2(local.x, local.z);
        if (xz.magnitude > localRadius)
        {
            xz = xz.normalized * localRadius;
            local.x = xz.x;
            local.z = xz.y;
            Vector3 lv = container.InverseTransformDirection(p.velocity);
            lv.x *= -wallBounciness;
            lv.z *= -wallBounciness;
            p.velocity = container.TransformDirection(lv);
        }

        // Top / bottom caps.
        if (local.y < -localHalfHeight || local.y > localHalfHeight)
        {
            local.y = Mathf.Clamp(local.y, -localHalfHeight, localHalfHeight);
            Vector3 lv = container.InverseTransformDirection(p.velocity);
            lv.y *= -wallBounciness;
            p.velocity = container.TransformDirection(lv);
        }

        p.position = container.TransformPoint(local);
    }

    void OnDrawGizmosSelected()
    {
        if (container == null) return;
        Gizmos.color = new Color(0.2f, 0.7f, 1f, 0.6f);
        Gizmos.matrix = container.localToWorldMatrix;

        int segments = 32;
        Vector3 prevTop = new Vector3(localRadius, localHalfHeight, 0f);
        Vector3 prevBot = new Vector3(localRadius, -localHalfHeight, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float a = i * Mathf.PI * 2f / segments;
            Vector3 top = new Vector3(Mathf.Cos(a) * localRadius, localHalfHeight, Mathf.Sin(a) * localRadius);
            Vector3 bot = new Vector3(Mathf.Cos(a) * localRadius, -localHalfHeight, Mathf.Sin(a) * localRadius);
            Gizmos.DrawLine(prevTop, top);
            Gizmos.DrawLine(prevBot, bot);
            Gizmos.DrawLine(top, bot);
            prevTop = top;
            prevBot = bot;
        }
    }
}
