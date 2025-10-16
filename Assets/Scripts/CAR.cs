using UnityEngine;
using UnityEngine.UI;

public class CAR : MonoBehaviour
{
    public GameObject car;
    public GasScript gas; // optional fuel script

    private float currentNeedleAngle = 0f;

    [Header("Physics")]
    [SerializeField] float extraGravityMultiplier = 2f;
    public Rigidbody rigid;
    public Transform COM;

    [Header("Wheels")]
    public WheelCollider wheelFL, wheelFR, wheelRL, wheelRR;

    [Header("Settings")]
    public float driveTorque = 500f;
    public float brakeForce = 3000f;
    public float handbrakeForce = 5000f;
    public float steerAngle = 30f;

    // NEW: make steering smooth & self-centering
    [Range(0.1f, 50f)] public float steerInRate = 15f;      // how fast we reach target steer
    [Range(0.1f, 50f)] public float steerReturnRate = 20f;  // how fast we return to center
    public KeyCode resetKey = KeyCode.R;

    public Image highlightSquare;
    public float growSpeed = 3f;
    public float maxScale = 1f;

    [Header("Speedometer / Tachometer")]
    public float maxSpeedKmh = 100f;
    public RectTransform needle;
    public float minNeedleAngle = -90f;
    public float maxNeedleAngle = 90f;

    [Header("Reset Options")]
    public float resetHeightOffset = 0.4f;
    public LayerMask groundMask = ~0;

    // === Flip/Drift Guard (reworked to not fight steering) ===
    [Header("Flip/Drift Guard")]
    public float minFlipDriftSpeedKmh = 60f; // must be above this speed
    public float minRollAngleDeg = 40f;      // AND absolute Z roll must be >= this
    public float lateralDamp = 50f;          // base sideways damping
    public float angDamp = 10f;              // roll/pitch damping
    public float uprightTorque = 0.5f;       // uprighting torque scale

    float hInput;
    float vInput;
    bool handbrake;
    float currentScale = 0f;
    bool pendingReset;

    Vector3 originalPosition;
    Quaternion originalRotation;

    // NEW: cached steering value so it can’t “stick”
    float currentSteerDeg = 0f;

    void Start()
    {
        if (rigid && COM)
            rigid.centerOfMass = transform.InverseTransformPoint(COM.position);

        if (highlightSquare != null)
            highlightSquare.enabled = false;

        if (rigid != null)
        {
            originalPosition = rigid.transform.position;
            originalRotation = rigid.transform.rotation;
        }
        else
        {
            originalPosition = transform.position;
            originalRotation = transform.rotation;
        }
    }

    void Update()
    {
        // --- Reset UI ---
        if (Input.GetKey(resetKey))
        {
            if (highlightSquare && !highlightSquare.enabled)
                highlightSquare.enabled = true;

            currentScale = Mathf.MoveTowards(currentScale, maxScale, Time.deltaTime * growSpeed);
            if (highlightSquare)
                highlightSquare.rectTransform.localScale = Vector3.one * currentScale;

            if (currentScale >= maxScale)
            {
                pendingReset = true;
                currentScale = 0f;
            }
        }
        else
        {
            currentScale = Mathf.MoveTowards(currentScale, 0f, Time.deltaTime * growSpeed);

            if (highlightSquare)
            {
                if (currentScale <= 0.01f && highlightSquare.enabled)
                    highlightSquare.enabled = false;

                highlightSquare.rectTransform.localScale = Vector3.one * currentScale;
            }
        }

        // raw feels snappier and avoids rare smoothing “hangs”
        hInput = Input.GetAxisRaw("Horizontal");
        vInput = Input.GetAxisRaw("Vertical");
        handbrake = Input.GetKey(KeyCode.Space);
    }

