using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Inventory : MonoBehaviour
{
    public List<Collectable_Item> items = new List<Collectable_Item>();
    public List<Slot> slots;
    public Dictionary<int, Collectable_Item> item_map = new Dictionary<int, Collectable_Item>();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (StaticData.item_map != null)
            this.item_map = StaticData.item_map;
        else
            StaticData.item_map = new Dictionary<int, Collectable_Item>();
        // Restore saved items
        if (StaticData.items != null)
            items = StaticData.items;
        else
            items = new List<Collectable_Item>();

        // 🔧 Try to automatically find all slots if not assigned
        if (slots == null || slots.Count == 0)
        {
            slots = new List<Slot>(FindObjectsOfType<Slot>());
            Debug.Log("Inventory: Found " + slots.Count + " slots automatically.");
        }

        // Rebuild the UI for any saved items
        foreach (Collectable_Item item in items)
        {
            foreach (Slot slot in slots)
            {
                if (!slot.isoccupied)
                {
                    slot.SetTexture(item.texture);
                    slot.item = item;
                    slot.itemId = item.item_id;
                    break;
                }
            }
        }
    }

    
    // Update is called once per frame
    void Update()
    {
    }

    public void AddItem(Collectable_Item item)
    {
        
        
        foreach (Slot slot in slots)
        {
            if (!slot.isoccupied)
            {
               slot.SetTexture(item.texture);
               slot.item = item;
               slot.itemId = item.item_id;
               items.Add(item);
               StaticData.items = items;
               StaticData.slotscontainssomething = true;
               StaticData.slots = slots;
               StaticData.item_map.Add(slot.itemId, item);
               break;
            }
        }
    }   
    
}
