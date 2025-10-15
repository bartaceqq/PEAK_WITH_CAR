using UnityEngine;

public class TruckOrbitCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;                 // CameraPivot on truck
    public Rigidbody targetRb;               // optional: truck rigidbody for velocity look-ahead

    [Header("Orbit")]
    public float distance = 7f;
    public float minDistance = 3f;
    public float maxDistance = 15f;
    public float yaw = 0f;                   // deg
    public float pitch = 12f;                // deg
    public float minPitch = -5f;
    public float maxPitch = 35f;

    [Header("Input (Old Input Manager)")]
    public float mouseXSens = 180f;          // deg/sec
    public float mouseYSens = 180f;          // deg/sec
    public float zoomSpeed  = 8f;            // units per scroll step

    [Header("Smoothing (critically damped)")]
    public float yawSmooth   = 0.12f;        // seconds to settle
    public float pitchSmooth = 0.12f;
    public float distSmooth  = 0.12f;
    public float followSmooth= 0.12f;        // arm end smoothing

    [Header("Look-ahead (optional)")]
    public float lookAheadStrength = 0.4f;   // meters per (m/s); 0 = off
    public float lookAheadMax = 2.5f;        // clamp

    [Header("Collision")]
    public float probeRadius = 0.25f;        // sphere radius
    public LayerMask clipMask = ~0;
    public float clipBuffer = 0.15f;

    // internal state for SmoothDamp
    float yawVel, pitchVel, distVel;
    Vector3 armVel;

    float yawT, pitchT, distT;

    void Start()
    {
        if (!target) { enabled = false; return; }
        yawT = yaw; pitchT = pitch; distT = Mathf.Clamp(distance, minDistance, maxDistance);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // --- Input (old system) ---
        float mx = Input.GetAxis("Mouse X");
        float my = Input.GetAxis("Mouse Y");
        yawT   += mx * mouseXSens * Time.deltaTime;
        pitchT -= my * mouseYSens * Time.deltaTime;
        pitchT  = Mathf.Clamp(pitchT, minPitch, maxPitch);

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > Mathf.Epsilon)
            distT = Mathf.Clamp(distT - scroll * zoomSpeed, minDistance, maxDistance);

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            bool locked = Cursor.lockState == CursorLockMode.Locked;
            Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = !locked;
        }
    }

    void LateUpdate()
    {
        if (!target) return;

        // --- Smooth angles & distance (critically damped) ---
        yaw   = SmoothDampAngle(yaw,   yawT,   ref yawVel,   yawSmooth);
        pitch = SmoothDampAngle(pitch, pitchT, ref pitchVel, pitchSmooth);
        distance = Mathf.SmoothDamp(distance, distT, ref distVel, distSmooth);

        // --- Base rotation ---
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);

        // --- Look-ahead by target velocity (gives ETS2 feel) ---
        Vector3 lookAhead = Vector3.zero;
        if (targetRb && lookAheadStrength > 0f)
        {
            Vector3 v = Vector3.ClampMagnitude(targetRb.linearVelocity * lookAheadStrength, lookAheadMax);
            lookAhead = v;
        }

        Vector3 targetPos = target.position + lookAhead;

        // --- Desired camera position (spring arm tip before collision) ---
        Vector3 desired = targetPos - rot * Vector3.forward * distance;

        // --- Collision: sphere cast from target to desired ---
        Vector3 dir = desired - targetPos;
        float dist = dir.magnitude;
        Vector3 camPos = desired;

        if (dist > 0.0001f)
        {
            dir /= dist;
            if (Physics.SphereCast(targetPos, probeRadius, dir, out var hit, dist, clipMask, QueryTriggerInteraction.Ignore))
            {
                camPos = hit.point - dir * clipBuffer;
            }
        }

        // --- Smooth follow of arm tip (prevents jitter on rough ground) ---
        transform.position = Vector3.SmoothDamp(transform.position, camPos, ref armVel, followSmooth);
        transform.rotation = rot;
    }

    // Unity doesn't have SmoothDampAngle, so roll our own
    static float SmoothDampAngle(float current, float target, ref float currentVelocity, float smoothTime)
    {
        target = Mathf.Repeat(target, 360f);
        current = Mathf.Repeat(current, 360f);
        float delta = Mathf.DeltaAngle(current, target);
        float goal = current + delta;
        return Mathf.SmoothDampAngle(current, goal, ref currentVelocity, Mathf.Max(0.0001f, smoothTime));
    }
}
