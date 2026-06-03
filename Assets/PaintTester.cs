using UnityEngine;

public class PaintTester : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PaintManager paintManager; // اسحب سكربت البينت مانيجر هنا

    [Header("Test Simulation Settings")]
    [SerializeField] private PaintManager.SurfaceType testSurface = PaintManager.SurfaceType.Metal;
    [SerializeField] [Range(1f, 10f)] private float simulatedVelocity = 5f;
    [SerializeField] [Range(0.1f, 5f)] private float simulatedViscosity = 1f;

    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        // عند الضغط بزر الفأرة الأيسر
        if (Input.GetMouseButton(0)) 
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // إطلاق شعاع للتأكد من أننا نضغط على اللوحة
            if (Physics.Raycast(ray, out hit))
            {
                // التأكد من أن الكائن المقذوف عليه هو اللوحة التي تحتوي على الـ Renderer المطلوبة
                if (hit.collider.gameObject == paintManager.gameObject || hit.transform == paintManager.transform)
                {
                    // جلب إحداثيات الـ UV لنقطة الاصطدام بدقة
                    Vector2 uv = hit.textureCoord;

                    // إرسال البيانات المحاكية إلى السكربت الأساسي لتجربة الرسم
                    paintManager.OnPhysicsImpact(uv, simulatedVelocity, simulatedViscosity, testSurface);
                }
            }
        }

        // اختصار لتغيير الألوان أثناء الاختبار برقم لوحة المفاتيح (1, 2, 3)
        if (Input.GetKeyDown(KeyCode.Alpha1)) paintManager.SetColorSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) paintManager.SetColorSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) paintManager.SetColorSlot(2);
    }
}