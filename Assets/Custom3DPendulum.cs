using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class Custom3DPendulum : MonoBehaviour
{
    [Header("Rope & Pivot Setup")]
    [SerializeField] private Transform pivotTransform;
    [SerializeField] private float initialLength = 3.0f;
    [SerializeField] private float mass = 1.0f;

    [Header("Blackburn Pendulum")]
    [SerializeField] private float lengthX = 3.0f;
    [SerializeField] private float lengthZ = 4.0f;

    [Header("Environmental Forces")]
    [SerializeField] private float gravityConstant = 9.81f;
    [SerializeField] private float baseDragCoefficient = 0.1f;
    [Range(0f, 1f)] [SerializeField] private float airHumidity = 0.5f;
    [SerializeField] private float humidityEffectFactor = 0.2f;

    [Header("Elasticity (Hooke's Law)")]
    [SerializeField] private bool useElasticity = false;
    [SerializeField] private float springConstant = 400f;
    [SerializeField] private float springDamping = 4f;

    [Header("Initial Launch Force (Impulse)")]
    [SerializeField] private Vector3 initialForce = new Vector3(8f, 0f, 6f);

    [Header("Paint Settings")]
    [SerializeField] private Color paintColor = Color.red;
    [SerializeField] private float paintViscosity = 0.5f;
    [SerializeField] private Transform paintOutlet;
    [SerializeField] private float paintDrainRate = 0.01f;
    [SerializeField] private float maxPaintLevel = 0.2f;
    [SerializeField] private float currentPaintLevel = 0.2f;
    [SerializeField] private float splatterInterval = 0.05f; // زمن أدنى بين رشّات

    [Header("Rope Visuals")]
    [SerializeField] private Material ropeMaterial;
    [SerializeField] private int ropeSegments = 20;
    [SerializeField] private float ropeWidth = 0.05f;
    [SerializeField] private float sagAmount = 0.3f;

    // حالة فيزيائية داخلية
    private Vector3 currentPosition;
    private Vector3 currentVelocity;
    private Vector3 currentAcceleration;

    private LineRenderer line;
    private Vector3 lastOutletPosition;
    private float lastSplatterTime = -999f;

    private void Start()
    {
        Debug.Log("Custom3DPendulum: Start running.");
        if (pivotTransform == null)
        {
            Debug.LogError("Custom3DPendulum: pivotTransform must be assigned.");
            enabled = false;
            return;
        }

        currentPosition = pivotTransform.position + (Vector3.down * initialLength);
        transform.position = currentPosition;
        currentVelocity = Vector3.zero;

        line = GetComponent<LineRenderer>();
        line.positionCount = ropeSegments + 1;
        line.startWidth = ropeWidth;
        line.endWidth = ropeWidth * 0.8f;
        if (ropeMaterial != null) line.material = ropeMaterial;

        if (paintOutlet != null)
            lastOutletPosition = paintOutlet.position;
        else
            lastOutletPosition = transform.position;

        // تطبيق نبضة ابتدائية
        ApplyInitialImpulse(initialForce);
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        // حساب متجه الحبل والمسافة
        Vector3 displacement = currentPosition - pivotTransform.position;
        float currentDistance = Mathf.Max(0.0001f, displacement.magnitude);
        Vector3 ropeDirection = displacement / currentDistance;

        // قوى خارجية
        Vector3 gravityForce = Vector3.down * (mass * gravityConstant);
        float totalDragCoefficient = baseDragCoefficient + (airHumidity * humidityEffectFactor);
        Vector3 dragForce = -totalDragCoefficient * currentVelocity;

        float safeLengthX = Mathf.Max(0.01f, lengthX);
        float safeLengthZ = Mathf.Max(0.01f, lengthZ);
        Vector3 asymmetricRestore = new Vector3(
            -displacement.x * gravityConstant / safeLengthX,
            0f,
            -displacement.z * gravityConstant / safeLengthZ
        );

        Vector3 totalForce = gravityForce + dragForce + asymmetricRestore;

        if (useElasticity)
        {
            float deltaL = currentDistance - initialLength;
            float springForceMagnitude = -springConstant * deltaL;
            float radialVelocity = Vector3.Dot(currentVelocity, ropeDirection);
            float dampingForceMagnitude = -springDamping * radialVelocity;
            Vector3 tensionForce = ropeDirection * (springForceMagnitude + dampingForceMagnitude);
            totalForce += tensionForce;

            currentAcceleration = totalForce / mass;
            currentVelocity += currentAcceleration * dt;
            currentPosition += currentVelocity * dt;
        }
        else
        {
            // حبل صلب: تكامل ثم إزالة المركبة على امتداد الحبل
            currentAcceleration = totalForce / mass;
            currentVelocity += currentAcceleration * dt;

            float velocityAlongRope = Vector3.Dot(currentVelocity, ropeDirection);
            currentVelocity -= velocityAlongRope * ropeDirection;

            currentPosition += currentVelocity * dt;

            // تصحيح هندسي للحفاظ على الطول
            Vector3 correctedDir = (currentPosition - pivotTransform.position).normalized;
            currentPosition = pivotTransform.position + correctedDir * initialLength;
        }

        // تحديث موقع الدلو
        transform.position = currentPosition;
        transform.up = (pivotTransform.position - transform.position).normalized;

        // تحديث LineRenderer
        UpdateRopeVisual();

        // احسب سرعة مخرج الطلاء (Outlet)
        Vector3 outletVel = Vector3.zero;
        if (paintOutlet != null)
        {
            outletVel = (paintOutlet.position - lastOutletPosition) / Mathf.Max(0.0001f, dt);
            lastOutletPosition = paintOutlet.position;
        }

        Vector3 totalOutletVelocity = currentVelocity + outletVel;

        // محاولة رَشّ الطلاء بناءً على حركة المخرج
        SimulatePaintSplatterEvent(totalOutletVelocity);
    }

    private void UpdateRopeVisual()
    {
        Vector3 p0 = pivotTransform.position;
        Vector3 p2 = transform.position;
        Vector3 mid = (p0 + p2) * 0.5f + Vector3.down * sagAmount;

        for (int i = 0; i <= ropeSegments; i++)
        {
            float t = i / (float)ropeSegments;
            float u = 1f - t;
            Vector3 pointOnCurve = (u * u * p0) + (2f * u * t * mid) + (t * t * p2);
            line.SetPosition(i, pointOnCurve);
        }
    }

    private void ApplyInitialImpulse(Vector3 force)
    {
        currentVelocity += force / Mathf.Max(0.0001f, mass);
    }

    private void SimulatePaintSplatterEvent(Vector3 outletVelocity)
    {
        if (paintOutlet == null) return;
        if (currentPaintLevel <= 0f) return;

        if (Time.time - lastSplatterTime < splatterInterval) return;

        // محاكاة مسار قذيفة بسيطة لاكتشاف احتكاك مع مستوى اللوحة y = 0
        Vector3 start = paintOutlet.position;
        Vector3 initVel = outletVelocity;

        if (initVel.sqrMagnitude < 1e-6f) return; // لا طرد فعلي

        if (TryComputeImpactPoint(start, initVel, out Vector3 impact))
        {
            // إطلاق الحدث المنفصل عن الجرافيكس
            PhysicsEvents.TriggerPaintSplatter(impact, paintColor, initVel.magnitude, paintViscosity);

            // استهلاك الطلاء يتناسب مع السرعة
            float consumption = paintDrainRate * (1f + initVel.magnitude * 0.1f) * Time.fixedDeltaTime;
            currentPaintLevel = Mathf.Max(0f, currentPaintLevel - consumption);

            lastSplatterTime = Time.time;
        }
    }

    /// <summary>
    /// يحاول إيجاد نقطة التأثير مع مستوى ثابت y = 0 عبر تكامل زمنى مبسط
    /// </summary>
    private bool TryComputeImpactPoint(Vector3 start, Vector3 v0, out Vector3 impact)
    {
        impact = Vector3.zero;
        float maxT = 2.5f;
        float step = 0.02f;

        for (float t = 0f; t <= maxT; t += step)
        {
            Vector3 pos = start + v0 * t + 0.5f * Physics.gravity * t * t;
            if (pos.y <= 0f)
            {
                impact = pos;
                return true;
            }
        }
        return false;
    }

    private Vector3 GetLocalPosition()
    {
        return pivotTransform.InverseTransformPoint(transform.position);
    }

    private void OnDrawGizmosSelected()
    {
        if (pivotTransform == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(pivotTransform.position, transform.position);
        Gizmos.DrawSphere(pivotTransform.position, 0.03f);
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(transform.position, 0.05f);
    }

    private void OnValidate()
    {
        initialLength = Mathf.Max(0.01f, initialLength);
        mass = Mathf.Max(0.0001f, mass);
        ropeSegments = Mathf.Clamp(ropeSegments, 2, 256);
        lengthX = Mathf.Max(0.01f, lengthX);
        lengthZ = Mathf.Max(0.01f, lengthZ);
    }
}
