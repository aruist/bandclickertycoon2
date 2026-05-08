using UnityEngine;

public class UIRegionValutMoneyDisplay : MonoBehaviour
{
    [SerializeField] private AnimatedNumberText vaultMoneyAnimatedNumberText;

    void OnEnable()
    {
        PlayerData.OnVaultMoneyChanged += OnVaultMoneyChanged;
        paIkKaHaLlItSiJa.OnRegionChanged += OnVaultMoneyChanged;
    }

    void OnDisable()
    {
        PlayerData.OnVaultMoneyChanged -= OnVaultMoneyChanged;
        paIkKaHaLlItSiJa.OnRegionChanged -= OnVaultMoneyChanged;
    }

    private void OnVaultMoneyChanged()
    {
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (vaultMoneyAnimatedNumberText == null) return;
        paIkKaHaLlItSiJa ph = paIkKaHaLlItSiJa.Instance;
        if (ph == null) vaultMoneyAnimatedNumberText.SetValue(0);
        double valutMoney = ph.GetCurrentRegionValutMoney();
        vaultMoneyAnimatedNumberText.SetValue(valutMoney);
    }

}
