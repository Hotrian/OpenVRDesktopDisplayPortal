using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class SliderScrollScript : MonoBehaviour
{
    public bool UseInt;
    public float ValMultiplier = 10f;

    public Slider Slider
    {
        get { return _slider ?? (_slider = GetComponent<Slider>()); }
    }

    private Slider _slider;

    public void OnScrollWheel()
    {
        if (!Slider.interactable) return;
        if (UseInt)
        {
            var v = (int)(Input.GetAxis("Mouse ScrollWheel") * ValMultiplier);
            var val = (int)Slider.value;
            if (v < 0)
            {
                val = Math.Max((int)Slider.minValue, val + v); // Return the larger of the two numbers
            }
            else if (v > 0)
            {
                val = Math.Min((int)Slider.maxValue, val + v); // Return the smaller of the two numbers
            }
            else return;
            Slider.value = val;
        }
        else
        {
            var v = Input.GetAxis("Mouse ScrollWheel") * ValMultiplier;
            var val = Slider.value;
            if (v < 0)
            {
                val = Math.Max(Slider.minValue, val + v); // Return the larger of the two numbers
            }
            else if (v > 0)
            {
                val = Math.Min(Slider.maxValue, val + v); // Return the smaller of the two numbers
            }
            else return;
            Slider.value = val;
        }

        Slider.onValueChanged.Invoke(Slider.value);
    }
}
