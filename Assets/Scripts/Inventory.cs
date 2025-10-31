using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Inventory : MonoBehaviour
{
    public List<Collectable_Item> items = new List<Collectable_Item>();
    public List<Slot> slots = new List<Slot>();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
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
               slot.setTexture(item.texture);
               slot.item = item;
               slot.itemid = item.item_id;
               items.Add(item);
               break;
            }
        }
    }
    
}
