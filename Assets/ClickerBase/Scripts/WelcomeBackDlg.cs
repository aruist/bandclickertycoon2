using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WhiteCat.Tween;
#if SC_OBFUS
using Beebyte.Obfuscator;
#endif

public class WelcomeBackDlg : MonoBehaviour {
    [SerializeField] private TextMeshProUGUI txtMoney;
    [SerializeField] private TextMeshProUGUI txtMoneySuffix;
    [SerializeField] private TextMeshProUGUI txtVault;
    public Tweener panelTweener;
    public ObjectPooler moneyBlastPool;
    public AudioClip acMoneyBlast;
    private double _profitMoney;
    public RectTransform rtMoneyBlast;

    public void UpdateMoney(double money)
    {
        string strMoney;
        string strSuffix;
        _profitMoney = money;
        NumToStr.GetNumStr(money, out strSuffix, out strMoney);
        txtMoney.text = strMoney;
        txtMoneySuffix.text = strSuffix;
    }

#if SC_OBFUS
    [SkipRename]
#endif
    public void keRAAnAPPaiN()
    {
        // pELiNhaLLitSIJa.Instance.ChangeMoney(_profitMoney);
        panelTweener.enabled = true;
        GameObject go = moneyBlastPool.GetPooledObject();
        go.transform.position = rtMoneyBlast.transform.position;
        go.SetActive(true);
        AudioManager.instance.PlayAudioClip(acMoneyBlast);
    }

#if SC_OBFUS
    [SkipRename]
#endif
    public void KAksiNkerTAistaRAHaT()
    {
        GameObject go = moneyBlastPool.GetPooledObject();
        go.transform.position = rtMoneyBlast.transform.position;
        go.SetActive(true);
        AudioManager.instance.PlayAudioClip(acMoneyBlast);
    }
}
