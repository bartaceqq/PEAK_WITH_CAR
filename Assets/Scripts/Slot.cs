using System;
using UnityEngine;
using UnityEngine.UI;

public class Slot : MonoBehaviour
{
    [Header("Slot Settings")]
    public Transform playerTransform;
    public KeyCode input = KeyCode.Alpha1;   // set per-slot in Inspector
    public bool isoccupied = false;
    public int itemId = -1;                  // use this if item is not assigned
    public RawImage image;
    public Collectable_Item item;
    public PlayerController player;

    void Start()
    {
        player = FindObjectOfType<PlayerController>();
        if (player != null) playerTransform = player.transform;
        else Debug.LogWarning("[Slot] No PlayerController found in the scene!");
    }

    void Update()
    {
        if (!Input.GetKeyDown(input)) return;

        Debug.Log($"[Slot] Key pressed: {input}");

        // Determine which id to use
        int idToUse = (item != null) ? item.item_id : itemId;

        if (idToUse < 0)
        {
            Debug.LogWarning("[Slot] No item selected for this slot (item and itemId are not set).");
            return;
        }
        
        if (StaticData.item_map.ContainsKey(idToUse))
        {
            Collectable_Item collectable = StaticData.item_map[idToUse];
            
            collectable.gameObject.SetActive(true);
            
            player.holding_item_id = idToUse;

            Debug.Log("Player is now holding item ID: " + idToUse);
        }
        else
        {
            Debug.LogWarning("Item ID " + idToUse + " was not found in the item_map!");
        }

    }

    public void SetTexture(Texture texture)
    {
        image.texture = texture;
        isoccupied = true;
        image.enabled = true;
    }
}