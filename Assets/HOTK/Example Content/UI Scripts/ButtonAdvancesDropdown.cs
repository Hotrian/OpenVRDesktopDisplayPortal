using UnityEngine;
using UnityEngine.UI;

public class ButtonAdvancesDropdown : MonoBehaviour
{
    public Dropdown Dropdown;

    public void ButtonPressed(string s)
    {
        if (Dropdown == null) return;
        var t = Dropdown.value + 1;
        if (t >= Dropdown.options.Count) t = 0;
        Dropdown.value = t;
    }
}
