using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElasticRopeSimulator : MonoBehaviour
{
    public class RopeNode
    {
        public Vector3 currentPosition;
        public Vector3 previousPosition;
        public Vector3 acceleration;
        public float mass;

        public RopeNode(Vector3 position, float mass)
        {
            this.currentPosition = position;
            this.previousPosition = position;
            this.mass = mass;
            this.acceleration = Vector3.zero;
        }
    }

    [Header("Rope Settings")]
    public Transform pivotTransform;
    public Transform bucketTransform;
    public int numberOfNodes = 15;
    public float totalRopeLength = 3.0f;
    public float springConstantK = 150f; // قيمة متزنة جداً
    public float dampingFactor = 4.0f;   // خمود عالي لفرملة الاهتزاز العشوائي
    [Range(0f, 1f)] public float airHumidityResistance = 0.1f; 
    public Vector3 gravity = new Vector3(0, -9.81f, 0);

    [Header("Bucket Mass Configuration")]
    public float emptyBucketMass = 1.0f;
    public SPHFluidSimulator fluidSimulator; 

    public List<Vector3> Positions = new List<Vector3>();

    private List<RopeNode> nodes;
    private float restLengthPerSegment;
    private LineRenderer lineRenderer; 
    private bool isInitialized = false;
    private float initializationTimer = 0f;

    [Header("Initial Angle Configuration")]
    [Range(-45f, 45f)] public float initialAngleDegrees = 25f; // زاوية ميل الحبل عند البداية

    void Start()
    {
        if (pivotTransform == null) pivotTransform = this.transform;
        
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = numberOfNodes;
        }

        InitRope();
    }

    // دالة جديدة لحساب اتجاه الميلان بذكاء (تميل في X و Z لصنع أشكال هندسية مذهلة)
    private Vector3 GetInitialDirection()
    {
        if (initialAngleDegrees == 0) return Vector3.down;

        float angleRad = initialAngleDegrees * Mathf.Deg2Rad;
        // دمج محور Z بقيمة بسيطة يضمن تأرجحاً دائرياً متقاطعاً (Lissajous Curve)
        Vector3 slantDir = new Vector3(Mathf.Sin(angleRad), -Mathf.Cos(angleRad), Mathf.Sin(angleRad) * 0.4f).normalized;
        return slantDir;
    }

    void InitRope()
    {
        nodes = new List<RopeNode>();
        Positions.Clear();
        restLengthPerSegment = totalRopeLength / (numberOfNodes - 1);
        Vector3 startPos = pivotTransform.position;
        Vector3 slantDirection = GetInitialDirection();

        for (int i = 0; i < numberOfNodes; i++)
        {
            // بناء الحبل مائلاً منذ البداية بدلاً من الخط العمودي
            Vector3 nodePos = startPos + slantDirection * (i * restLengthPerSegment);
            float nodeMass = (i == numberOfNodes - 1) ? Mathf.Max(0.1f, emptyBucketMass) : 0.05f; 
            nodes.Add(new RopeNode(nodePos, nodeMass));
            Positions.Add(nodePos);
        }

        if (bucketTransform != null)
        {
            bucketTransform.position = nodes[nodes.Count - 1].currentPosition;
        }
        
        isInitialized = true;
    }

    public void ApplyExternalHandForce(Vector3 impulse)
    {
        if (!isInitialized || nodes == null || nodes.Count == 0) return;
        RopeNode bucketNode = nodes[nodes.Count - 1];
        if(bucketNode.mass > 0 && !float.IsNaN(bucketNode.previousPosition.x))
            bucketNode.previousPosition -= impulse / bucketNode.mass;
    }

    void FixedUpdate()
    {
        if (!isInitialized || nodes == null || nodes.Count == 0) return;

        // إطار الإحماء لضمان استقرار المحرك عند أول ثانية
        initializationTimer += Time.fixedDeltaTime;
        if (initializationTimer < 0.5f)
        {
            ResetNodesToDefaultSlantedLine(); // تم تغييرها لتبقي الحبل مائلاً أثناء الإحماء!
            return;
        }

        // حساب الكتلة الديناميكية للـ Bucket
        if (fluidSimulator != null)
            nodes[nodes.Count - 1].mass = emptyBucketMass + fluidSimulator.GetFluidMass();
        else
            nodes[nodes.Count - 1].mass = emptyBucketMass;

        if (nodes[nodes.Count - 1].mass <= 0) nodes[nodes.Count - 1].mass = 1.0f;

        // تفكيك وقت الإطار إلى 5 خطوات برمجية صغيرة جداً لضمان الثبات المطلق ومنع الرقص المجنون
        int subSteps = 5;
        float subDt = Time.fixedDeltaTime / subSteps;
        Vector3 slantDirection = GetInitialDirection();

        for (int step = 0; step < subSteps; step++)
        {
            // تثبيت المسمار في السقف دائماً
            nodes[0].currentPosition = pivotTransform.position;
            nodes[0].previousPosition = pivotTransform.position;

            // 1. حساب تسارع الجاذبية ومقاومة الهواء
            for (int i = 1; i < nodes.Count; i++) 
            {
                Vector3 velocity = (nodes[i].currentPosition - nodes[i].previousPosition) / subDt;
                Vector3 airDragForce = -velocity * airHumidityResistance;
                nodes[i].acceleration = gravity + (airDragForce / nodes[i].mass);
            }

            // 2. حساب قوى النوابض الداخلية (قانون هوك المطور)
            for (int i = 0; i < nodes.Count - 1; i++)
            {
                RopeNode nodeA = nodes[i];
                RopeNode nodeB = nodes[i + 1];

                Vector3 delta = nodeB.currentPosition - nodeA.currentPosition;
                float currentLength = delta.magnitude;
                if (currentLength < 0.001f) currentLength = 0.001f;

                Vector3 direction = delta / currentLength;
                float extension = currentLength - restLengthPerSegment;

                Vector3 springForce = direction * (springConstantK * extension);

                Vector3 velA = (nodeA.currentPosition - nodeA.previousPosition) / subDt;
                Vector3 velB = (nodeB.currentPosition - nodeB.previousPosition) / subDt;
                Vector3 relativeVelocity = velB - velA;
                Vector3 dampingForce = direction * Vector3.Dot(relativeVelocity, direction) * dampingFactor;

                Vector3 totalForce = springForce + dampingForce;

                if (i > 0) nodeA.acceleration += totalForce / nodeA.mass;
                nodeB.acceleration -= totalForce / nodeB.mass;
            }

            // 3. تكامل فيرليه الدقيق جداً
            for (int i = 1; i < nodes.Count; i++)
            {
                RopeNode node = nodes[i];
                Vector3 temp = node.currentPosition;
                
                Vector3 nextPos = 2f * node.currentPosition - node.previousPosition + node.acceleration * (subDt * subDt);

                if (float.IsNaN(nextPos.x) || float.IsInfinity(nextPos.x))
                {
                    // الحفاظ على الميلان حتى في حالة الخطأ الرياضي
                    Vector3 defaultPos = pivotTransform.position + slantDirection * (i * restLengthPerSegment);
                    node.currentPosition = defaultPos;
                    node.previousPosition = defaultPos;
                    node.acceleration = Vector3.zero;
                }
                else
                {
                    node.currentPosition = nextPos;
                    node.previousPosition = temp;
                }
            }
        }

        // تحديث مصفوفة العرض في الـ Inspector بعد انتهاء الخطوات الفرعية الآمنة
        for (int i = 0; i < nodes.Count; i++)
        {
            Positions[i] = nodes[i].currentPosition;
        }

        // 4. قيادة وتوجيه الدلو المرئي بكل سلاسة
        if (bucketTransform != null)
        {
            bucketTransform.position = nodes[nodes.Count - 1].currentPosition;
            Vector3 lookDir = nodes[nodes.Count - 2].currentPosition - nodes[nodes.Count - 1].currentPosition;
            if (lookDir != Vector3.zero)
            {
                bucketTransform.rotation = Quaternion.LookRotation(lookDir) * Quaternion.Euler(90, 0, 0);
            }
        }

        // 5. رسم خط الحبل النظيف
        if (lineRenderer != null)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                lineRenderer.SetPosition(i, nodes[i].currentPosition);
            }
        }
    }

    // دالة الإحماء المطورة لتبقي الحبل مائلاً قبل أن يفلت ويتأرجح
    void ResetNodesToDefaultSlantedLine()
    {
        Vector3 startPos = pivotTransform.position;
        Vector3 slantDirection = GetInitialDirection();

        for (int i = 0; i < nodes.Count; i++)
        {
            Vector3 defaultPos = startPos + slantDirection * (i * restLengthPerSegment);
            nodes[i].currentPosition = defaultPos;
            nodes[i].previousPosition = defaultPos;
            nodes[i].acceleration = Vector3.zero;
            Positions[i] = defaultPos;
        }
        if (bucketTransform != null)
        {
            bucketTransform.position = nodes[nodes.Count - 1].currentPosition;
            bucketTransform.rotation = Quaternion.identity;
        }
    }
}