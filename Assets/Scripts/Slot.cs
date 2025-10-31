using UnityEngine;
using UnityEngine.UI;

public class Slot : MonoBehaviour
{
    public bool isoccupied = false;
    public int itemid;
    public RawImage image;
    public Collectable_Item item;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void setTexture(Texture texture)
    {
        image.texture = texture;
        isoccupied = true;
        image.enabled = true;
    }
}
