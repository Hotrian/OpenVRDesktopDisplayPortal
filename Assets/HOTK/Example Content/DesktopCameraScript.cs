using UnityEngine;
using System.Collections;

public class DesktopCameraScript : MonoBehaviour
{
    public float size = 200f;
    void LateUpdate()
    {
        Camera.main.orthographicSize = Screen.height / size;
    }
}
