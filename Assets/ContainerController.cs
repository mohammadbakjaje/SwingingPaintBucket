using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Runtime controller for the container (Bucket_Parent). Lets you move, tilt and shake the
/// cylinder while in Play mode so you can watch the SPH fluid slosh and respond.
///
/// Uses the new Input System package (reads Keyboard.current directly — no Input Action asset
/// required). Standalone and decoupled: it only moves a Transform. Attach it to Bucket_Parent
/// (or drag Bucket_Parent into 'target') and hit Play.
///
/// Default controls:
///   Move:   A / D  (left-right),  W / S  (forward-back),  R / F  (up-down)
///   Tilt:   Q / E  (roll),        Arrow keys (pitch & yaw)
///   Shake:  hold  Space  — or press  T  for a single timed shake burst
///   Reset:  Backspace  — return to starting position & rotation
/// </summary>
public class ContainerController : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("The container to control. Defaults to this GameObject's transform if left empty.")]
    public Transform target;

    [Header("Movement")]
    public float moveSpeed = 3f;
    public float verticalSpeed = 2f;

    [Header("Rotation")]
    public float tiltSpeed = 90f;   // degrees per second

    [Header("Shake")]
    [Tooltip("How far the cylinder jerks around while shaking (world units).")]
    public float shakePositionAmount = 0.15f;
    [Tooltip("How much the cylinder rocks while shaking (degrees).")]
    public float shakeRotationAmount = 8f;
    [Tooltip("How fast the shaking oscillates.")]
    public float shakeFrequency = 30f;
    [Tooltip("Duration of a single timed shake burst (T key), in seconds.")]
    public float shakeBurstDuration = 0.6f;

    // --- Internal state ---
    private Vector3 basePosition;       // position without shake offset
    private Quaternion baseRotation;    // rotation without shake offset
    private Vector3 startPosition;      // original position for Reset
    private Quaternion startRotation;   // original rotation for Reset
    private float shakeBurstTimer;
    private float seedX, seedY, seedZ;

    void Awake()
    {
        if (target == null) target = transform;
        basePosition = startPosition = target.position;
        baseRotation = startRotation = target.rotation;

        // Fixed seeds keep each axis' noise independent (no Random needed at runtime).
        seedX = 0.123f;
        seedY = 4.567f;
        seedZ = 8.910f;
    }

    void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return; // no keyboard connected / not yet ready

        float dt = Time.deltaTime;

        HandleMovement(kb, dt);
        HandleRotation(kb, dt);
        HandleReset(kb);

        // Store the "clean" transform, then layer shake on top so shaking never accumulates.
        basePosition = target.position;
        baseRotation = target.rotation;

        ApplyShake(kb, dt);
    }

    void HandleMovement(Keyboard kb, float dt)
    {
        Vector3 move = Vector3.zero;
        if (kb.aKey.isPressed) move.x -= 1f;
        if (kb.dKey.isPressed) move.x += 1f;
        if (kb.wKey.isPressed) move.z += 1f;
        if (kb.sKey.isPressed) move.z -= 1f;
        target.position += move * moveSpeed * dt;

        float vertical = 0f;
        if (kb.rKey.isPressed) vertical += 1f;
        if (kb.fKey.isPressed) vertical -= 1f;
        target.position += Vector3.up * vertical * verticalSpeed * dt;
    }

    void HandleRotation(Keyboard kb, float dt)
    {
        float pitch = 0f, yaw = 0f, roll = 0f;
        if (kb.upArrowKey.isPressed) pitch -= 1f;
        if (kb.downArrowKey.isPressed) pitch += 1f;
        if (kb.leftArrowKey.isPressed) yaw -= 1f;
        if (kb.rightArrowKey.isPressed) yaw += 1f;
        if (kb.qKey.isPressed) roll -= 1f;
        if (kb.eKey.isPressed) roll += 1f;

        target.Rotate(pitch * tiltSpeed * dt, yaw * tiltSpeed * dt, roll * tiltSpeed * dt, Space.World);
    }

    void HandleReset(Keyboard kb)
    {
        if (kb.backspaceKey.wasPressedThisFrame)
        {
            target.position = startPosition;
            target.rotation = startRotation;
            basePosition = startPosition;
            baseRotation = startRotation;
        }
    }

    void ApplyShake(Keyboard kb, float dt)
    {
        if (kb.tKey.wasPressedThisFrame) shakeBurstTimer = shakeBurstDuration;
        if (shakeBurstTimer > 0f) shakeBurstTimer -= dt;

        bool shaking = kb.spaceKey.isPressed || shakeBurstTimer > 0f;
        if (!shaking) return;

        // Perlin noise centered at 0 gives smooth back-and-forth jitter on each axis.
        float t = Time.time * shakeFrequency;
        float ox = (Mathf.PerlinNoise(seedX, t) - 0.5f) * 2f;
        float oy = (Mathf.PerlinNoise(seedY, t) - 0.5f) * 2f;
        float oz = (Mathf.PerlinNoise(seedZ, t) - 0.5f) * 2f;

        Vector3 posOffset = new Vector3(ox, oy, oz) * shakePositionAmount;
        Quaternion rotOffset = Quaternion.Euler(new Vector3(ox, oy, oz) * shakeRotationAmount);

        target.position = basePosition + posOffset;
        target.rotation = baseRotation * rotOffset;
    }
}
