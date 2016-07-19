using UnityEngine;
using System.Collections;

public class AttachUIToScreen : MonoBehaviour
{
    public bool SnapToTop;
    public bool SnapToRight;
    public Vector3 Offset;
	public void Update ()
	{
	    if (SnapToTop)
            gameObject.transform.position = SnapToRight ? new Vector3(Screen.width + Offset.x, Screen.height + Offset.y, Offset.z) : new Vector3(Offset.x, Screen.height + Offset.y, Offset.z);
        else
            gameObject.transform.position = SnapToRight ? new Vector3(Screen.width + Offset.x, Offset.y, Offset.z) : Offset;
    }
}
