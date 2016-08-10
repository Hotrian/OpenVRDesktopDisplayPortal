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

    private Slider _slider;
    public void OnOffsetChanged()
    {
        if (OffsetField != null) OffsetField.SetSafeValue(Slider.value);
        if (Overlay == null) return;
        float dx = DesktopPortalController.Instance.XSlider.Slider.value,
              dy = DesktopPortalController.Instance.YSlider.Slider.value,
              dz = DesktopPortalController.Instance.ZSlider.Slider.value;
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
