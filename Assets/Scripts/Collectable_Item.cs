using UnityEngine;

public class Collectable_Item : MonoBehaviour
{
     public Texture texture;
    [SerializeField] private GameObject item_object;
    [SerializeField] private Inventory inventory;
    public bool collected = false;
    public int item_id;

    private Renderer itemRenderer;
    private Color originalColor;

    void Start()
    {
        if (item_object == null)
            item_object = this.gameObject;

        itemRenderer = item_object.GetComponent<Renderer>();
        if (itemRenderer != null)
            originalColor = itemRenderer.material.color;
    }

    public void Highlight(bool state)
    {
        Debug.Log("proslo1");
        if (!collected){
            Debug.Log("proslo2");
        if (itemRenderer == null) return;

        if (state)
        {
            itemRenderer.material.color = Color.yellow; // Highlight color
        }
        else
            itemRenderer.material.color = originalColor; // Restore
        }
    }
    public void PickUp()
    {
        if (!collected){
        
        MeshRenderer meshRenderer = this.GetComponent<MeshRenderer>();
        itemRenderer.material.color = originalColor;
        meshRenderer.enabled = false;
        collected = true;
    }}
}