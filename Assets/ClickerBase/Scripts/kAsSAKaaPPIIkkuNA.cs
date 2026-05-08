using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using OneP.InfinityScrollView;
using TMPro;

public class kAsSAKaaPPIIkkuNA : MonoBehaviour {
    public TextMeshProUGUI txtInfo;
    public bool bWelcomeBack;
    public GameObject[] goWelcomeParticles;
    public GameObject goKatseELMainos;
    public InfinityScrollView scrollView;

    private bool mInitialized = false;
	// Use this for initialization

	void Start () {
        mInitialized = true;
        scrollView.Setup(paIkKaHaLlItSiJa.Instance.GetPlacesCount());
        RefreshUI();
    }

    void OnEnable()
    {
        if (mInitialized)
        {
            RefreshUI();
        }
    }
    void OnDisable()
    {

    }

    private void RefreshUI()
    {
        if (goKatseELMainos != null)
        {
            if (mAiNOsPomO.instance != null && mAiNOsPomO.instance.onKOPAlkiNToA()) goKatseELMainos.SetActive(true);
            else goKatseELMainos.SetActive(false);
        }

        if (goWelcomeParticles != null)
        {
            for (int i = 0; i < goWelcomeParticles.Length; i++)
            {
                if (goWelcomeParticles[i] != null) goWelcomeParticles[i].SetActive(bWelcomeBack);
            }
        }

        if (bWelcomeBack && pELiNhaLLitSIJa.Instance != null)
        {
            long _lastUsedTicks = pELiNhaLLitSIJa.Instance.playerData._lastusedTicks;
            long _currentTicks = DateTime.UtcNow.Ticks;
            TimeSpan timeSpan = TimeSpan.FromTicks(_currentTicks - _lastUsedTicks);
            double totalSeconds = timeSpan.TotalSeconds;

            double totalProfit = paIkKaHaLlItSiJa.Instance.FillIdleEarnings(totalSeconds);

            string money;
            string suffix;
            NumToStr.GetNumStr(totalProfit, out suffix, out money);

            if (txtInfo != null) txtInfo.SetText("You earned\n" + money + " " + suffix + "\nwhile you were gone");
            pELiNhaLLitSIJa.Instance.playerData._lastusedTicks = DateTime.UtcNow.Ticks;
        }
        else
        {
            if (txtInfo != null) txtInfo.SetText("All track vaults.\nIncrease vault sizes to collect more!");
        }

        bWelcomeBack = false;
    }

    public void kaTSEelMaINOs()
    {

    }

    public void kERaaRAhat()
    {
        if (paIkKaHaLlItSiJa.Instance == null)
            return;

        double totalCollected = paIkKaHaLlItSiJa.Instance.CollectAllVaults();
        if (totalCollected > 0d)
            SoundFXManager.PlayUIOneShot(SoundFXManager.DefaultSounds.UI_PICKUPCOINS);

        RefreshUI();
    }

}
