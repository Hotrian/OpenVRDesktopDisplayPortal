using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(InputField))]
public class DodgeMatchInputField : MonoBehaviour
{
    public HOTK_Overlay Overlay;
    public InputValue Value;

    public InputField InputField
    {
        get { return _inputField ?? (_inputField = GetComponent<InputField>()); }
    }
    
    private InputField _inputField;

    private float _lastSafeValue;

    public void OnEnable()
    {
        if (Overlay == null) return;
        switch (Value)
        {
            case InputValue.X:
                InputField.text = Overlay.DodgeGazeOffset.x.ToString();
                break;
            case InputValue.Y:
                InputField.text = Overlay.DodgeGazeOffset.y.ToString();
                break;
            case InputValue.Speed:
                if (Overlay.DodgeGazeSpeed < 0f) Overlay.DodgeGazeSpeed = 0f;
                if (Overlay.DodgeGazeSpeed > 1f) Overlay.DodgeGazeSpeed = 1f;
                InputField.text = Overlay.DodgeGazeSpeed.ToString();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void OnDodgeChanged()
    {
        float f;
        if (!float.TryParse(InputField.text, out f))
        {
            InputField.text = _lastSafeValue.ToString();
            return;
        }
        if (Overlay == null) return;
        switch (Value)
        {
            case InputValue.X:
                Overlay.DodgeGazeOffset.x = f;
                _lastSafeValue = f;
                break;
            case InputValue.Y:
                Overlay.DodgeGazeOffset.y = f;
                _lastSafeValue = f;
                break;
            case InputValue.Speed:
                if (f < 0f) f = 0f;
                if (f > 1f) f = 1f;
                Overlay.DodgeGazeSpeed = f;
                _lastSafeValue = f;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public enum InputValue
    {
        X,
        Y,
        Speed,
    }
}
