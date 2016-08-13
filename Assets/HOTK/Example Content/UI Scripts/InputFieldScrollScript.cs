using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(InputField))]
public class InputFieldScrollScript : MonoBehaviour
{
    public bool UseInt;
    public float ValMultiplier = 10f;
    public bool UseLimits;
    public bool UseLowerLimit;
    public bool UseUpperLimit;
    public int MinVal;
    public int MaxVal;

    public InputField InputField
    {
        get { return _inputField ?? (_inputField = GetComponent<InputField>()); }
    }

    private InputField _inputField;

    public void OnScrollWheel()
    {
        if (!InputField.interactable) return;
        if (UseInt)
        {
            var v = (int) (Input.GetAxis("Mouse ScrollWheel") * ValMultiplier);
            int val;
            if (!int.TryParse(InputField.text, out val)) return;
            if (UseLimits)
            {
                if (v < 0)
                {
                    if (UseLowerLimit) val = Math.Max(MinVal, val + v); // Return the larger of the two numbers
                    else val += v;
                }else if (v > 0)
                {
                    if (UseUpperLimit) val = Math.Min(MaxVal, val + v); // Return the smaller of the two numbers
                    else val += v;
                }else return;
            }
            else if (v != 0) val += v;
            else return;

            InputField.text = val.ToString();
        }
        else
        {
            var v = Input.GetAxis("Mouse ScrollWheel") * ValMultiplier;
            float val;
            if (!float.TryParse(InputField.text, out val)) return;
            if (UseLimits)
            {
                if (v < 0)
                {
                    if (UseLowerLimit) val = Math.Max(MinVal, val + v); // Return the larger of the two numbers
                    else val += v;
                }
                else if (v > 0)
                {
                    if (UseUpperLimit) val = Math.Min(MaxVal, val + v); // Return the smaller of the two numbers
                    else val += v;
                }
                else return;
            }
            else if (v != 0) val += v;
            else return;

            val = (int)(val*10f)/10f; // floor to nearest 1/10th

            InputField.text = val.ToString(CultureInfo.InvariantCulture);
        }

        InputField.onEndEdit.Invoke(InputField.text);
    }
}
