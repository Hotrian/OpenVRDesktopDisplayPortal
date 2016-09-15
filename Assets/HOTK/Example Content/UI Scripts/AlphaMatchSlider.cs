using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class AlphaMatchSlider : MonoBehaviour
{
    public HOTK_Overlay Overlay;
    public InputValue Value;

    public Slider Slider
    {
        get { return _slider ?? (_slider = GetComponent<Slider>()); }
    }

    public InputField LinkedField;
    public InputField LinkedField2;
    public InputField LinkedField3;

    private Slider _slider;

    public void OnEnable()
    {
        if (Overlay == null) return;
        switch (Value)
        {
            case InputValue.AlphaStart:
                Slider.value = Overlay.Alpha;
                break;
            case InputValue.AlphaEnd:
                Slider.value = Overlay.Alpha2;
                break;
            case InputValue.AlphaSpeed:
                Slider.value = Overlay.AlphaSpeed;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void OnAlphaChanged()
    {
        if (Overlay == null) return;
        var f = (int) (Slider.value * 100f) / 100f;
        switch (Value)
        {
            case InputValue.AlphaStart:
                if (f >= 0)
                {
                    Overlay.Alpha = f;
                }
                else
                {
                    f = Overlay.Alpha;
                }
                break;
            case InputValue.AlphaEnd:
                if (f >= 0)
                {
                    Overlay.Alpha2 = f;
                }
                else
                {
                    f = Overlay.Alpha2;
                }
                break;
            case InputValue.AlphaSpeed:
                if (f >= 0)
                {
                    Overlay.AlphaSpeed = f;
                }
                else
                {
                    f = Overlay.AlphaSpeed;
                }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        Slider.value = f;
        if (LinkedField != null)
            LinkedField.text = f.ToString(CultureInfo.InvariantCulture);
        if (LinkedField2 != null)
            LinkedField2.text = f.ToString(CultureInfo.InvariantCulture);
        if (LinkedField3 != null)
            LinkedField3.text = f.ToString(CultureInfo.InvariantCulture);
    }

    public enum InputValue
    {
        AlphaStart,
        AlphaEnd,
        AlphaSpeed
    }
}
