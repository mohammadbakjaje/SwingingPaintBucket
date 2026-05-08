using UnityEngine;

public class PendulumMotion : MonoBehaviour
{


    [Header("Rope Visual Settings")]
    public Transform bucketHandle;
    public Material ropeMaterial;       // هنا سنضع صورة الحبل
    public int ropeSegments = 20;       // عدد القطع التي يتكون منها الحبل (كلما زاد، زادت النعومة)
    public float ropeWidth = 0.05f;     // عرض الحبل


    [Header("Initialization")]
    [Range(0f, 1f)] // هذا السطر يجعل المتغير يظهر كـ Slider في Unity
    public float initialFillAmount = 1.0f; // 1 يعني ممتلئ تماماً، 0 فارغ

    [Header("Pendulum Settings")]
    public Transform pivot;
    public float length = 2f;          // L
    public float gravity = 9.81f;      // g
    public float maxAngle = 30f;       // θmax
    public float mass = 1f;            // m
    public float dampingCoefficient = 0.15f; // b

    [Header("Paint Settings")]
    public PaintGrid paintGrid;
    public Transform paintOutlet;
    public Color paintColor = Color.red;
    public float paintDrainRate = 0.01f;
    public float maxPaintLevel = 0.2f;
    public float currentPaintLevel = 0.2f;

    [Header("Liquid Visuals")]
    public Transform paintLiquid;
    public Renderer liquidRenderer;
    public float emptyLevelY = -0.2f;
    public float fullLevelY = 0.5f;
    public Vector3 scaleAtFull = new Vector3(1f, 1f, 1f);
    public Vector3 scaleAtEmpty = new Vector3(0.5f, 1f, 0.5f);

    private LineRenderer line;
    private float timeElapsed = 0f;
    private Vector3 lastOutletPosition;
    private float currentLiquidTilt = 0f;

    void Start()
    {
        currentPaintLevel = initialFillAmount * maxPaintLevel;
        if (liquidRenderer != null)
            liquidRenderer.material.color = paintColor;

        line = GetComponent<LineRenderer>();
        line.positionCount = 2;
        lastOutletPosition = paintOutlet.position;
        UpdateLiquidVisuals();
        line = GetComponent<LineRenderer>();
        // تعيين الماتيريال الجديد (يجب أن تسند مادة في Inspector)
        if (ropeMaterial != null)
            line.material = ropeMaterial;

        line.positionCount = ropeSegments + 1; // تعيين عدد النقاط الكلي
        line.startWidth = ropeWidth;
        line.endWidth = ropeWidth * 0.8f;      //
    }

    void Update()
    {
        paintGrid.currentPaintColor = paintColor;
        timeElapsed += Time.deltaTime;

        // 1. حساب حركة النواس المتخامد (Damped Pendulum Physics)
        float thetaMaxRad = maxAngle * Mathf.Deg2Rad;
        float omega = Mathf.Sqrt(gravity / length);
        float k = dampingCoefficient / mass;

        float theta = thetaMaxRad * Mathf.Exp(-k * timeElapsed) * Mathf.Cos(omega * timeElapsed);

        // تحديث الموقع بناءً على المعادلات القطبية
        float x = pivot.position.x + length * Mathf.Sin(theta);
        float y = pivot.position.y - length * Mathf.Cos(theta);
        transform.position = new Vector3(x, y, 0);

        // جعل الدلو يشير دائماً لنقطة التثبيت
        transform.up = (pivot.position - transform.position).normalized;

        // رسم الخيط
        Vector3 p0 = pivot.position;         // البداية من الأعلى

        // 🔴 التعديل هنا: إذا قمنا بتعيين المقبض، نستخدم موقعه، وإلا نستخدم المركز العادي كاحتياط
        Vector3 p2 = (bucketHandle != null) ? bucketHandle.position : transform.position;

        // حساب نقطة المنتصف p1 لتعطي انحناءً وهمياً
        Vector3 midPoint = (p0 + p2) / 2f;
        Vector3 gravityDirection = Vector3.down;

        float sagAmount = 0.3f;
        Vector3 p1 = midPoint + (gravityDirection * sagAmount);

        // 2. تكرار (Loop) لحساب كل نقطة على المنحنى
        for (int i = 0; i <= ropeSegments; i++)
        {
            float t = i / (float)ropeSegments;
            float u = 1f - t;
            float tt = t * t;
            float uu = u * u;

            Vector3 pointOnCurve = (uu * p0) + (2f * u * t * p1) + (tt * p2);
            line.SetPosition(i, pointOnCurve);
        }
        // 2. محاكاة ميلان سطح السائل (Sloshing Effect)
        float targetTilt = -theta * Mathf.Rad2Deg * 0.3f;
        currentLiquidTilt = Mathf.Lerp(currentLiquidTilt, targetTilt, Time.deltaTime * 5f);
        paintLiquid.localRotation = Quaternion.Euler(0f, 0f, currentLiquidTilt);

        // 3. منطق خروج الطلاء والاصطدام
        if (currentPaintLevel <= 0f) return;

        HandlePaintPhysics();
    }

