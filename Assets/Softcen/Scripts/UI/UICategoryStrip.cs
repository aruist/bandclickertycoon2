using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class UICategoryStrip : MonoBehaviour
{
    [SerializeField] private UIImprovementTapButton[] tabs;

    void Awake()
    {
        for(int i = 0; i < tabs.Length; i++)
        {
            if (tabs[i] == null) continue;
            tabs[i].Bind(this, i);
        }
        ClearSelection();
        if (tabs[0] != null) tabs[0].SetActive(true);
    }

    public void ClearSelection()
    {
        for(int i = 0; i < tabs.Length; i++)
        {
            if (tabs[i] == null) continue;
            tabs[i].SetActive(false);
        }

    }

}
