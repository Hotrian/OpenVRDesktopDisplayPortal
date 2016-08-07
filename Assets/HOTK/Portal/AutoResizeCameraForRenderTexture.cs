using UnityEngine;

[RequireComponent(typeof(Camera))]
public class AutoResizeCameraForRenderTexture : MonoBehaviour
{
    public Camera Camera
    {
        get { return _camera ?? (_camera = GetComponent<Camera>()); }
    }

    private Camera _camera;
    public void ResizeCamera(RenderTexture render)
    {
        Camera.orthographicSize = render.height / 2f;
        Camera.aspect = (float)render.width / (float)render.height;
    }
}
