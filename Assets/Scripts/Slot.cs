using System;
using UnityEngine;
using UnityEngine.UI;

public class Slot : MonoBehaviour
{
    [Header("Slot Settings")]
    public Transform playerTransform;
    public KeyCode input = KeyCode.T;  // you can assign this in Inspector (e.g. Alpha1, Alpha2, etc.)
    public bool isoccupied = false;
    public int itemId;
    public RawImage image;
    public Collectable_Item item;
    public PlayerController player;

    public void Start()
    {
        player = FindObjectOfType<PlayerController>();
        if (player != null)
            playerTransform = player.transform;
        else
            Debug.LogWarning("No PlayerController found in the scene!");
    }

    void Update()
    {
        if (Input.GetKeyDown(input))
        {
            Debug.Log(StaticData.items[0]);
            Debug.Log("Slot key pressed: " + input);
        }
        if (Input.GetKeyDown(input) && item != null)
        {
            Debug.Log("slot clicked + " + input.ToString());
            foreach (Collectable_Item itemik in player.itemstouse)
            {               
                Debug.Log(itemik.item_id + " = ID");
                if (itemik.item_id == item.item_id)
                {
                    // Enable the GameObject
                    if (itemik.gameObject != null)
                    {
                        itemik.holding_item.gameObject.SetActive(true);
                    }

                 

                    
                }
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