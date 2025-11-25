using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class Slot : MonoBehaviour
{
    [Header("Slot Settings")]
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
        if (player != null) playerTransform = player.transform;
        else Debug.LogWarning("[Slot] No PlayerController found in the scene!");

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

            if (!StaticData.item_map.ContainsKey(idToUse))
            {
                Debug.LogWarning("Item ID " + idToUse + " not found in map.");
                return;
            }

            Collectable_Item collectable = StaticData.item_map[idToUse];

            // ✅ toggle equip / unequip
            if (!holding)
            {
                collectable.gameObject.SetActive(true);
                player.holding_item_id = idToUse; // ✅ enable shooting
                holding = true;
                Debug.Log("Equipped: " + collectable.name);
            }
            else
            {
                collectable.gameObject.SetActive(false);
                player.holding_item_id = 0;       // ✅ disable shooting
                holding = false;
                Debug.Log("Unequipped: " + collectable.name);
            }
        }

        // ✅ AIMING ONLY WHEN HOLDING
        if (holding && player.holding_item_id == itemId)
        {
            Collectable_Item collectable = StaticData.item_map[itemId];

            if (Input.GetMouseButton(1))
            {
                collectable.transform.localPosition = aimingpos;
            }

            if (Input.GetMouseButtonUp(1))
            {
                collectable.transform.localPosition = holdingpos;
            }
        }
    }

    public void SetTexture(Texture texture)
    {
        image.texture = texture;
        isoccupied = true;
        image.enabled = true;
    }
}
