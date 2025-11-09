using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("UI & Camera")]
    public GameObject CarUI;
    public Camera CarCamera;
    public GameObject PlayerCanvas;

    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float runSpeed = 9f;
    public float jumpHeight = 2f;
    public float gravity = -9.81f;

    [Header("Mouse Settings")]
    public float mouseSensitivity = 2f;
    public Transform cameraTransform; // assign your camera here

    [Header("Interaction Settings")]
    public Inventory inventory;
    public Cursor_Manager cursor_manager;
    public float rayDistance = 10f;
    public LayerMask itemMask; // assign the "items" layer
    private Collectable_Item collectable_Item;
    public bool lookingAtItem = false;
    public bool grabbing = false;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private float pitch = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        // Lock the cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleMovement();
        HandleMouseLook();
        HandleItemInteraction();
    }

    void HandleMovement()
    {
        isGrounded = controller.isGrounded;

        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        Vector3 move = transform.right * moveX + transform.forward * moveZ;

        float speed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;

        controller.Move(move * speed * Time.deltaTime);

        // Jumping
        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        if (Input.GetButtonDown("Jump") && isGrounded)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        // Gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Rotate the body left/right
        transform.Rotate(Vector3.up * mouseX);

        // Rotate the camera up/down
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -80f, 80f);

        cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    void HandleItemInteraction()
    {
        // Cursor feedback
        if (grabbing)
            cursor_manager.grab();
        else
            cursor_manager.normal();

        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        RaycastHit hit;

        // Check for items
        if (Physics.Raycast(ray, out hit, rayDistance))
        {
            Collectable_Item hitItem = hit.collider.GetComponent<Collectable_Item>();
            if (hitItem != null)
            {
                grabbing = true;

                // Check if parent drawer is open
                bool canAccess = true;
                Transform parent = hit.collider.transform.parent;
                if (parent != null)
                {
                    PullOut pullOut = parent.GetComponent<PullOut>();
                    if (pullOut != null)
                        canAccess = pullOut.isout;
                }

                if (canAccess)
                {
                    // Unhighlight previous item
                    if (collectable_Item != null && collectable_Item != hitItem)
                        collectable_Item.Highlight(false);

                    collectable_Item = hitItem;
                    collectable_Item.Highlight(true);
                    lookingAtItem = true;

                    // Pickup
                    if (Input.GetKeyDown(KeyCode.E) && !collectable_Item.collected)
                    {
                        inventory.AddItem(collectable_Item);
                        collectable_Item.PickUp();
                    }
                }
            }
        }
        else
        {
            lookingAtItem = false;
            if (collectable_Item != null)
            {
                collectable_Item.Highlight(false);
                collectable_Item = null;
            }
            grabbing = false;
        }

        // Optional: add interactions for drawers or monitors like in your Looker script
    }

    public void TurnOffCarProperities()
    {
        CarUI.SetActive(false);
        CarCamera.enabled = false;
        PlayerCanvas.SetActive(true);
    }
}
    