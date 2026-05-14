using UnityEngine;

public class StartMusic : MonoBehaviour
{
    [SerializeField] private string startmusicId;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (SongLibraryManager.Instance == null) return;
        SongLibraryManager.Instance.LoadAndPlaySong(startmusicId, (success, message) =>
        {
            #if SOFTCEN_DEBUG
            Debug.Log(message);
            #endif
        });

    }

}
