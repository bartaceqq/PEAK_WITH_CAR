using UnityEngine;
using UnityEngine.UI;

public class GasScript : MonoBehaviour
{
    [Header("References")] 
    public Car_Controller car;                    // drag your CAR here
    public RectTransform backBar;       // white bar (background)
    public RectTransform gasBar;        // yellow bar (foreground)

    [Header("Fuel")]
    public float maxFuel = 100f;
    public float idleDrainPerSec = 0.05f;     // drains even when stopped
    public float drainPerMeter = 0.02f;       // extra drain by distance driven

    private float fuel;
    private Vector3 lastPos;

    public bool IsEmpty => fuel <= 0.0001f;
    public float Fuel01 => maxFuel > 0 ? fuel / maxFuel : 0f;

    void Start()
    {
        fuel = maxFuel;
        if (car && car.rigid)
            lastPos = car.rigid.transform.position;

        // --- Align both bars ---
        if (backBar)
        {
            backBar.pivot = new Vector2(0f, 0.5f);
            backBar.anchorMin = new Vector2(0f, 0.5f);
            backBar.anchorMax = new Vector2(0f, 0.5f);
            backBar.anchoredPosition = new Vector2(0f, backBar.anchoredPosition.y);
        }

        if (gasBar)
        {
            gasBar.pivot = new Vector2(0f, 0.5f);   // same as backBar, left-aligned
            gasBar.anchorMin = new Vector2(0f, 0.5f);
            gasBar.anchorMax = new Vector2(0f, 0.5f);
            gasBar.anchoredPosition = new Vector2(0f, backBar ? backBar.anchoredPosition.y : 0f);
        }

        UpdateVisual();   // initialize width
    }

    void Update()
    {
        if (!car || !car.rigid) return;

        // distance-based drain
        Vector3 pos = car.rigid.transform.position;
        float metersThisFrame = Vector3.Distance(pos, lastPos);
        lastPos = pos;

        float drain =
            idleDrainPerSec * Time.deltaTime +
            drainPerMeter * metersThisFrame;

        fuel = Mathf.Max(0f, fuel - drain);
        UpdateVisual();
    }

    void UpdateVisual()
    {
        if (!gasBar) return;

        // Use actual rendered width (rect.width), not sizeDelta.x
        float fullWidth = 1f;
        if (backBar && backBar.rect.width > 0f) fullWidth = backBar.rect.width;
        else if (gasBar.rect.width > 0f)        fullWidth = gasBar.rect.width;

        float pct = Mathf.Clamp01(Fuel01);

        // keep current height, change only width
        var size = gasBar.sizeDelta;
        size.x = fullWidth * pct;
        gasBar.sizeDelta = size;

        // Keep yellow bar aligned with white bar’s left edge
        if (backBar)
        {
            Vector2 anchored = gasBar.anchoredPosition;
            anchored.x = backBar.anchoredPosition.x;
            anchored.y = backBar.anchoredPosition.y;
            gasBar.anchoredPosition = anchored;
        }
    }

    public void AddFuel(float amount)
    {
        fuel = Mathf.Clamp(fuel + amount, 0f, maxFuel);
        UpdateVisual();
    }
}
