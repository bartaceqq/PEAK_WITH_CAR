using UnityEngine;
using UnityEngine.UI;

public class CAR : MonoBehaviour
{
    public GameObject car;

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

    float hInput;
    float vInput;
    bool handbrake;
    private float currentScale = 0f;

    // Store original position and rotation for reset
    private Vector3 originalPosition;
    private Quaternion originalRotation;

    void Start()
    {
        if (rigid && COM)
            rigid.centerOfMass = transform.InverseTransformPoint(COM.position);

        if (highlightSquare != null)
            highlightSquare.enabled = false;

        // Save original car position and rotation
        originalPosition = car.transform.position;
        originalRotation = car.transform.rotation;
    }

    void Update()
    {
        if (Input.GetKey(resetKey))
        {
            if (!highlightSquare.enabled)
                highlightSquare.enabled = true;

            // grow toward max scale
            currentScale = Mathf.MoveTowards(currentScale, maxScale, Time.deltaTime * growSpeed);
            highlightSquare.rectTransform.localScale = Vector3.one * currentScale;

            // Reset car when highlight is fully grown
            if (currentScale >= maxScale)
            {
                ResetCar();
                currentScale = 0f;
            }
        }
        else
        {
            // shrink back
            currentScale = Mathf.MoveTowards(currentScale, 0f, Time.deltaTime * growSpeed);

            // hide when fully shrunk
            if (currentScale <= 0.01f && highlightSquare.enabled)
                highlightSquare.enabled = false;

            highlightSquare.rectTransform.localScale = Vector3.one * currentScale;
        }

        // --- Input ---
        hInput = Input.GetAxis("Horizontal");
        vInput = Input.GetAxis("Vertical");
        handbrake = Input.GetKey(KeyCode.Space);
    }

    void FixedUpdate()
    {
        // --- Extra Gravity ---
        rigid.AddForce(Vector3.down * extraGravityMultiplier * Physics.gravity.magnitude, ForceMode.Acceleration);

        // --- Steering ---
        float steer = hInput * steerAngle;
        wheelFL.steerAngle = steer;
        wheelFR.steerAngle = steer;

        // --- Current speed (in km/h) ---
        float currentSpeedKmh = rigid.linearVelocity.magnitude * 3.6f; // convert from m/s → km/h

        // --- Speed Limiter (cap at 100 km/h) ---
        if (currentSpeedKmh > maxSpeedKmh)
        {
            // Clamp the velocity vector to the max speed
            rigid.linearVelocity = rigid.linearVelocity.normalized * (maxSpeedKmh / 3.6f);
            currentSpeedKmh = maxSpeedKmh;
        }

        // --- Drive / Brake Logic ---
        float motorTorque = 0f;
        float brakeTorque = 0f;

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

        // --- Handbrake ---
        if (handbrake)
        {
            wheelRL.brakeTorque = handbrakeForce;
            wheelRR.brakeTorque = handbrakeForce;
        }
        else
        {
            wheelRL.brakeTorque = brakeTorque;
            wheelRR.brakeTorque = brakeTorque;
        }

        // --- Apply drive torque (RWD) ---
        wheelFL.motorTorque = motorTorque * 0.5f;
        wheelFR.motorTorque = motorTorque * 0.5f;
        wheelRL.motorTorque = motorTorque;
        wheelRR.motorTorque = motorTorque;

        // --- Light braking for front wheels too ---
        wheelFL.brakeTorque = brakeTorque;
        wheelFR.brakeTorque = brakeTorque;

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

    // --- Reset Car ---
    void ResetCar()
    {
        rigid.linearVelocity = Vector3.zero;
        rigid.angularVelocity = Vector3.zero;
        car.transform.position = originalPosition;
        car.transform.rotation = originalRotation;

        wheelFL.steerAngle = 0f;
        wheelFR.steerAngle = 0f;
        wheelRL.brakeTorque = 0f;
        wheelRR.brakeTorque = 0f;
    }
}
