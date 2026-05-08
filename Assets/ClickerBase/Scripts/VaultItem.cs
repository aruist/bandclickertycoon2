using UnityEngine;
using UnityEngine.UI;
using OneP.InfinityScrollView;
using TMPro;

public class VaultItem : InfinityBaseItem
{
    [SerializeField] private TextMeshProUGUI txtTitle;
    [SerializeField] private TextMeshProUGUI txtCoins;
    [SerializeField] private TextMeshProUGUI txtSub;
    [SerializeField] private TextMeshProUGUI txtNaPPain;
    [SerializeField] private Image imgFill;

    [SerializeField] private Color evenColor;
    [SerializeField] private Color oddColor;
    [SerializeField] private Image imgBackground;

    bool _initialized;
    string _vaultsuffixStr;
    string _vaultmoneyStr;
    string _moneysuffixStr;
    string _moneyStr;
    private int paikkaId;
    private float _timer;
    private double _vaultSize;

    void Awake()
    {
        _initialized = false;
    }

    void Update()
    {
        if (!_initialized) return;
        _timer += Time.deltaTime;
        if (_timer < 0.25f) return;
        _timer = 0;
        UpdateMoney();
    }

    public override void Reload(InfinityScrollView _infinity, int _index)
    {
        paikkaId = _index;
        base.Reload(_infinity, _index);

        Color col = imgBackground.color;
        if (_index % 2 == 0)
        {
            col.b = evenColor.b;
            col.g = evenColor.g;
            col.r = evenColor.r;
            col.a = evenColor.a;
        }
        else
        {
            col.b = oddColor.b;
            col.g = oddColor.g;
            col.r = oddColor.r;
            col.a = oddColor.a;
        }
        imgBackground.color = col;
        //txtTitle.text = paIkKaHaLlItSiJa.Instance.AnNnaPaIkaNNImI(_index);
        paIkKaHaLlItSiJa paIkKaHaLlItSiJa = paIkKaHaLlItSiJa.Instance;
        if (paIkKaHaLlItSiJa == null)
        {
            txtTitle.SetText("-");
            txtCoins.SetText("-");
            txtSub.SetText("-");
            return;
        }
        string paikkaNimi;
        int currentVault;
        int maxVault;
        double vaultSize;
        paIkKaHaLlItSiJa.AnnAPaikkaTIEdot(_index, out paikkaNimi, out currentVault, out maxVault, out vaultSize);
        _vaultSize = vaultSize;
        txtTitle.SetText(paikkaNimi);
        NumToStr.GetNumStr(vaultSize, out _vaultsuffixStr, out _vaultmoneyStr);

        UpdateMoney();

        currentVault++;
        txtSub.SetText("Vault " + currentVault.ToString() + "/" + maxVault.ToString() + ", Max size: " + _vaultmoneyStr + " " + _vaultsuffixStr);
        _initialized = true;
        _timer = 0;
    }

    public void PaiVItANAppAin()
    {
        // TODO: Vault Upgrade
        Debug.LogWarning("VaultItem, Implementation missing!", gameObject);
    }

    private void UpdateMoney()
    {
        paIkKaHaLlItSiJa paIkKaHaLlItSiJa = paIkKaHaLlItSiJa.Instance;
        if (paIkKaHaLlItSiJa == null) return;

        double currentMoney = paIkKaHaLlItSiJa.GetMoney(paikkaId);
        // Debug.Log($"VaultItem currentMoney: {currentMoney}, paikkaId: {paikkaId}");
        NumToStr.GetNumStr(currentMoney, out _moneysuffixStr, out _moneyStr);
        if (txtCoins != null) txtCoins.SetText(_moneyStr + " " + _moneysuffixStr);

        float fillAmount = _vaultSize == 0 ? 0: (float)(currentMoney / _vaultSize);
        if (fillAmount > 1) fillAmount = 1;
        if (fillAmount < 0) fillAmount = 0;
        if (imgFill != null) imgFill.fillAmount = fillAmount;

    }

}
