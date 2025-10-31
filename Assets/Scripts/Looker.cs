using UnityEngine;

public class Looker : MonoBehaviour
{
    public Cursor_Manager cursor_manager;
    [SerializeField] private Camera cam;
    [SerializeField] private float rayDistance = 10f;
    [SerializeField] private float interactionCooldown = 0.5f;

    private float nextUseTime = 0f;

    void FixedUpdate()
    {
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance))
        {
            if (hit.collider.CompareTag("drawer"))
            {
                cursor_manager.grab();

                PullOut pullOut = hit.collider.GetComponent<PullOut>();
                if (pullOut != null && Time.time >= nextUseTime)
                {
                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        pullOut.pullthedrawer();
                        nextUseTime = Time.time + interactionCooldown;
                    }
                }
            }
            else
            {
                cursor_manager.normal();
            }
        }
        else
        {
            cursor_manager.normal();
        }
    }
}