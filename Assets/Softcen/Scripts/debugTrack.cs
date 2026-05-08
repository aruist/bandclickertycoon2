using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class debugTrack : MonoBehaviour {
	#if SOFTCEN_DEBUG
	public static debugTrack instance;
	#endif
	void Awake () {
		#if SOFTCEN_DEBUG
		instance = this;
		#else
		DestroyObject(gameObject);
		#endif
	}

	#if SOFTCEN_DEBUG
	#endif
}
