using UnityEngine;
using System.Collections;
using WhiteCat.Tween;
#if SC_OBFUS
using Beebyte.Obfuscator;
#endif

public class DialogPanel : MonoBehaviour {
    public Tweener[] tweenerAnims;

    void OnEnable()
    {
        for (int i = 0; i < tweenerAnims.Length; i++)
        {
            tweenerAnims[i].isForward = true;
            tweenerAnims[i].normalizedTime = 0;
            tweenerAnims[i].enabled = true;
        }
    }

#if SC_OBFUS
    [SkipRename]
#endif
    public void CloseDialog()
    {
        SoundFXManager.PlayUIOneShot(SoundFXManager.DefaultSounds.UI_KEYBOARD_CLICK);
        for (int i=0; i < tweenerAnims.Length; i++)
        {
            tweenerAnims[i].enabled = true;
        }
    }
}
