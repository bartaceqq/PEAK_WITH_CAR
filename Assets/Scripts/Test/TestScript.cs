using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TestScript : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float mouseSensitivity = 2f;
    public float groundCheckDist = 0.3f;
    public LayerMask groundMask = ~0; // everything by default

    Rigidbody rb;
    Camera cam;
    float xLook; // camera pitch

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        cam = GetComponentInChildren<Camera>();

        // Keep capsule upright
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // --- Mouse look ---
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Yaw rotates the body (only around Y)
        transform.Rotate(Vector3.up * mouseX);

        // Pitch rotates only the camera
        xLook = Mathf.Clamp(xLook - mouseY, -80f, 80f);
        cam.transform.localRotation = Quaternion.Euler(xLook, 0f, 0f);
    }

    void FixedUpdate()
    {
        // --- Movement without tipping ---
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 inputDir = (transform.right * h + transform.forward * v).normalized;

        // Project onto ground to avoid pushing the capsule over on slopes
        Vector3 moveDir = inputDir;
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 1f + groundCheckDist, groundMask))
            moveDir = Vector3.ProjectOnPlane(inputDir, hit.normal).normalized;

        Vector3 targetPos = rb.position + moveDir * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(targetPos);

        // kill accidental spin just in case
        rb.angularVelocity = Vector3.zero;
    }
}
