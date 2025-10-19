using System.Collections;
using TMPro;
using UnityEngine;

public class Speeder : MonoBehaviour
{
    public int speeder = 0;
    public int maxSpeed;
    public TMP_Text speedText;

    private bool canChangeSpeed = true; // prevents spamming

    void Start()
    {
        speeder = 0;
        maxSpeed = 0;
        UpdateUI();
    }

    void Update()
    {
        if (canChangeSpeed)
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                if (speeder < 5)
                {
                    speeder++;
                    maxSpeed = speeder * 40;
                    UpdateUI();
                    StartCoroutine(SpeedChangeCooldown());
                }
            }
            else if (Input.GetKey(KeyCode.LeftControl))
            {
                if (speeder > 0)
                {
                    speeder--;
                    maxSpeed = speeder * 40;
                    UpdateUI();
                    StartCoroutine(SpeedChangeCooldown());
                }
            }
        }
    }

    void UpdateUI()
    {
        if (speedText != null)
        {
            if (speeder == 0)
                speedText.SetText("N");
            else
                speedText.SetText(speeder.ToString());
        }
    }

    IEnumerator SpeedChangeCooldown()
    {
        canChangeSpeed = false;            // block further input
        yield return new WaitForSeconds(2f); // wait 2 seconds
        canChangeSpeed = true;             // allow input again
    }
}