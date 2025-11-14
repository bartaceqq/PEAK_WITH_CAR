using UnityEngine;
using UnityEngine.SceneManagement;

public class GetScene : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void MoveToScene()
    {
        if (StaticData.items != null)
        {
            SceneManager.LoadScene(1);
        }
    }
}
