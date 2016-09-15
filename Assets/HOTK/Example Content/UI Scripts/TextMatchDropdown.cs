using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class TextMatchDropdown : MonoBehaviour
{
    public Dropdown Dropdown;

    public Text Text
    {
        get { return _text ?? (_text = GetComponent<Text>()); }
    }

    private Text _text;

    public void OnEnable()
    {
        DropdownChanged(null);
    }

    public void DropdownChanged(string s)
    {
        if (Dropdown == null) return;
        Text.text = Dropdown.options[Dropdown.value].text;
    }
}