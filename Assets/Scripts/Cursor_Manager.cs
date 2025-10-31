using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Cursor_Manager : MonoBehaviour
{
    [SerializeField] TMP_Text cursorText;
    [SerializeField] RawImage cursorImage;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void grab()
    {
        cursorImage.gameObject.SetActive(true);
        cursorText.gameObject.SetActive(false);
    }
    public void normal()
    {
        cursorImage.gameObject.SetActive(false);
        cursorText.gameObject.SetActive(true);
    }
}
