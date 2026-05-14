using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlaceObjects : MonoBehaviour {
	[SerializeField] private List<PlaceObjectItem> objList;
	//private Stack<Transform> stackDestroy;
    public bool movingUp = true;
	public float outOfViewPos = -50f;
	//private int currentLevel;
	public ObjectPooler _effectPool;

	// Use this for initialization
	void Awake () {
		objList = new List<PlaceObjectItem> ();
		//stackDestroy = new Stack<Transform> ();
		for (int i = 0; i < transform.childCount; i++) {
            PlaceObjectItem poi = transform.GetChild(i).GetComponent<PlaceObjectItem>();
            if (poi != null)
            {
                objList.Add(poi);
            }
        }
	}

    public void InitPlace(int level)
    {
        //currentLevel = level;
        if (objList != null)
        {
            for (int i = 0; i < objList.Count; i++)
            {
                if (objList[i] == null) continue;
                objList[i].Initialize();
                if (i < level)
                {
                    objList[i].SetObject(outOfViewPos, true);
                }
                else
                {
                    objList[i].SetObject(outOfViewPos, false);
                }
            }
        }
    }

	public Transform UpdateLevel(int level) {
		Transform lastTransform = null;
		//currentLevel = level;
		for (int i = 0; i < objList.Count; i++) {
			if (i < level) {
				if (!objList[i].isPurchased)
                {
					objList[i].StartActivate(outOfViewPos);
				}
            }
		}
		return lastTransform;
	}
}
