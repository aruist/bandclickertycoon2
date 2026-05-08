using UnityEngine;
//using GoogleMobileAds.Api;
using System.Collections.Generic;
using System;
#if SC_OBFUS
using Beebyte.Obfuscator;
#endif
using UnityEngine.Advertisements; // Using the Unity Ads namespace.

public class mAiNOsPomO : MonoBehaviour
{
     public static mAiNOsPomO instance;
     public static event Action<int, bool> OnPAlKiNtO;

//     public AdPosition adMobPosition;
//     public string[] adMobBannerKeywords;
//     private BannerView adMobBannerView;
//     private List<int> rewardVideoList;
//     private int mRewardVideoId = -1;

//     private bool mInitialized = false;

    public enum RewardVideo
    {
        NONE,
        KERROIN,
    }

//     public enum AdOperators
//     {
//         Chartboost,
//         Unity,
//         Applovin
//     }

    void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        // rewardVideoList = new List<int>();
    }

// #if SC_OBFUS
//     [ObfuscateLiterals]
// #endif
//     void Start()
//     {
// #if SOFTCEN_DEBUG
//         bool enableTestMode = true;
// #else
//         bool enableTestMode = false;
// #endif
//         RequestAdMobBanner();
//         // Unity Ads:
//         if (Advertisement.isSupported)
//         {
// #if UNITY_ANDROID
//             Advertisement.Initialize("1182360", enableTestMode);
// #elif UNITY_IOS
//             Advertisement.Initialize("1182361", enableTestMode);
// #endif
//         }

//         mInitialized = true;
//     }

    public bool onKOPAlkiNToA()
    {
        // if (Advertisement.IsReady("rewardedVideo"))
        // {
        //     return true;
        // }
        return false;
    }

     public void nAYtapALkiNto(int id)
    {
        Debug.LogError("Not implemented");
    }

//     public void nAYtapALkiNto(int id)
//     {
//         // rewardVideoList.Clear();
//         // // if (Advertisement.IsReady("rewardedVideo"))
//         // // {
//         // //     rewardVideoList.Add((int)AdOperators.Unity);
//         // // }

//         // if (rewardVideoList.Count > 0)
//         // {
//         //     switch (rewardVideoList[UnityEngine.Random.Range(0, rewardVideoList.Count)])
//         //     {
//         //         case (int)AdOperators.Chartboost:
//         //             break;
//         //         case (int)AdOperators.Unity:
//         //             mRewardVideoId = id;
//         //             var options = new ShowOptions { resultCallback = hoIDAhoMma };
//         //             Advertisement.Show("rewardedVideo", options);
//         //             break;
//         //         case (int)AdOperators.Applovin:
//         //             break;
//         //     }
//         // }
//         // else if (OnPAlKiNtO != null)
//         // {
//         //     OnPAlKiNtO(id, false);
//         // }
//     }

//     private void hoIDAhoMma(ShowResult result)
//     {
//         tArKiSTaPaLkINto(result == ShowResult.Finished);
//     }

//     private void tArKiSTaPaLkINto(bool tuLOs)
//     {
//         bool onkoOk = false;
//         if (tuLOs)
//         {
//             switch (mRewardVideoId)
//             {
//                 case (int)RewardVideo.KERROIN:
//                     onkoOk = true;
//                     break;
//                 default:
//                     break;
//             }
//         }
//         if (OnPAlKiNtO != null)
//         {
//             OnPAlKiNtO(mRewardVideoId, onkoOk);
//             mRewardVideoId = (int)RewardVideo.NONE;
//         }
//     }



// #if SC_OBFUS
//     [ObfuscateLiterals]
// #endif
//     private void RequestAdMobBanner()
//     {
// #if UNITY_EDITOR
//         string adUnitId = "unused";
// #elif UNITY_ANDROID
// 		string adUnitId = "ca-app-pub-7159985667273944/8166992312";
// #elif UNITY_IOS
// 		string adUnitId = "ca-app-pub-7159985667273944/9504124716";
// #else
// 		string adUnitId = "unexpected_platform";
// #endif

//         if (adMobBannerView != null)
//         {
//             adMobBannerView.Destroy();
//         }
//         adMobBannerView = new BannerView(adUnitId, AdSize.SmartBanner, adMobPosition);
//         AdRequest request = new AdRequest.Builder()
// #if SOFTCEN_DEBUG
//         .AddTestDevice(AdRequest.TestDeviceSimulator)
//         .AddTestDevice("B1C7958170E35E796CE2606E00EE3658") // Android
//         .AddTestDevice("A9A49E1EF7AE2AAA6ECE0F1A5FF638EA") // Android
//         .AddTestDevice("405408742C2FCB884206B2E6FBD27798") // Android New
//         .AddTestDevice("6728F97B9F105B95A782D36EBE332E36") // Galaxy S4
//         .AddTestDevice("eb6075ef9d4e5caebb4aa9e1833980e5") // IOS
//         .AddTestDevice("961561C7EBC127C25D41A47A2D6BCA1B") // Galaxy S4 softcen.test
//         .AddTestDevice("8d76fb1fdef21089d52356477277eeba") // iPad mini
//         .AddTestDevice("C303FC5CBE87A2A7AB3C736CDD768BFF") // Galaxy S7
// #endif
//         .Build();
//         if (adMobBannerKeywords.Length > 0)
//         {
//             for (int i = 0; i < adMobBannerKeywords.Length; i++)
//             {
//                 request.Keywords.Add(adMobBannerKeywords[i]);
//             }
//         }

//         adMobBannerView.LoadAd(request);
//     }

    public void HideBanner()
    {
        // if (adMobBannerView != null)
        // {
        //     adMobBannerView.Destroy();
        // }
    }

//     void OnApplicationPause(bool pauseStatus)
//     {
//         if (mInitialized)
//         {
//             if (pauseStatus)
//             {
//                 HideBanner();
//             }
//             else
//             {
//                 RequestAdMobBanner();
//             }
//         }
//     }

}
