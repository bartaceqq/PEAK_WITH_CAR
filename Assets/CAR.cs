using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CAR : MonoBehaviour
{
    [Header("Physics")]
    [SerializeField] float extraGravityMultiplier = 2f;
    public Rigidbody rigid;
    public Transform COM; // assign in Inspector (slightly below center)

    [Header("Wheels")]
    public WheelCollider wheel1, wheel2, wheel3, wheel4;
    public float drivespeed = 500f;
    public float steerspeed = 30f;

    [Header("Anti-roll & Stability")]
    [SerializeField] float antiRollFront = 8000f;
    [SerializeField] float antiRollRear = 10000f;
    [SerializeField] float baseExtraG = 1.2f;
    [SerializeField] float extraGAtSpeed = 1.8f;
    [SerializeField] float highSpeedKmh = 80f;

    float horizontalInput;
    float verticalInput;

    void Start()
    {
        if (rigid)
        {
            // center of mass
            if (COM != null)
                rigid.centerOfMass = transform.InverseTransformPoint(COM.position);

            // physics setup
            rigid.interpolation = RigidbodyInterpolation.Interpolate;
            rigid.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rigid.angularDamping = 0.6f; // slightly damp rotation
            rigid.solverIterations = 12;
            rigid.solverVelocityIterations = 12;
        }
    }

    void Update()
    {
        // cache inputs (old Input Manager)
        horizontalInput = Input.GetAxis("Horizontal");
        verticalInput = Input.GetAxis("Vertical");
    }

    void FixedUpdate()
    {
        // --- Ground checks ---
        bool flG = wheel1.GetGroundHit(out WheelHit hitFL);
        bool frG = wheel2.GetGroundHit(out WheelHit hitFR);
        bool rlG = wheel3.GetGroundHit(out WheelHit hitRL);
        bool rrG = wheel4.GetGroundHit(out WheelHit hitRR);

        float speedKmh = rigid.linearVelocity.magnitude * 3.6f;
        float speedLerp = Mathf.InverseLerp(0f, highSpeedKmh, speedKmh);

        // --- Drift factor (based on sideways slip) ---
        float drift = 0f;
        if (flG) drift = Mathf.Max(drift, Mathf.Abs(hitFL.sidewaysSlip));
        if (frG) drift = Mathf.Max(drift, Mathf.Abs(hitFR.sidewaysSlip));
        if (rlG) drift = Mathf.Max(drift, Mathf.Abs(hitRL.sidewaysSlip));
        if (rrG) drift = Mathf.Max(drift, Mathf.Abs(hitRR.sidewaysSlip));
        float driftFactor = Mathf.InverseLerp(0.15f, 0.6f, drift);

        // --- Adaptive extra gravity ---
        if (flG || frG || rlG || rrG)
        {
            float extraG = Mathf.Lerp(baseExtraG, extraGAtSpeed, speedLerp);
            extraG *= Mathf.Lerp(1f, 0.6f, driftFactor); // less "glue" when drifting
            rigid.AddForce(Physics.gravity * extraGravityMultiplier * extraG, ForceMode.Acceleration);
        }

        // --- Drive and steer ---
        float motor = verticalInput * drivespeed;
        wheel1.motorTorque = motor;
        wheel2.motorTorque = motor;
        wheel3.motorTorque = motor;
        wheel4.motorTorque = motor;

        float steer = steerspeed * horizontalInput;
        wheel1.steerAngle = steer;
        wheel2.steerAngle = steer;

        // --- Anti-roll (scales down during drifts) ---
        ApplyAntiRoll(wheel1, wheel2, antiRollFront * Mathf.Lerp(1f, 0.4f, driftFactor)); // front
        ApplyAntiRoll(wheel3, wheel4, antiRollRear * Mathf.Lerp(1f, 0.4f, driftFactor));  // rear
    }

    void ApplyAntiRoll(WheelCollider left, WheelCollider right, float force)
    {
        float travelL = 1f, travelR = 1f;
        bool lG = left.GetGroundHit(out WheelHit hitL);
        bool rG = right.GetGroundHit(out WheelHit hitR);

        if (lG)
            travelL = (-left.transform.InverseTransformPoint(hitL.point).y - left.radius) / left.suspensionDistance;
        if (rG)
            travelR = (-right.transform.InverseTransformPoint(hitR.point).y - right.radius) / right.suspensionDistance;

        float antiRoll = (travelL - travelR) * force;

        if (lG) rigid.AddForceAtPosition(left.transform.up * -antiRoll, left.transform.position);
        if (rG) rigid.AddForceAtPosition(right.transform.up *  antiRoll, right.transform.position);
    }
}
