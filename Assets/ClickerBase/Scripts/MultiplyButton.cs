using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if SC_OBFUS
using Beebyte.Obfuscator;
#endif

public class MultiplyButton : MonoBehaviour {
	public GameObject[] taps;
	public TextMeshProUGUI txtBtn;

	// Use this for initialization
	void Start () {
		UpdateButtonText ();
	}

#if SC_OBFUS
    [SkipRename]
#endif
    public void NapinPainallus() {
		SoundFXManager.PlayUIOneShot(SoundFXManager.DefaultSounds.UI_KEYBOARD_CLICK);
        pELiNhaLLitSIJa.Instance.Multiply++;
		UpdateButtonText ();
	}

	private void UpdateButtonText() {
		taps [0].SetActive (false);
		taps [1].SetActive (false);
		taps [2].SetActive (false);
		if (pELiNhaLLitSIJa.Instance == null) return;

		int currentSelection = pELiNhaLLitSIJa.Instance.Multiply;
		if (currentSelection == 0) {
			txtBtn.SetText("1X");
			taps [0].SetActive (true);
		} else if (currentSelection == 1) {
			txtBtn.SetText("10X");
			taps [1].SetActive (true);
		} else {
			txtBtn.SetText("50X");
			taps [2].SetActive (true);
		}
	}
}
