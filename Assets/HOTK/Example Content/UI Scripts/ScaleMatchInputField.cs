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

    public InputField LinkedField;
    public InputField LinkedField2;

    private InputField _inputField;

    private float _lastSafeValue;

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
        if (!float.TryParse(InputField.text, out f))
        {
            InputField.text = _lastSafeValue.ToString();
            if (LinkedField != null)
                LinkedField.text = InputField.text;
            if (LinkedField2 != null)
                LinkedField2.text = InputField.text;
            return;
        }
        if (Overlay == null) return;
        switch (Value)
        {
            case InputValue.ScaleStart:
                if (f >= 0)
                {
                    Overlay.Scale = f;
                    _lastSafeValue = f;
                }
                else
                {
                    InputField.text = Overlay.Scale.ToString();
                    _lastSafeValue = Overlay.Scale;
                }
                break;
            case InputValue.ScaleEnd:
                if (f >= 0)
                {
                    Overlay.Scale2 = f;
                    _lastSafeValue = f;
                }
                else
                {
                    InputField.text = Overlay.Scale2.ToString();
                    _lastSafeValue = Overlay.Scale2;
                }
                break;
            case InputValue.ScaleSpeed:
                if (f >= 0)
                {
                    Overlay.ScaleSpeed = f;
                    _lastSafeValue = f;
                }
                else
                {
                    InputField.text = Overlay.ScaleSpeed.ToString();
                    _lastSafeValue = Overlay.ScaleSpeed;
                }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (LinkedField != null)
            LinkedField.text = InputField.text;
        if (LinkedField2 != null)
            LinkedField2.text = InputField.text;
    }

    public enum InputValue
    {
        ScaleStart,
        ScaleEnd,
        ScaleSpeed
    }
}
