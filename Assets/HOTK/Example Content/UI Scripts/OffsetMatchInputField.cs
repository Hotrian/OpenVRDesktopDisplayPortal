using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(InputField))]
public class OffsetMatchInputField : MonoBehaviour
{
    public HOTK_Overlay Overlay;
    public OffsetValue Value;

    public Slider Slider;
    public InputField InputField
    {
        get { return _inputField ?? (_inputField = GetComponent<InputField>()); }
    }
    private InputField _inputField;

    public OffsetMatchSlider LinkedOffsetMatchSlider;

    private float _lastSafeValue;

    public void OnEnable()
    {
        if (Overlay == null) return;
        switch (Value)
        {
            case OffsetValue.X:
                InputField.text = Overlay.AnchorOffset.x.ToString();
                LinkedOffsetMatchSlider = Slider.GetComponent<OffsetMatchSlider>();
                break;
            case OffsetValue.Y:
                InputField.text = Overlay.AnchorOffset.y.ToString();
                LinkedOffsetMatchSlider = Slider.GetComponent<OffsetMatchSlider>();
                break;
            case OffsetValue.Z:
                InputField.text = Overlay.AnchorOffset.z.ToString();
                LinkedOffsetMatchSlider = Slider.GetComponent<OffsetMatchSlider>();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void SetSafeValue(float val)
    {
        InputField.text = val.ToString();
        _lastSafeValue = val;
    }

    public void OnOffsetChanged()
    {
        float f;
        if (!float.TryParse(InputField.text, out f))
        {
            InputField.text = _lastSafeValue.ToString();
            return;
        }
        if (Slider != null) LinkedOffsetMatchSlider.SetBaseValue(f);
        if (Overlay == null) return;
        float dx = Overlay.AnchorOffset.x, dy = Overlay.AnchorOffset.y, dz = Overlay.AnchorOffset.z;
        switch (Value)
        {
            case OffsetValue.X:
                dx = f;
                _lastSafeValue = f;
                break;
            case OffsetValue.Y:
                dy = f;
                _lastSafeValue = f;
                break;
            case OffsetValue.Z:
                dz = f;
                _lastSafeValue = f;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        Overlay.AnchorOffset = new Vector3(dx, dy, dz);
    }

    public enum OffsetValue
    {
        X,
        Y,
        Z
    }
}
