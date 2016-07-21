using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(InputField))]
public class RotationMatchInputField : MonoBehaviour
{
    public static InputField XInput;
    public static InputField YInput;
    public static InputField ZInput;

    public HOTK_Overlay Overlay;
    public RotationAxis Axis;

    public RotationMatchSlider Slider;
    public InputField InputField
    {
        get { return _inputField ?? (_inputField = GetComponent<InputField>()); }
    }

    private InputField _inputField;
    
    public void Awake()
    {
        switch (Axis)
        {
            case RotationAxis.X:
                XInput = InputField;
                break;
            case RotationAxis.Y:
                YInput = InputField;
                break;
            case RotationAxis.Z:
                ZInput = InputField;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (Slider == null) return;
        InputField.text = Slider.Slider.value.ToString();
    }

    public void OnInputChanged()
    {
        float f;
        if (!float.TryParse(InputField.text, out f)) return;
        if (Slider != null) { Slider.IgnoreNextUpdate(); Slider.Slider.value = f; }
        if (Overlay == null) return;
        float dx = int.Parse(XInput.text), dy = int.Parse(YInput.text), dz = int.Parse(ZInput.text);
        switch (Axis)
        {
            case RotationAxis.X:
                dx = f;
                break;
            case RotationAxis.Y:
                dy = f;
                break;
            case RotationAxis.Z:
                dz = f;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        Overlay.transform.rotation = Quaternion.Euler(dx, dy, dz);
    }

    public enum RotationAxis
    {
        X,
        Y,
        Z
    }
}
