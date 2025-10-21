using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CAR : MonoBehaviour
{
    [Header("Car Setup")]
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

    // Smooth steering & self-centering
    [Range(0.1f, 500f)] public float steerInRate = 120f;      
    [Range(0.1f, 500f)] public float steerReturnRate = 100f;  
    public KeyCode resetKey = KeyCode.R;

    public Image highlightSquare;
    public float growSpeed = 3f;
    public float maxScale = 1f;

    [Header("Speedometer / Tachometer")]
    public float maxSpeedKmh = 100f;
    public RectTransform needle;
    public float minNeedleAngle = -90f;
    public float maxNeedleAngle = 90f;
    public TMP_Text speedText;

    [Header("Reset Options")]
    public float resetHeightOffset = 0.4f;
    public LayerMask groundMask = ~0;

    [Header("Flip/Drift Guard")]
    public float minFlipDriftSpeedKmh = 60f;
    public float minRollAngleDeg = 40f;
    public float lateralDamp = 50f;
    public float angDamp = 10f;
    public float uprightTorque = 0.5f;

    [Header("Speeder Settings")]
    public int speeder = 0;
    public int maxSpeeder = 5;
    private bool canChangeSpeeder = true;  // 2-second cooldown

    float hInput;
    float vInput;
    bool handbrake;
    float currentScale = 0f;
    bool pendingReset;
    float currentSteerDeg = 0f;

    Vector3 originalPosition;
    Quaternion originalRotation;

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

        speeder = 0;
        UpdateSpeederUI();
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

        // --- Input ---
        hInput = Input.GetAxisRaw("Horizontal");
        vInput = Input.GetAxisRaw("Vertical");
        handbrake = Input.GetKey(KeyCode.Space);

        HandleSpeederInput();
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

        // --- Current Speed ---
        float currentSpeedKmh = rigid.linearVelocity.magnitude * 3.6f;

        // --- Max speed based on speeder ---
        float speederMaxSpeed = speeder * 40f;
        if (currentSpeedKmh > speederMaxSpeed)
        {
            rigid.linearVelocity = rigid.linearVelocity.normalized * (speederMaxSpeed / 3.6f);
            currentSpeedKmh = speederMaxSpeed;
        }

        // --- Steering ---
        float targetSteer = hInput * steerAngle;
        float rate = (Mathf.Abs(hInput) > 0.01f) ? steerInRate : steerReturnRate;
        currentSteerDeg = Mathf.MoveTowards(currentSteerDeg, targetSteer, rate * Time.fixedDeltaTime);

        if (Mathf.Abs(currentSteerDeg) < 0.05f && Mathf.Abs(hInput) < 0.01f)
            currentSteerDeg = 0f;

        if (wheelFL) wheelFL.steerAngle = currentSteerDeg;
        if (wheelFR) wheelFR.steerAngle = currentSteerDeg;

        // --- Flip/Drift Guard ---
        bool allowFlipDrift = (GetRollZAbs() >= minRollAngleDeg) && (currentSpeedKmh > minFlipDriftSpeedKmh);
        bool activeSteer = Mathf.Abs(hInput) > 0.15f;

        if (!allowFlipDrift && !activeSteer)
        {
            float speed01 = Mathf.InverseLerp(0f, 50f, currentSpeedKmh);
            float scaledLateralDamp = Mathf.Lerp(lateralDamp * 0.25f, lateralDamp, speed01);

            Vector3 localVel = transform.InverseTransformDirection(rigid.linearVelocity);
            localVel.x = Mathf.MoveTowards(localVel.x, 0f, Time.fixedDeltaTime * scaledLateralDamp);
            rigid.linearVelocity = transform.TransformDirection(localVel);

            Vector3 av = rigid.angularVelocity;
            av.x = Mathf.MoveTowards(av.x, 0f, Time.fixedDeltaTime * angDamp);
            av.z = Mathf.MoveTowards(av.z, 0f, Time.fixedDeltaTime * angDamp);
            rigid.angularVelocity = av;

            Quaternion toUpright = Quaternion.FromToRotation(transform.up, Vector3.up);
            toUpright.ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 0.01f)
            {
                float torqueMag = Mathf.Deg2Rad * angle * uprightTorque;
                rigid.AddTorque(axis * torqueMag, ForceMode.Acceleration);
            }
        }

        // --- Drive / Brake ---
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

        // --- Apply Torque ---
        if (wheelFL) wheelFL.motorTorque = motorTorque * 0.5f;
        if (wheelFR) wheelFR.motorTorque = motorTorque * 0.5f;
        if (wheelRL) wheelRL.motorTorque = motorTorque;
        if (wheelRR) wheelRR.motorTorque = motorTorque;

        if (wheelFL) wheelFL.brakeTorque = brakeTorque;
        if (wheelFR) wheelFR.brakeTorque = brakeTorque;

        // --- Anti-roll / Rollover ---
        ApplyAntiRoll(wheelFL, wheelFR);
        ApplyAntiRoll(wheelRL, wheelRR);
        ApplyRollForces();

        // --- Tachometer ---
        UpdateTachometer(currentSpeedKmh);
    }

    void HandleSpeederInput()
    {
        if (!canChangeSpeeder) return;

        if (Input.GetKey(KeyCode.LeftShift) && speeder < maxSpeeder)
        {
            speeder++;
            UpdateSpeederUI();
            StartCoroutine(SpeederCooldown());
        }
        else if (Input.GetKey(KeyCode.LeftControl) && speeder > 0)
        {
            speeder--;
            UpdateSpeederUI();
            StartCoroutine(SpeederCooldown());
        }
    }

    void UpdateSpeederUI()
    {
        if (speedText != null)
            speedText.SetText(speeder == 0 ? "N" : speeder.ToString());
    }

    IEnumerator SpeederCooldown()
    {
        canChangeSpeeder = false;
        yield return new WaitForSeconds(2f);
        canChangeSpeeder = true;
    }

    float GetRollZAbs()
    {
        float z = Mathf.DeltaAngle(0f, transform.eulerAngles.z);
        return Mathf.Abs(z);
    }

    void ApplyRollForces()
    {
        if (!rigid) return;

        Vector3 localVelocity = transform.InverseTransformDirection(rigid.linearVelocity);
        float rollForce = localVelocity.x * rigid.linearVelocity.magnitude * 0.05f;
        rigid.AddTorque(transform.forward * -rollForce, ForceMode.Acceleration);
    }

    void ApplyAntiRoll(WheelCollider wheelL, WheelCollider wheelR)
    {
        if (wheelL == null || wheelR == null) return;

        WheelHit hit;
        float travelL = 1f;
        float travelR = 1f;

        if (wheelL.GetGroundHit(out hit))
            travelL = (-wheelL.transform.InverseTransformPoint(hit.point).y - wheelL.radius) / wheelL.suspensionDistance;

        if (wheelR.GetGroundHit(out hit))
            travelR = (-wheelR.transform.InverseTransformPoint(hit.point).y - wheelR.radius) / wheelR.suspensionDistance;

        float antiRollForce = (travelL - travelR) * 5000f;

        if (wheelL.GetGroundHit(out _))
            rigid.AddForceAtPosition(wheelL.transform.up * -antiRollForce, wheelL.transform.position);
        if (wheelR.GetGroundHit(out _))
            rigid.AddForceAtPosition(wheelR.transform.up * antiRollForce, wheelR.transform.position);
    }

    void UpdateTachometer(float speedKmh)
    {
        if (needle == null) return;

        float t = Mathf.InverseLerp(0f, maxSpeeder * 40f, speedKmh);
        float targetAngle = Mathf.Lerp(minNeedleAngle, maxNeedleAngle, t);
        currentNeedleAngle = Mathf.LerpAngle(currentNeedleAngle, targetAngle, Time.deltaTime * 5f);
        needle.localRotation = Quaternion.Euler(0f, 0f, currentNeedleAngle);
    }

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

        currentSteerDeg = 0f;
        if (wheelFL) wheelFL.steerAngle = 0f;
        if (wheelFR) wheelFR.steerAngle = 0f;
    }
}
