using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class OffsetMatchSlider : MonoBehaviour
{
    public HOTK_Overlay Overlay;
    public OffsetValue Value;
    
    public OffsetMatchInputField OffsetField;
    public Slider Slider
    {
        get { return _slider ?? (_slider = GetComponent<Slider>()); }
    }

    public static OffsetMatchSlider XSlider;
    public static OffsetMatchSlider YSlider;
    public static OffsetMatchSlider ZSlider;

    private Slider _slider;

    public void OnEnable()
    {
        if (Overlay == null) return;
        switch (Value)
        {
            case OffsetValue.X:
                XSlider = this;
                break;
            case OffsetValue.Y:
                YSlider = this;
                break;
            case OffsetValue.Z:
                ZSlider = this;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void OnOffsetChanged()
    {
        if (OffsetField != null) OffsetField.SetSafeValue(Slider.value);
        if (Overlay == null) return;
        float dx = XSlider.Slider.value, dy = YSlider.Slider.value, dz = ZSlider.Slider.value;
        Overlay.AnchorOffset = new Vector3(dx, dy, dz);
    }

    public void SetBaseValue(float val)
    {
        Slider.minValue = val - 2f;
        Slider.maxValue = val + 2f;
        Slider.value = val;
    }

    public void DoAdjustValue(float val)
    {
        Slider.value = Mathf.Round((Slider.value + val) * 100f) / 100f;
    }

    public enum OffsetValue
    {
        X,
        Y,
        Z
    }
}
