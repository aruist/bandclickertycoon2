using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ajat : MonoBehaviour {
    //public ajattieto tieto;

    // void Awake()
    // {
    //     //lataa();
    // }

    // public bool lataa()
    // {
    //     string tiedosto = "agbvf8s?encrypt=true&password=bgs7qqlg28kkz5tW5g&saveLocation=playerprefs&tag=myTag";
    //     #if SOFTCEN_DEBUG
    //     Debug.Log("lataa() " + tiedosto);
    //     #endif
    //     if (ES2.Exists(tiedosto))
    //     {
    //         try
    //         {
    //             tieto = ES2.Load<ajattieto>(tiedosto);
    //         }
    //         catch
    //         {
    //             tieto = new ajattieto();
    //             uusiIlmainenArkku();
    //             return false;
    //         }
    //     } else
    //     {
    //         tieto = new ajattieto();
    //         uusiIlmainenArkku();
    //     }
    //     return true;
    // }

    // public bool tallenna()
    // {
    //     string tiedosto = "agbvf8s?encrypt=true&password=bgs7qqlg28kkz5tW5g&saveLocation=playerprefs&tag=myTag";
    //     #if SOFTCEN_DEBUG
    //     Debug.Log("tallenna() " + tiedosto);
    //     #endif
    //     try
    //     {
    //         ES2.Save(tieto, tiedosto);
    //     }
    //     catch
    //     {
    //         return false;
    //     }
    //     return true;
    // }

    // public void uusiIlmainenArkku()
    // {
    //     if (tieto != null)
    //     {
    //         tieto._ilmainenLaskuri++;
    //         tieto._ilmainenArkkuAlku = DateTime.UtcNow.Ticks;
    //         tallenna();
    //     }

    // }

    // public long annaIlmainenArkkuAika()
    // {
    //     long kesto = 0;
    //     if (tieto._ilmainenLaskuri <= 1)
    //         kesto = TimeSpan.FromMinutes(60).Ticks;
    //     else if (tieto._ilmainenLaskuri <= 2)
    //         kesto = TimeSpan.FromMinutes(120).Ticks;
    //     else if (tieto._ilmainenLaskuri <= 3)
    //         kesto = TimeSpan.FromMinutes(180).Ticks;
    //     else if (tieto._ilmainenLaskuri <= 4)
    //         kesto = TimeSpan.FromMinutes(240).Ticks;
    //     else
    //         kesto = TimeSpan.FromMinutes(300).Ticks;

    //     long ticks = DateTime.UtcNow.Ticks;
    //     return kesto - (ticks - tieto._ilmainenArkkuAlku);
    // }

    // public string annaAika(long aika)
    // {
    //     string aikaJono = "";
    //     TimeSpan timeSpan = TimeSpan.FromTicks(aika);
    //     if (timeSpan.TotalSeconds <= 0)
    //     {
    //         aikaJono = "0 s";
    //     }
    //     else if (timeSpan.Hours > 60)
    //     {
    //         aikaJono = "> 60 hours";
    //     }
    //     else
    //     {
    //         if (timeSpan.TotalSeconds <= 60)
    //         {
    //             aikaJono = timeSpan.TotalSeconds.ToString() + " s";
    //         }
    //         else if (timeSpan.TotalMinutes <= 60)
    //         {
    //             aikaJono = timeSpan.Minutes.ToString() + "m " + timeSpan.Seconds.ToString() + "s";
    //         }
    //         else
    //         {
    //             aikaJono = timeSpan.Hours.ToString() + "h " + timeSpan.Minutes.ToString() + "m";
    //         }
    //     }
    //     return aikaJono;
    // }

    // public void PaIVItaKokoNAIsKAtseLUAika(double aIKA)
    // {
    //     if (tieto != null)
    //         tieto.ASetAPelaTTuAIka(aIKA);
    // }

    // public TimeSpan KOkonaISPeLAtTuAIka
    // {
    //     get
    //     {
    //         if (tieto != null)
    //         {
    //             return tieto.KOkonaISPeLAtTuAIka;
    //         }
    //         return TimeSpan.Zero;
    //     }
    // }
}
