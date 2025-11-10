using UnityEngine;

public class Collectable_Item : MonoBehaviour
{
    public Collectable_Item holding_item;
    [Header("Inventory & Settings")]
    public Texture texture;
    [SerializeField] private GameObject item_object;   // kept
    [SerializeField] private Inventory inventory;      // kept (optional)
    public bool collected = false;
    public int item_id;

    [Header("Held Placement (per-item tuning)")]
    public Vector3 offsetPosition = new Vector3(0f, -0.3f, 1f); // in front of player
    public Vector3 offsetRotation = Vector3.zero;               // extra tilt
    public Vector3 heldScale = Vector3.one;                      // scale while held
    public float followSmoothness = 15f;                         // follow lerp

    private Renderer itemRenderer;
    private Color originalColor;        

    // Hold/follow internals
    private bool isHeld = false;
    private Transform playerTransform;
    private Vector3 originalScale;
    private Rigidbody rb;

    void Start()
    {
        // keep your original fallback
        if (item_object == null)
            item_object = this.gameObject;

        itemRenderer = item_object.GetComponent<Renderer>();
        if (itemRenderer != null)
            originalColor = itemRenderer.material.color;

        originalScale = item_object.transform.localScale;
        rb = item_object.GetComponent<Rigidbody>();
    }

    void Update()
    {
        // smooth follow while held
        if (isHeld && playerTransform != null)
        {
            Vector3 targetPos = playerTransform.position + playerTransform.TransformDirection(offsetPosition);
            item_object.transform.position = Vector3.Lerp(item_object.transform.position, targetPos, Time.deltaTime * followSmoothness);

            Quaternion targetRot = playerTransform.rotation * Quaternion.Euler(offsetRotation);
            item_object.transform.rotation = Quaternion.Slerp(item_object.transform.rotation, targetRot, Time.deltaTime * followSmoothness);
        }
    }

    // ====== YOUR ORIGINAL METHODS (kept) ======

    public void Highlight(bool state)
    {
        Debug.Log("proslo1");
        if (!collected)
        {
            Debug.Log("proslo2");
            if (itemRenderer == null) return;

            if (state) itemRenderer.material.color = Color.yellow;  // highlight
            else        itemRenderer.material.color = originalColor; // restore
        }
    }

    public void PickUp()
    {
        if (!collected)
        {
            // keep your original behavior
            MeshRenderer meshRenderer = item_object.GetComponent<MeshRenderer>();
            if (itemRenderer != null) itemRenderer.material.color = originalColor;
            if (meshRenderer) meshRenderer.enabled = false;

            collected = true;
        }
    }

    /// <summary>
    /// Forces the object to jump in front of the player instantly (your original idea).
    /// </summary>
    public void PlaceInFrontOfPlayer(Transform player)
    {
        if (!player) return;
        item_object.transform.position = player.position + player.TransformDirection(offsetPosition);
        item_object.transform.rotation = player.rotation * Quaternion.Euler(offsetRotation);
    }

    // ====== NEW: hold / drop that uses offsets & follows player ======

    /// <summary>
    /// Toggle holding this item relative to the given player transform.
    /// </summary>
    public void ToggleHold(Transform player)
    {
        if (!isHeld) Hold(player);
        else         Drop();
    }

    /// <summary>
    /// Start holding: item follows player with offsets; scale can change while held.
    /// </summary>
    public void Hold(Transform player)
    {
        if (!player) return;

        playerTransform = player;
        isHeld = true;

        // make sure it’s placed correctly right away
        PlaceInFrontOfPlayer(playerTransform);

        // apply held scale
        item_object.transform.localScale = heldScale;

        // physics safe: freeze while held
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // ensure it’s visible if your PickUp hid it
        MeshRenderer meshRenderer = item_object.GetComponent<MeshRenderer>();
        if (meshRenderer) meshRenderer.enabled = true;
    }

    /// <summary>
    /// Stop holding: restore scale and physics; drop a little forward to avoid clipping.
    /// </summary>
    public void Drop()
    {
        isHeld = false;
        playerTransform = null;

        // restore original scale
        item_object.transform.localScale = originalScale;

        // place slightly forward so it doesn’t clip into player
        item_object.transform.position += item_object.transform.forward * 0.5f;

        // restore physics
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }
    }
}
