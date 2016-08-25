using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Valve.VR;

[RequireComponent(typeof(Dropdown))]
public class DropdownSaveLoadController : MonoBehaviour
{
    public HOTK_Overlay OverlayToSave;

    public OffsetMatchSlider XSlider;
    public OffsetMatchSlider YSlider;
    public OffsetMatchSlider ZSlider;

    public RotationMatchSlider RXSlider;
    public RotationMatchSlider RYSlider;
    public RotationMatchSlider RZSlider;

    public DropdownMatchEnumOptions DeviceDropdown;
    public DropdownMatchEnumOptions PointDropdown;
    public DropdownMatchEnumOptions AnimationDropdown;

    public InputField AlphaStartField;
    public InputField AlphaEndField;
    public InputField AlphaSpeedField;
    public InputField ScaleStartField;
    public InputField ScaleEndField;
    public InputField ScaleSpeedField;

    public InputField AlphaDodgeField;
    public InputField ScaleDodgeField;
    public InputField AlphaNoneField;
    public InputField ScaleNoneField;

    public InputField DodgeXField;
    public InputField DodgeYField;
    public InputField DodgeSpeedField;

    public Button SaveButton;
    public Button LoadButton;
    public Button DeleteButton;
    public Text DeleteButtonText;

    public EventTrigger DeleteButtonTriggers;

    public InputField SaveName;
    public Button SaveNewButton;
    public Button CancelNewButton;

    public HSVPickerPortalScript ColorPicker;

    public Dropdown Dropdown
    {
        get { return _dropdown ?? (_dropdown = GetComponent<Dropdown>()); }
    }

    private Dropdown _dropdown;

    private static string NewString = "New..";

    public void OnEnable()
    {
        if (PortalSettingsSaver.CurrentProgramSettings == null) PortalSettingsSaver.LoadProgramSettings();
        ReloadOptions();
        if (PortalSettingsSaver.CurrentProgramSettings != null && !string.IsNullOrEmpty(PortalSettingsSaver.CurrentProgramSettings.LastProfile)) OnLoadPressed(true);
    }

    private void ReloadOptions()
    {
        Dropdown.ClearOptions();
        var strings = new List<string> { NewString };
        strings.AddRange(PortalSettingsSaver.SavedProfiles.Select(config => config.Key));

        Dropdown.AddOptions(strings);

        // If no settings loaded yet, select "New"
        if (string.IsNullOrEmpty(PortalSettingsSaver.Current))
        {
            Dropdown.value = 0;
            OnValueChanges();
        }
        else // If settings are loaded, try and select the current settings
        {
            for (var i = 0; i < Dropdown.options.Count; i++)
            {
                if (Dropdown.options[i].text != PortalSettingsSaver.Current) continue;
                Dropdown.value = i;
                OnValueChanges();
                break;
            }
        }
    }

    private bool _savingNew;

    public void OnValueChanges()
    {
        CancelConfirmingDelete();
        if (_savingNew)
        {
            Dropdown.interactable = false;
            SaveName.interactable = true;
            CancelNewButton.interactable = true;
            DeleteButton.interactable = false;
            LoadButton.interactable = false;
            SaveButton.interactable = false;
        }
        else
        {
            Dropdown.interactable = true;
            SaveName.interactable = false;
            SaveNewButton.interactable = false;
            CancelNewButton.interactable = false;
            if (Dropdown.options[Dropdown.value].text == NewString)
            {
                DeleteButton.interactable = false;
                LoadButton.interactable = false;
                SaveButton.interactable = true;
            }
            else
            {
                DeleteButton.interactable = true;
                LoadButton.interactable = true;
                SaveButton.interactable = true;
            }
        }
    }

