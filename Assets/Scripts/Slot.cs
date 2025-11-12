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

        if (StaticData.item_map.TryGetValue(idToUse, out var collectable))
        {
            collectable.gameObject.SetActive(true);
            Debug.Log($"[Slot] Activated {idToUse} → {collectable.name}");
        }
        else
        {
            Debug.LogWarning($"[Slot] item_map does not contain id {idToUse}. Did you populate StaticData.item_map?");
        }
    }

    public void SetTexture(Texture texture)
    {
        image.texture = texture;
        isoccupied = true;
        image.enabled = true;
    }
}