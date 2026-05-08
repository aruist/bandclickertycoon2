using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Money : MonoBehaviour {
    [SerializeField] private AnimatedNumberText animatedNumberText;

    void OnEnable()
    {
        PlayerData.OnMoneyChanged += OnMoneyChanged;
        paIkKaHaLlItSiJa.OnRegionChanged += OnMoneyChanged;

    }
    void OnDisable()
    {
        PlayerData.OnMoneyChanged -= OnMoneyChanged;
        paIkKaHaLlItSiJa.OnRegionChanged -= OnMoneyChanged;
    }


    // Use this for initialization
    void Start () {
        OnMoneyChanged();
    }


    private void OnMoneyChanged()
    {
        if (animatedNumberText == null || paIkKaHaLlItSiJa.Instance == null)
            return;

        animatedNumberText.SetValue(paIkKaHaLlItSiJa.Instance.GetCurrentRegionMoney());
    }

}
