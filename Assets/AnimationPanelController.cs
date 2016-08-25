using System;
using UnityEngine;
using UnityEngine.UI;

public class AnimationPanelController : MonoBehaviour
{
    public Dropdown Dropdown;
    public InputField AlphaEnd;
    public InputField AlphaSpeed;
    public InputField ScaleEnd;
    public InputField ScaleSpeed;

    public GameObject AlphaScalePanel;
    public GameObject DodgeGazePanel;
    public GameObject NonePanel;

    public void OnValueChanges()
    {
        if (Dropdown == null) return;
        var parsed = (HOTK_Overlay.AnimationType) Enum.Parse(typeof(HOTK_Overlay.AnimationType), Dropdown.options[Dropdown.value].text);
        switch (parsed)
        {
            case HOTK_Overlay.AnimationType.None:
                AlphaScalePanel.SetActive(false);
                DodgeGazePanel.SetActive(false);
                NonePanel.SetActive(true);
                break;
            case HOTK_Overlay.AnimationType.Alpha:
            case HOTK_Overlay.AnimationType.Scale:
            case HOTK_Overlay.AnimationType.AlphaAndScale:
                AlphaEnd.interactable = parsed == HOTK_Overlay.AnimationType.Alpha || parsed == HOTK_Overlay.AnimationType.AlphaAndScale;
                AlphaSpeed.interactable = parsed == HOTK_Overlay.AnimationType.Alpha || parsed == HOTK_Overlay.AnimationType.AlphaAndScale;
                ScaleEnd.interactable = parsed == HOTK_Overlay.AnimationType.Scale || parsed == HOTK_Overlay.AnimationType.AlphaAndScale;
                ScaleSpeed.interactable = parsed == HOTK_Overlay.AnimationType.Scale || parsed == HOTK_Overlay.AnimationType.AlphaAndScale;
                DodgeGazePanel.SetActive(false);
                NonePanel.SetActive(false);
                AlphaScalePanel.SetActive(true);
                break;
            case HOTK_Overlay.AnimationType.DodgeGaze:
                AlphaScalePanel.SetActive(false);
                NonePanel.SetActive(false);
                DodgeGazePanel.SetActive(true);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
