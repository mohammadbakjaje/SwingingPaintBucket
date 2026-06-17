using UnityEngine;
using System.Collections.Generic;

public class CustomVerletPendulum : MonoBehaviour
{
    [Header("Rope Settings (إعدادات الحبل الرياضية)")]
    public int segmentCount = 15;
    public float ropeLength = 5f;
    public float ropeWidth = 0.04f;
    public Vector3 gravity = new Vector3(0f, -9.81f, 0f);
    [Range(0f, 2f)] public float airResistance = 0.02f; // جعلناها خفيفة ليدور طويلاً

    [Header("Torsion Settings (معادلات الفتل المخصصة)")]
    public float torsionSpringK = 40f; 
    public float torsionDampingB = 2f;

    [Header("Target Bucket (مجسم الدلو الخاص بك)")]
    public Transform bucketTransform;

    [Header("Fluid Sync")]
    public Transform fluidSimulationObject; // اسحب كائن الطلاء هنا

    [Header("Inspector Initial Setup (إعدادات البدء للدوائر)")]
    [Range(-60f, 60f)] public float initialAngleX = 35f; // زاوية الميلان الابتدائية
    [Range(-60f, 60f)] public float initialAngleZ = 0f; 
    
    [Tooltip("مقدار القوة الجانبية المدارية. جرب قيمة بين 3 و 5")]
    public float orbitalPushSpeed = 4f; 

    private List<RopeNode> nodesList = new List<RopeNode>();
    private LineRenderer lineRenderer;

    private class RopeNode
    {
        public Vector3 position;
        public Vector3 previousPosition;
        public bool isFixed;
    }

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer != null)
        {
            lineRenderer.startWidth = ropeWidth;
            lineRenderer.endWidth = ropeWidth;
            lineRenderer.positionCount = segmentCount;
        }

        // 1. بناء الحبل مائلاً في الفضاء
        InitializeRope();

        // 2. 🔥 [الحل السحري]: حقن سرعة مدارية جانبية في كل عقد الحبل بالتناسب!
        // بما أن الحبل مائل على محور X، سندفعه على محور X الجانبي ليصنع مداراً دائرياً حقيقياً
        if (nodesList.Count > 1)
        {
            for (int i = 1; i < nodesList.Count; i++)
            {
                float graduationFactor = (float)i / (nodesList.Count - 1);
                
                // في فيزياء فيرلت، حقن السرعة يتم عبر إزاحة الموقع السابق بعكس اتجاه الدفع
                Vector3 pushVector = Vector3.right * (orbitalPushSpeed * graduationFactor);
                nodesList[i].previousPosition -= pushVector * Time.fixedDeltaTime;
            }
        }
    }

    public void InitializeRope()
    {
        if (bucketTransform == null) return;

        Vector3 startPos = transform.position;
        Vector3 tiltedDirection = Quaternion.Euler(initialAngleX, 0f, initialAngleZ) * Vector3.down;
        float segmentLength = ropeLength / (segmentCount - 1);

        nodesList.Clear();
        for (int i = 0; i < segmentCount; i++)
        {
            RopeNode node = new RopeNode();
            node.position = startPos + tiltedDirection * (i * segmentLength);
            node.previousPosition = node.position;
            node.isFixed = (i == 0);
            nodesList.Add(node);
        }

        bucketTransform.position = nodesList[nodesList.Count - 1].position;
        bucketTransform.rotation = Quaternion.FromToRotation(Vector3.up, -tiltedDirection);
    }

    void OnValidate()
    {
        if (!Application.isPlaying && bucketTransform != null)
        {
            Vector3 tiltedDirection = Quaternion.Euler(initialAngleX, 0f, initialAngleZ) * Vector3.down;
            bucketTransform.position = transform.position + tiltedDirection * ropeLength;
            bucketTransform.rotation = Quaternion.FromToRotation(Vector3.up, -tiltedDirection);
            
            LineRenderer lr = GetComponent<LineRenderer>();
            if (lr != null && segmentCount > 1)
            {
                lr.positionCount = segmentCount;
                float segmentLength = ropeLength / (segmentCount - 1);
                for (int i = 0; i < segmentCount; i++)
                {
                    lr.SetPosition(i, transform.position + tiltedDirection * (i * segmentLength));
                }
            }
        }
    }

    void FixedUpdate()
    {
        if (nodesList.Count == 0 || bucketTransform == null) return;

        nodesList[0].position = transform.position;
        float dt = Time.fixedDeltaTime;

        // حساب حركة العقد بناءً على فيزياء فيرلت
        for (int i = 1; i < nodesList.Count; i++)
        {
            Vector3 velocity = nodesList[i].position - nodesList[i].previousPosition;
            velocity *= (1f - airResistance * dt);
            nodesList[i].previousPosition = nodesList[i].position;
            Vector3 totalAcceleration = gravity;

            if (i > 1 && i < nodesList.Count - 1)
            {
                Vector3 straightTarget = (nodesList[i - 1].position + nodesList[i + 1].position) * 0.5f;
                Vector3 bendDisplacement = straightTarget - nodesList[i].position;
                Vector3 torsionForce = (bendDisplacement * torsionSpringK) - (velocity * torsionDampingB);
                totalAcceleration += torsionForce;
            }

            nodesList[i].position += velocity + totalAcceleration * dt * dt;
        }

        // تطبيق قيود طول الحبل (Distance Constraints)
        float segmentLength = ropeLength / (segmentCount - 1);
        for (int k = 0; k < 5; k++)
        {
            for (int i = 0; i < nodesList.Count - 1; i++)
            {
                RopeNode nodeA = nodesList[i];
                RopeNode nodeB = nodesList[i + 1];
                Vector3 delta = nodeB.position - nodeA.position;
                float currentDist = delta.magnitude;
                float error = currentDist - segmentLength;
                Vector3 repairVector = delta.normalized * error;

                if (nodeA.isFixed) nodeB.position -= repairVector;
                else
                {
                    nodeA.position += repairVector * 0.5f;
                    nodeB.position -= repairVector * 0.5f;
                }
            }
        }

        // تحديث موقع ومظهر الدلو النهائي بناءً على آخر عقدة في الحبل المفتل
        RopeNode lastNode = nodesList[nodesList.Count - 1];
        RopeNode secondLastNode = nodesList[nodesList.Count - 2];
        bucketTransform.position = lastNode.position;
        Vector3 lastSegmentDir = (lastNode.position - secondLastNode.position).normalized;
        if (lastSegmentDir != Vector3.zero)
        {
            bucketTransform.rotation = Quaternion.FromToRotation(Vector3.up, -lastSegmentDir);
        }

        if (fluidSimulationObject != null)
        {
            fluidSimulationObject.position = bucketTransform.position;
            fluidSimulationObject.rotation = bucketTransform.rotation;
        }

        if (lineRenderer != null)
        {
            for (int i = 0; i < nodesList.Count; i++)
            {
                lineRenderer.SetPosition(i, nodesList[i].position);
            }
        }
    }
}