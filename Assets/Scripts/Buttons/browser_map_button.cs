using UnityEngine;

public class browser_map_button : MonoBehaviour
{
    public GameObject print_button;
    public bool button_pressed = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
       
    }

    public void clickmap()
    {
        if (!button_pressed)
        {
            print_button.SetActive(true);
            button_pressed = true;
        }
        else
        {
            print_button.SetActive(false);
            button_pressed = false;
        } 
    }
}