    void HandlePaintPhysics()
    {
        Vector3 outletDirection = -paintOutlet.up;
        float fluidSpeed = Mathf.Sqrt(2 * gravity * currentPaintLevel);
        Vector3 fluidVelocity = outletDirection * fluidSpeed;

        // حساب سرعة الدلو نفسه لإضافتها لسرعة القذف
        Vector3 outletVelocity = (paintOutlet.position - lastOutletPosition) / Time.deltaTime;
        lastOutletPosition = paintOutlet.position;

        Vector3 totalVelocity = fluidVelocity + outletVelocity;

        // فحص المسار (Projectile Trajectory) يدوياً
        float boardY = 0f;
        bool isHittingBoard = false;
        Vector3 impactPoint = Vector3.zero;

        for (int i = 0; i < 40; i++)
        {
            float t = i * 0.02f; // خطوة زمنية صغيرة للدقة
            Vector3 point = paintOutlet.position + totalVelocity * t + 0.5f * Physics.gravity * t * t;

            if (point.y <= boardY)
            {
                isHittingBoard = true;
                impactPoint = point;
                break;
            }
        }

        // 4. تنفيذ الرسم الفعلي
        if (isHittingBoard)
        {
            // استهلاك الطلاء
            currentPaintLevel -= paintDrainRate * Time.deltaTime;
            currentPaintLevel = Mathf.Clamp(currentPaintLevel, 0f, maxPaintLevel);

            // رسم النقطة المركزية
            paintGrid.PaintAtPoint(impactPoint, 0.15f, 1.0f);

            // رسم الرذاذ (Splatter) حول المركز
            for (int i = 0; i < 3; i++)
            {
                Vector2 randomOffset = Random.insideUnitCircle * 0.3f;
                Vector3 splatterPoint = impactPoint + new Vector3(randomOffset.x, 0, randomOffset.y);
                paintGrid.PaintAtPoint(splatterPoint, 0.05f, 0.4f);
            }

            // تحديث شكل السائل (تناقص الحجم والموقع)
            UpdateLiquidVisuals();
        }
    }

    void UpdateLiquidVisuals()
    {
        if (paintLiquid == null) return;

        // إذا كان الطلاء صفر أو أقل، نخفي السائل تماماً
        if (currentPaintLevel <= 0.001f)
        {
            paintLiquid.gameObject.SetActive(false);
            return;
        }
        else
        {
            paintLiquid.gameObject.SetActive(true);
        }

        float fillPercentage = currentPaintLevel / maxPaintLevel;

        // حساب الموقع والحجم (الـ Lerp الذي شرحناه سابقاً)
        float newY = Mathf.Lerp(emptyLevelY, fullLevelY, fillPercentage);
        paintLiquid.localPosition = new Vector3(paintLiquid.localPosition.x, newY, paintLiquid.localPosition.z);

        Vector3 newScale = Vector3.Lerp(scaleAtEmpty, scaleAtFull, fillPercentage);
        paintLiquid.localScale = new Vector3(newScale.x, paintLiquid.localScale.y, newScale.z);
    }
}