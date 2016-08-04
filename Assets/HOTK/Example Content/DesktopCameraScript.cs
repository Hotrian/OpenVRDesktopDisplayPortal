using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Camera))]
public class DesktopCameraScript : MonoBehaviour
{
    public Camera Camera
    {
        get { return _camera ?? (_camera = GetComponent<Camera>()); }
    }
    
    private Camera _camera;
    public float size = 200f;
    void LateUpdate()
    {
        Camera.orthographicSize = Screen.height / size;
    }
}
