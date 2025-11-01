using UnityEngine;
using UnityEngine.SceneManagement;

public class print_button : MonoBehaviour
{
    public static bool printed = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void clickprint()
    {
        printed = true;
        SceneManager.LoadScene(2);
    }
}
