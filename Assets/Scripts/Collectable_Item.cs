using Seagull.City_03.SceneProps;
using System.Collections.Generic;
using UnityEngine;

public class Collectable_Item : MonoBehaviour
{
    public GameObject incaseobject;
    private Renderer[] childRenderers;
    public GameObject box;
    public PrefabGroup prefab;
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
    public Vector3 heldScale = Vector3.one;                     // scale while held
    public float followSmoothness = 15f;                        // follow lerp

    private Renderer itemRenderer;
    private Color originalColor;

    // Hold/follow internals
    private bool isHeld = false;
    private Transform playerTransform;
    private Vector3 originalScale;
    private Rigidbody rb;

    void Start()
    {
        // 🔧 make sure static map exists
        if (StaticData.item_map == null)
            StaticData.item_map = new Dictionary<int, Collectable_Item>();

        if (item_object == null)
            item_object = this.gameObject;

        // 🔧 if holding_item is not set in Inspector, fall back to this
        if (holding_item == null)
            holding_item = this;

        // ✅ register THIS item ID only once, and map to holding_item
        if (!StaticData.item_map.ContainsKey(item_id))
            StaticData.item_map.Add(item_id, holding_item);

        childRenderers = item_object.GetComponentsInChildren<Renderer>();

        if (childRenderers.Length > 0)
            originalColor = childRenderers[0].material.color;

        originalScale = item_object.transform.localScale;
        rb = item_object.GetComponent<Rigidbody>();
    }

    void Update()
    {
        // smooth follow while held
        if (isHeld && playerTransform != null)
        {
            Vector3 targetPos = playerTransform.position +
                                playerTransform.TransformDirection(offsetPosition);
            item_object.transform.position =
                Vector3.Lerp(item_object.transform.position, targetPos,
                    Time.deltaTime * followSmoothness);

            Quaternion targetRot = playerTransform.rotation *
                                   Quaternion.Euler(offsetRotation);
            item_object.transform.rotation =
                Quaternion.Slerp(item_object.transform.rotation, targetRot,
                    Time.deltaTime * followSmoothness);
        }
    }

    // ====== ORIGINAL METHODS (kept) ======

    public void Highlight(bool state)
    {
        
    }

    public void PickUp()
    {
        if (!collected)
        {
            MeshRenderer meshRenderer = item_object.GetComponent<MeshRenderer>();
            if (itemRenderer != null) itemRenderer.material.color = originalColor;

            // hide the ground mesh only – holding_item is separate
            if (meshRenderer) meshRenderer.enabled = false;

            collected = true;
        }
    }

    public void PlaceInFrontOfPlayer(Transform player)
    {
        if (!player) return;

        item_object.transform.position =
            player.position + player.TransformDirection(offsetPosition);
        item_object.transform.rotation =
            player.rotation * Quaternion.Euler(offsetRotation);
    }

    // ====== NEW: hold / drop that uses offsets & follows player ======

    public void ToggleHold(Transform player)
    {
        if (!isHeld) Hold(player);
        else Drop();
    }

    public void Hold(Transform player)
    {
        if (!player) return;

        playerTransform = player;
        isHeld = true;

        PlaceInFrontOfPlayer(playerTransform);

        item_object.transform.localScale = heldScale;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        MeshRenderer meshRenderer = item_object.GetComponent<MeshRenderer>();
        if (meshRenderer) meshRenderer.enabled = true;
    }

    public void Drop()
    {
        isHeld = false;
        playerTransform = null;

        item_object.transform.localScale = originalScale;
        item_object.transform.position += item_object.transform.forward * 0.5f;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }
    }
}
