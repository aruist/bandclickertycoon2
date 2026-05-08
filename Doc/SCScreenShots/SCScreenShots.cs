using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

public class SCScreenShots : MonoBehaviour {
    public int waitingFrames = 5;
    public Camera targetCamera;
    public static SCScreenShots instance;
    [Tooltip("the key that will trigger a screenshot")]
    public KeyCode ScreenShotKey = KeyCode.F1;

    [Tooltip("a List of ScreenShotsSizes")]
    public List<SCScreenShotSize> screenShotSizes = new List<SCScreenShotSize>();

    MethodInfo getGroup;
    object gameViewSizesInstance;
    bool screenShotOngoing = false;
    private RenderTexture renderTexture;
    private Texture2D screenShotTexture;
    private string _ScreenShotPath;
    private string currentName;
    private int currentWidth;
    private int currentHeight;
    private int currentIndex;
    private int sizeIndex;
    private bool takeNextScreenShot = false;
    private bool waitingSize = false;
    private int frameCounter;


    public enum GameViewSizeType
    {
        AspectRatio, FixedResolution
    }

    void Awake()
    {
#if UNITY_EDITOR
        if (instance == null)
        {
            DontDestroyOnLoad(gameObject); //no not destory this GameObject
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
#else
        Destroy(gameObject);
#endif
    }

    #if !UNITY_EDITOR
    // Use this for initialization
    void Start () {
        frameCounter = 0;
        CreateScreenShotFolder();

        var sizesType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizes");
        var singleType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
        var instanceProp = singleType.GetProperty("instance");
        getGroup = sizesType.GetMethod("GetGroup");
        gameViewSizesInstance = instanceProp.GetValue(null, null);

        AddCustomSizesIfNotExists();
    }

    // Update is called once per frame
    void Update () {
        if (waitingSize)
        {
            frameCounter++;
            if (frameCounter > waitingFrames)
            {
                waitingSize = false;
                frameCounter = 0;
                Debug.Log("Create textures w: " + currentWidth + ", h: " + currentHeight);
                renderTexture = new RenderTexture(currentWidth, currentHeight, 24, RenderTextureFormat.ARGB32);
                renderTexture.Create();
                screenShotTexture = new Texture2D(currentWidth, currentHeight, TextureFormat.RGB24, false);
                targetCamera.targetTexture = renderTexture;
                RenderTexture.active = renderTexture;
                targetCamera.Render();
                StartCoroutine("TakeScreenShots");
            }
        }
        if (takeNextScreenShot)
        {
            frameCounter++;
            if (frameCounter > waitingFrames)
            {
                NextScreenShot();
            }
        }
        if (screenShotOngoing)
            return;

        if (Input.GetKeyDown(ScreenShotKey) && screenShotSizes.Count > 0)
        {
            sizeIndex = 0;
            NextScreenShot();
        }
    }

    private void NextScreenShot()
    {
        frameCounter = 0;
        takeNextScreenShot = false;
        for (int i=sizeIndex; i < screenShotSizes.Count; i++)
        {
            SCScreenShotSize size = screenShotSizes[i];
            currentIndex = FindSize(GameViewSizeGroupType.Android, (int)size.Size.x, (int)size.Size.y);
            if (size.Enable && currentIndex >= 0)
            {
                screenShotOngoing = true;
                currentWidth = (int)size.Size.x;
                currentHeight = (int)size.Size.y;
                currentName = size.Name;
                SetSize(currentIndex);
                waitingSize = true;
                return;
            }
        }
        screenShotOngoing = false;
    }

    private void AddCustomSizesIfNotExists()
    {
        for (int i=0; i < screenShotSizes.Count; i++)
        {
            SCScreenShotSize size = screenShotSizes[i];
            if (size.Enable && FindSize(GameViewSizeGroupType.Android, (int)size.Size.x, (int)size.Size.y) < 0)
            {
                AddCustomSize(GameViewSizeType.FixedResolution, GameViewSizeGroupType.Android, (int)size.Size.x, (int)size.Size.y, size.Name);
            }
        }
    }

    object GetGroup(GameViewSizeGroupType type)
    {
        return getGroup.Invoke(gameViewSizesInstance, new object[] { (int)type });
    }

    public int FindSize(GameViewSizeGroupType sizeGroupType, int width, int height)
    {
        var group = GetGroup(sizeGroupType);
        var groupType = group.GetType();
        var getBuiltinCount = groupType.GetMethod("GetBuiltinCount");
        var getCustomCount = groupType.GetMethod("GetCustomCount");
        int sizesCount = (int)getBuiltinCount.Invoke(group, null) + (int)getCustomCount.Invoke(group, null);
        var getGameViewSize = groupType.GetMethod("GetGameViewSize");
        var gvsType = getGameViewSize.ReturnType;
        var widthProp = gvsType.GetProperty("width");
        var heightProp = gvsType.GetProperty("height");
        var indexValue = new object[1];
        for (int i = 0; i < sizesCount; i++)
        {
            indexValue[0] = i;
            var size = getGameViewSize.Invoke(group, indexValue);
            int sizeWidth = (int)widthProp.GetValue(size, null);
            int sizeHeight = (int)heightProp.GetValue(size, null);
            if (sizeWidth == width && sizeHeight == height)
                return i;
        }
        return -1;
    }

    public void AddCustomSize(GameViewSizeType viewSizeType, GameViewSizeGroupType sizeGroupType, int width, int height, string text)
    {
        var group = GetGroup(sizeGroupType);
        var addCustomSize = getGroup.ReturnType.GetMethod("AddCustomSize"); // or group.GetType().
        var gvsType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSize");
        var ctor = gvsType.GetConstructor(new Type[] { typeof(int), typeof(int), typeof(int), typeof(string) });
        var newSize = ctor.Invoke(new object[] { (int)viewSizeType, width, height, text });
        addCustomSize.Invoke(group, new object[] { newSize });
    }
    public void SetSize(int index)
    {
        var gvWndType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
        //BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var gvWnd = EditorWindow.GetWindow(gvWndType);
        var SizeSelectionCallback = gvWndType.GetMethod("SizeSelectionCallback",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        SizeSelectionCallback.Invoke(gvWnd, new object[] { index, null });
    }


    //private int i = 0;
    private int stangeInt = 0;
    IEnumerator TakeScreenShots()
    {
        //i = 0;
        stangeInt = 0;

        if (stangeInt == 0)
        {
            stangeInt++;
            yield return null;
        }

        if (stangeInt == 1)
        {
            yield return new WaitForEndOfFrame();
            string TimeTag = System.DateTime.Now.ToString().Replace("/", "").Replace(" ", "").Replace(":", ""); //get DatetimeTag
            string NewFileName = _ScreenShotPath + "/" + currentName + "_" + TimeTag + ".png";
            //Debug.Log("Save textures w: " + targetCamera.targetTexture.width + ", h: " + targetCamera.targetTexture.height);
            screenShotTexture.ReadPixels(new Rect(0, 0, targetCamera.targetTexture.width, targetCamera.targetTexture.height), 0, 0);
            screenShotTexture.Apply();
            targetCamera.targetTexture = null;
            RenderTexture.active = null;
            renderTexture.Release();
            Destroy(renderTexture);
            byte[] bytes = screenShotTexture.EncodeToPNG();
            Debug.Log(string.Format("Took screenshot to: {0}", NewFileName));
            System.IO.File.WriteAllBytes(NewFileName, bytes);

            stangeInt++;
            yield return null;

        }
        sizeIndex++;
        frameCounter = 0;
        takeNextScreenShot = true;
    }
    private void CreateScreenShotFolder()
    {
        _ScreenShotPath = Application.dataPath + "/../ScreenShots";
        if (!Directory.Exists(_ScreenShotPath))
        {
            Directory.CreateDirectory(_ScreenShotPath);
        }
    }
    #endif

}

[System.Serializable]
public class SCScreenShotSize
{
    public bool Enable = true;
    public string Name;
    public Vector2 Size;

}

