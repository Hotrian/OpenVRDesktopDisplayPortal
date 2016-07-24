using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class RotationMatchSlider : MonoBehaviour
{
    public static Slider XSlider;
    public static Slider YSlider;
    public static Slider ZSlider;

    public HOTK_Overlay Overlay;
    public RotationAxis Axis;
    
    public RotationMatchInputField RotationField;
    public Slider Slider
    {
        get { return _slider ?? (_slider = GetComponent<Slider>()); }
    }

    private Slider _slider;

    public void Awake()
    {
        switch (Axis)
        {
            case RotationAxis.X:
                XSlider = Slider;
                break;
            case RotationAxis.Y:
                YSlider = Slider;
                break;
            case RotationAxis.Z:
                ZSlider = Slider;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void DoAdjustValue(float val)
    {
        Slider.value += val;
    }

    private bool _ignoreNextUpdate;
    public void IgnoreNextUpdate()
    {
        _ignoreNextUpdate = true;
    }

    public void OnSliderChanged()
    {
        if (_ignoreNextUpdate)
        {
            _ignoreNextUpdate = false;
            return;
        }
        if (RotationField != null) RotationField.SetSafeValue(Slider.value);
        if (Overlay == null) return;
        float dx = XSlider.value,
              dy = YSlider.value,
              dz = ZSlider.value;
        switch (Axis)
        {
            case RotationAxis.X:
                dx = Slider.value;
                break;
            case RotationAxis.Y:
                dy = Slider.value;
                break;
            case RotationAxis.Z:
                dz = Slider.value;
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
