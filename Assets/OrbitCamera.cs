using UnityEngine;

public class OrbitCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;          // assign the car's CameraPivot

    [Header("Orbit")]
    public float distance = 6f;
    public float minDistance = 2f;
    public float maxDistance = 12f;
    public float yaw = 0f;            // horizontal angle (around Y)
    public float pitch = 15f;         // vertical angle (around X)
    public float minPitch = -10f;
    public float maxPitch = 70f;

    [Header("Sensitivity")]
    public float mouseXSens = 180f;   // deg/sec
    public float mouseYSens = 180f;   // deg/sec
    public float zoomSpeed  = 6f;     // units per scroll step

    [Header("Smoothing")]
    public float followDamp = 20f;    // position smoothing

    [Header("Collision (optional)")]
    public LayerMask clipMask = ~0;   // what blocks the camera
    public float clipBuffer = 0.2f;

    void Start()
    {
        // optional: lock cursor; press Esc to toggle
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // mouse look (OLD Input Manager)
        float mx = Input.GetAxis("Mouse X");
        float my = Input.GetAxis("Mouse Y");
        yaw   += mx * mouseXSens * Time.deltaTime;
        pitch -= my * mouseYSens * Time.deltaTime;
        pitch  = Mathf.Clamp(pitch, minPitch, maxPitch);

        // zoom
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > Mathf.Epsilon)
            distance = Mathf.Clamp(distance - scroll * zoomSpeed, minDistance, maxDistance);

        // toggle cursor
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

        // desired camera position in world space
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredPos = target.position - rot * Vector3.forward * distance;

        // simple collision: pull camera closer if obstructed
        if (Physics.Linecast(target.position, desiredPos, out var hit, clipMask, QueryTriggerInteraction.Ignore))
        {
            desiredPos = hit.point + hit.normal * clipBuffer;
        }

        // smooth follow & set rotation
        transform.position = Vector3.Lerp(transform.position, desiredPos, 1f - Mathf.Exp(-followDamp * Time.deltaTime));
        transform.rotation = rot;
    }
}
