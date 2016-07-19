using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class RotationMatchSlider : MonoBehaviour
{
    public HOTK_Overlay Overlay;
    public RotationAxis Axis;

    public InputField InputField;
    public Slider Slider
    {
        get { return _slider ?? (_slider = GetComponent<Slider>()); }
    }

    private Slider _slider;

    public void OnSliderChanged()
    {
        if (InputField != null) InputField.text = Slider.value.ToString();
        if (Overlay == null) return;
        float dx = Overlay.transform.eulerAngles.x, dy = Overlay.transform.eulerAngles.y, dz = Overlay.transform.eulerAngles.z;
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
        Overlay.transform.eulerAngles = new Vector3(dx, dy, dz);
    }

    public enum RotationAxis
    {
        X,
        Y,
        Z
    }
}
