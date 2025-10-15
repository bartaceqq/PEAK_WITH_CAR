using UnityEngine;
using UnityEngine.UI;

public class CAR : MonoBehaviour
{
    public GameObject car;
    public GasScript gas;   // drag your GasScript here in Inspector

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
    public KeyCode resetKey = KeyCode.R;

    public Image highlightSquare;       // assign the child Image
    public float growSpeed = 3f;        // how fast the square grows
    public float maxScale = 1f;

    [Header("Speedometer / Tachometer")]
    public float maxSpeedKmh = 100f;     // Maximum speed
    public RectTransform needle;         // Assign the needle Image
    public float minNeedleAngle = -90f;  // Angle for 0 km/h
    public float maxNeedleAngle = 90f;   // Angle for 100 km/h

    [Header("Reset Options")]
    [Tooltip("How high to place the car above ground when resetting.")]
    public float resetHeightOffset = 0.4f;
    [Tooltip("Layers considered as ground when snapping on reset.")]
    public LayerMask groundMask = ~0;

    float hInput;
    float vInput;
    bool handbrake;
    private float currentScale = 0f;

    // Store original position and rotation for reset
    private Vector3 originalPosition;
    private Quaternion originalRotation;

    // Defer reset to FixedUpdate for physics-safe teleport
    private bool pendingReset;

    void Start()
    {
        if (rigid && COM)
            rigid.centerOfMass = transform.InverseTransformPoint(COM.position);

        if (highlightSquare != null)
            highlightSquare.enabled = false;

        // Save original car position and rotation (use the Rigidbody's transform)
        if (rigid != null)
        {
            originalPosition = rigid.transform.position;
            originalRotation = rigid.transform.rotation;
        }
        else if (car != null)
        {
            originalPosition = car.transform.position;
            originalRotation = car.transform.rotation;
        }
        else
        {
            originalPosition = transform.position;
            originalRotation = transform.rotation;
        }
    }

    void Update()
    {
        // --- Reset hold UI + trigger ---
        if (Input.GetKey(resetKey))
        {
            if (highlightSquare && !highlightSquare.enabled)
                highlightSquare.enabled = true;

            // grow toward max scale
            currentScale = Mathf.MoveTowards(currentScale, maxScale, Time.deltaTime * growSpeed);
            if (highlightSquare)
                highlightSquare.rectTransform.localScale = Vector3.one * currentScale;

            // Reset car when highlight is fully grown
            if (currentScale >= maxScale)
            {
                pendingReset = true;     // defer to FixedUpdate
                currentScale = 0f;
            }
        }
        else
        {
            // shrink back
            currentScale = Mathf.MoveTowards(currentScale, 0f, Time.deltaTime * growSpeed);

            // hide when fully shrunk
            if (highlightSquare)
            {
                if (currentScale <= 0.01f && highlightSquare.enabled)
                    highlightSquare.enabled = false;

                highlightSquare.rectTransform.localScale = Vector3.one * currentScale;
            }
        }

        // --- Input ---
        hInput = Input.GetAxis("Horizontal");
        vInput = Input.GetAxis("Vertical");
        handbrake = Input.GetKey(KeyCode.Space);
    }

