using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(InputField))]
public class AlphaMatchInputField : MonoBehaviour
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
            case InputValue.AlphaStart:
                InputField.text = Overlay.Alpha.ToString();
                break;
            case InputValue.AlphaEnd:
                InputField.text = Overlay.Alpha2.ToString();
                break;
            case InputValue.AlphaSpeed:
                InputField.text = Overlay.AlphaSpeed.ToString();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void OnAlphaChanged()
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
            case InputValue.AlphaStart:
                if (f >= 0)
                {
                    Overlay.Alpha = f;
                    _lastSafeValue = f;
                }
                else
                {
                    InputField.text = Overlay.Alpha.ToString();
                    _lastSafeValue = Overlay.Alpha;
                }
                break;
            case InputValue.AlphaEnd:
                if (f >= 0)
                {
                    Overlay.Alpha2 = f;
                    _lastSafeValue = f;
                }
                else
                {
                    InputField.text = Overlay.Alpha2.ToString();
                    _lastSafeValue = Overlay.Alpha2;
                }
                break;
            case InputValue.AlphaSpeed:
                if (f >= 0)
                {
                    Overlay.AlphaSpeed = f;
                    _lastSafeValue = f;
                }
                else
                {
                    InputField.text = Overlay.AlphaSpeed.ToString();
                    _lastSafeValue = Overlay.AlphaSpeed;
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
        AlphaStart,
        AlphaEnd,
        AlphaSpeed
    }
}
