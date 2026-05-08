using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ArKKuAIkA : MonoBehaviour {
    public ArKKuTyyPPi.tyyppi tyyppi;
    private ArKKuHaLLitSija ah;
    private float timer = 0;
    public TextMeshProUGUI tmp;
	// Use this for initialization
	void Start () {
        ah = ArKKuHaLLitSija.instance;
        tmp = GetComponent<TextMeshProUGUI> ();
	}

	// Update is called once per frame
	void Update () {
        if (ah == null || tmp == null) return;
        timer += Time.deltaTime;
        if (timer > 1f) {
            timer -= 1f;
            tmp.SetText (ah.aika (ArKKuTyyPPi.tyyppi.ILMAINEN));
        }
	}
}
