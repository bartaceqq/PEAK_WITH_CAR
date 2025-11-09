using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody))]
public class TestScript : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float groundCheckDist = 0.3f;
    public LayerMask groundMask = ~0; // everything by default

    [Header("Mouse Look")]
    public float mouseSensitivity = 2f;
    public Transform cameraHolder; // assign your Camera's transform here in Inspector

    private Rigidbody rb;
    private float xLook; // camera pitch

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Lock body rotation so Rigidbody won't tip
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (cameraHolder == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null)
                cameraHolder = cam.transform;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            Cursor.lockState = CursorLockMode.None;  // Unlocks the mouse
            Cursor.visible = true;  
            SceneManager.LoadScene(4);
        }
        HandleMouseLook();
    }

    void FixedUpdate()
    {
        HandleMovement();
    }

    void HandleMouseLook()
    {
        // --- Mouse look ---
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Rotate body (yaw)
        Quaternion bodyRot = Quaternion.Euler(0f, mouseX, 0f);
        rb.MoveRotation(rb.rotation * bodyRot);

        // Rotate camera (pitch)
        xLook = Mathf.Clamp(xLook - mouseY, -80f, 80f);
        cameraHolder.localRotation = Quaternion.Euler(xLook, 0f, 0f);
    }

    void HandleMovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 inputDir = (transform.right * h + transform.forward * v).normalized;

        // Project onto ground (so movement follows slope)
        Vector3 moveDir = inputDir;
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 1f + groundCheckDist, groundMask))
            moveDir = Vector3.ProjectOnPlane(inputDir, hit.normal).normalized;

        Vector3 targetPos = rb.position + moveDir * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(targetPos);
        rb.angularVelocity = Vector3.zero;
    }
}
