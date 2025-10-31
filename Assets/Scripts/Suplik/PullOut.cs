using UnityEngine;

public class PullOut : MonoBehaviour
{
    public bool isout = false;

    [SerializeField]private GameObject _drawer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void pullthedrawer()
    {
        if (!isout)
        {
            
            Vector3 pos = _drawer.transform.position;
            _drawer.transform.position = new Vector3(pos.x, pos.y, pos.z +0.5f);
            isout = true;
        }
        else
        {
            Vector3 pos = _drawer.transform.position;
            _drawer.transform.position = new Vector3(pos.x, pos.y, pos.z  -0.5f);
            isout = false;
        }
    }
}