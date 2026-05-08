using UnityEngine;

public class tyHjANaPpAiN : MonoBehaviour {
    public GameObject goTyhjaNappain;
	void OnEnable () {
        goTyhjaNappain.SetActive(false);
	}
    void OnDisable()
    {
        goTyhjaNappain.SetActive(true);
    }
}
