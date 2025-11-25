using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Looker : MonoBehaviour
{
    public Inventory inventory;
    public Cursor_Manager cursor_manager;
    [SerializeField] private Camera cam;
    [SerializeField] private float rayDistance = 10f;
    [SerializeField] private float interactionCooldown = 0.1f;
    private Collectable_Item collectable_Item;
    private float nextUseTime = 0f;
    public bool lookingatitem = false;
    public bool grabbing = false;

    void Update()
    {
        if (grabbing)
        {
            cursor_manager.grab();
        }
        else
        {
            cursor_manager.normal();
        }
        
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance))
        {
            switch (hit.collider.gameObject.tag)
            {
              case "drawer":
                  grabbing = true;
              

                  PullOut pullOut = hit.collider.GetComponent<PullOut>();
               
                  if (Input.GetKeyDown(KeyCode.E) && !lookingatitem)
                  {
                      pullOut.pullthedrawer();
                        
                  }
                  break;
              
              case "monitor":
                  grabbing = true;
                  if (Input.GetKeyDown(KeyCode.E) )
                  {
                      Cursor.lockState = CursorLockMode.None;  // Unlocks the mouse
                      Cursor.visible = true;  
                      SceneManager.LoadScene(2);
                  }

                  break;
              default:
                  grabbing = false;
                  break;
            }
         
           

         
        }
        else
        {
            grabbing = false;
           
        }
        LayerMask mask = LayerMask.GetMask("items");
        Ray ray2 = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit2;

        if (Physics.Raycast(ray2, out hit2, rayDistance, mask))
        {
            Collectable_Item hitItem = hit2.collider.GetComponentInParent<Collectable_Item>();

            if (hitItem != null)
            {
                
                grabbing = true;
                

                bool canacces = false;
                    Transform parent = hit2.collider.transform.parent;

                    if (parent != null)
                    {
                        PullOut pullOut = parent.GetComponent<PullOut>();
                        if (pullOut != null)
                        {
                            Debug.Log("Parent has PullOut");
                            canacces = pullOut.isout;
                        }
                        else
                        {
                            Debug.Log("Parent doesn't have PullOut");
                            canacces = true; // or false depending if items without drawer should be accessible
                        }
                    }


                    if(canacces){
                    grabbing = true;

                    // Unhighlight previous item if it's different
                    if (collectable_Item != null && collectable_Item != hitItem)
                    {
                        
                        collectable_Item.Highlight(false);
                    }

                    collectable_Item = hitItem;
                    collectable_Item.Highlight(true);
                    lookingatitem = true;

                    // ✅ Pickup happens when pressing E while looking at the current item
                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        if (!collectable_Item.collected)
                        {

                            inventory.AddItem(collectable_Item);
                        collectable_Item.PickUp();
                    }}
                }



                collectable_Item = hitItem;
                collectable_Item.Highlight(true);
                lookingatitem = true;
            }
        }
        else
        {
            lookingatitem = false;                                                            
            // Raycast didn't hit anything → remove highlight
          
            if (collectable_Item != null)
            {
                collectable_Item.Highlight(false);
                collectable_Item = null;
            }
        }


    }
}