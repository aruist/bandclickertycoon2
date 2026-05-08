using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WhiteCat.Tween;

public class arkku_old: MonoBehaviour {

    public float avaamisAika = 0.75f;
    public enum eTila
    {
        Auki,
        Kiinni
    };

    public eTila vanhaTila = eTila.Kiinni;
    public eTila nykyinenTila = eTila.Kiinni;

	// Use this for initialization
	void Start () {
        vanhaTila = eTila.Kiinni;
    }
	
    void Update()
    {
        if (nykyinenTila != vanhaTila)
        {
        }
    }

    public void Avaa()
    {
        Avaa(avaamisAika);
    }

    public void Avaa(float aika)
    {
        avaamisAika = aika;
        nykyinenTila = eTila.Auki;
    }
}
