using UnityEngine;
using UnityEngine.UI;   // for Image

public class Car_Controller : MonoBehaviour
{
    [Header("Wheel Colliders")]
    public WheelCollider frontLeftWheel;
    public WheelCollider frontRightWheel;
    public WheelCollider rearLeftWheel;
    public WheelCollider rearRightWheel;

    [Header("Car Settings")]
    public float motorPower = 150f;
    public float brakePower = 300f;
    public float maxSteerAngle = 30f;

    [Header("Physics")]
    public Rigidbody rigid;

    [Header("Fuel (optional)")]
    public GasScript gas;

    [Header("Speeder (optional)")]
    public Speeder speeder;

    [Header("Tachometer (needle only)")]
    public RectTransform needle;
    [Tooltip("Angle (deg) when speed = 0")]
    public float startAngle = 180f;
    [Tooltip("Angle (deg) when speed = gaugeMaxKmh")]
    public float endAngle = 0f;
    public float gaugeMaxKmh = 200f;
    public float needleSmoothing = 6f;

    // -------- Auto Flip --------
    [Header("Auto Flip")]
    public KeyCode flipKey = KeyCode.R;         // manual upright
    public float minFlipAngle = 60f;
    public float flipCheckDelay = 2.0f;
    public float liftHeight = 1.0f;
    public LayerMask groundMask = ~0;

    // -------- Hold-to-Reset UI (Highlight Square) --------
    [Header("Hold-to-Reset UI")]
    public KeyCode resetKey = KeyCode.R;        // hold to teleport back to spawn
    public Image highlightSquare;               // UI image to scale
    public float growSpeed = 3f;                // fill speed
    public float maxScale = 1f;                 // target scale when filled

    // -------- Reset-to-Spawn options --------
    [Header("Reset To Spawn")]
    public float resetHeightOffset = 0.4f;      // how high above ground to place at spawn

    // internals
    float _needleAngle;
    float _flipTimer;
    Vector3 originalPosition;
    Quaternion originalRotation;
    float currentScale = 0f;                    // UI fill
    bool pendingReset = false;                  // trigger flag

    void Awake()
    {
        if (!rigid) rigid = GetComponent<Rigidbody>();
        originalPosition = transform.position;
        originalRotation = transform.rotation;

        if (highlightSquare) {
            highlightSquare.enabled = false;
            highlightSquare.rectTransform.localScale = Vector3.zero;
        }
    }

    void Update()
    {
        // --- Hold-to-Reset highlight square ---
        if (Input.GetKey(resetKey))
        {
            if (highlightSquare && !highlightSquare.enabled) highlightSquare.enabled = true;

            currentScale = Mathf.MoveTowards(currentScale, maxScale, Time.deltaTime * growSpeed);
            if (highlightSquare) highlightSquare.rectTransform.localScale = Vector3.one * currentScale;

            if (currentScale >= maxScale)
            {
                pendingReset = true;     // execute in FixedUpdate (physics-safe)
                currentScale = 0f;
            }
        }
        else
        {
            currentScale = Mathf.MoveTowards(currentScale, 0f, Time.deltaTime * growSpeed);
            if (highlightSquare)
            {
                if (currentScale <= 0.01f && highlightSquare.enabled) highlightSquare.enabled = false;
                highlightSquare.rectTransform.localScale = Vector3.one * currentScale;
            }
        }
    }

    void FixedUpdate()
    {
        if (!rigid) return;

        // --- consume pending hold-to-reset (spawn) ---
        if (pendingReset)
        {
            DoPhysicsSafeReset();
            pendingReset = false;
        }

        float throttle = Input.GetAxis("Vertical");     // W/S
        float steer    = Input.GetAxis("Horizontal");   // A/D

        // --- Steering (front only)
        float steerAngle = steer * maxSteerAngle;
        if (frontLeftWheel)  frontLeftWheel.steerAngle  = steerAngle;
        if (frontRightWheel) frontRightWheel.steerAngle = steerAngle;

        // --- Fuel check
        bool outOfFuel = gas && gas.IsEmpty;

        // --- Gear / Neutral (from Speeder)
        int currentGear = speeder ? speeder.speeder : 1;    // 0 = N, 1..5 = kvalty
        bool isNeutral  = speeder && currentGear == 0;

        // --- Motor torque (RWD) – v neutrálu neposílat nic
        float motor = (!outOfFuel && !isNeutral) ? (throttle * motorPower) : 0f;
        if (rearLeftWheel)  rearLeftWheel.motorTorque  = rearLeftWheel.isGrounded  ? motor : 0f;
        if (rearRightWheel) rearRightWheel.motorTorque = rearRightWheel.isGrounded ? motor : 0f;
        if (frontLeftWheel)  frontLeftWheel.motorTorque  = 0f;
        if (frontRightWheel) frontRightWheel.motorTorque = 0f;

        // --- Brakes (Space) + light idle brake (also when no fuel / neutral)
        float brake = Input.GetKey(KeyCode.Space) ? brakePower : 0f;
        if (!Input.GetKey(KeyCode.Space) && (Mathf.Abs(throttle) < 0.01f || outOfFuel || isNeutral))
            brake = 100f;

        if (frontLeftWheel)  frontLeftWheel.brakeTorque  = brake;
        if (frontRightWheel) frontRightWheel.brakeTorque = brake;
        if (rearLeftWheel)   rearLeftWheel.brakeTorque   = brake;
        if (rearRightWheel)  rearRightWheel.brakeTorque  = brake;

        // --- Speed limiter from Speeder (0 = no limit)
        float speedKmh = rigid.linearVelocity.magnitude * 3.6f;
        int limit = speeder ? speeder.maxSpeed : 0;
        if (limit > 0 && speedKmh > limit)
        {
            rigid.linearVelocity = rigid.linearVelocity.normalized * (limit / 3.6f);
            speedKmh = limit;
        }

        // --- Tachometer
        UpdateNeedle(speedKmh);

        // --- Auto flip + manual flip
        HandleAutoFlip();
        if (Input.GetKey(flipKey)) FixRotation();
    }