    void FixedUpdate()
    {
        if (pendingReset)
        {
            DoPhysicsSafeReset();
            pendingReset = false;
        }

        if (!rigid) return;

        // --- Extra Gravity ---
        rigid.AddForce(Vector3.down * extraGravityMultiplier * Physics.gravity.magnitude, ForceMode.Acceleration);

        // --- Downforce for stability ---
        float downforce = rigid.linearVelocity.magnitude * 15f;
        rigid.AddForce(-transform.up * downforce);

        // --- Current Speed (km/h) ---
        float currentSpeedKmh = rigid.linearVelocity.magnitude * 3.6f;

        // --- Speed Limit ---
        if (currentSpeedKmh > maxSpeedKmh)
        {
            rigid.linearVelocity = rigid.linearVelocity.normalized * (maxSpeedKmh / 3.6f);
            currentSpeedKmh = maxSpeedKmh;
        }

        // --- Smooth Steering with return-to-center (prevents “stuck angle”) ---
        float targetSteer = hInput * steerAngle;
        float rate = (Mathf.Abs(hInput) > 0.01f) ? steerInRate : steerReturnRate;
        currentSteerDeg = Mathf.MoveTowards(currentSteerDeg, targetSteer, rate * Time.fixedDeltaTime);

        // snap exactly to zero when near center (removes tiny residuals)
        if (Mathf.Abs(currentSteerDeg) < 0.05f && Mathf.Abs(hInput) < 0.01f)
            currentSteerDeg = 0f;

        if (wheelFL) wheelFL.steerAngle = currentSteerDeg;
        if (wheelFR) wheelFR.steerAngle = currentSteerDeg;

        // === Flip/Drift Guard: don’t fight while actively steering ===
        bool allowFlipDrift = (GetRollZAbs() >= minRollAngleDeg) && (currentSpeedKmh > minFlipDriftSpeedKmh);
        bool activeSteer = Mathf.Abs(hInput) > 0.15f; // treat as user intends to steer

        if (!allowFlipDrift && !activeSteer)
        {
            // lateral damping scaled by speed; very gentle at low speed
            float speed01 = Mathf.InverseLerp(0f, 50f, currentSpeedKmh);
            float scaledLateralDamp = Mathf.Lerp(lateralDamp * 0.25f, lateralDamp, speed01);

            // 1) Kill lateral (sideways) sliding to prevent drifting
            Vector3 localVel = transform.InverseTransformDirection(rigid.linearVelocity);
            localVel.x = Mathf.MoveTowards(localVel.x, 0f, Time.fixedDeltaTime * scaledLateralDamp);
            rigid.linearVelocity = transform.TransformDirection(localVel);

            // 2) Dampen roll/pitch spin so it won't flip
            Vector3 av = rigid.angularVelocity;
            av.x = Mathf.MoveTowards(av.x, 0f, Time.fixedDeltaTime * angDamp); // pitch
            av.z = Mathf.MoveTowards(av.z, 0f, Time.fixedDeltaTime * angDamp); // roll
            rigid.angularVelocity = av;

            // 3) Uprighting torque (align car up to world up smoothly)
            Quaternion toUpright = Quaternion.FromToRotation(transform.up, Vector3.up);
            toUpright.ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 0.01f)
            {
                float torqueMag = Mathf.Deg2Rad * angle * uprightTorque;
                rigid.AddTorque(axis * torqueMag, ForceMode.Acceleration);
            }
        }

        // --- Drive / Brake Logic ---
        float motorTorque = 0f;
        float brakeTorque = 0f;

        if (gas && gas.IsEmpty)
        {
            motorTorque = 0f;
            brakeTorque = 100f;
        }
        else
        {
            if (vInput > 0f)
            {
                motorTorque = vInput * driveTorque;
                brakeTorque = 0f;
            }
            else if (vInput < 0f)
            {
                motorTorque = vInput * (driveTorque * 0.6f);
                brakeTorque = 0f;
            }
            else
            {
                motorTorque = 0f;
                brakeTorque = 100f;
            }
        }

        // --- Handbrake ---
        if (handbrake)
        {
            if (wheelRL) wheelRL.brakeTorque = handbrakeForce;
            if (wheelRR) wheelRR.brakeTorque = handbrakeForce;
        }
        else
        {
            if (wheelRL) wheelRL.brakeTorque = brakeTorque;
            if (wheelRR) wheelRR.brakeTorque = brakeTorque;
        }

        // --- Apply Torque (RWD bias) ---
        if (wheelFL) wheelFL.motorTorque = motorTorque * 0.5f;
        if (wheelFR) wheelFR.motorTorque = motorTorque * 0.5f;
        if (wheelRL) wheelRL.motorTorque = motorTorque;
        if (wheelRR) wheelRR.motorTorque = motorTorque;

        // --- Light brake for front wheels ---
        if (wheelFL) wheelFL.brakeTorque = brakeTorque;
        if (wheelFR) wheelFR.brakeTorque = brakeTorque;

