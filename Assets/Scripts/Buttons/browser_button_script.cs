using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{
    public GameObject browser_page;
    public bool pageison = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void clickbutton()
    {
        if (!pageison)
        {
            browser_page.SetActive(true);
            pageison = true;
        }
        else
        {
            browser_page.SetActive(false);
            pageison = false;
        }
    }
}
