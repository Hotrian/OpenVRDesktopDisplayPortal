using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Dropdown))]
public class DropdownApplicationModeController : MonoBehaviour
{
    public DesktopPortalController PortalController;
    //public TwitchChatTester TwitchController;
    public VLCPlayerController VLCController;

    public GameObject PortalPanel;
    public GameObject VLCPanel;

    public Dropdown ModeDropdown
    {
        get { return _modeDropdown ?? (_modeDropdown = GetComponent<Dropdown>()); }
    }

    private Dropdown _modeDropdown;

	public void Start()
	{
	    SetProgramMode();
	}

    public void SetProgramMode()
    {
        switch (ModeDropdown.options[ModeDropdown.value].text)
        {
            case "Mode: Desktop Display Portal":
                MultiModeController.CurrentController = PortalController;
                MultiModeController.CurrentPanel = PortalPanel;
                break;
            case "Mode: TwitchChat":
                Debug.LogWarning("Implementation incomplete");
                break;
            case "Mode: VLC Player":
                MultiModeController.CurrentController = VLCController;
                MultiModeController.CurrentPanel = VLCPanel;
                break;
            default:
                throw new NotImplementedException();
        }
    }
}
