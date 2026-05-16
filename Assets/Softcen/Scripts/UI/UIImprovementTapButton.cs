using System;
using UnityEngine;
using UnityEngine.UI;

public class UIImprovementTapButton : MonoBehaviour
{
    public static event Action<int> OnImprovementTapChanged;

    [SerializeField] Image image;
    [SerializeField] Color colorActive;
    [SerializeField] Color colorInActive;
    private UICategoryStrip categoryStrip;
    private int index;

    public Image GetImage => image;

    public void Bind(UICategoryStrip categoryStrip, int index)
    {
        this.categoryStrip = categoryStrip;
        this.index = index;
    }

    public void SetActive(bool status)
    {
        if (image == null) return;
        image.color = status ? colorActive : colorInActive;
    }

    public void ButtonPressed()
    {
        if (categoryStrip != null) categoryStrip.ClearSelection();
        SetActive(true);
        SoundFXManager.PlayUIOneShot(SoundFXManager.DefaultSounds.UI_KEYBOARD_CLICK);
        OnImprovementTapChanged?.Invoke(index);
    }
}
