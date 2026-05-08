using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if SCSOCIAL
using Softcen.Plugins;
#endif
#if SC_OBFUS
using Beebyte.Obfuscator;
#endif

public class SCGooglePlayServicesButton : MonoBehaviour {
    public Text txtDescription;
	// Use this for initialization
	void Awake () {
#if UNITY_IOS || !SCSOCIAL
        Destroy(gameObject);
#else
        UpdateButtonText();
#endif 
    }

#if SCSOCIAL
    void OnEnable()
    {
        SCSocialServices.SocialStateChangedEvent += SCSocialServices_SocialStateChangedEvent;    
    }

    void OnDisable()
    {
        SCSocialServices.SocialStateChangedEvent -= SCSocialServices_SocialStateChangedEvent;
    }

    private void SCSocialServices_SocialStateChangedEvent()
    {
        UpdateButtonText();
    }


    private void UpdateButtonText()
    {
        if (SCPlugins.SocialServices.isAuthenticated())
        {
            txtDescription.text = "Sign out Google Play Services";
        } else
        {
            txtDescription.text = "Sign in Google Play Services";
        }
    }

#if SC_OBFUS
    [SkipRename]
#endif
    public void SignButtonPress()
    {
        if (SCPlugins.SocialServices.isAuthenticated())
        {
            SCPlugins.SocialServices.SignOut();
        }
        else
        {
            SCPlugins.SocialServices.SignIn();
        }
        UpdateButtonText();
    }
#endif
}