    public void OnLoadPressed(bool startup = false) // Loads an existing save
    {
        if (startup) HOTK_TrackedDeviceManager.Instance.FindControllers();
        CancelConfirmingDelete();
        PortalSettings settings;
        if (!PortalSettingsSaver.SavedProfiles.TryGetValue(Dropdown.options[Dropdown.value].text, out settings)) return;
        Debug.Log(startup ? "Loading last used settings " + Dropdown.options[Dropdown.value].text : "Loading saved settings " + Dropdown.options[Dropdown.value].text);
        PortalSettingsSaver.Current = Dropdown.options[Dropdown.value].text;
        if (!startup) PortalSettingsSaver.SaveProgramSettings();

        if (settings.SaveFileVersion < 2)
        {
            if (settings.Device == HOTK_Overlay.AttachmentDevice.Screen || settings.Device == HOTK_Overlay.AttachmentDevice.World)
                settings.Z += 1;
            if (settings.Device == HOTK_Overlay.AttachmentDevice.Screen)
                settings.ScreenOffsetPerformed = true;

            settings.OutlineDefaultR =  0f; settings.OutlineDefaultG =  0f; settings.OutlineDefaultB =  0f; settings.OutlineDefaultA = 0f;
            settings.OutlineAimingR =   1f; settings.OutlineAimingG =   0f; settings.OutlineAimingB =   0f; settings.OutlineAimingA = 1f;
            settings.OutlineTouchingR = 0f; settings.OutlineTouchingG = 1f; settings.OutlineTouchingB = 0f; settings.OutlineTouchingA = 1f;
            settings.OutlineScalingR =  0f; settings.OutlineScalingG =  0f; settings.OutlineScalingB =  1f; settings.OutlineScalingA = 1f;
            settings.SaveFileVersion = 2;
        }
        if (settings.SaveFileVersion == 2)
        {
            settings.Backside = DesktopPortalController.BacksideTexture.Blue;
            settings.SaveFileVersion = 3;
        }
        if (settings.SaveFileVersion == 3)
        {
            settings.GrabEnabled = true;
            settings.ScaleEnabled = true;
            settings.SaveFileVersion = 4;
        }
        if (settings.SaveFileVersion == 4)
        {
            settings.HapticsEnabled = true;
            settings.SaveFileVersion = 5;
        }
        if (settings.SaveFileVersion == 5)
        {
            settings.DodgeOffsetX = 2f;
            settings.DodgeOffsetY = 0f;
            settings.DodgeOffsetSpeed = 0.1f;
            settings.SaveFileVersion = 6;
        }
        DesktopPortalController.Instance.ScreenOffsetPerformed = settings.ScreenOffsetPerformed;

        // Recenter XYZ Sliders
        XSlider.Slider.minValue = settings.X - 2f;
        XSlider.Slider.maxValue = settings.X + 2f;
        YSlider.Slider.minValue = settings.Y - 2f;
        YSlider.Slider.maxValue = settings.Y + 2f;
        ZSlider.Slider.minValue = settings.Z - 2f;
        ZSlider.Slider.maxValue = settings.Z + 2f;
        XSlider.Slider.value = settings.X;
        YSlider.Slider.value = settings.Y;
        ZSlider.Slider.value = settings.Z;
        // Disable Rotation sliders so only one call to update the overlay occurs
        RXSlider.IgnoreNextUpdate();
        RYSlider.IgnoreNextUpdate();
        RZSlider.IgnoreNextUpdate();
        RXSlider.Slider.value = settings.RX;
        RYSlider.Slider.value = settings.RY;
        RZSlider.Slider.value = settings.RZ;
        RXSlider.OnSliderChanged();
        RYSlider.OnSliderChanged();
        RZSlider.OnSliderChanged();

        if (RXSlider.RotationField != null) RXSlider.RotationField.SetSafeValue(settings.RX);
        if (RYSlider.RotationField != null) RYSlider.RotationField.SetSafeValue(settings.RY);
        if (RZSlider.RotationField != null) RZSlider.RotationField.SetSafeValue(settings.RZ);

        // Swap Selected Controllers when Saved Controller is absent and the other Controller is present
        DeviceDropdown.SetToOption(((settings.Device == HOTK_Overlay.AttachmentDevice.LeftController && HOTK_TrackedDeviceManager.Instance.LeftIndex == OpenVR.k_unTrackedDeviceIndexInvalid && HOTK_TrackedDeviceManager.Instance.RightIndex != OpenVR.k_unTrackedDeviceIndexInvalid) ? HOTK_Overlay.AttachmentDevice.RightController : // Left Controller not found but Right Controller found. Use Right Controller.
                                   ((settings.Device == HOTK_Overlay.AttachmentDevice.RightController && HOTK_TrackedDeviceManager.Instance.RightIndex == OpenVR.k_unTrackedDeviceIndexInvalid && HOTK_TrackedDeviceManager.Instance.LeftIndex != OpenVR.k_unTrackedDeviceIndexInvalid) ? HOTK_Overlay.AttachmentDevice.LeftController : // Right Controller not found but Left Controller found. Use Left Controller.
                                     settings.Device)).ToString()); // Use Device setting otherwise
        PointDropdown.SetToOption(settings.Point.ToString());
        AnimationDropdown.SetToOption(settings.Animation.ToString());

        AlphaStartField.text = settings.AlphaStart.ToString();
        AlphaEndField.text = settings.AlphaEnd.ToString();
        AlphaSpeedField.text = settings.AlphaSpeed.ToString();
        ScaleStartField.text = settings.ScaleStart.ToString();
        ScaleEndField.text = settings.ScaleEnd.ToString();
        ScaleSpeedField.text = settings.ScaleSpeed.ToString();

        AlphaDodgeField.text = settings.AlphaStart.ToString();
        ScaleDodgeField.text = settings.ScaleStart.ToString();
        AlphaNoneField.text = settings.AlphaStart.ToString();
        ScaleNoneField.text = settings.ScaleStart.ToString();

        DodgeXField.text = settings.DodgeOffsetX.ToString();
        DodgeYField.text = settings.DodgeOffsetY.ToString();
        DodgeSpeedField.text = settings.DodgeOffsetSpeed.ToString();

        AlphaStartField.onEndEdit.Invoke("");
        AlphaEndField.onEndEdit.Invoke("");
        AlphaSpeedField.onEndEdit.Invoke("");
        ScaleStartField.onEndEdit.Invoke("");
        ScaleEndField.onEndEdit.Invoke("");
        ScaleSpeedField.onEndEdit.Invoke("");

        AlphaDodgeField.onEndEdit.Invoke("");
        ScaleDodgeField.onEndEdit.Invoke("");
        AlphaNoneField.onEndEdit.Invoke("");
        ScaleNoneField.onEndEdit.Invoke("");

        DodgeXField.onEndEdit.Invoke("");
        DodgeYField.onEndEdit.Invoke("");
        DodgeSpeedField.onEndEdit.Invoke("");


        DesktopPortalController.Instance.OutlineColorDefault =  new Color(settings.OutlineDefaultR,     settings.OutlineDefaultG,   settings.OutlineDefaultB,   settings.OutlineDefaultA);
        DesktopPortalController.Instance.OutlineColorAiming =   new Color(settings.OutlineAimingR,      settings.OutlineAimingG,    settings.OutlineAimingB,    settings.OutlineAimingA);
        DesktopPortalController.Instance.OutlineColorTouching = new Color(settings.OutlineTouchingR,    settings.OutlineTouchingG,  settings.OutlineTouchingB,  settings.OutlineTouchingA);
        DesktopPortalController.Instance.OutlineColorScaling =  new Color(settings.OutlineScalingR,     settings.OutlineScalingG,   settings.OutlineScalingB,   settings.OutlineScalingA);

        DesktopPortalController.Instance.CurrentBacksideTexture = settings.Backside;

        DesktopPortalController.Instance.GrabEnabledToggle.isOn = settings.GrabEnabled;
        DesktopPortalController.Instance.ScaleEnabledToggle.isOn = settings.ScaleEnabled;
        DesktopPortalController.Instance.HapticsEnabledToggle.isOn = settings.HapticsEnabled;

        ColorPicker.LoadButtonColors();
    }

