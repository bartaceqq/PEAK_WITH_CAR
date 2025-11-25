using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Cursor_Manager : MonoBehaviour
{
    public bool havinggun = false;
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
        if (!havinggun)
        {
            cursorImage.gameObject.SetActive(false);
            cursorText.gameObject.SetActive(true);
        }
    }

    public void hide()
    {
       
        cursorText.text = "";
    }

    public void reveal()
    {
        cursorText.text = "X";
    }
}
