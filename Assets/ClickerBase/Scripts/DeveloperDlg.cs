using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DeveloperDlg : MonoBehaviour {

	// Use this for initialization
	void Awake () {

	}

    public void ResetGame()
	{
        #if SOFTCEN_DEBUG
        pELiNhaLLitSIJa.Instance.playerData.Init();
        pELiNhaLLitSIJa.Instance.playerData._changed = true;
        pELiNhaLLitSIJa.Instance.Kerroin.alusta();
        pELiNhaLLitSIJa.Instance.Save();
		SceneManager.LoadScene(GameConsts.Skenes.Home);
		#endif
	}

	public void IncMoney(int amount) {
        #if SOFTCEN_DEBUG
        IncMoney((double)amount);
        #endif
    }

    public void IncMoney(double amount) {
        // #if SOFTCEN_DEBUG
        // pELiNhaLLitSIJa.Instance.playerData.ChangeMoney (amount);
		// #endif
	}

}