    private bool _confirmingDelete;
    private string _deleteTextDefault = "Delete the selected profile.";
    private string _deleteTextConfirm = "Really Delete?";

    public void OnDeleteButtonTooltip(bool forced = false)
    {
        if (_confirmingDelete)
        {
            if (forced || TooltipController.Instance.GetTooltipText() == _deleteTextDefault)
                TooltipController.Instance.SetTooltipText(_deleteTextConfirm);
        }
        else
        {
            if (forced || TooltipController.Instance.GetTooltipText() == _deleteTextConfirm)
                TooltipController.Instance.SetTooltipText(_deleteTextDefault);
        }
    }

    public void CancelConfirmingDelete()
    {
        _confirmingDelete = false;
        DeleteButtonText.color = new Color(0.196f, 0.196f, 0.196f, 1f);
        OnDeleteButtonTooltip();
    }

    public void OnDeletePressed()
    {
        if (!_confirmingDelete)
        {
            _confirmingDelete = true;
            DeleteButtonText.color = Color.red;
            OnDeleteButtonTooltip();
        }
        else
        {
            PortalSettingsSaver.DeleteProfile(Dropdown.options[Dropdown.value].text);
            CancelConfirmingDelete();
            if (PortalSettingsSaver.SavedProfiles.Count == 0)
                PortalSettingsSaver.LoadDefaultProfiles();
            ReloadOptions();
        }
    }

