using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class EventSystemFixer : MonoBehaviour {
	public Canvas myCanvas;
	public EventSystem myEventSystem;

	// Use this for initialization
	void Start () {
		//Debug.Log ("Canvas scalefactor: " + myCanvas.scaleFactor);
		if (myCanvas.scaleFactor >= 1)
			myEventSystem.pixelDragThreshold = (int)(5 * myCanvas.scaleFactor);
		else
			myEventSystem.pixelDragThreshold = 5;	
	}
	
}
