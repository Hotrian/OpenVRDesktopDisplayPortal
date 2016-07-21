using UnityEngine;

public class ApplicationDropdownScript : MonoBehaviour
{
    public DesktopPortalController Controller;

    public void Awake()
    {
        if (Controller != null)
        {
            Controller.StopRefreshing();
        }
    }

    public void OnDestroy()
    {
        if (Controller != null)
        {
            Controller.StartRefreshing();
        }
    }
}