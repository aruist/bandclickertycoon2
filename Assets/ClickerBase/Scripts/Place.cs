using UnityEngine;
using WhiteCat.Paths;
using System.Collections;

public class Place : MonoBehaviour {
	[SerializeField] private Transform cameraTarget;
	public int id;
    public BezierPath camPath;
    public Path.KeyframeList keyFrameList;
    public float camSpeed = 5;
	public PlaceObjects[] placeObjects;
    public GameObject goGroundWork;
	//public Transform spotLight;
	public float spotTransitionTime = 0.5f;
	private bool moveSpot = false;
	//private Vector3 spotTarget;
	private float m_timer;
    // Use this for initialization

    void Awake()
    {
        if (BeatArenaCameraDirector.Instance != null) BeatArenaCameraDirector.Instance.BindPlaceCameraTarget(cameraTarget);
    }

    void Start () {
		if (paIkKaHaLlItSiJa.Instance == null) return;
        for (int i = 0; i < placeObjects.Length; i++) {
            int lvl = paIkKaHaLlItSiJa.Instance.GetLevel(id, i);
            placeObjects[i].InitPlace(lvl-1);
		}
        CheckGroundWorkClose();
    }

	// void Update() {
	// 	if (moveSpot) {
	// 		m_timer += Time.deltaTime;
	// 		//spotLight.position = Vector3.Lerp (spotLight.position, spotTarget, m_timer / spotTransitionTime);
	// 	}
	// }

	void OnEnable() {
		paIkKaHaLlItSiJa.onImprovePurchased += PlacesManager_onImprovePurchased;
	}
    void OnDisable()
    {
        paIkKaHaLlItSiJa.onImprovePurchased -= PlacesManager_onImprovePurchased;
    }

    private void CheckGroundWorkClose()
    {
		if (paIkKaHaLlItSiJa.Instance == null) return;

        if (goGroundWork != null)
        {
            int lvl = paIkKaHaLlItSiJa.Instance.GetLevel(id, 0);
            if (lvl > 25 && goGroundWork.activeSelf)
                goGroundWork.SetActive(false);
            else if (lvl <= 25 && !goGroundWork.activeSelf)
                goGroundWork.SetActive(true);
        }
    }

    void PlacesManager_onImprovePurchased (int placeId, int improvementId, int level)
	{
		Debug.Log ("PlacesManager_onImprovePurchased " + placeId + ", " + improvementId + ", " + level);
		if (placeId == id && improvementId < placeObjects.Length) {
			placeObjects [improvementId].UpdateLevel (level-1);
			/*if (tr != null) {
				GameObject go = DissapearPool.instance.GetPooledObject ();
				Vector3 pos = tr.position;
				pos.y = 0;
				go.transform.position = pos;
				go.SetActive (true);
				spotTarget = pos;
				spotTarget.y = spotLight.position.y;
				m_timer = 0f;
				moveSpot = true;
			}*/
		}
        CheckGroundWorkClose();
    }

}
