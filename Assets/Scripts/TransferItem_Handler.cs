using System.Collections.Generic;
using UnityEngine;

public class TransferItem_Handler : MonoBehaviour
{
    public List<Collectable_Item> items = new List<Collectable_Item>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void TransferItem()
    {
        foreach (Collectable_Item item in items)
        {
            if (StaticData.item_map != null)
            {
                if (StaticData.item_map.ContainsKey(item.item_id))
                {
                    StaticData.item_map[item.item_id] = item;
                }
            }
        }
    }
}