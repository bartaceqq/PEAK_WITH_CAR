using UnityEngine;

public class FillGas : MonoBehaviour
{
    [SerializeField] private GasScript gasScript;
    [SerializeField] private GameObject gastext;
    private bool inside = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (inside && Input.GetKey(KeyCode.E))
        {
            gasScript.AddFuel(0.01f);
        } 
    }

    void OnTriggerEnter(Collider other) 
    {
        
       
            inside = true;
            gastext.SetActive(true);
        
    }

    void OnTriggerExit(Collider other)
    {
        inside = false;
        gastext.SetActive(false);
    }
}
