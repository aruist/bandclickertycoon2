using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class AnimatedNumberText : MonoBehaviour
{
    [SerializeField] private float animationDurationSec = 0.4f;
    [SerializeField] private TextMeshProUGUI txtValue;

    [SerializeField] private double displayedValue;
    private double startValue;
    private double targetValue;
    private double currentValue;
    private float animTimer;
    private bool isAnimating;

    void Awake()
    {
        currentValue = -1;
    }

    public void Bind(TextMeshProUGUI value, float duration)
    {
        txtValue = value;
        animationDurationSec = duration;
    }

    public double DisplayedValue
    {
        get { return displayedValue; }
    }

    void Update()
    {
        if (!isAnimating)
            return;

        animTimer += Time.deltaTime;
        float t = animationDurationSec > 0f ? animTimer / animationDurationSec : 1f;
        if (t >= 1f)
        {
            t = 1f;
            isAnimating = false;
        }

        displayedValue = LerpDouble(startValue, targetValue, t);
        RefreshText(displayedValue);
    }

    public void SetValue(double value, bool instant = false)
    {
        targetValue = value;

        if (instant || animationDurationSec <= 0f)
        {
            isAnimating = false;
            animTimer = 0f;
            startValue = value;
            displayedValue = value;
            RefreshText(displayedValue);
            return;
        }

        startValue = displayedValue;
        animTimer = 0f;
        isAnimating = true;
    }

    public void ForceRefresh()
    {
        RefreshText(displayedValue);
    }

    private void RefreshText(double value)
    {
        if (currentValue == value) return;
        string money = NumToStr.GetNumStr(value);
        if (txtValue != null) txtValue.SetText(money);
        currentValue = value;
    }

    private double LerpDouble(double a, double b, double t)
    {
        if (t < 0)
            t = 0;
        if (t > 1)
            t = 1;
        return a * (1 - t) + b * t;
    }
}
