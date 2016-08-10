using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Valve.VR;

[RequireComponent(typeof(Dropdown))]
public class DropdownMatchEnumOptions : MonoBehaviour
{
    public HOTK_Overlay Overlay;
    public EnumSelection EnumOptions;
    public Dropdown Dropdown
    {
        get { return _dropdown ?? (_dropdown = GetComponent<Dropdown>()); }
    }

    private Dropdown _dropdown;
    private bool _ignoreNextChange; // Skip the next SetDropdownState call (used for loading so the change doesn't get called multiple times)

    public void OnEnable()
    {
        if (Dropdown == null) return;
        Dropdown.ClearOptions();
        var strings = new List<string>(); // Builds a list of strings for this dropdown
        switch (EnumOptions)
        {
            case EnumSelection.AttachmentDevice:
                UpdateDeviceDropdown();
                HOTK_TrackedDeviceManager.OnControllerIndicesUpdated += UpdateDeviceDropdown;
                break;
            case EnumSelection.AttachmentPoint:
                strings.AddRange(from object e in Enum.GetValues(typeof(HOTK_Overlay.AttachmentPoint)) select e.ToString());
                Dropdown.AddOptions(strings);
                Dropdown.value = strings.IndexOf(Overlay.AnchorPoint.ToString());
                break;
            case EnumSelection.AnimationType:
                strings.AddRange(from object e in Enum.GetValues(typeof(HOTK_Overlay.AnimationType)) select e.ToString());
                Dropdown.AddOptions(strings);
                Dropdown.value = strings.IndexOf(Overlay.AnimateOnGaze.ToString());
                break;
            case EnumSelection.CaptureMode:
                strings.AddRange(CaptureModeNames.Where((t, i) => CaptureModesEnabled[i]));
                Dropdown.AddOptions(strings);
                _ignoreNextChange = true;
                if (DesktopPortalController.Instance.SelectedWindowSettings != null)
                    Dropdown.value = strings.IndexOf(CaptureModeNames[(int)DesktopPortalController.Instance.SelectedWindowSettings.captureMode]);
                break;
            case EnumSelection.Framerate:
                strings.AddRange(FramerateModeNames);
                Dropdown.AddOptions(strings);
                if (DesktopPortalController.Instance.SelectedWindowSettings != null)
                    Dropdown.value = strings.IndexOf(FramerateModeNames[(int)DesktopPortalController.Instance.SelectedWindowSettings.framerateMode]);
                break;
            case EnumSelection.MouseMode:
                strings.AddRange(MouseModeNames);
                Dropdown.AddOptions(strings);
                if (DesktopPortalController.Instance.SelectedWindowSettings != null)
                    Dropdown.value = strings.IndexOf(MouseModeNames[(int)DesktopPortalController.Instance.SelectedWindowSettings.interactionMode]);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        Dropdown.onValueChanged.Invoke(Dropdown.value);
    }

    private bool SetToRightController = false;
    private bool SetToLeftController = false;

    // Handles reselecting the given controller when controller indices have been swapped
    public void SwapControllers()
    {
        if (HOTK_TrackedDeviceManager.Instance.LeftIndex != OpenVR.k_unTrackedDeviceIndexInvalid && HOTK_TrackedDeviceManager.Instance.RightIndex != OpenVR.k_unTrackedDeviceIndexInvalid) return; // If both controllers are found, don't swap selected name
        if (Dropdown.options[Dropdown.value].text == HOTK_Overlay.AttachmentDevice.LeftController.ToString()) SetToRightController = true;
        else if (Dropdown.options[Dropdown.value].text == HOTK_Overlay.AttachmentDevice.RightController.ToString()) SetToLeftController = true;
    }

    // Update the device dropdown when Devices change
    private void UpdateDeviceDropdown()
    {
        var strings = new List<string>
        {
            HOTK_Overlay.AttachmentDevice.World.ToString(), HOTK_Overlay.AttachmentDevice.Screen.ToString()
        };
        if (HOTK_TrackedDeviceManager.Instance.LeftIndex != OpenVR.k_unTrackedDeviceIndexInvalid) strings.Add(HOTK_Overlay.AttachmentDevice.LeftController.ToString());
        if (HOTK_TrackedDeviceManager.Instance.RightIndex != OpenVR.k_unTrackedDeviceIndexInvalid) strings.Add(HOTK_Overlay.AttachmentDevice.RightController.ToString());
        Dropdown.ClearOptions();
        Dropdown.AddOptions(strings);
        if (SetToRightController) Dropdown.value = strings.IndexOf(HOTK_Overlay.AttachmentDevice.RightController.ToString());
        else if (SetToLeftController) Dropdown.value = strings.IndexOf(HOTK_Overlay.AttachmentDevice.LeftController.ToString());
        else Dropdown.value = strings.IndexOf(Overlay.AnchorDevice.ToString());
        SetToRightController = false;
        SetToLeftController = false;
    }

    public void SetDropdownState(string val)
    {
        if (_ignoreNextChange)
        {
            _ignoreNextChange = false;
            return;
        }
        switch (EnumOptions)
        {
            case EnumSelection.AttachmentDevice:
                Overlay.AnchorDevice = (HOTK_Overlay.AttachmentDevice) Enum.Parse(typeof (HOTK_Overlay.AttachmentDevice), Dropdown.options[Dropdown.value].text);
                DesktopPortalController.Instance.CheckOverlayOffsetPerformed();
                break;
            case EnumSelection.AttachmentPoint:
                Overlay.AnchorPoint = (HOTK_Overlay.AttachmentPoint) Enum.Parse(typeof (HOTK_Overlay.AttachmentPoint), Dropdown.options[Dropdown.value].text);
                break;
            case EnumSelection.AnimationType:
                Overlay.AnimateOnGaze = (HOTK_Overlay.AnimationType) Enum.Parse(typeof (HOTK_Overlay.AnimationType), Dropdown.options[Dropdown.value].text);
                break;
            case EnumSelection.Framerate:
                var fpsIndex = FramerateModeNames.IndexOf(Dropdown.options[Dropdown.value].text);
                Overlay.Framerate = fpsIndex == -1 ? HOTK_Overlay.FramerateMode._24FPS : (HOTK_Overlay.FramerateMode)fpsIndex;
                DesktopPortalController.Instance.SelectedWindowSettings.framerateMode = Overlay.Framerate;
                break;
            case EnumSelection.CaptureMode:
                var index = CaptureModeNames.IndexOf(Dropdown.options[Dropdown.value].text);
                DesktopPortalController.Instance.SelectedWindowSettings.captureMode = index == -1 ? DesktopPortalController.CaptureMode.GdiDirect : (DesktopPortalController.CaptureMode)index; // Fallback to GDI Direct if they were using a now disabled capture method
                break;
            case EnumSelection.MouseMode:
                DesktopPortalController.Instance.SelectedWindowSettings.interactionMode = (DesktopPortalController.MouseInteractionMode) MouseModeNames.IndexOf(Dropdown.options[Dropdown.value].text);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void SetToOption(string text, bool ignoreChange = false)
    {
        if (ignoreChange)
            _ignoreNextChange = true;
        for (var i = 0; i < Dropdown.options.Count; i ++)
        {
            if (Dropdown.options[i].text != text) continue;
            Dropdown.value = i;
            break;
        }
    }

    public enum EnumSelection
    {
        AttachmentDevice,
        AttachmentPoint,
        AnimationType,
        Framerate,
        CaptureMode,
        MouseMode
    }

    public static readonly List<string> CaptureModeNames = new List<string>
    {
        "GDI Direct", "GDI Indirect", "W8/W10 Replication API"
    };

    public static readonly List<bool> CaptureModesEnabled = new List<bool>()
    {
        true,
        true,
        false
    };

    public static readonly List<string> MouseModeNames = new List<string>
    {
        "Full Interaction", "Window On Top", "Click Interaction Only", "No Interaction"
    };

    public static readonly List<string> FramerateModeNames = new List<string>
    {
        "1 FPS",
        "2 FPS",
        "5 FPS",
        "10 FPS",
        "15 FPS",
        "24 FPS",
        "30 FPS",
        "60 FPS",
        "90 FPS",
        "120 FPS",
        "Unlimited",
    };
}
