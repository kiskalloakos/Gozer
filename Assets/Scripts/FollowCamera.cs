using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FollowCamera : MonoBehaviour
{
    public Transform target;
    public int assetsPixelsPerUnit = PixelArtStandard.PixelsPerUnit;
    public int referenceResolutionY = PixelArtStandard.ReferenceHeight;
    public bool snapToPixelGrid = false;

    Camera viewCamera;

    void Awake()
    {
        viewCamera = GetComponent<Camera>();
        ConfigureCamera();
    }

    void OnValidate()
    {
        if (!viewCamera) viewCamera = GetComponent<Camera>();
        ConfigureCamera();
    }

    void ConfigureCamera()
    {
        if (!viewCamera || assetsPixelsPerUnit <= 0 || referenceResolutionY <= 0) return;
        viewCamera.orthographic = true;
        viewCamera.orthographicSize = referenceResolutionY / (assetsPixelsPerUnit * 2f);
    }

    void LateUpdate()
    {
        if (!target) return;
        float x = target.position.x;
        float y = target.position.y;
        if (snapToPixelGrid && assetsPixelsPerUnit > 0)
        {
            x = Mathf.Round(x * assetsPixelsPerUnit) / assetsPixelsPerUnit;
            y = Mathf.Round(y * assetsPixelsPerUnit) / assetsPixelsPerUnit;
        }
        transform.position = new Vector3(x, y, -10);
    }
}
