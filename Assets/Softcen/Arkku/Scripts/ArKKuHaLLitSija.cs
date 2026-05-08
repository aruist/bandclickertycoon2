using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class ArKKuHaLLitSija : MonoBehaviour {
    public static ArKKuHaLLitSija instance;
    public ArKKu ilmainenArkku;
    public ArKKu[] slots;

    void Awake() {
        if (instance == null) {
            instance = this;
            DontDestroyOnLoad (gameObject);
            for (int i=0; i < slots.Length; i++) {
                slots [i].Tyyppi = ArKKuTyyPPi.tyyppi.TYHJA;
            }
        } else {
            Destroy (gameObject);
        }    
    }
	// Use this for initialization
	void Start () {
        ilmainenArkku = new ArKKu (ArKKuTyyPPi.tyyppi.ILMAINEN);
        ilmainenArkku.KaynnistaAika ();            
	}

    public string aika(ArKKuTyyPPi.tyyppi t) {
        if (t == ArKKuTyyPPi.tyyppi.ILMAINEN)
            return ilmainenArkku.aika ();
        return "";
    }
	
}
