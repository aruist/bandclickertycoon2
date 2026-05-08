using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DebugOnly : MonoBehaviour {
    public Text txtProfitPerSec;
    public paIkKaHaLlItSiJa pm;

    void Awake()
    {
#if SOFTCEN_DEBUG
        txtProfitPerSec.gameObject.SetActive(true);
#else
        txtProfitPerSec.gameObject.SetActive(false);
#endif
    }
    void Start()
    {
        Invoke("UpdateProfit", 1f);
    }
    void OnEnable()
    {
        PlayerData.OnProfitChanged += PlayerData_OnProfitChanged;
        keRrOIn.OnkeRrOInMuuTTUnut += KeRrOIn_OnkeRrOInMuuTTUnut;
    }

    void OnDisable()
    {
        PlayerData.OnProfitChanged -= PlayerData_OnProfitChanged;
        keRrOIn.OnkeRrOInMuuTTUnut -= KeRrOIn_OnkeRrOInMuuTTUnut;
        CancelInvoke();
    }

    private void PlayerData_OnProfitChanged()
    {
        UpdateProfit();
    }

    public void ChangeMoney(double amount)
    {
        // #if SOFTCEN_DEBUG
        // pELiNhaLLitSIJa.Instance.ChangeMoney(amount);
        // #endif
    }

    public void ChangeMoney2()
    {
        // #if SOFTCEN_DEBUG
        // pELiNhaLLitSIJa.Instance.ChangeMoney(100000000d);
        // #endif
    }

    private void UpdateProfit()
    {
        txtProfitPerSec.text = "Profit/sec: " + NumToStr.GetNumStr(pm.GetProfitPerSec());
    }

    private void KeRrOIn_OnkeRrOInMuuTTUnut()
    {
        #if SOFTCEN_DEBUG
        Debug.Log("***** KeRrOIn_OnkeRrOInMuuTTUnut ****");
        #endif
        UpdateProfit();
    }


}
