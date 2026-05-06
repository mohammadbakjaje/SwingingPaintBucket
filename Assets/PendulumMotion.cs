using UnityEngine;

public class PendulumMotion : MonoBehaviour
{
    public Transform pivot;
    public Transform paintOutlet;
    public float fluidHeight = 0.5f;
    public float length = 2f;          // L
    public float gravity = 9.81f;      // g
    public float maxAngle = 30f;       // θmax (degrees)
    public float mass = 1f;            // m
    public float dampingCoefficient = 0.15f; // b
    private Vector3 lastOutletPosition;
    private LineRenderer line;
    private float timeElapsed = 0f;
    public PaintGrid paintGrid;
    float paintTimer = 0f;
    public float paintInterval = 0.05f;
    void Start()
    {
        line = GetComponent<LineRenderer>();
        line.positionCount = 2;
    }

    void Update()
    {
        timeElapsed += Time.deltaTime;


        float thetaMaxRad = maxAngle * Mathf.Deg2Rad;

        float omega = Mathf.Sqrt(gravity / length);

        float k = dampingCoefficient / mass;

        float theta =
            thetaMaxRad *
            Mathf.Exp(-k * timeElapsed) *
            Mathf.Cos(omega * timeElapsed);

        float angularVelocity =
            -thetaMaxRad * Mathf.Exp(-k * timeElapsed) *
            (omega * Mathf.Sin(omega * timeElapsed) + k * Mathf.Cos(omega * timeElapsed));

        float angularAcceleration =
            -omega * omega * theta;

        float x = pivot.position.x + length * Mathf.Sin(theta);
        float y = pivot.position.y - length * Mathf.Cos(theta);

        transform.position = new Vector3(x, y, 0);

        transform.up = (pivot.position - transform.position).normalized;

        line.SetPosition(0, pivot.position);
        line.SetPosition(1, transform.position);


        // اتجاه خروج الطلاء
        Vector3 outletDirection = -paintOutlet.up;
        float fluidSpeed = Mathf.Sqrt(2 * gravity * fluidHeight);

        Vector3 fluidVelocity = outletDirection * fluidSpeed;

        Vector3 outletVelocity =
            (paintOutlet.position - lastOutletPosition) / Time.deltaTime;

        lastOutletPosition = paintOutlet.position;

        Vector3 totalVelocity = fluidVelocity + outletVelocity;

        // رسم مسار الطلاء
        float boardY = 0f; // ارتفاع اللوحة

        for (int i = 0; i < 40; i++)
        {
            float t = i * 0.05f;

            Vector3 point =
                paintOutlet.position +
                totalVelocity * t +
                0.5f * Physics.gravity * t * t;

            // رسم المسار
            Debug.DrawLine(point, point + Vector3.up * 0.02f, Color.blue);

            // 🔴 تحقق من الاصطدام مع اللوحة
            // 🔴 تحقق من الاصطدام مع اللوحة
            if (point.y <= boardY)
            {
                paintTimer += Time.deltaTime;

                if (paintTimer >= paintInterval)
                {
                    float speed = totalVelocity.magnitude;
                    float radius = Mathf.Clamp(speed * 0.02f, 0.1f, 0.5f);

                    int sprayCount = 5;

                    for (int s = 0; s < sprayCount; s++)
                    {
                        Vector3 randomDir =
                            outletDirection +
                            Random.insideUnitSphere * 0.2f;

                        randomDir.Normalize();

                        Vector3 sprayVelocity =
                            randomDir * speed;

                        float t2 = 0.1f;

                        Vector3 sprayPoint =
                            paintOutlet.position +
                            sprayVelocity * t2 +
                            0.5f * Physics.gravity * t2 * t2;

                        paintGrid.PaintAtPoint(sprayPoint, radius, speed);
                    }

                    paintTimer = 0f;
                }

                // 🔴 يجب أن يكون هنا
                break;
            }
        }
    }
}
