using UnityEngine;

public class PendulumMotion : MonoBehaviour
{
    public Transform pivot;

    public float length = 2f;
    public float gravity = 9.81f;
    public float angle = 45f;

    private float angularVelocity = 0f;
    private LineRenderer line;

    void Start()
    {
        line = GetComponent<LineRenderer>();
        line.positionCount = 2;
    }

    void Update()
    {
        float angleRad = angle * Mathf.Deg2Rad;

        float angularAcceleration =
            -(gravity / length) * Mathf.Sin(angleRad);

        angularVelocity += angularAcceleration * Time.deltaTime;

        angle += angularVelocity * Time.deltaTime;

        float x = pivot.position.x +
                  length * Mathf.Sin(angleRad);

        float y = pivot.position.y -
                  length * Mathf.Cos(angleRad);

        transform.position = new Vector3(x, y, 0);

        line.SetPosition(0, pivot.position);
        line.SetPosition(1, transform.position);
    }
}