using UnityEngine;
using System.Collections;

public class DissapearPool : ObjectPooler {

	public static DissapearPool instance;

	void Awake()  {
		if (instance != null) {
			Destroy (gameObject);
			return;
		}

		instance = this;
		DontDestroyOnLoad (gameObject);
	}
}
