using UnityEngine;
using System.Collections;

public class PortalAdditionalSettingsPanel : MonoBehaviour
{
    public HSVPickerPortalScript Picker;

    public void Start()
    {
        TogglePanel(false);
    }

    public void TogglePanelOpen()
    {
        TogglePanel(!gameObject.activeSelf);
    }

    private void TogglePanel(bool open)
    {
        gameObject.SetActive(open);
        if (!open) Picker.ClosePanel();
    }
}
