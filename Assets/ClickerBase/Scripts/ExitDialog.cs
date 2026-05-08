using UnityEngine;
using System.Collections;
#if SC_OBFUS
using Beebyte.Obfuscator;
#endif

public class ExitDialog : MonoBehaviour {
#if SC_OBFUS
    [SkipRename]
#endif
    public void ExitGameButton()
    {
        SoundFXManager.PlayUIOneShot(SoundFXManager.DefaultSounds.UI_KEYBOARD_CLICK);
        pELiNhaLLitSIJa.Instance.ExitGame();
    }
}
