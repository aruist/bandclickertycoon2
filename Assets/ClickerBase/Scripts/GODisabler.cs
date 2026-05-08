using UnityEngine;

public class GODisabler : MonoBehaviour {
	public float time = 1f;
	private float _timer;
	// Use this for initialization
	void OnEnable () {
		_timer = 0f;
	}
	
	// Update is called once per frame
	void Update () {
		_timer += Time.deltaTime;
		if (_timer >= time) {
			gameObject.SetActive (false);
		}
	}
}
