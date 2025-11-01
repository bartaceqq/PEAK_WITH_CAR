using UnityEngine;

public class print_checker : MonoBehaviour
{
    public bool printed = false;
    [SerializeField] private GameObject printed_map;
    void Start()
    {
       
    }
    void Update()
    {
        
        if (print_button.printed)
        {  
            Cursor.lockState = CursorLockMode.Locked; // Locks it to the center
            Cursor.visible = false;                   // Hides the cursor

          printed_map.SetActive(true);
          print_button.printed = false; 
         
        }
       
    }
}

    