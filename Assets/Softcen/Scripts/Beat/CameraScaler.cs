using UnityEngine;

public class CameraScaler : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [SerializeField] private SpriteRenderer venueBackground;

    private int lastScreenWidth = -1;
    private int lastScreenHeight = -1;

    private void OnEnable()
    {
        EnsureCameraReference();
        UpdateCameraSize(true);
    }

    private void Update()
    {
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            UpdateCameraSize();
        }
    }

    private void EnsureCameraReference()
    {
        if (cam == null)
        {
            cam = Camera.main;
        }
    }

    private void UpdateCameraSize(bool force = false)
    {
        if (cam == null || venueBackground == null || Screen.height == 0)
        {
            return;
        }

        if (!force && Screen.width == lastScreenWidth && Screen.height == lastScreenHeight)
        {
            return;
        }

        Bounds backgroundBounds = venueBackground.bounds;
        float backgroundWidth = backgroundBounds.size.x;
        float backgroundHeight = backgroundBounds.size.y;

        if (backgroundWidth <= 0f || backgroundHeight <= 0f)
        {
            return;
        }

        float aspectRatio = (float)Screen.width / Screen.height;
        float verticalLimit = backgroundHeight * 0.5f;
        float horizontalLimit = (backgroundWidth * 0.5f) / aspectRatio;

        float targetSize = Mathf.Min(verticalLimit, horizontalLimit);
        cam.orthographic = true;
        cam.orthographicSize = Mathf.Max(targetSize, 0.01f);

        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
    }
}