    /// <summary>
    /// Overwrite an existing save, or save a new one
    /// </summary>
    public void OnSavePressed()
    {
        CancelConfirmingDelete();
        if (Dropdown.options[Dropdown.value].text == NewString) // Start creating a new save
        {
            _savingNew = true;
            OnValueChanges();
        }
        else // Overwrite an existing save
        {
            PortalSettings settings;
            if (!PortalSettingsSaver.SavedProfiles.TryGetValue(Dropdown.options[Dropdown.value].text, out settings)) return;
            Debug.Log("Overwriting saved settings " + Dropdown.options[Dropdown.value].text);
            settings.SaveFileVersion = PortalSettings.CurrentSaveVersion;

            settings.X = OverlayToSave.AnchorOffset.x; settings.Y = OverlayToSave.AnchorOffset.y; settings.Z = OverlayToSave.AnchorOffset.z;
            settings.RX = RXSlider.Slider.value; settings.RY = RYSlider.Slider.value; settings.RZ = RZSlider.Slider.value;

            settings.Device = OverlayToSave.AnchorDevice;
            settings.Point = OverlayToSave.AnchorPoint;
            settings.Animation = OverlayToSave.AnimateOnGaze;

            settings.AlphaStart = OverlayToSave.Alpha;
            settings.AlphaEnd = OverlayToSave.Alpha2;
            settings.AlphaSpeed = OverlayToSave.AlphaSpeed;
            settings.ScaleStart = OverlayToSave.Scale;
            settings.ScaleEnd = OverlayToSave.Scale2;
            settings.ScaleSpeed = OverlayToSave.ScaleSpeed;

            settings.ScreenOffsetPerformed = DesktopPortalController.Instance.ScreenOffsetPerformed;

            settings.OutlineDefaultR = DesktopPortalController.Instance.OutlineColorDefault.r;
            settings.OutlineDefaultG = DesktopPortalController.Instance.OutlineColorDefault.g;
            settings.OutlineDefaultB = DesktopPortalController.Instance.OutlineColorDefault.b;
            settings.OutlineDefaultA = DesktopPortalController.Instance.OutlineColorDefault.a;
            settings.OutlineAimingR = DesktopPortalController.Instance.OutlineColorAiming.r;
            settings.OutlineAimingG = DesktopPortalController.Instance.OutlineColorAiming.g;
            settings.OutlineAimingB = DesktopPortalController.Instance.OutlineColorAiming.b;
            settings.OutlineAimingA = DesktopPortalController.Instance.OutlineColorAiming.a;
            settings.OutlineTouchingR = DesktopPortalController.Instance.OutlineColorTouching.r;
            settings.OutlineTouchingG = DesktopPortalController.Instance.OutlineColorTouching.g;
            settings.OutlineTouchingB = DesktopPortalController.Instance.OutlineColorTouching.b;
            settings.OutlineTouchingA = DesktopPortalController.Instance.OutlineColorTouching.a;
            settings.OutlineScalingR = DesktopPortalController.Instance.OutlineColorScaling.r;
            settings.OutlineScalingG = DesktopPortalController.Instance.OutlineColorScaling.g;
            settings.OutlineScalingB = DesktopPortalController.Instance.OutlineColorScaling.b;
            settings.OutlineScalingA = DesktopPortalController.Instance.OutlineColorScaling.a;

            settings.Backside = DesktopPortalController.Instance.CurrentBacksideTexture;

            settings.GrabEnabled = DesktopPortalController.Instance.GrabEnabledToggle.isOn;
            settings.ScaleEnabled = DesktopPortalController.Instance.ScaleEnabledToggle.isOn;
            settings.HapticsEnabled = DesktopPortalController.Instance.HapticsEnabledToggle.isOn;

            settings.DodgeOffsetX = OverlayToSave.DodgeGazeOffset.x;
            settings.DodgeOffsetY = OverlayToSave.DodgeGazeOffset.y;
            settings.DodgeOffsetSpeed = OverlayToSave.DodgeGazeSpeed;

            PortalSettingsSaver.SaveProfiles();
        }
    }

