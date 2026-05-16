using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class UICategoryStrip : MonoBehaviour
{
    [SerializeField] private UIImprovementTapButton[] tabs;

    void Awake()
    {
        for(int i = 0; i < tabs.Length; i++)
        {
            if (tabs[i] == null) continue;
            tabs[i].Bind(this, 0);
        }
        ClearSelection();
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
