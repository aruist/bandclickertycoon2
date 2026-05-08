using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class MoneyCollectedPanel : MonoBehaviour, IPointerDownHandler {
    public static event Action OnCollectedMoneyPressed;

    public void OnPointerDown(PointerEventData data)
    {
        //Debug.Log("OnPointerDown");
        if (OnCollectedMoneyPressed != null)
            OnCollectedMoneyPressed();
    }
}