    void UpdateNeedle(float speedKmh)
    {
        if (!needle || gaugeMaxKmh <= 0f) return;

        float t = Mathf.Clamp01(speedKmh / gaugeMaxKmh);
        float target = Mathf.Lerp(startAngle, endAngle, t);
        _needleAngle = Mathf.LerpAngle(_needleAngle, target, Time.deltaTime * needleSmoothing);
        needle.localRotation = Quaternion.Euler(0f, 0f, _needleAngle);
    }

    // ---------------- Auto Flip ----------------
    void HandleAutoFlip()
    {
        float upDot = Vector3.Dot(transform.up, Vector3.up);
        bool tipped = upDot < Mathf.Cos(minFlipAngle * Mathf.Deg2Rad);

        if (tipped)
        {
            _flipTimer += Time.fixedDeltaTime;
            if (_flipTimer >= flipCheckDelay)
            {
                FixRotation();
                _flipTimer = 0f;
            }
        }
        else _flipTimer = 0f;
    }

    public void FixRotation()
    {
        if (!rigid) return;

        rigid.linearVelocity = Vector3.zero;
        rigid.angularVelocity = Vector3.zero;

        Vector3 pos = transform.position + Vector3.up * liftHeight;
        Quaternion rot;

        if (Physics.Raycast(transform.position + Vector3.up * 5f, Vector3.down,
            out RaycastHit hit, 50f, groundMask))
        {
            pos = hit.point + hit.normal * liftHeight;
            Vector3 fwd = Vector3.ProjectOnPlane(transform.forward, hit.normal).normalized;
            if (fwd.sqrMagnitude < 1e-4f) fwd = Vector3.ProjectOnPlane(transform.right, hit.normal).normalized;
            rot = Quaternion.LookRotation(fwd, hit.normal);
        }
        else
        {
            Vector3 fwd = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            if (fwd.sqrMagnitude < 1e-4f) fwd = Vector3.forward;
            rot = Quaternion.LookRotation(fwd, Vector3.up);
        }

        bool wasKin = rigid.isKinematic;
        rigid.isKinematic = true;
        rigid.MovePosition(pos);
        rigid.MoveRotation(rot);
        rigid.isKinematic = wasKin;
        rigid.Sleep();

        ResetWheels();
    }

    void ResetWheels()
    {
        if (frontLeftWheel)  { frontLeftWheel.motorTorque = 0f;  frontLeftWheel.brakeTorque = 50f; frontLeftWheel.steerAngle = 0f; }
        if (frontRightWheel) { frontRightWheel.motorTorque = 0f; frontRightWheel.brakeTorque = 50f; frontRightWheel.steerAngle = 0f; }
        if (rearLeftWheel)   { rearLeftWheel.motorTorque = 0f;   rearLeftWheel.brakeTorque = 50f; }
        if (rearRightWheel)  { rearRightWheel.motorTorque = 0f;  rearRightWheel.brakeTorque = 50f; }
    }

    // ---------------- Teleport to Spawn (for hold-to-reset) ----------------
    public void DoPhysicsSafeReset()
    {
        if (!rigid) return;

        rigid.linearVelocity = Vector3.zero;
        rigid.angularVelocity = Vector3.zero;

        if (frontLeftWheel)  { frontLeftWheel.motorTorque = 0f;  frontLeftWheel.brakeTorque = brakePower; frontLeftWheel.steerAngle = 0f; }
        if (frontRightWheel) { frontRightWheel.motorTorque = 0f; frontRightWheel.brakeTorque = brakePower; frontRightWheel.steerAngle = 0f; }
        if (rearLeftWheel)   { rearLeftWheel.motorTorque = 0f;   rearLeftWheel.brakeTorque = brakePower; }
        if (rearRightWheel)  { rearRightWheel.motorTorque = 0f;  rearRightWheel.brakeTorque = brakePower; }

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

        if (frontLeftWheel)  frontLeftWheel.steerAngle = 0f;
        if (frontRightWheel) frontRightWheel.steerAngle = 0f;
    }

    // Optional: call from UI Button to reset instantly
    public void ResetToSpawnNow() => DoPhysicsSafeReset();
}
