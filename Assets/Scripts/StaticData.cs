using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class StaticData
{
    public static List<Collectable_Item> items;
    public static List<Slot> slots;
    public static bool slotscontainssomething = false;
    public static Dictionary<int, Collectable_Item> item_map;
}   