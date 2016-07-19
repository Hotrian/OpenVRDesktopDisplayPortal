using System;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;

[RequireComponent(typeof(InputField))]
public class InputFieldHideAnimationSettingsWhenNotSelected : MonoBehaviour
{
    public Dropdown Dropdown;
    public SettingValue Setting;

    public InputField InputField
    {
        get { return _inputField ?? (_inputField = GetComponent<InputField>()); }
    }

    private InputField _inputField;
    public void OnValueChanges()
	{
	    if (Dropdown == null) return;
        var anim = (HOTK_Overlay.AnimationType)Enum.Parse(typeof(HOTK_Overlay.AnimationType), Dropdown.options[Dropdown.value].text);
        if (Setting == SettingValue.Alpha)
        {
            if (anim != HOTK_Overlay.AnimationType.Alpha && anim != HOTK_Overlay.AnimationType.AlphaAndScale)
            {
                InputField.interactable = false;
            }
            else
            {
                InputField.interactable = true;
            }
        }
        else if(Setting == SettingValue.Scale)
        {
            if (anim != HOTK_Overlay.AnimationType.Scale && anim != HOTK_Overlay.AnimationType.AlphaAndScale)
            {
                InputField.interactable = false;
            }
            else
            {
                InputField.interactable = true;
            }
        }

    }

    public enum SettingValue
    {
        Alpha,
        Scale,
    }
}
