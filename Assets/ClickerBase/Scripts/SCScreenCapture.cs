using UnityEngine;
using System.IO;
using System.Collections;

public class SCScreenCapture : MonoBehaviour {
    public int pic = 0;
    public string storePath;
    public string filename;
    public bool useFrameRate = false;
    public int frameRate = 30;
    public bool singleShot = false;
    private bool grap = false;
    public bool useTimeStamp = false;

    void Awake()
    {
        //DontDestroyOnLoad(this.gameObject);
    }

    void Start()
    {
        if (useFrameRate)
        {
            Time.captureFramerate = frameRate;
        }
    }

    void OnPostRender()
    {
        if (grap)
        {
            string timeStr = System.DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            grap = false;
            Texture2D sshot = new Texture2D(Screen.width, Screen.height, TextureFormat.ARGB32, false);
            sshot.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            sshot.Apply();
            byte[] pngShot = sshot.EncodeToPNG();
            Destroy(sshot);
            if (useTimeStamp)
                File.WriteAllBytes(storePath + filename + "_" + timeStr +".png", pngShot);
            else
                File.WriteAllBytes(storePath + filename + ".png", pngShot);

            Debug.Log("Save " + storePath + filename + ".png");

            //Application.CaptureScreenshot(storePath + filename + pic.ToString() + ".png");
            pic++;
        }

    }
    void Update()
    {
        if (singleShot)
        {
            if (Input.GetKeyUp(KeyCode.S))
            {
                grap = true;
            }
           return;
        }
        if (Input.GetKey(KeyCode.S))
        {
            ScreenCapture.CaptureScreenshot(storePath + filename + pic.ToString() + ".png");
            pic++;
        }
    }
}
