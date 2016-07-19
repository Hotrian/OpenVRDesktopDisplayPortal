using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(InputField))]
public class RotationMatchInputField : MonoBehaviour
{
    public HOTK_Overlay Overlay;
    public RotationAxis Axis;

    public Slider Slider;
    public InputField InputField
    {
        get { return _inputField ?? (_inputField = GetComponent<InputField>()); }
    }

    private InputField _inputField;

    public void OnEnable()
    {
        if (Overlay == null) return;
        switch (Axis)
        {
            case RotationAxis.X:
                InputField.text = Overlay.transform.eulerAngles.x.ToString();
                break;
            case RotationAxis.Y:
                InputField.text = Overlay.transform.eulerAngles.y.ToString();
                break;
            case RotationAxis.Z:
                InputField.text = Overlay.transform.eulerAngles.z.ToString();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void OnInputChanged()
    {
        float f;
        if (!float.TryParse(InputField.text, out f)) return;
        if (Slider != null) Slider.value = f;
        if (Overlay == null) return;
        float dx = Overlay.transform.eulerAngles.x, dy = Overlay.transform.eulerAngles.y, dz = Overlay.transform.eulerAngles.z;
        switch (Axis)
        {
            case RotationAxis.X:
                dx = f;
                break;
            case RotationAxis.Y:
                dy = f;
                break;
            case RotationAxis.Z:
                dz = f;
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
