using UnityEngine;
using System.Collections;
using System;
public class KAyttOLiITTyma : MonoBehaviour
{
    public GameObject goKerronNappain;
    public GameObject goKassakaappiNappain;
    public GameObject goArkkuNappain;

    public GameObject[] goOpAsTus;

    public DialogPanel[] dialogs;

    //public WelcomeBackDlg dlgWelcomeBack;

    private bool _initialized = false;

    void Start()
    {
        _initialized = true;
        for (int i=0; i < goOpAsTus.Length; i++)
        {
            goOpAsTus[i].SetActive(false);
        }

        TaRKisTaNaPpAImEt();
        Invoke("tARkistARahaKERtyMa", 0.5f);
        InitNotifications();
        TaRKisTaOpAsTuS();
    }

    private void InitNotifications()
    {
        #if SCNOTIFICATIONS
        OneSignal.StartInit("1dba3a08-d6c4-4f0d-b92e-48980ab017a5")
            .HandleNotificationOpened(HandleNotificationOpened)
            .EndInit();
        #endif
    }

    private bool paikkarahaakeratty = false;
    private void TaRKisTaOpAsTuS()
    {
        if (paIkKaHaLlItSiJa.Instance == null) return;

        if (paIkKaHaLlItSiJa.Instance.IsImprovementsProgressReached(paIkKaHaLlItSiJa.improvementProgress.OPASTUS))
        {
            SetOnCollectedMoney(true);
        }
        else
        {
            SetOnCollectedMoney(false);
        }

        if (!paIkKaHaLlItSiJa.Instance.HasAnyProgress())
        {
            goOpAsTus[0].SetActive(true);
        } else if (goOpAsTus[0].activeSelf)
        {
            goOpAsTus[0].SetActive(false);
        }
    }

    private void SetOnCollectedMoney(bool state)
    {
        if (state)
        {
            if (!paikkarahaakeratty)
            {
                paikkarahaakeratty = true;
                PlaceItem.OnCollectedMoneyChanged += PlaceItem_OnCollectedMoneyChanged;
            }
            if (!regMoneyPressed)
            {
                regMoneyPressed = true;
                MoneyCollectedPanel.OnCollectedMoneyPressed += MoneyCollectedPanel_OnCollectedMoneyPressed;
            }
        }
        else
        {
            if (paikkarahaakeratty)
            {
                paikkarahaakeratty = false;
                PlaceItem.OnCollectedMoneyChanged -= PlaceItem_OnCollectedMoneyChanged;
                goOpAsTus[1].SetActive(false);
            }
            if (regMoneyPressed)
            {
                regMoneyPressed = false;
                MoneyCollectedPanel.OnCollectedMoneyPressed -= MoneyCollectedPanel_OnCollectedMoneyPressed;
            }
        }
    }

    private void PlaceItem_OnCollectedMoneyChanged()
    {
        if (!paIkKaHaLlItSiJa.Instance.IsImprovementsProgressReached(paIkKaHaLlItSiJa.improvementProgress.OPASTUS))
        {
            // lopetetaan help
            goOpAsTus[1].SetActive(false);
            SetOnCollectedMoney(false);
        }
        else
        {
            goOpAsTus[1].SetActive(true);
            SetOnCollectedMoney(true);
        }
    }

    private bool regMoneyPressed = false;

    private void MoneyCollectedPanel_OnCollectedMoneyPressed()
    {
        if (goOpAsTus[1].activeSelf)
        {
            goOpAsTus[1].SetActive(false);
        }
        if (!paIkKaHaLlItSiJa.Instance.TarvitaankoOpastusta())
        {
            // lopetetaan help
            SetOnCollectedMoney(false);
        }
    }

    private void TaRKisTaNaPpAImEt()
    {
        if (paIkKaHaLlItSiJa.Instance == null) return;
        if (paIkKaHaLlItSiJa.Instance.IsImprovementsProgressReached(paIkKaHaLlItSiJa.improvementProgress.STEP1))
        {
            goKerronNappain.SetActive(true);
        }
        else
        {
            goKerronNappain.SetActive(false);
        }
        if (paIkKaHaLlItSiJa.Instance.IsImprovementsProgressReached(paIkKaHaLlItSiJa.improvementProgress.STEP2))
        {
            goKassakaappiNappain.SetActive(true);
        }
        else
        {
            goKassakaappiNappain.SetActive(false);
        }
        if (paIkKaHaLlItSiJa.Instance.IsImprovementsProgressReached(paIkKaHaLlItSiJa.improvementProgress.STEP3))
        {
            goArkkuNappain.SetActive(true);
            if (onImprovementPurchaseListener)
            {
                paIkKaHaLlItSiJa.onImprovePurchased -= PaIkKaHaLlItSiJa_onImprovePurchased;
                onImprovementPurchaseListener = false;
            }
        }
        else
        {
            goArkkuNappain.SetActive(false);
        }

    }
    // Gets called when the player opens the notification.
    // private static void HandleNotificationOpened(OSNotificationOpenedResult result)
    // {
    // }

    private bool onImprovementPurchaseListener = false;
    void OnEnable()
    {
        onImprovementPurchaseListener = true;
        paIkKaHaLlItSiJa.onImprovePurchased += PaIkKaHaLlItSiJa_onImprovePurchased;
    }

    private void PaIkKaHaLlItSiJa_onImprovePurchased(int arg1, int arg2, int arg3)
    {
        #if SOFTCEN_DEBUG
        Debug.Log("PaIkKaHaLlItSiJa_onImprovePurchased", gameObject);
        #endif
        TaRKisTaOpAsTuS();
        TaRKisTaNaPpAImEt();
    }