        // --- Anti-roll and rollover dynamics ---
        ApplyAntiRoll(wheelFL, wheelFR);
        ApplyAntiRoll(wheelRL, wheelRR);
        ApplyRollForces();

        // --- Update Tachometer ---
        UpdateTachometer(currentSpeedKmh);
    }

    // --- Helper: absolute Z roll in degrees (0..180) ---
    float GetRollZAbs()
    {
        float z = Mathf.DeltaAngle(0f, transform.eulerAngles.z);
        return Mathf.Abs(z);
    }

    // --- Rollover Torque ---
    void ApplyRollForces()
    {
        if (rigid == null) return;

        Vector3 localVelocity = transform.InverseTransformDirection(rigid.linearVelocity);
        float rollForce = localVelocity.x * rigid.linearVelocity.magnitude * 0.05f;

        rigid.AddTorque(transform.forward * -rollForce, ForceMode.Acceleration);
    }

    // --- Anti-roll stability ---
    void ApplyAntiRoll(WheelCollider wheelL, WheelCollider wheelR)
    {
        if (wheelL == null || wheelR == null) return;

        WheelHit hit;
        float travelL = 1.0f;
        float travelR = 1.0f;

        bool groundedL = wheelL.GetGroundHit(out hit);
        if (groundedL)
            travelL = (-wheelL.transform.InverseTransformPoint(hit.point).y - wheelL.radius) / wheelL.suspensionDistance;

        bool groundedR = wheelR.GetGroundHit(out hit);
        if (groundedR)
            travelR = (-wheelR.transform.InverseTransformPoint(hit.point).y - wheelR.radius) / wheelR.suspensionDistance;

        float antiRollForce = (travelL - travelR) * 5000f; // tweak 2000–8000 for your car

        if (groundedL)
            rigid.AddForceAtPosition(wheelL.transform.up * -antiRollForce, wheelL.transform.position);
        if (groundedR)
            rigid.AddForceAtPosition(wheelR.transform.up * antiRollForce, wheelR.transform.position);
    }

    // --- Tachometer Needle ---
    void UpdateTachometer(float speedKmh)
    {
        if (needle == null) return;

        float t = Mathf.InverseLerp(0f, maxSpeedKmh, speedKmh);
        float targetAngle = Mathf.Lerp(minNeedleAngle, maxNeedleAngle, t);
        currentNeedleAngle = Mathf.LerpAngle(currentNeedleAngle, targetAngle, Time.deltaTime * 5f);
        needle.localRotation = Quaternion.Euler(0f, 0f, currentNeedleAngle);
    }

    // --- Physics-safe Reset ---
    void DoPhysicsSafeReset()
    {
        if (!rigid) return;

        rigid.linearVelocity = Vector3.zero;
        rigid.angularVelocity = Vector3.zero;

        if (wheelFL) { wheelFL.motorTorque = 0f; wheelFL.brakeTorque = brakeForce; wheelFL.steerAngle = 0f; }
        if (wheelFR) { wheelFR.motorTorque = 0f; wheelFR.brakeTorque = brakeForce; wheelFR.steerAngle = 0f; }
        if (wheelRL) { wheelRL.motorTorque = 0f; wheelRL.brakeTorque = brakeForce; }
        if (wheelRR) { wheelRR.motorTorque = 0f; wheelRR.brakeTorque = brakeForce; }

        bool wasKinematic = rigid.isKinematic;
        rigid.isKinematic = true;

        Vector3 targetPos = originalPosition;
        Quaternion targetRot = originalRotation;

        if (Physics.Raycast(originalPosition + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 50f, groundMask))
        {
            targetPos = hit.point + Vector3.up * resetHeightOffset;
            Vector3 forward = Vector3.ProjectOnPlane(originalRotation * Vector3.forward, hit.normal).normalized;
            if (forward.sqrMagnitude < 1e-4f)
                forward = Vector3.Cross(hit.normal, Vector3.right).normalized;

            targetRot = Quaternion.LookRotation(forward, hit.normal);
        }

        rigid.transform.SetPositionAndRotation(targetPos, targetRot);
        rigid.isKinematic = wasKinematic;
        rigid.Sleep();

        // ensure steering is fully centered after a reset
        currentSteerDeg = 0f;
        if (wheelFL) wheelFL.steerAngle = 0f;
        if (wheelFR) wheelFR.steerAngle = 0f;
    }
}
