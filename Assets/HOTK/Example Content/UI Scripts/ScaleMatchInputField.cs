using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(InputField))]
public class ScaleMatchInputField : MonoBehaviour
{
    public HOTK_Overlay Overlay;
    public InputValue Value;

    public InputField InputField
    {
        get { return _inputField ?? (_inputField = GetComponent<InputField>()); }
    }

    private InputField _inputField;

    public void OnEnable()
    {
        if (Overlay == null) return;
        switch (Value)
        {
            case InputValue.ScaleStart:
                InputField.text = Overlay.Scale.ToString();
                break;
            case InputValue.ScaleEnd:
                InputField.text = Overlay.Scale2.ToString();
                break;
            case InputValue.ScaleSpeed:
                InputField.text = Overlay.ScaleSpeed.ToString();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void OnScaleChanged()
    {
        float f;
        if (!float.TryParse(InputField.text, out f)) return;
        if (Overlay == null) return;
        switch (Value)
        {
            case InputValue.ScaleStart:
                if (f >= 0)
                {
                    Overlay.Scale = f;
                }
                else
                {
                    InputField.text = Overlay.Scale.ToString();
                }
                break;
            case InputValue.ScaleEnd:
                if (f >= 0)
                {
                    Overlay.Scale2 = f;
                }
                else
                {
                    InputField.text = Overlay.Scale2.ToString();
                }
                break;
            case InputValue.ScaleSpeed:
                if (f >= 0)
                {
                    Overlay.ScaleSpeed = f;
                }
                else
                {
                    InputField.text = Overlay.ScaleSpeed.ToString();
                }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public enum InputValue
    {
        ScaleStart,
        ScaleEnd,
        ScaleSpeed
    }
}
