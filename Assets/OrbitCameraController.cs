using UnityEngine;

public class OrbitCameraController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 10f;
    public float zoomSpeed = 20f;
    public float rotationSpeed = 50f; // سرعة الإمالة

    void Update()
    {
        // 1. الحركة (فوق، تحت، يمين، يسار) باستخدام الأسهم
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector3 move = new Vector3(horizontal, vertical, 0) * moveSpeed * Time.deltaTime;
        transform.Translate(move);

        // 2. الزوم (تقريب وتبعيد) باستخدام عجلة الماوس
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (!Mathf.Approximately(scrollInput, 0f))
        {
            transform.Translate(Vector3.forward * scrollInput * zoomSpeed * Time.deltaTime);
        }

        // 3. الإمالة (لترى الرسمة في الأسفل) باستخدام حرفي W و S
        // W للإمالة للأسفل، S للإمالة للأعلى
        if (Input.GetKey(KeyCode.W))
        {
            transform.Rotate(Vector3.left * rotationSpeed * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.S))
        {
            transform.Rotate(Vector3.right * rotationSpeed * Time.deltaTime);
        }
    }
}