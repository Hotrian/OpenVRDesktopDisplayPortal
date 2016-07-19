using UnityEngine;
using System.Collections;

public class ButtonSwapControllers : MonoBehaviour
{
    public DropdownMatchEnumOptions DeviceDropdown;

    public void OnButtonClicked()
    {
        if (DeviceDropdown != null) DeviceDropdown.SwapControllers();
        HOTK_TrackedDeviceManager.Instance.SwapControllers();
    }
}
