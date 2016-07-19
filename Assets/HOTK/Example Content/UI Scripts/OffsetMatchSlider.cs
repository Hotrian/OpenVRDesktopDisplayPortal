using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class OffsetMatchSlider : MonoBehaviour
{
    public HOTK_Overlay Overlay;
    public OffsetValue Value;

    public InputField InputField;
    public Slider Slider
    {
        get { return _slider ?? (_slider = GetComponent<Slider>()); }
    }

    private Slider _slider;

    public void OnOffsetChanged()
    {
        if (InputField != null) InputField.text = Slider.value.ToString();
        if (Overlay == null) return;
        float dx = Overlay.AnchorOffset.x, dy = Overlay.AnchorOffset.y, dz = Overlay.AnchorOffset.z;
        switch (Value)
        {
            case OffsetValue.X:
                dx = Slider.value;
                break;
            case OffsetValue.Y:
                dy = Slider.value;
                break;
            case OffsetValue.Z:
                dz = Slider.value;
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
