    using System.Collections.Generic;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public int holding_item_id = 0;
        public List<Collectable_Item> items = new List<Collectable_Item>();
        [Header("Shooting")]
        public Camera playerCamera;        // Assign MainCamera in Inspector
        public GameObject projectilePrefab; // Assign your projectile prefab
        public Transform firePoint;         // Optional: empty transform at gun barrel
        public float shootForce = 30f;
        public Animator animator;

        [Header("Movement")]
        public float moveSpeed = 5f;
        public float runSpeed = 9f;
        public float gravity = -9.81f;
        public float jumpHeight = 1.5f;

        [Header("Mouse Look")]
        public float mouseSensitivity = 2f;
        public Transform cameraHolder;     // Assign Camera here

        [Header("Car / UI")]
        public Car_Controller CarController;
        public GameObject CarUI;
        public Camera CarCamera;
        public GameObject PlayerCanvas;
        public bool switched = false;

        [Header("Item Interaction")]
        public Inventory inventory;
        public Cursor_Manager cursor_manager;
        public float rayDistance = 10f;
        public LayerMask itemMask;          // assign "items" layer
        private Collectable_Item collectable_Item;
        public bool lookingAtItem = false;
        public bool grabbing = false;

        public CharacterController controller;
        private Vector3 velocity;
        private float pitch = 0f;
        private bool isGrounded;

        void Start()
        {
            TransferItem_Handler transfer_handler = new TransferItem_Handler();
            transfer_handler.items = this.items;
            transfer_handler.TransferItem();
            

            // Lock cursor
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
            HandleMouseLook();
            HandleMovement();
            HandleItemInteraction();
            HandleShooting();
            HandleCarSwitch();
            if (Input.GetKeyDown(KeyCode.M))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                SceneManager.LoadScene(4);
            }
            if (Input.GetKeyDown(KeyCode.L))
            {
                foreach (KeyValuePair<int, Collectable_Item> entry in StaticData.item_map)
                {
                    Debug.Log(entry.Key + " → " + entry.Value);
                }
            }
        }

        void HandleMovement()
        {
            isGrounded = controller.isGrounded;

            if (isGrounded && velocity.y < 0)
                velocity.y = -2f;

            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            Vector3 move = transform.right * h + transform.forward * v;

            float speed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : moveSpeed;
            controller.Move(move * speed * Time.deltaTime);

            // Jump
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

            transform.Rotate(Vector3.up * mouseX);

            pitch -= mouseY;
            pitch = Mathf.Clamp(pitch, -80f, 80f);
            if (cameraHolder != null)
                cameraHolder.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        void HandleShooting()
        {
            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                if (animator != null) animator.SetTrigger("Shoot");
                Shoot();
            }
        }

        void Shoot()
        {
            if (!playerCamera ) return;
            if (holding_item_id != 1) return;
            Vector3 direction = playerCamera.transform.forward;
            Vector3 spawnPos = firePoint ? firePoint.position : playerCamera.transform.position;
            
            GameObject bullet = Instantiate(projectilePrefab, spawnPos, Quaternion.LookRotation(direction));
            Rigidbody rb = bullet.GetComponent<Rigidbody>();
            if (rb != null)
                rb.linearVelocity = direction * shootForce;
        }

        public void HandleCarSwitch()
        {
            if (Input.GetKeyDown(KeyCode.T))
            {
                if (!switched)
                {
                    CarUI.SetActive(false);
                    CarCamera.enabled = false;
                    PlayerCanvas.SetActive(true);
                    switched = true;
                }
                else
                {
                    switched = false;
                    CarUI.SetActive(true);
                    CarCamera.enabled = true;
                    PlayerCanvas.SetActive(false);
                }

                if (CarController != null) CarController.EnterCar();
            }
        }

        void HandleItemInteraction()
        {
            if (cursor_manager != null)
                if (cursor_manager != null)
                {
                    if (grabbing)
                        cursor_manager.grab();
                    else
                        cursor_manager.normal();
                }


            Ray ray = new Ray(cameraHolder.position, cameraHolder.forward);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, rayDistance, itemMask))
            {
                Collectable_Item hitItem = hit.collider.GetComponent<Collectable_Item>();
                if (hitItem != null)
                {
                    grabbing = true;

                    bool canAccess = true;
                    Transform parent = hit.collider.transform.parent;
                    if (parent != null)
                    {
                        PullOut drawer = parent.GetComponent<PullOut>();
                        if (drawer != null)
                            canAccess = drawer.isout;
                    }

                    if (canAccess)
                    {
                        if (collectable_Item != null && collectable_Item != hitItem)
                            collectable_Item.Highlight(false);

                        collectable_Item = hitItem;
                        collectable_Item.Highlight(true);
                        lookingAtItem = true;

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
        }
    }
