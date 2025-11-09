using UnityEngine;
using UnityEngine.UIElements;
using Image = UnityEngine.UI.Image;

public class Car_Controller : MonoBehaviour
{
    
    [SerializeField] private GameObject Car;
    [Header("Wheel Colliders")]
    public WheelCollider frontLeftWheel;
    public WheelCollider frontRightWheel;
    public WheelCollider rearLeftWheel;
    public WheelCollider rearRightWheel;
    
    [Header("Player Movement")]
    public GameObject player;
    
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
    public float startAngle = 180f;
    public float endAngle = 0f;
    public float gaugeMaxKmh = 200f;
    public float needleSmoothing = 6f;

    [Header("Reset To Spawn (Hold R)")]
    public KeyCode resetKey = KeyCode.R;
    public Image highlightSquare;          // optional UI feedback
    public float growSpeed = 3f;
    public float maxScale = 1f;
    public float resetHeightOffset = 0.4f;
    public LayerMask groundMask = ~0;

    [Header("Stationary Reset (F)")]
    public KeyCode stationaryResetKey = KeyCode.F;
    public float stationarySpeedKmh = 2f;
    public float liftOnReset = 0.6f;

    // --- internals ---
    float _needleAngle;
    Vector3 originalPosition;
    Quaternion originalRotation;

    bool _wantStationaryReset;
    bool _pendingSpawnReset;
    float _resetHoldProgress;

    void Awake()
    {
        if (!rigid) rigid = GetComponent<Rigidbody>();
        originalPosition = transform.position;
        originalRotation = transform.rotation;

        if (highlightSquare)
        {
            highlightSquare.enabled = false;
            highlightSquare.rectTransform.localScale = Vector3.zero;
        }
    }

    void Update()
    
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log("pressed T");
            ExitCar();
        }
        float speedKmh = rigid.linearVelocity.magnitude * 3.6f;
        Debug.Log(speedKmh);
        if (Input.GetKey(KeyCode.F))
        {
         
            if (speedKmh < 10)
            {
                
                Vector3 position = Car.transform.position;
                position.y +=2f;
                Car.transform.position = position;
                Car.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            } 
        }

        // handle R hold (UI + progress)
        if (Input.GetKey(resetKey))
        {
            if (highlightSquare && !highlightSquare.enabled)
                highlightSquare.enabled = true;

            _resetHoldProgress = Mathf.MoveTowards(_resetHoldProgress, maxScale, Time.deltaTime * growSpeed);
            if (highlightSquare)
                highlightSquare.rectTransform.localScale = Vector3.one * _resetHoldProgress;

            if (_resetHoldProgress >= maxScale)
            {
                _pendingSpawnReset = true;
                _resetHoldProgress = 0f;
            }
        }
        else
        {
            _resetHoldProgress = Mathf.MoveTowards(_resetHoldProgress, 0f, Time.deltaTime * growSpeed);
            if (highlightSquare)
            {
                highlightSquare.rectTransform.localScale = Vector3.one * _resetHoldProgress;
                if (_resetHoldProgress <= 0.01f)
                    highlightSquare.enabled = false;
            }
        }
    }

    void FixedUpdate()
    {
     
        if (!rigid) return;

        float throttle = Input.GetAxis("Vertical");
        float steer = Input.GetAxis("Horizontal");

        float steerAngle = steer * maxSteerAngle;
        if (frontLeftWheel) frontLeftWheel.steerAngle = steerAngle;
        if (frontRightWheel) frontRightWheel.steerAngle = steerAngle;

        bool outOfFuel = gas && gas.IsEmpty;
        int currentGear = speeder ? speeder.speeder : 1;
        bool isNeutral = speeder && currentGear == 0;

        float motor = (!outOfFuel && !isNeutral) ? (throttle * motorPower) : 0f;
        if (rearLeftWheel) rearLeftWheel.motorTorque = rearLeftWheel.isGrounded ? motor : 0f;
        if (rearRightWheel) rearRightWheel.motorTorque = rearRightWheel.isGrounded ? motor : 0f;
        if (frontLeftWheel) frontLeftWheel.motorTorque = 0f;
        if (frontRightWheel) frontRightWheel.motorTorque = 0f;

        float brake = Input.GetKey(KeyCode.Space) ? brakePower : 0f;
        if (!Input.GetKey(KeyCode.Space) && (Mathf.Abs(throttle) < 0.01f || outOfFuel || isNeutral))
            brake = 100f;

        if (frontLeftWheel) frontLeftWheel.brakeTorque = brake;
        if (frontRightWheel) frontRightWheel.brakeTorque = brake;
        if (rearLeftWheel) rearLeftWheel.brakeTorque = brake;
        if (rearRightWheel) rearRightWheel.brakeTorque = brake;

        float speedKmh = rigid.linearVelocity.magnitude * 3.6f;
        int limit = speeder ? speeder.maxSpeed : 0;
        if (limit > 0 && speedKmh > limit)
        {
            rigid.linearVelocity = rigid.linearVelocity.normalized * (limit / 3.6f);
            speedKmh = limit;
        }

        UpdateNeedle(speedKmh);

        // consume F reset
        if (_wantStationaryReset)
        {
            _wantStationaryReset = false;
            if (speedKmh <= stationarySpeedKmh)
                StationaryReset();
        }

        // consume R full spawn reset
        if (_pendingSpawnReset)
        {
            _pendingSpawnReset = false;
            DoPhysicsSafeReset();
        }
    }

    void UpdateNeedle(float speedKmh)
    {
        if (!needle || gaugeMaxKmh <= 0f) return;
        float t = Mathf.Clamp01(speedKmh / gaugeMaxKmh);
        float target = Mathf.Lerp(startAngle, endAngle, t);
        _needleAngle = Mathf.LerpAngle(_needleAngle, target, Time.deltaTime * needleSmoothing);
        needle.localRotation = Quaternion.Euler(0f, 0f, _needleAngle);
    }

    // --- F stationary upright ---
    void StationaryReset()
    {
        rigid.linearVelocity = Vector3.zero;
        rigid.angularVelocity = Vector3.zero;

        Vector3 newPos = rigid.position + Vector3.up * liftOnReset;
        Quaternion newRot = originalRotation;

        bool wasKin = rigid.isKinematic;
        rigid.isKinematic = true;
        rigid.MovePosition(newPos);
        rigid.MoveRotation(newRot);
        rigid.isKinematic = wasKin;
        rigid.Sleep();
    }

    // --- R hold full respawn ---
    public void DoPhysicsSafeReset()
    {
        rigid.linearVelocity = Vector3.zero;
        rigid.angularVelocity = Vector3.zero;

        bool wasKin = rigid.isKinematic;
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
        rigid.isKinematic = wasKin;
        rigid.Sleep();
    }

    public void ExitCar()
    {
     
            rigid.linearVelocity = Vector3.zero;
            rigid.angularVelocity = Vector3.zero;
        

       
            speeder.speeder = 0;
            speeder.enabled = false;
        
            gas.enabled = false;

        // Activate player
       
            player.SetActive(true);
            PlayerMovement pm = player.GetComponent<PlayerMovement>();
            if (pm != null)
                pm.TurnOffCarProperities();
        
    }

    public void ResetToSpawnNow() => DoPhysicsSafeReset();
}
