using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Inventory : MonoBehaviour
{
    public List<Collectable_Item> items = new List<Collectable_Item>();
    public List<Slot> slots;
    public Dictionary<int, Collectable_Item> item_map = new Dictionary<int, Collectable_Item>();

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

        // auto-find slots if needed
        if (slots == null || slots.Count == 0)
        {
            slots = new List<Slot>(FindObjectsOfType<Slot>());
            Debug.Log("Inventory: Found " + slots.Count + " slots automatically.");
        }

        // rebuild UI for saved items
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

    void Update() { }

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

                // ❌ removed: StaticData.item_map.Add(slot.itemId, item.holding_item);
                // mapping is now handled once in Collectable_Item.Start()

                break;
            }
        }
    }
}
