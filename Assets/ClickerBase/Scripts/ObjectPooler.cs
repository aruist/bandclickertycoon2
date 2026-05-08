using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ObjectPooler : MonoBehaviour {
	public int id = 0;

	public GameObject pooledObject;
	public int pooledAmount = 10;
	public bool willGrow = true;
	public bool preWarm = false;
	public Transform preWarmPosition;

	public int maxCount;

	protected List<GameObject> pooledObjects;
	private int i1;


	virtual protected void Start () {
		//Debug.Log("ObjectPooler Start " + gameObject.name);
		maxCount = 0;
		pooledObjects = new List<GameObject>();
		CreatePool();
	}

	private void CreatePool()
	{
		for (int i=0; i < pooledAmount; i++) {
			GameObject go = (GameObject)Instantiate (pooledObject);
			go.transform.SetParent(this.transform, false);
			if (preWarm) {
				go.transform.position = preWarmPosition.position;
			}
			go.SetActive(preWarm);
			pooledObjects.Add(go);
		}
		maxCount = Mathf.Max(maxCount, pooledObjects.Count);
		
	}


	public GameObject GetPooledObject()
	{
		if (pooledObjects == null) {
			pooledObjects = new List<GameObject>();
		}
		for (i1=0; i1 < pooledObjects.Count; i1++) {
			if (!pooledObjects[i1].activeSelf) {
				return pooledObjects[i1];
			}
		}

		if (willGrow) {
			GameObject go = (GameObject)Instantiate(pooledObject);
			go.transform.SetParent(this.transform, false);
			pooledObjects.Add(go);
			maxCount = Mathf.Max(maxCount, pooledObjects.Count);
			return go;
		}

		return null;
	}

	virtual public void DisableAllObjects() {
		if (pooledObjects == null) {
			pooledObjects = new List<GameObject>();
		}
		for (int i=0; i < pooledObjects.Count; i++) {
            if (pooledObjects[i] != null) {
                if (pooledObjects[i].activeSelf) {
                    pooledObjects[i].SetActive(false);
                }
            }

		}
	}

	public void ClearPool()
	{
		//Debug.Log("ClearPool " + gameObject.name + ", count: " + pooledObjects.Count);
		if (pooledObjects != null)
		{
			for (int i=0; i < pooledObjects.Count; i++)
			{
				if (pooledObjects[i] != null)
				{
					Destroy(pooledObjects[i].gameObject);
				}
			}
			pooledObjects.Clear();
		}
	}
	
	public void InitPool(GameObject go, int startCount)
	{
		pooledObject = go;
		pooledAmount = startCount;
		// Clear old ones
		ClearPool();
		CreatePool();
	}

}
