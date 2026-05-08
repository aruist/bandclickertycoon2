using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour {
    public static AudioManager instance;
    public AudioClip acUIButton;
    public AudioClip acCoin;

    private AudioSource audioSource;
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            audioSource = GetComponent<AudioSource>();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlayAudioClip(AudioClip clip)
    {
        audioSource.PlayOneShot(clip);
    }

    public void PlayUIButtonClick()
    {
        audioSource.PlayOneShot(acUIButton);
    }

    public void PlayCoin()
    {
        audioSource.PlayOneShot(acCoin);
    }
}
