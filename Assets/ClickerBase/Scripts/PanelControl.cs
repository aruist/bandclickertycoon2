using UnityEngine;
using System.Collections;

public class PanelControl : MonoBehaviour {
	public Animator anim;


	// Use this for initialization
	void Start () {
		anim = GetComponent<Animator> ();
	}

	public void ChangePosition() {
		anim.SetTrigger ("ChangePos");
	}
}
