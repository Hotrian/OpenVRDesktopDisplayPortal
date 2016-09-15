using UnityEngine;
using System.Collections;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class SliderLinkScript : MonoBehaviour
{
    public Slider Slider
    {
        get { return _slider ?? (_slider = GetComponent<Slider>()); }
    }

    private Slider _slider;

    public SliderLinkScript LinkedSlider;

    public void UpdateLink()
    {
        if (LinkedSlider == null) return;

        LinkedSlider.Slider.value = Slider.value;
    }
}
