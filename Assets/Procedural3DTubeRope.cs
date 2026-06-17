using UnityEngine;

/// <summary>
/// سكربت توليد مجسم حبل ثلاثي الأبعاد (3D Tube) ديناميكي فائق الأداء.
/// تم حل مشكلة الانفصال والإزاحة في الفضاء (World to Local Space Fix).
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Procedural3DTubeRope : MonoBehaviour
{
    [Header("Rope Connections")]
    public Transform suspensionPoint;  // نقطة التعليق العلوية
    public Transform bucketAnchor;     // كائن الدلو السفلي

    [Header("Rope Appearance")]
    public float ropeRadius = 0.03f;   // سمك الحبل
    [Range(6, 16)]
    public int radialSegments = 8;     // دقة تدويرة الحبل

    private Mesh ropeMesh;
    private Vector3[] vertices;
    private int[] triangles;

    void Start()
    {
        // [إصلاح أمان] تصفير موقع الكائن تلقائياً لمنع طيران الحبل في السماء
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        ropeMesh = new Mesh();
        ropeMesh.name = "Dynamic_3D_Rope_Tube";
        ropeMesh.MarkDynamic(); 
        
        GetComponent<MeshFilter>().mesh = ropeMesh;

        int vertexCount = radialSegments * 2;
        vertices = new Vector3[vertexCount];
        ropeMesh.vertices = vertices; 

        int triangleCount = radialSegments * 6;
        triangles = new int[triangleCount];

        int tIdx = 0;
        for (int i = 0; i < radialSegments; i++)
        {
            int next = (i + 1) % radialSegments;

            triangles[tIdx++] = i;
            triangles[tIdx++] = next;
            triangles[tIdx++] = i + radialSegments;

            triangles[tIdx++] = next;
            triangles[tIdx++] = next + radialSegments;
            triangles[tIdx++] = i + radialSegments;
        }

        ropeMesh.triangles = triangles;
    }

    void LateUpdate()
    {
        if (suspensionPoint == null || bucketAnchor == null) return;

        // [التعديل الجوهري] تحويل نقاط الإحداثيات من العالم إلى الفضاء المحلي للكائن لمنع الانفصال
        Vector3 pStart = transform.InverseTransformPoint(suspensionPoint.position);
        Vector3 pEnd = transform.InverseTransformPoint(bucketAnchor.position);

        Vector3 dir = pEnd - pStart;
        float length = dir.magnitude;
        if (length < 0.001f) return;

        Quaternion rotation = Quaternion.LookRotation(dir);
        Vector3 right = rotation * Vector3.right;
        Vector3 up = rotation * Vector3.up;

        for (int i = 0; i < radialSegments; i++)
        {
            float angle = i * Mathf.PI * 2f / radialSegments;
            Vector3 circleOffset = (Mathf.Cos(angle) * right + Mathf.Sin(angle) * up) * ropeRadius;

            vertices[i] = pStart + circleOffset;
            vertices[i + radialSegments] = pEnd + circleOffset;
        }

        ropeMesh.vertices = vertices;
        ropeMesh.RecalculateNormals();
        ropeMesh.RecalculateBounds();
    }
}