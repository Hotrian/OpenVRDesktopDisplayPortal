using System;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;

[RequireComponent(typeof(ColorPicker))]
public class HSVPickerPortalScript : MonoBehaviour
{
    public ColorPicker Picker
    {
        get
        {
            return _picker ?? (_picker = GetComponent<ColorPicker>());
        }
    }
    private ColorPicker _picker;

    public DesktopPortalController.OutlineColor Mode;

    public Button DefaultButton;
    public Button AimedButton;
    public Button TouchingButton;
    public Button ScalingButton;
    public Image DefaultButtonImage;
    public Image AimedButtonImage;
    public Image TouchingButtonImage;
    public Image ScalingButtonImage;

    public SVBoxSlider HSVBoxSlider;

    public void OnEnable()
    {
        SetButtonColors(DefaultButton, DefaultButtonImage, DesktopPortalController.Instance.OutlineColorDefault);
        SetButtonColors(AimedButton, AimedButtonImage, DesktopPortalController.Instance.OutlineColorAiming);
        SetButtonColors(TouchingButton, TouchingButtonImage, DesktopPortalController.Instance.OutlineColorTouching);
        SetButtonColors(ScalingButton, ScalingButtonImage, DesktopPortalController.Instance.OutlineColorScaling);
    }

    public void OpenPickerForMode(int mode)
    {
        OpenPickerForMode((DesktopPortalController.OutlineColor)mode);
    }

    public void OpenPickerForMode(DesktopPortalController.OutlineColor mode)
    {
        ClosePanel();
        Mode = mode;
        Picker.CurrentColor = DesktopPortalController.Instance.GetOutlineColor(mode);
        if (HSVBoxSlider != null) HSVBoxSlider.HSVChanged(Picker.H, Picker.S, Picker.V);
        OpenPanel();
    }

    public void OnAcceptButton()
    {
        switch (Mode)
        {
            case DesktopPortalController.OutlineColor.Default:
                SetButtonColors(DefaultButton, DefaultButtonImage, Picker.CurrentColor);
                break;
            case DesktopPortalController.OutlineColor.Aiming:
                SetButtonColors(AimedButton, AimedButtonImage, Picker.CurrentColor);
                break;
            case DesktopPortalController.OutlineColor.Touching:
                SetButtonColors(TouchingButton, TouchingButtonImage, Picker.CurrentColor);
                break;
            case DesktopPortalController.OutlineColor.Scaling:
                SetButtonColors(ScalingButton, ScalingButtonImage, Picker.CurrentColor);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        DesktopPortalController.Instance.SetOutlineColor(Mode, Picker.CurrentColor);
        ClosePanel();
    }

    public void LoadButtonColors()
    {
        SetButtonColors(DefaultButton, DefaultButtonImage, DesktopPortalController.Instance.OutlineColorDefault);
        SetButtonColors(AimedButton, AimedButtonImage, DesktopPortalController.Instance.OutlineColorAiming);
        SetButtonColors(TouchingButton, TouchingButtonImage, DesktopPortalController.Instance.OutlineColorTouching);
        SetButtonColors(ScalingButton, ScalingButtonImage, DesktopPortalController.Instance.OutlineColorScaling);
    }

    private static void SetButtonColors(Button button, Image fill, Color color)
    {
        var edgeColor = new Color(color.r, color.g, color.b, 0.5f + (color.a * 0.5f));
        fill.color = new Color(color.r, color.g, color.b, 0.2f + (color.a * 0.8f));
        var colorBlock = button.colors;
        colorBlock.normalColor = edgeColor;
        colorBlock.highlightedColor = edgeColor;
        colorBlock.pressedColor = edgeColor;
        button.colors = colorBlock;
    }

    public void OnCancelButton()
    {
        ClosePanel();
    }

    public void OpenPanel()
    {
        gameObject.SetActive(true);
    }

    public void ClosePanel()
    {
        gameObject.SetActive(false);
    }
}