    public void OnSaveNewPressed()
    {
        if (string.IsNullOrEmpty(SaveName.text) || PortalSettingsSaver.SavedProfiles.ContainsKey(SaveName.text)) return;
        _savingNew = false;
        Debug.Log("Adding saved settings " + SaveName.text);
        PortalSettingsSaver.SavedProfiles.Add(SaveName.text, ConvertToPortalSettings(OverlayToSave));
        PortalSettingsSaver.SaveProfiles();
        PortalSettingsSaver.Current = SaveName.text;
        SaveName.text = "";
        ReloadOptions();
    }

    /// <summary>
    /// Create a new Save
    /// </summary>
    private PortalSettings ConvertToPortalSettings(HOTK_Overlay o) // Create a new save state
    {
        return new PortalSettings()
        {
            SaveFileVersion = PortalSettings.CurrentSaveVersion,
            
            X = o.AnchorOffset.x,
            Y = o.AnchorOffset.y,
            Z = o.AnchorOffset.z,
            RX = o.transform.eulerAngles.x,
            RY = o.transform.eulerAngles.y,
            RZ = o.transform.eulerAngles.z,

            Device = o.AnchorDevice,
            Point = o.AnchorPoint,
            Animation = o.AnimateOnGaze,

            AlphaStart = o.Alpha,
            AlphaEnd = o.Alpha2,
            AlphaSpeed = o.AlphaSpeed,
            ScaleStart = o.Scale,
            ScaleEnd = o.Scale2,
            ScaleSpeed = o.ScaleSpeed,

            ScreenOffsetPerformed = DesktopPortalController.Instance.ScreenOffsetPerformed,

            OutlineDefaultR = DesktopPortalController.Instance.OutlineColorDefault.r,
            OutlineDefaultG = DesktopPortalController.Instance.OutlineColorDefault.g,
            OutlineDefaultB = DesktopPortalController.Instance.OutlineColorDefault.b,
            OutlineDefaultA = DesktopPortalController.Instance.OutlineColorDefault.a,
            OutlineAimingR = DesktopPortalController.Instance.OutlineColorAiming.r,
            OutlineAimingG = DesktopPortalController.Instance.OutlineColorAiming.g,
            OutlineAimingB = DesktopPortalController.Instance.OutlineColorAiming.b,
            OutlineAimingA = DesktopPortalController.Instance.OutlineColorAiming.a,
            OutlineTouchingR = DesktopPortalController.Instance.OutlineColorTouching.r,
            OutlineTouchingG = DesktopPortalController.Instance.OutlineColorTouching.g,
            OutlineTouchingB = DesktopPortalController.Instance.OutlineColorTouching.b,
            OutlineTouchingA = DesktopPortalController.Instance.OutlineColorTouching.a,
            OutlineScalingR = DesktopPortalController.Instance.OutlineColorScaling.r,
            OutlineScalingG = DesktopPortalController.Instance.OutlineColorScaling.g,
            OutlineScalingB = DesktopPortalController.Instance.OutlineColorScaling.b,
            OutlineScalingA = DesktopPortalController.Instance.OutlineColorScaling.a,

            Backside = DesktopPortalController.Instance.CurrentBacksideTexture,

            GrabEnabled = DesktopPortalController.Instance.GrabEnabledToggle.isOn,
            ScaleEnabled = DesktopPortalController.Instance.ScaleEnabledToggle.isOn,
            HapticsEnabled = DesktopPortalController.Instance.HapticsEnabledToggle.isOn,

            DodgeOffsetX = o.DodgeGazeOffset.x,
            DodgeOffsetY = o.DodgeGazeOffset.y,
            DodgeOffsetSpeed = o.DodgeGazeSpeed,
        };
    }

    public void OnCancelNewPressed()
    {
        _savingNew = false;
        SaveName.text = "";
        OnValueChanges();
    }

    public void OnSaveNameChanged()
    {
        if (string.IsNullOrEmpty(SaveName.text) || SaveName.text == NewString)
        {
            SaveNewButton.interactable = false;
        }
        else
        {
            SaveNewButton.interactable = true;
        }
    }
}
