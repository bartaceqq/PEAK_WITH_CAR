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

    void Update()
    {
        if (Input.GetKeyDown(input) && item != null)
        {
            // Toggle hold/drop
            item.ToggleHold(playerTransform);
        }
    }

    public void SetTexture(Texture texture)
    {
        image.texture = texture;
        isoccupied = true;
        image.enabled = true;
    }
}