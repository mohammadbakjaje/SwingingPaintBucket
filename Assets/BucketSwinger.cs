using UnityEngine;

public class BucketSwinger : MonoBehaviour
{
    [Header("Artistic Physics Kick")]
    public GameObject bucketObject; // اسحب كائن الدلو (BucketVisual) هنا في الـ Inspector
    
    // قوة الدفع الفيزيائية (تفاوت قيم X و Z هو السر لصنع المسار البيضاوي المتداخل)
    public Vector3 pushForce = new Vector3(8f, 0f, 4f); 

    void Update()
    {
        // عند ضغط زر المسافة (Space) نضرب الدلو ضربة فيزيائية خاطفة
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ApplyPhysicsKick();
        }
    }

    public void ApplyPhysicsKick()
    {
        if (bucketObject != null)
        {
            // الحصول على مكون الفيزياء المدمج في الدلو
            Rigidbody bucketRb = bucketObject.GetComponent<Rigidbody>();

            if (bucketRb != null)
            {
                // إعطاء الدلو دفعة حركية مفاجئة (VelocityChange تعني دفع فوري يتجاهل الكتلة مؤقتاً لبدء الحركة)
                bucketRb.AddForce(pushForce, ForceMode.VelocityChange);
                
                Debug.Log("تم توجيه ركلة فيزيائية ناجحة للدلو! راقب الأرجحة الحلزونية الآن.");
            }
            else
            {
                // أمان إضافي: إذا كان الـ Rigidbody موجوداً في الكائن الأب أو كائن فرعي
                Rigidbody parentRb = bucketObject.GetComponentInParent<Rigidbody>();
                if (parentRb != null)
                {
                    parentRb.AddForce(pushForce, ForceMode.VelocityChange);
                    Debug.Log("تم دفع الـ Rigidbody في الكائن الأب للدلو بنجاح!");
                }
                else
                {
                    Debug.LogError("لم يتم العثور على مكوّن Rigidbody على الدلو. تأكد من وجوده لكي يتحرك فيزيائياً!");
                }
            }
        }
        else
        {
            Debug.LogError("رجاءً اسحب كائن الدلو إلى خانة Bucket Object في الـ Inspector!");
        }
    }
}