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
    public int maxParticlesInBucket = 300;  // تمت زيادة العدد لضمان كثافة الخيط
    public float particleMass = 0.05f;     
    
    [Range(0f, 5f)]
    public float viscosity = 1.5f;         // اللزوجة (كلما زادت، تماسك الخيط أكثر ولم يتقطع)
    public float viscosityMu { get { return viscosity; } set { viscosity = value; } }
    
    public float outletRadius = 0.04f;     // قطر الفتحة (يتحكم بسُمك خيط الطلاء)
    public float gravity = -9.81f;

    [Header("Paint Quantity Settings")]
    public int totalPaintDroplets = 2000;  // مخزون أكبر ليدوم الرسم أطول
    public float flowRateSpeed = 3.0f;     // سرعة دفع الطلاء للأسفل
    public bool continuousFlow = true;     

    private List<SPHParticle> particles;

    void Start()
    {
        particles = new List<SPHParticle>();
        
        for (int i = 0; i < maxParticlesInBucket; i++)
        {
            // توليد الجسيمات بناءً على قطر الفتحة المخصص
            Vector3 randomOffset = transform.up * Random.Range(-0.1f, 0.1f) + 
                                   transform.right * Random.Range(-outletRadius, outletRadius) + 
                                   transform.forward * Random.Range(-outletRadius, outletRadius);
            particles.Add(new SPHParticle(transform.position + randomOffset));
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
                // سحب الطلاء داخل الدلو
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
                // --- التعديل الفيزيائي الجوهري ---
                // 1. تطبيق الجاذبية
                p.velocity.y += gravity * dt;
                
                // 2. تطبيق الكبح اللزج (Viscous Drag): يمنع الجسيمات السفلية من الهروب والتمزق
                // هذا يحافظ على تماسك عمود السائل ليضرب اللوحة كخيط متصل!
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
                        // إعادة التدوير من خلال الفتحة المخصصة
                        p.position = bucketPos + Vector3.up * 0.1f + 
                                     new Vector3(Random.Range(-outletRadius, outletRadius), 0, Random.Range(-outletRadius, outletRadius));
                        p.velocity = Vector3.zero;
                    }
                    else
                    {
                        particles.RemoveAt(i);
                        totalPaintDroplets--;

                        if (totalPaintDroplets > 0)
                        {
                            Vector3 spawnPos = bucketPos + Vector3.up * 0.1f + 
                                               new Vector3(Random.Range(-outletRadius, outletRadius), 0, Random.Range(-outletRadius, outletRadius));
                            particles.Add(new SPHParticle(spawnPos));
                        }
                        continue;
                    }
                }
            }
        }
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