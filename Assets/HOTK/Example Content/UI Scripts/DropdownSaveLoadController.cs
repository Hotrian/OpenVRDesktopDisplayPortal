using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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

    public Button SaveButton;
    public Button LoadButton;
    public Button DeleteButton;
    public Text DeleteButtonText;

    public EventTrigger DeleteButtonTriggers;

    public InputField SaveName;
    public Button SaveNewButton;
    public Button CancelNewButton;

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
        // Disable X and Y sliders so only one call to update the overlay occurs
        XSlider.Slider.minValue = settings.X - 2f;
        XSlider.Slider.maxValue = settings.X + 2f;
        YSlider.Slider.minValue = settings.Y - 2f;
        YSlider.Slider.maxValue = settings.Y + 2f;
        ZSlider.Slider.minValue = settings.Z - 2f;
        ZSlider.Slider.maxValue = settings.Z + 2f;
        XSlider.Slider.value = settings.X;
        YSlider.Slider.value = settings.Y;
        ZSlider.Slider.value = settings.Z;

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

        DeviceDropdown.SetToOption(settings.Device.ToString());
        PointDropdown.SetToOption(settings.Point.ToString());
        AnimationDropdown.SetToOption(settings.Animation.ToString());

        AlphaStartField.text = settings.AlphaStart.ToString();
        AlphaEndField.text = settings.AlphaEnd.ToString();
        AlphaSpeedField.text = settings.AlphaSpeed.ToString();
        ScaleStartField.text = settings.ScaleStart.ToString();
        ScaleEndField.text = settings.ScaleEnd.ToString();
        ScaleSpeedField.text = settings.ScaleSpeed.ToString();

        AlphaStartField.onEndEdit.Invoke("");
        AlphaEndField.onEndEdit.Invoke("");
        AlphaSpeedField.onEndEdit.Invoke("");
        ScaleStartField.onEndEdit.Invoke("");
        ScaleEndField.onEndEdit.Invoke("");
        ScaleSpeedField.onEndEdit.Invoke("");
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

            settings.AlphaStart = OverlayToSave.Alpha; settings.AlphaEnd = OverlayToSave.Alpha2; settings.AlphaSpeed = OverlayToSave.AlphaSpeed;
            settings.ScaleStart = OverlayToSave.Scale; settings.ScaleEnd = OverlayToSave.Scale2; settings.ScaleSpeed = OverlayToSave.ScaleSpeed;
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