  void FixedUpdate()
{
    // Perform physics-safe reset at the start of the physics step
    if (pendingReset)
    {
        DoPhysicsSafeReset();
        pendingReset = false;
    }

    if (!rigid) return;

    // --- Extra Gravity ---
    rigid.AddForce(Vector3.down * extraGravityMultiplier * Physics.gravity.magnitude, ForceMode.Acceleration);

    // --- Steering ---
    float steer = hInput * steerAngle;
    if (wheelFL) wheelFL.steerAngle = steer;
    if (wheelFR) wheelFR.steerAngle = steer;

    // --- Current speed (in km/h) ---
    float currentSpeedKmh = rigid.linearVelocity.magnitude * 3.6f; // convert from m/s → km/h

    // --- Speed Limiter (cap at maxSpeedKmh) ---
    if (currentSpeedKmh > maxSpeedKmh)
    {
        rigid.linearVelocity = rigid.linearVelocity.normalized * (maxSpeedKmh / 3.6f);
        currentSpeedKmh = maxSpeedKmh;
    }

    // --- Drive / Brake Logic ---
    float motorTorque = 0f;
    float brakeTorque = 0f;

    if (gas && gas.IsEmpty)
    {
        // no fuel -> no drive torque
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

    // --- Apply drive torque (RWD bias) ---
    if (wheelFL) wheelFL.motorTorque = motorTorque * 0.5f;
    if (wheelFR) wheelFR.motorTorque = motorTorque * 0.5f;
    if (wheelRL) wheelRL.motorTorque = motorTorque;
    if (wheelRR) wheelRR.motorTorque = motorTorque;

    // --- Light braking for front wheels too ---
    if (wheelFL) wheelFL.brakeTorque = brakeTorque;
    if (wheelFR) wheelFR.brakeTorque = brakeTorque;

    // --- Update tachometer needle ---
    UpdateTachometer(currentSpeedKmh);
}


    // --- Tachometer Needle Rotation ---
    void UpdateTachometer(float speedKmh)
    {
        if (needle == null) return;

        // Normalize speed between 0 and 1
        float t = Mathf.InverseLerp(0f, maxSpeedKmh, speedKmh);

        // Calculate rotation angle
        float targetAngle = Mathf.Lerp(minNeedleAngle, maxNeedleAngle, t);

        // Smooth rotation for realism
        currentNeedleAngle = Mathf.LerpAngle(currentNeedleAngle, targetAngle, Time.deltaTime * 5f);

        // Apply rotation (pivot should be bottom-center)
        needle.localRotation = Quaternion.Euler(0f, 0f, currentNeedleAngle);
    }

    // --- Physics-safe Reset ---
    void DoPhysicsSafeReset()
    {
        if (!rigid) return;

        // 1) Kill motion and stop wheel forces this frame
        rigid.linearVelocity = Vector3.zero;
        rigid.angularVelocity = Vector3.zero;

        if (wheelFL) { wheelFL.motorTorque = 0f; wheelFL.brakeTorque = brakeForce; wheelFL.steerAngle = 0f; }
        if (wheelFR) { wheelFR.motorTorque = 0f; wheelFR.brakeTorque = brakeForce; wheelFR.steerAngle = 0f; }
        if (wheelRL) { wheelRL.motorTorque = 0f; wheelRL.brakeTorque = brakeForce; }
        if (wheelRR) { wheelRR.motorTorque = 0f; wheelRR.brakeTorque = brakeForce; }

        // 2) Temporarily pause physics on the body
        bool wasKinematic = rigid.isKinematic;
        rigid.isKinematic = true;

        // 3) Find a safe pose near the original position, snapped to ground if possible
        Vector3 targetPos = originalPosition;
        Quaternion targetRot = originalRotation;

        if (Physics.Raycast(originalPosition + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 50f, groundMask))
        {
            targetPos = hit.point + Vector3.up * resetHeightOffset;

            // upright with ground normal, preserve heading from originalRotation
            Vector3 forward = Vector3.ProjectOnPlane(originalRotation * Vector3.forward, hit.normal).normalized;
            if (forward.sqrMagnitude < 1e-4f)
                forward = Vector3.Cross(hit.normal, Vector3.right).normalized;

            targetRot = Quaternion.LookRotation(forward, hit.normal);
        }

        // 4) Move the rigidbody transform (avoid moving a different child)
        rigid.transform.SetPositionAndRotation(targetPos, targetRot);

        // 5) Resume physics cleanly and sleep
        rigid.isKinematic = wasKinematic;
        rigid.Sleep();
    }
}
