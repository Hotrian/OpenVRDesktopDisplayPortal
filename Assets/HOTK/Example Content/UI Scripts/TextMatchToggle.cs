using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class TextMatchToggle : MonoBehaviour
{
    public string EnabledText = "Enabled";
    public string DisabledText = "Disabled";

    public Toggle Toggle;

    public Text Text
    {
        get { return _text ?? (_text = GetComponent<Text>()); }
    }

    private Text _text;

    public void OnEnable()
    {
        ToggleChanged(null);
    }

    public void ToggleChanged(string s)
    {
        if (Toggle == null || EnabledText == null || DisabledText == null) return;
        Text.text = Toggle.isOn ? EnabledText : DisabledText;
    }
}
