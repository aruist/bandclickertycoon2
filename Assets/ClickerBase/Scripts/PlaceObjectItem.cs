using UnityEngine;
using System.Collections;
using WhiteCat.Paths;
using Softcen.Clicker.Core;

public class PlaceObjectItem : MonoBehaviour {

    public enum state
    {
        NotActive,
        Start,
        Moving,
    }
    [SerializeField] private ScenePoolPrewarm.ParticleFX particleFX;

    public bool isActiveUp = true;
    public float delayBeforeStart = 5f;
    public float moveTime = 2f;
    public ParticleSystem _particleSystem;
	private ObjectPooler _effectPool;
    public bool isActivated = false;

    private state _state;
    private float _activePosition;
    private float _timer;
    // Update is called once per frame
    private Renderer rend;
    private MoveAlongPathWithSpeed pathSpeed;
    private bool _effectPlayed;
    [SerializeField] private Vector3 originalPos;
    private GameObject _cachedGO;

    void Awake()
    {
        rend = GetComponent<Renderer>();
        pathSpeed = GetComponent<MoveAlongPathWithSpeed>();
    }
	void Update () {
        if (_state == state.NotActive || _cachedGO == null)
            return;

        _timer += Time.deltaTime;
        if (_state == state.Start)
        {
            if (_timer >= delayBeforeStart)
            {
                _timer = 0;
                _state = state.Moving;
                if (!_effectPlayed)
                {
                    PooledFxPool.Spawn(particleFX, transform.position);
                    _effectPlayed = true;
                }
            }
        }
        else if (_state == state.Moving)
        {
            Vector3 pos = transform.localPosition;
            pos.y = Mathf.Lerp(pos.y, _activePosition, _timer / moveTime);
            if (_timer >= moveTime)
            {
                pos.y = _activePosition;
                _timer = 0;
                enabled = false;
				if (pathSpeed != null)
				{
					pathSpeed.enabled = true;
				}
                if (!isActiveUp) _cachedGO.SetActive(false);
            }
            transform.localPosition = pos;
        }

	}

	public void StartActivate(float activePos, ObjectPooler pool)
    {
        if (_cachedGO == null) return;
		_effectPool = pool;
        _effectPlayed = false;
        enabled = true;
        if (isActiveUp) _activePosition = originalPos.y;
        else _activePosition = activePos;
        _timer = 0f;
        _state = state.Start;
        isActivated = true;
        if (!_cachedGO.activeSelf) _cachedGO.SetActive(true);
    }

    public void Initialize()
    {
        originalPos = transform.localPosition;
        _cachedGO = gameObject;
    }

    public void SetObject(float yPos, bool isActive)
    {
        if (_cachedGO == null) return;
        if (pathSpeed != null)
        {
            pathSpeed.enabled = isActive;
        }

		isActivated = isActive;
		_state = state.NotActive;
		Vector3 pos = transform.localPosition;
        if (isActiveUp && isActive) pos.y = originalPos.y;
		else pos.y = yPos;
		transform.localPosition = pos;
        gameObject.SetActive(isActive);
		enabled = false;
        if (isActive && !isActiveUp) gameObject.SetActive(false);
    }
}
