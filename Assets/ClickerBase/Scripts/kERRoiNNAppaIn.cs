using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class kERRoiNNAppaIn : MonoBehaviour {
    public Text kerroinArvo;
    public Text kerroinAika;
    public GameObject goKerroinAikaBg;
    public GameObject goEmptyButton;

    private CanvasGroup canvasRyhma;
    private Button button;
    private bool timerRunning = false;
    private float _timer = 0;
    private float _nextCheckTime;
    void Awake()
    {
        canvasRyhma = GetComponent<CanvasGroup>();
        button = GetComponent<Button>();
    }

    void OnEnable()
    {
        goEmptyButton.SetActive(false);
        keRrOIn.OnkeRrOInMuuTTUnut += KeRrOIn_OnkeRrOInMuuTTUnut;
    }

    private void KeRrOIn_OnkeRrOInMuuTTUnut()
    {
        tarkistaNappain();
    }

    void OnDisable()
    {
        goEmptyButton.SetActive(true);
        keRrOIn.OnkeRrOInMuuTTUnut -= KeRrOIn_OnkeRrOInMuuTTUnut;
    }

    // Use this for initialization
    void Start () {
        tarkistaNappain();
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
    }

    private void paivitaAika()
    {
        if (pELiNhaLLitSIJa.Instance != null && pELiNhaLLitSIJa.Instance.Kerroin != null)
        {
            long aika = pELiNhaLLitSIJa.Instance.Kerroin.anNAJaljellaOLEvaAIka();
            if (aika < 0)
            {
                //pELiNhaLLitSIJa.Instance.Kerroin.alusta();
                //tarkistaNappain();
                kerroinAika.text = "";
            } else
            {
                kerroinAika.text = NumToStr.annaAikaTickseista(aika);
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

    private void tarkistaNappain()
    {
        if (pELiNhaLLitSIJa.Instance != null && pELiNhaLLitSIJa.Instance.Kerroin != null)
        {
            button.interactable = true;
            canvasRyhma.alpha = 1f;
            if (pELiNhaLLitSIJa.Instance.Kerroin.KerroinArvo > 0)
            {
                timerRunning = true;
                kerroinArvo.text = pELiNhaLLitSIJa.Instance.Kerroin.KerroinArvo.ToString() + "X";
                kerroinAika.gameObject.SetActive(true);
                goKerroinAikaBg.SetActive(true);
                paivitaAika();
            }
            else
            {
                kerroinAika.gameObject.SetActive(false);
                goKerroinAikaBg.SetActive(false);
                timerRunning = false;
                kerroinArvo.text = "-";
            }

        }
        else
        {
            timerRunning = false;
            kerroinAika.gameObject.SetActive(false);
            goKerroinAikaBg.SetActive(false);
            button.interactable = false;
            canvasRyhma.alpha = 0.5f;
        }
    }	
}
