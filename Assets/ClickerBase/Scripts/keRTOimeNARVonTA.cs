using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class keRTOimeNARVonTA : MonoBehaviour {
    public AudioClip acSpin;
    public AudioClip acFireworks;
    public Button btnSpin;
    public Button napKATsoMaINOs;

    public Text txtNykyinenKerroin;
    public Text txtAika;
    public GameObject goKello;

    public GameObject wheel;
    public Text txtCurrent;
    public GameObject[] partikkelit;

    private bool _isStarted = false;
    private float _currentLerpRotationTime;
    private float _finalAngle;
    private float _startAngle = 0;
    private float[] _sectorsAngles;
    private Image btnImage;
    private Image btnKatsoImage;
    private Color btnColor;
    private Color btnMainosColor;

    private bool mMainosTallennettu = false;
    private bool mInitialized = false;

    private bool timerRunning = false;
    private float _timer = 0;
    private float _nextCheckTime;

    // Use this for initialization
    void Start () {
        txtNykyinenKerroin.text = "";
        btnImage = btnSpin.GetComponent<Image>();
        btnKatsoImage = napKATsoMaINOs.GetComponent<Image>();
        btnColor = btnImage.color;
        btnMainosColor = btnKatsoImage.color;
        aseTANapPAiN(true);
        taRKIstNappAIMet();
        aSeTaNyKyINenKeRrOin();
        mInitialized = true;
    }

    private void aSeTaNyKyINenKeRrOin()
    {
        double k = pELiNhaLLitSIJa.Instance.Kerroin.KerroinArvo;
        if (k > 1)
        {
            txtNykyinenKerroin.text = "Current multiplier: " + k.ToString() + "X"; ;
            paivitaAika();
            goKello.SetActive(true);
            timerRunning = true;
        }
        else
        {
            txtNykyinenKerroin.text = "";
            txtAika.text = "";
            goKello.SetActive(false);
        }
    }

    private void taRKIstNappAIMet()
    {
        if (!mMainosTallennettu)
        {
            if (!mAiNOsPomO.instance.onKOPAlkiNToA())
            {
                txtCurrent.text = "No reward this time, please try again later!";
                napKATsoMaINOs.gameObject.SetActive(false);
                btnSpin.gameObject.SetActive(false);
            }
            else
            {
                txtCurrent.text = "";
                napKATsoMaINOs.gameObject.SetActive(true);
                aseTAKaTsElUNapPAiN(true);
                btnSpin.gameObject.SetActive(false);
            }
        }
        else
        {
            // Mainos tallennettu
            txtCurrent.text = "";
            napKATsoMaINOs.gameObject.SetActive(false);
            btnSpin.gameObject.SetActive(true);
            aseTANapPAiN(true);
        }
    }

    void OnEnable()
    {
        mAiNOsPomO.OnPAlKiNtO += MAiNOsPomO_OnPAlKiNtO;
        if (mInitialized)
        {
            taRKIstNappAIMet();
            aSeTaNyKyINenKeRrOin();
        }
    }

    void OnDisable()
    {
        mAiNOsPomO.OnPAlKiNtO -= MAiNOsPomO_OnPAlKiNtO;
    }

    public void kAtSelEMaINos()
    {
        mAiNOsPomO.instance.nAYtapALkiNto((int)mAiNOsPomO.RewardVideo.KERROIN);
        aseTAKaTsElUNapPAiN(false);
        txtCurrent.text = "Commercial break...";
    }

    private void MAiNOsPomO_OnPAlKiNtO(int arg1, bool arg2)
    {
        if (arg1 == (int)mAiNOsPomO.RewardVideo.KERROIN)
        {
            if (arg2)
            {
                // Palkinto hyv�ksytty
                txtCurrent.text = "Reward accepted, please spin wheel now.";
                mMainosTallennettu = true;
            }
            else
            {
                // Palkintoa ei hyv�ksytty
                txtCurrent.text = "Reward was not accepted, please try again.";
                mMainosTallennettu = false;
            }
        }
        taRKIstNappAIMet();
    }

    private void aseTAKaTsElUNapPAiN(bool paalla)
    {
        if (paalla)
        {
            napKATsoMaINOs.interactable = true;
            btnMainosColor.a = 1f;
        }
        else
        {
            napKATsoMaINOs.interactable = false;
            btnMainosColor.a = 0.5f;
        }
        btnKatsoImage.color = btnColor;
    }

    private void aseTANapPAiN(bool paalla)
    {
        if (paalla)
        {
            btnSpin.interactable = true;
            btnColor.a = 1f;
        }
        else
        {
            btnSpin.interactable = false;
            btnColor.a = 0.5f;
        }
        btnImage.color = btnColor;
    }

    void Update()
    {
        if (timerRunning)
        {
            _timer += Time.deltaTime;
            if (_timer >= _nextCheckTime)
            {
                _timer = 0;
                paivitaAika();
            }
        }

        if (!_isStarted)
            return;

        float maxLerpRotationTime = 3f;

        // increment timer once per frame
        _currentLerpRotationTime += Time.deltaTime;
        if (_currentLerpRotationTime > maxLerpRotationTime || wheel.transform.eulerAngles.z == _finalAngle)
        {
            AudioManager.instance.PlayAudioClip(acFireworks);
            _currentLerpRotationTime = maxLerpRotationTime;
            _isStarted = false;
            _startAngle = _finalAngle % 360;
            miKAKeRROinTULi();
            //aseTANapPAiN(true);
            taRKIstNappAIMet();
            aSeTaNyKyINenKeRrOin();
            for (int i=0; i < partikkelit.Length; i++)
            {
                if (!partikkelit[i].activeSelf)
                    partikkelit[i].SetActive(true);
            }
        }

        // Calculate current position using linear interpolation
        float t = _currentLerpRotationTime / maxLerpRotationTime;

        // This formulae allows to speed up at start and speed down at the end of rotation.
        // Try to change this values to customize the speed
        t = t * t * t * (t * (6f * t - 15f) + 10f);

        float angle = Mathf.Lerp(_startAngle, _finalAngle, t);
        wheel.transform.eulerAngles = new Vector3(0, 0, angle);
    }

    public void pyoRAYtapYORaa()
    {
        SoundFXManager.PlayUIOneShot(SoundFXManager.DefaultSounds.UI_KEYBOARD_CLICK);
        AudioManager.instance.PlayAudioClip(acSpin);
        mMainosTallennettu = false;
        aseTANapPAiN(false);
        _currentLerpRotationTime = 0f;

        _sectorsAngles = new float[] { 45, 90, 135, 180, 225, 270, 315, 360};

        int fullCircles = 5;
        float randomFinalAngle = _sectorsAngles[UnityEngine.Random.Range(0, _sectorsAngles.Length)];

        // Here we set up how many circles our wheel should rotate before stop
        _finalAngle = -(fullCircles * 360 + randomFinalAngle);
        _isStarted = true;
        txtCurrent.text = "Spin...";
    }

    private int miKAKeRROinTULi()
    {
        int retVal = 0;
        switch ((int)_startAngle)
        {
            case 0:
                retVal = 3;
                break;
            case -315:
                retVal = 2;
                break;
            case -270:
                retVal = 3;
                break;
            case -225:
                retVal = 2;
                break;
            case -180:
                retVal = 3;
                break;
            case -135:
                retVal = 2;
                break;
            case -90:
                retVal = 4;
                break;
            case -45:
                retVal = 2;
                break;
            default:
                retVal = 0;
                break;
        }

        txtCurrent.text = "Multiplier " + retVal.ToString() + "X";
        pELiNhaLLitSIJa.Instance.Kerroin.aSEtaKErroIN(retVal, DateTime.UtcNow.Ticks, TimeSpan.FromMinutes(120).Ticks);
        pELiNhaLLitSIJa.Instance.TallennaKerroin();
#if SOFTCEN_DEBUG
        Debug.Log("Uusi kerroin on " + retVal + "X, " + _startAngle);
#endif
        return retVal;
    }

    private void paivitaAika()
    {
        if (pELiNhaLLitSIJa.Instance != null && pELiNhaLLitSIJa.Instance.Kerroin != null)
        {
            long aika = pELiNhaLLitSIJa.Instance.Kerroin.anNAJaljellaOLEvaAIka();
            if (aika < 0)
            {
                pELiNhaLLitSIJa.Instance.Kerroin.alusta();
                aSeTaNyKyINenKeRrOin();
            }
            else
            {
                txtAika.text = NumToStr.annaAikaTickseista(aika);
                TimeSpan timeSpan = TimeSpan.FromTicks(aika);
                if (timeSpan.TotalMinutes > 60)
                {
                    _nextCheckTime = 60.1f;
                }
                else
                {
                    _nextCheckTime = 1f;
                }
            }
        }
        else
        {
            timerRunning = false;
        }
    }

}
