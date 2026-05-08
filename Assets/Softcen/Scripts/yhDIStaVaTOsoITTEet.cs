using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if SC_OBFUS
using Beebyte.Obfuscator;
#endif

public class yhDIStaVaTOsoITTEet : MonoBehaviour {

#if SC_OBFUS
    [SkipRename]
#endif
    public void liSAApeLEJa()
    {
        SoundFXManager.PlayUIOneShot(SoundFXManager.DefaultSounds.UI_KEYBOARD_CLICK);
        UrlMoreGames();
    }

#if SC_OBFUS
    [ObfuscateLiterals]
#endif
    private void UrlMoreGames()
    {
#if UNITY_ANDROID
        Application.OpenURL("market://search?q=softcen");
#endif
#if UNITY_IOS
        Application.OpenURL("http://appstore.com/softcen");
#endif
    }

#if SC_OBFUS
    [SkipRename]
#endif
    public void liNTuVIs()
    {
        SoundFXManager.PlayUIOneShot(SoundFXManager.DefaultSounds.UI_KEYBOARD_CLICK);
        UrlTwitter();
    }

#if SC_OBFUS
    [ObfuscateLiterals]
#endif
    private void UrlTwitter()
    {
        string twitterpage = "https://www.twitter.com/softcen";
        Application.OpenURL(twitterpage);
    }
#if SC_OBFUS
    [SkipRename]
#endif
    public void nAAMakIRjA()
    {
        SoundFXManager.PlayUIOneShot(SoundFXManager.DefaultSounds.UI_KEYBOARD_CLICK);
        UrlFacebook();
    }

#if SC_OBFUS
    [ObfuscateLiterals]
#endif
    private void UrlFacebook()
    {
        if (checkPackageAppIsPresent("com.facebook.katana"))
        {
#if SOFTCEN_DEBUG
            Debug.Log("com.facebook.katana found");
#endif
            Application.OpenURL("fb://page/435248659944994"); //there is Facebook app installed so let's use it
        }
        else
        {
#if SOFTCEN_DEBUG
            Debug.Log("com.facebook.katana NOT found");
#endif
            string fbpage = "https://www.facebook.com/435248659944994";
            Application.OpenURL(fbpage);
        }
    }

    public bool checkPackageAppIsPresent(string package)
    {
#if UNITY_ANDROID
        AndroidJavaClass up = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject ca = up.GetStatic<AndroidJavaObject>("currentActivity");
        AndroidJavaObject packageManager = ca.Call<AndroidJavaObject>("getPackageManager");

        //take the list of all packages on the device
        AndroidJavaObject appList = packageManager.Call<AndroidJavaObject>("getInstalledPackages", 0);
        int num = appList.Call<int>("size");
        for (int i = 0; i < num; i++)
        {
            AndroidJavaObject appInfo = appList.Call<AndroidJavaObject>("get", i);
            string packageNew = appInfo.Get<string>("packageName");
            if (packageNew.CompareTo(package) == 0)
            {
                return true;
            }
        }
#endif
        return false;
    }
}
