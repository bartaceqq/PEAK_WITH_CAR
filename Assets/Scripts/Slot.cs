using System;
using UnityEngine;
using UnityEngine.UI;

public class Slot : MonoBehaviour
{
    [Header("Slot Settings")]
    public Cursor_Manager cursor;
    public Transform playerTransform;
    public KeyCode input = KeyCode.Alpha1;
    public bool isoccupied = false;
    public int itemId = -1;
    public RawImage image;
    public Collectable_Item item;
    public PlayerController player;

    private Vector3 aimingpos = new Vector3();
    private Vector3 holdingpos = new Vector3();
    public bool holding = false;

    void Start()
    {
        player = FindObjectOfType<PlayerController>();
        if (player != null)
            playerTransform = player.transform;
        else
            Debug.LogWarning("[Slot] No PlayerController found in the scene!");

        aimingpos = new Vector3(0f, -0.09f, 0.25f);
        holdingpos = new Vector3(0.1f, -0.15f, 0.25f);
    }

    void Update()
    {
        // SLOT KEY PRESS
        if (Input.GetKeyDown(input))
        {
            int idToUse = (item != null) ? item.item_id : itemId;

            if (idToUse < 0)
            {
                Debug.LogWarning("[Slot] No item selected for this slot.");
                return;
            }

            Collectable_Item collectable = GetItemFromPlayer(idToUse);

            if (collectable == null)
            {
                Debug.LogWarning("Item with ID " + idToUse + " not found in player items.");
                return;
            }

            // ✅ toggle equip / unequip
            if (!holding)
            {
                collectable.gameObject.SetActive(true);
                player.holding_item_id = idToUse;
                holding = true;

               

                Debug.Log("Equipped: " + collectable.name);
            }
            else
            {
                collectable.gameObject.SetActive(false);
                player.holding_item_id = 0;
                holding = false;

         

                Debug.Log("Unequipped: " + collectable.name);
            }
        }

        // ✅ ONLY handle item positioning now
        if (holding && player.holding_item_id == itemId)
        {
            Collectable_Item collectable = GetItemFromPlayer(itemId);

            if (collectable == null)
                return;

            if (Input.GetMouseButton(1))
                collectable.transform.localPosition = aimingpos;

            if (Input.GetMouseButtonUp(1))
                collectable.transform.localPosition = holdingpos;
        }
    }

    public void SetTexture(Texture texture)
    {
        image.texture = texture;
        isoccupied = true;
        image.enabled = true;
    }

    // ✅ NEW helper (only change)
    private Collectable_Item GetItemFromPlayer(int id)
    {
        foreach (var i in player.items)
        {
            if (i != null && i.item_id == id)
                return i;
        }
        return null;
    }
}
