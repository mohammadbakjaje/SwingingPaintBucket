using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SPHFluidSimulator : MonoBehaviour
{
    public class SPHParticle
    {
        public Vector3 position;
        public Vector3 velocity;
        public Vector3 force;
        public float density;
        public float pressure;
        public GameObject visualMesh; // المجسم البصري للقطرة في اللعبة

        public SPHParticle(Vector3 pos)
        {
            this.position = pos;
            this.velocity = Vector3.zero;
            this.force = Vector3.zero;
            this.density = 0f;
            this.pressure = 0f;
        }
    }

    [Header("Fluid Flow Configuration")]
    public Transform paintOutlet;       // فوهة الخروج أسفل الدلو
    public DynamicPixelCanvas canvasTarget; // اللوحة الأرضية
    public int maxParticlesInBucket = 300;  
    public float particleMass = 0.05f;     
    
    [Range(0f, 5f)]
    public float viscosity = 1.5f;         
    public float viscosityMu { get { return viscosity; } set { viscosity = value; } }
    
    public float outletRadius = 0.04f;     
    public float gravity = -9.81f;

    [Header("Paint Visual Settings")]
    public GameObject dropPrefab;               // البريفاب البصري للقطرة (كرة حمراء مثلاً)
    [Range(0.01f, 0.5f)] public float dropWidth = 0.04f; // سُمك خيط الطلاء
    public float stretchMultiplier = 0.03f;     // مدى تمدد القطرة مع السرعة لتظهر كخيط متصل
    public Color paintColor = Color.red;        // لون الطلاء السائل

    [Header("Paint Quantity Settings")]
    public int totalPaintDroplets = 2000;  
    public float flowRateSpeed = 3.0f;     
    public bool continuousFlow = true;     

    private List<SPHParticle> particles;
    private GameObject visualsContainer; // حاوية عالمية منفصلة لمنع تشوه الحجم (Scale)

    void Start()
    {
        particles = new List<SPHParticle>();

        // إنشاء حاوية مستقلة تماماً في المشهد لتجنب وراثة حجم الدلو المشوه
        visualsContainer = new GameObject("Fluid_Visuals_Container");
        
        for (int i = 0; i < maxParticlesInBucket; i++)
        {
            Vector3 randomOffset = transform.up * Random.Range(-0.1f, 0.1f) + 
                                   transform.right * Random.Range(-outletRadius, outletRadius) + 
                                   transform.forward * Random.Range(-outletRadius, outletRadius);
            
            Vector3 spawnPos = transform.position + randomOffset;
            SPHParticle p = new SPHParticle(spawnPos);

            // إنشاء المظهر البصري للقطرة فوراً عند البداية
            if (dropPrefab != null)
            {
                p.visualMesh = Instantiate(dropPrefab, spawnPos, Quaternion.identity, visualsContainer.transform);
                p.visualMesh.transform.localScale = Vector3.one * dropWidth;
                
                // تلوين القطرة تلقائياً باللون المحدد
                Renderer rend = p.visualMesh.GetComponent<Renderer>();
                if (rend != null) rend.material.color = paintColor;
            }

            particles.Add(p);
        }
    }

    public float GetFluidMass()
    {
        if (particles == null) return 0f;
        return particles.Count * particleMass;
    }

    void FixedUpdate()
    {
        if (particles == null || (particles.Count == 0 && totalPaintDroplets <= 0)) return;

        float dt = Time.fixedDeltaTime;
        Vector3 bucketPos = transform.position;
        Vector3 targetOutletPos = (paintOutlet != null) ? paintOutlet.position : bucketPos + Vector3.down * 0.15f;

        for (int i = particles.Count - 1; i >= 0; i--)
        {
            SPHParticle p = particles[i];

            if (p.position.y > targetOutletPos.y)
            {
                p.velocity = Vector3.down * flowRateSpeed;
                p.position += p.velocity * dt;

                Vector3 horizontalDelta = p.position - bucketPos;
                horizontalDelta.y = 0;
                if (horizontalDelta.magnitude > outletRadius)
                {
                    p.position = bucketPos + horizontalDelta.normalized * outletRadius + (p.position.y - bucketPos.y) * Vector3.up;
                }
            }
            else
            {
                p.velocity.y += gravity * dt;
                p.velocity -= p.velocity * (viscosity * 0.5f) * dt; 
                p.position += p.velocity * dt;

                if (p.position.y <= 0.05f)
                {
                    if (canvasTarget != null)
                    {
                        canvasTarget.RegisterIncomingParticle(p.position, p.velocity, viscosity);
                    }

                    if (continuousFlow)
                    {
                        p.position = bucketPos + Vector3.up * 0.1f + 
                                     new Vector3(Random.Range(-outletRadius, outletRadius), 0, Random.Range(-outletRadius, outletRadius));
                        p.velocity = Vector3.zero;
                    }
                    else
                    {
                        if (p.visualMesh != null) Destroy(p.visualMesh);
                        particles.RemoveAt(i);
                        totalPaintDroplets--;

                        if (totalPaintDroplets > 0)
                        {
                            Vector3 spawnPos = bucketPos + Vector3.up * 0.1f + 
                                               new Vector3(Random.Range(-outletRadius, outletRadius), 0, Random.Range(-outletRadius, outletRadius));
                            
                            SPHParticle newParticle = new SPHParticle(spawnPos);
                            if (dropPrefab != null)
                            {
                                newParticle.visualMesh = Instantiate(dropPrefab, spawnPos, Quaternion.identity, visualsContainer.transform);
                                newParticle.visualMesh.transform.localScale = Vector3.one * dropWidth;
                                Renderer rend = newParticle.visualMesh.GetComponent<Renderer>();
                                if (rend != null) rend.material.color = paintColor;
                            }
                            particles.Add(newParticle);
                        }
                        continue;
                    }
                }
            }

            // تحديث موقع وتشكيل القطرة المرئية في اللعبة
            if (p.visualMesh != null)
            {
                p.visualMesh.transform.position = p.position;

                // سحر التسييل: جعل القطرة تتجه وتتمدد بناءً على اتجاه وسرعة سقوطها
                if (p.velocity.magnitude > 0.1f)
                {
                    p.visualMesh.transform.up = p.velocity.normalized;
                    float stretch = Mathf.Max(dropWidth, p.velocity.magnitude * stretchMultiplier);
                    p.visualMesh.transform.localScale = new Vector3(dropWidth, stretch, dropWidth);
                }
                else
                {
                    p.visualMesh.transform.localScale = Vector3.one * dropWidth;
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (visualsContainer != null) Destroy(visualsContainer);
    }

    void OnDrawGizmos()
    {
        if (particles == null) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < particles.Count; i++)
        {
            Gizmos.DrawSphere(particles[i].position, 0.02f);
        }
    }
}