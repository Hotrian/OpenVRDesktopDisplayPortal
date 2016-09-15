using UnityEngine;
using UnityEngine.UI;

public class ButtonFlipsToggle : MonoBehaviour
{
    public Toggle Toggle;

    public void FlipToggle(string s)
    {
        if (Toggle == null) return;
        Toggle.isOn = !Toggle.isOn;
    }
}
