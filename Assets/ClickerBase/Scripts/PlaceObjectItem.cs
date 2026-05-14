using UnityEngine;
using System.Collections;
using WhiteCat.Paths;
using Softcen.Clicker.Core;
using System.Collections.Generic;

public class PlaceObjectItem : MonoBehaviour {

    public enum state
    {
        NotActive,
        Start,
        Moving,
    }
    [SerializeField] private ScenePoolPrewarm.ParticleFX particleFX;
    [SerializeField] private List<PlaceObjectItemActivate> activateAfterShowUp;

    public bool showWhenPurchased = true;
    public float delayBeforeStart = 0f;
    public float moveTime = 2f;
    public ParticleSystem _particleSystem;
    public bool isPurchased = false;

    private state _state;
    private float _activePosition;
    private float _timer;
    // Update is called once per frame
    private Renderer rend;
    private MoveAlongPathWithSpeed pathSpeed;
    private bool _effectPlayed;
    [SerializeField] private Vector3 originalPos;
    private GameObject _cachedGO;
    private Vector3 targetPos;
    private Vector3 startPos;

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
                if (!_effectPlayed && !showWhenPurchased)
                {
                    // Item dissapear so we need to show effect right away
                    PooledFxPool.Spawn(particleFX, transform.position);
                    _effectPlayed = true;
                }
            }
        }
        if (_state == state.Moving)
        {
            float t = _timer / moveTime;
            t = Mathf.Clamp01(t);
            t = Mathf.SmoothStep(0f, 1f,t);
            transform.localPosition = Vector3.LerpUnclamped(startPos, targetPos, t);

            if (_timer >= moveTime)
            {
                transform.localPosition = targetPos;
                if (!_effectPlayed && showWhenPurchased)
                {
                    // Item show up now it is right time to show effect
                    PooledFxPool.Spawn(particleFX, transform.position);
                    _effectPlayed = true;
                }
                enabled = false;
                _timer = 0;
                _state = state.NotActive;
                if (!showWhenPurchased) _cachedGO.SetActive(false);
                TryActivateAfterShowUp(true);
            }
        }
	}

    private void TryActivateAfterShowUp(bool state)
    {
        if (activateAfterShowUp == null || activateAfterShowUp.Count == 0) return;
        for (int i=0; i < activateAfterShowUp.Count; i++)
        {
            if (activateAfterShowUp[i] == null) continue;
            activateAfterShowUp[i].ActivateScript(state);
        }
    }

	public void StartActivate(float outOfViewPosY)
    {
        if (_cachedGO == null) _cachedGO = gameObject;

        Vector3 pos = transform.localPosition;
        isPurchased = true;
        _timer = 0f;
        _state = state.Start;
        _effectPlayed = false;
        enabled = true;
        _cachedGO.SetActive(true);

        // Item purchased it is time to movi it
        if (showWhenPurchased)
        {
            // Move from out of screen to screen
            pos.y = outOfViewPosY;
            transform.localPosition = pos;
            startPos = pos;
            targetPos = originalPos;
        }
        else
        {
            // Move from screen to out of screen
            startPos = pos;
            targetPos = originalPos;
            targetPos.y = outOfViewPosY;
        }

        // if (showWhenPurchased) _activePosition = originalPos.y;
        // else _activePosition = outOfViewPosY;
        // _timer = 0f;
        // _state = state.Start;
        // isPurchased = true;
        // if (!_cachedGO.activeSelf) _cachedGO.SetActive(true);
    }

    public void Initialize()
    {
        originalPos = transform.localPosition;
        _cachedGO = gameObject;
    }

    public void SetObject(float outOfViewPosY, bool isPurchased)
    {
        if (_cachedGO == null) _cachedGO = gameObject;
        if (pathSpeed != null)
        {
            pathSpeed.enabled = isPurchased;
        }

		this.isPurchased = isPurchased;
		_state = state.NotActive;
		Vector3 pos = transform.localPosition;
        if (isPurchased)
        {
            // Item purhased
            if (showWhenPurchased) {
                pos.y = originalPos.y;
                gameObject.SetActive(true);
                TryActivateAfterShowUp(true);
                enabled = false; // this script can be turned off
        		transform.localPosition = pos;
                return;
            }
            else  {
                // No need to show anymore and no need to be active
                TryActivateAfterShowUp(false);
                gameObject.SetActive(false);
                return;
            }
        }
        else
        {
            // Item Nor purhased yet
            if (showWhenPurchased)
            {
                pos.y = outOfViewPosY;
        		transform.localPosition = pos;
                TryActivateAfterShowUp(false);
                gameObject.SetActive(false);
                return;
            }
            else
            {
                // Item should show if not purhased yet
                pos.y = originalPos.y;
        		transform.localPosition = pos;
                TryActivateAfterShowUp(true);
                gameObject.SetActive(true);
            }

        }

		// Vector3 pos = transform.localPosition;
        // if (showWhenPurchased && isActive) pos.y = originalPos.y;
		// else pos.y = yPos;
		// transform.localPosition = pos;
        // gameObject.SetActive(isActive);
		// enabled = false;
        // if (isActive && !showWhenPurchased) gameObject.SetActive(false);
    }
}
