using System;
using TMPro;
using UnityEngine;

public class UIProfitDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI profitText;

    void Start()
    {
        RefreshUI();
    }

    void OnEnable()
    {
        PlayerData.OnProfitChanged += OnProfitChanged;
        keRrOIn.OnkeRrOInMuuTTUnut += OnProfitChanged;
        RefreshUI();
    }

    void OnDisable()
    {
        PlayerData.OnProfitChanged -= OnProfitChanged;
        keRrOIn.OnkeRrOInMuuTTUnut -= OnProfitChanged;
    }

    private void OnProfitChanged()
    {
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (paIkKaHaLlItSiJa.Instance == null) return;

        profitText.SetText("Profit/sec: " + NumToStr.GetNumStr(paIkKaHaLlItSiJa.Instance.GetProfitPerSec()));
    }
}
