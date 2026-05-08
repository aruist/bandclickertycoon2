using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
#if SC_OBFUS
using Beebyte.Obfuscator;
#endif

public class vaIHToPaNeeLi : MonoBehaviour {
    public static event Action OnVaihtoValmis;

    Animator _anim;
    private bool seuraavaTrig = true;
    private bool initialized = false;

    void Start()
    {
        Debug.Log("Start");
        initialized = true;
        StartCoroutine(aloitaAnimointi());
        //anim.SetBool("Seuraava", seuraavaTrig);
    }
    public Animator anim
    {
        get
        {
            if (_anim == null)
                _anim = GetComponent<Animator>();
            return _anim;
        }
    }
    public void seuraava()
    {
        seuraavaTrig = true;
        Debug.Log("seuraava " + seuraavaTrig);

        //anim.SetBool("Seuraava", true);
    }
    public void edellinen()
    {
        seuraavaTrig = false;
        Debug.Log("edellinen " + seuraavaTrig);
        //anim.SetBool("Seuraava", false);
    }
    void OnEnable()
    {
        if (initialized)
        {
            StartCoroutine(aloitaAnimointi());

            /*if (anim.isInitialized)
                anim.ResetTrigger("FadeOut");

            anim.SetBool("Seuraava", seuraavaTrig);
            if (seuraavaTrig)
            {
                anim.Play("VaihtoPaneeli");

                Debug.Log("Next " + anim.isInitialized);
            }
            else
            {
                anim.Play("VaihtoPaneeliEdellinen");
                Debug.Log("Prev " + anim.isInitialized);
            }*/
        }
    }

#if SC_OBFUS
    [SkipRename]
#endif
    public void voiALOittaaVAIHto()
    {
        if (OnVaihtoValmis != null)
            OnVaihtoValmis();
    }

    public void haivyta()
    {
        if (anim.isInitialized)
            anim.SetTrigger("FadeOut");
    }
#if SC_OBFUS
    [SkipRename]
#endif
    public void suljePanEEli()
    {
#if SOFTCEN_DEBUG
        Debug.Log("suljePanEEli Called");
#endif
        if (gameObject.activeSelf)
            StartCoroutine(sulje());
    }

    private IEnumerator sulje()
    {
        yield return new WaitForEndOfFrame();
        gameObject.SetActive(false);
    }

    private IEnumerator aloitaAnimointi()
    {
        yield return new WaitUntil(() => anim.isInitialized);
        anim.ResetTrigger("FadeOut");
        Debug.Log("aloitaAnimointi " + seuraavaTrig);
        if (seuraavaTrig)
        {
            anim.SetTrigger("Next");
            //anim.Play("VaihtoPaneeli");
            //Debug.Log("Next " + anim.isInitialized);
        }
        else
        {
            anim.SetTrigger("Prev");
            //anim.Play("VaihtoPaneeliEdellinen");
            //Debug.Log("Prev " + anim.isInitialized);
        }

    }
}
