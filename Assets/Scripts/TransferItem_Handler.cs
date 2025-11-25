using System.Collections.Generic;

public static class TransferItem_Handler
{
    public static void TransferItems(List<Collectable_Item> items)
    {
        if (StaticData.item_map == null)
        {
            UnityEngine.Debug.LogWarning("StaticData.item_map is NULL — nothing to transfer!");
            return;
        }

        if (items == null) return;

        foreach (Collectable_Item item in items)
        {
            if (item == null) continue;

            StaticData.item_map[item.item_id] = item;
        }
    }
}