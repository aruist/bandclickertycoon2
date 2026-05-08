using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class arkkuikkuna : MonoBehaviour {
    public Text ilmainenAika;
    public GameObject goIlmainenAikaPartikkeli;

    private bool mIlmainenAikaValmis = false;
    public Camera m_CurrentOrbitCamera = null;

    // Touch phases
    TouchPhase previousSingleTouchPhase = TouchPhase.Canceled;
    TouchPhase currentSingleTouchPhase = TouchPhase.Canceled;
    // Touch stationary time and duration variables
    public float TouchStationaryTime = 1.0f;
    public float TouchStationaryDuration = 0.0f;

    private float ajastin;
    private pELiNhaLLitSIJa gm;
	// Use this for initialization
	void OnEnable () {
        ajastin = 0;
        gm = pELiNhaLLitSIJa.Instance;
        PaivitaIlmainenArkku();
    }

	// Update is called once per frame
	void Update () {
        ajastin += Time.deltaTime;
        if (ajastin >= 1f)
        {
            ajastin -= 1f;
            if (!mIlmainenAikaValmis)
                PaivitaIlmainenArkku();
        }

        // If there is single touch input
        if (Input.touchCount == 1)
        {
            // Get touch
            Touch touch = Input.GetTouch(0);

            // Touch began
            if (touch.phase == TouchPhase.Began)
            {
                // Keep current touch phase
                currentSingleTouchPhase = TouchPhase.Began;

                // reset touch stationary duration
                TouchStationaryDuration = 0;
            }
            // Touch moves
            else if (touch.phase == TouchPhase.Moved)
            {
                // Keep current touch phase
                currentSingleTouchPhase = TouchPhase.Moved;

                // reset touch stationary duration
                TouchStationaryDuration = 0;
            }
            // Touch stationary
            else if (touch.phase == TouchPhase.Stationary)
            {
                // Check if there is a hit on a chest
                GameObject pMain = GetHitChest(touch.position);
                if (pMain != null)
                {
                    // Keep current touch phase
                    currentSingleTouchPhase = TouchPhase.Stationary;

                    // If last touch phase is stationary
                    if (previousSingleTouchPhase == TouchPhase.Stationary)
                    {
                        // Increase stationary duration
                        TouchStationaryDuration += Time.deltaTime;

                        // Toggle lock/unlock if stationary has reached it limitation duration
                        if (TouchStationaryDuration > TouchStationaryTime && TouchStationaryDuration < TouchStationaryTime + 1)
                        {
                            // Toggle lock/unlock
                            Debug.Log("Toggle lock");
                            //pMain.ToggleLock();
                            TouchStationaryDuration = TouchStationaryTime * 2;
                        }
                    }
                }
            }
            // Touch ended
            else if (touch.phase == TouchPhase.Ended)
            {
                // Keep current touch phase
                currentSingleTouchPhase = TouchPhase.Ended;

                // If last touch phase is not stationary
                if (previousSingleTouchPhase == TouchPhase.Began || (previousSingleTouchPhase == TouchPhase.Stationary && TouchStationaryDuration < TouchStationaryTime))
                {
                    // Toggle open/close
                    GameObject pMain = GetHitChest(touch.position);
                    if (pMain != null)
                    {
                        Debug.Log("Toggle open");
                        //pMain.ToggleOpen();
                    }
                }

                // Reset touch stationary duration
                TouchStationaryDuration = 0;
            }

            // Store current touch phase in previous touch phase
            previousSingleTouchPhase = currentSingleTouchPhase;

            // set currentSingleTouchPhase to TouchPhase.Canceled
            currentSingleTouchPhase = TouchPhase.Canceled;
        }
        // Mouse inputs
        else
        {
            // User pressed the left mouse up
            if (Input.GetMouseButtonUp(0))
            {
                MouseButtonUp(0);
            }
            // User pressed the right mouse up
            else if (Input.GetMouseButtonUp(1))
            {
                MouseButtonUp(1);
            }
        }

    }

    private void PaivitaIlmainenArkku()
    {
        // long jaljella = gm.mAjat.annaIlmainenArkkuAika();
        // if (jaljella <= 0)
        // {
        //     mIlmainenAikaValmis = true;
        //     ilmainenAika.text = "Tap to open";
        //     goIlmainenAikaPartikkeli.SetActive(true);
        // }
        // else
        // {
        //     if (goIlmainenAikaPartikkeli.activeSelf)
        //         goIlmainenAikaPartikkeli.SetActive(false);
        //     mIlmainenAikaValmis = false;
        //     ilmainenAika.text = gm.mAjat.annaAika(jaljella);
        // }
    }

    GameObject GetHitChest(Vector3 hitPosition)
    {
        // Make sure we have orbit camera
        if (m_CurrentOrbitCamera == null)
        {
            Debug.Log("Orbit camera not found!");
        }
        else
        {
            // We need to actually hit an object
            RaycastHit hitt;
            if (Physics.Raycast(m_CurrentOrbitCamera.ScreenPointToRay(hitPosition), out hitt, 1000))
            {
                if (hitt.collider)
                {
                    if (hitt.collider.tag.Equals("arkku"))
                    {
                        // Return go of the chest that was hit
                        return hitt.collider.gameObject;
                    }
                }
            }
        }

        return null;
    }

    // Toggle Open or lock
    void MouseButtonUp(int Button)
    {
        GameObject pMain = GetHitChest(Input.mousePosition);
        if (pMain != null)
        {
            if (Button == 0)
            {
                Debug.Log("Toggle open " + pMain.name);
                //pMain.ToggleOpen();
            }
            else if (Button == 1)
            {
                Debug.Log("Toggle lock" + pMain.name);
                //pMain.ToggleLock();
            }
        }
    }

}
