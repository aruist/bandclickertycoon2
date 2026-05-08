using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
#if SC_OBFUS
using Beebyte.Obfuscator;
#endif

public class Settings : MonoBehaviour {
    public GameObject goDevButton;
    public Slider sliderAudio;
    public Slider sliderMusic;
    public AnimationCurve audioCurve;

    // Use this for initialization
    void Awake () {
		#if SOFTCEN_DEBUG
		goDevButton.SetActive(true);
		#else
		goDevButton.SetActive(false);
		#endif
	}

    void OnEnable()
    {
        sliderAudio.value = getSliderVal(pELiNhaLLitSIJa.Instance.playerData._aanivoluumi);
        /*float valueTarget = 1f - (pELiNhaLLitSIJa.Instance.playerData._musiikkivoluumi / -80f);
        for (int i=0; i < 100; i++)
        {
            float value = audioCurve.Evaluate((float)i/100f);
            if (value >= valueTarget)
            {
                valueTarget = (float)i/100f;
                break;
            }
        }*/
        //float volume = Mathf.Lerp(0, 1, t);
        //Debug.Log("value: " + valueTarget + ", orig volume: " + pELiNhaLLitSIJa.Instance.playerData._musiikkivoluumi);

        sliderMusic.value = getSliderVal(pELiNhaLLitSIJa.Instance.playerData._musiikkivoluumi);
    }

    private float getSliderVal(float vol)
    {
        float valueTarget = 1f - (vol / -80f);
        for (int i = 0; i < 100; i++)
        {
            float value = audioCurve.Evaluate((float)i / 100f);
            if (value >= valueTarget)
            {
                valueTarget = (float)i / 100f;
                break;
            }
        }
        if (valueTarget < 0)
            valueTarget = 0;
        else if (valueTarget > 1)
            valueTarget = 1f;
        return valueTarget;
    }

#if SC_OBFUS
    [SkipRename]
#endif
    public void OnSliderAudioChanged()
    {
        float value = audioCurve.Evaluate(sliderAudio.value);
        float volume = Mathf.Lerp(-80, 0, value);
        pELiNhaLLitSIJa.Instance.SetSFXVolume(volume);
    }
#if SC_OBFUS
    [SkipRename]
#endif
    public void OnSliderMusicChanged()
    {
        float value = audioCurve.Evaluate(sliderMusic.value);
        float volume = Mathf.Lerp(-80, 0, value);
        //Debug.Log("slider value: " + sliderMusic.value + ", curve value: " + value + ", volume: " + volume);
        pELiNhaLLitSIJa.Instance.SetMusicVolume(volume);
    }


}