    void OnDisable()
    {
        if (onImprovementPurchaseListener)
        {
            paIkKaHaLlItSiJa.onImprovePurchased -= PaIkKaHaLlItSiJa_onImprovePurchased;
            onImprovementPurchaseListener = false;
        }
        CancelInvoke();

        if (paikkarahaakeratty)
        {
            paikkarahaakeratty = false;
            PlaceItem.OnCollectedMoneyChanged -= PlaceItem_OnCollectedMoneyChanged;
        }

    }

    void Update()
    {
        // if (Input.GetKeyUp(KeyCode.Escape))
        // {
        //     for (int i = 0; i < dialogs.Length; i++)
        //     {
        //         if (dialogs[i].gameObject.activeSelf)
        //         {
        //             dialogs[i].CloseDialog();
        //             return;
        //         }
        //     }
        //     AvaaNakyma("ExitGame");
        // }
    }

    public void AvaaNakyma(string name)
    {
        DialogPanel dlg = getDialog(name);
        if (dlg != null)
        {
            dlg.gameObject.SetActive(true);
        }
    }

    public DialogPanel getDialog(string name)
    {
        for (int i = 0; i < dialogs.Length; i++)
        {
            if (dialogs[i].gameObject.name.Equals(name))
            {
                return dialogs[i];
            }
        }
#if SOFTCEN_DEBUG
        Debug.LogError("getDialog not found: " + name);
#endif
        return null;
    }

    public void KEhiTTajanTYoKAlu()
    {
        #if SOFTCEN_DEBUG
        SoundFXManager.PlayUIOneShot(SoundFXManager.DefaultSounds.UI_KEYBOARD_CLICK);
        AvaaNakyma("KEhiTTajanTYoKAlu");
        #endif
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (!pauseStatus && _initialized)
        {
            tARkistARahaKERtyMa();
        }
    }

    public void AvAaAsETukSet()
    {
        SoundFXManager.PlayUIOneShot(SoundFXManager.DefaultSounds.UI_KEYBOARD_CLICK);
        AvaaNakyma(GameConsts.Nakymat.Menu);
    }

    public void AvaAKaSSaKAApPi()
    {
        // SoundFXManager.PlayUIOneShot(SoundFXManager.DefaultSounds.UI_KEYBOARD_CLICK);
        // AvaaNakyma(GameConsts.Nakymat.Vault);
    }
    public void KIItoKSetAuKAsu()
    {
        SoundFXManager.PlayUIOneShot(SoundFXManager.DefaultSounds.UI_KEYBOARD_CLICK);
        AvaaNakyma(GameConsts.Nakymat.Credits);
    }

    private void tARkistARahaKERtyMa()
    {
        // if (paIkKaHaLlItSiJa.Instance == null || pELiNhaLLitSIJa.Instance == null) return;

        // DialogPanel dlg = getDialog(GameConsts.Nakymat.Vault);
        // // if (dlg == null || !paIkKaHaLlItSiJa.Instance.IsImprovementsProgressReached(paIkKaHaLlItSiJa.improvementProgress.STEP2)) return;

        // //WelcomeBackDlg dlgWelcomeBack = dlg.gameObject.GetComponent<WelcomeBackDlg>();
        // if (!dlg.gameObject.activeSelf)
        // {
        //     long _lastUsedTicks = pELiNhaLLitSIJa.Instance.playerData._lastusedTicks;
        //     long _currentTicks = DateTime.UtcNow.Ticks;
        //     TimeSpan timeSpan = TimeSpan.FromTicks(_currentTicks - _lastUsedTicks);
        //     double totalSeconds = timeSpan.TotalSeconds;
        //     #if SOFTCEN_DEBUG
        //     Debug.Log("tARkistARahaKERtyMa: " + totalSeconds + " sec");
        //     #endif
        //     //if (totalSeconds >= GameConsts.Game.welcomeRefresh)
        //     {
        //         //double profit = paIkKaHaLlItSiJa.Instance.GetProfitPerSec();
        //         //double totalProfit = profit * totalSeconds;
        //         //if (totalProfit > 0)
        //         {
        //             #if SOFTCEN_DEBUG
        //             //Debug.Log("Pause profit: " + profit + ", time: " + NumToStr.GetTimeStr(totalSeconds));
        //             #endif
        //             //dlgWelcomeBack.UpdateMoney(totalProfit);
        //             kAsSAKaaPPIIkkuNA ikkuna = dlg.gameObject.GetComponent<kAsSAKaaPPIIkkuNA>();
        //             if (ikkuna != null)
        //             {
        //                 ikkuna.bWelcomeBack = true;
        //                 dlg.gameObject.SetActive(true);
        //                 //pELiNhaLLitSIJa.Instance.playerData._lastusedTicks = DateTime.UtcNow.Ticks;
        //             }
        //         }
        //     }
        // }
    }

    public void AvAAArKKuNaKYmA()
    {
        SoundFXManager.PlayUIOneShot(SoundFXManager.DefaultSounds.UI_KEYBOARD_CLICK);
        AvaaNakyma(GameConsts.Nakymat.Arkut);
    }

    public void AvAAkeRToiMENArvoNTa()
    {
        SoundFXManager.PlayUIOneShot(SoundFXManager.DefaultSounds.UI_KEYBOARD_CLICK);
        AvaaNakyma(GameConsts.Nakymat.KertoimenArvonta);
    }

}
