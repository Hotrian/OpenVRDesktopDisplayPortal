using System;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class TooltipController : MonoBehaviour
{
    public static TooltipController Instance
    {
        get { return _instance ?? (_instance = FindObjectOfType<TooltipController>()); }
    }

    private static TooltipController _instance;

    public static GameObject Tooltip
    {
        get { return _tooltip; }
    }

    private static GameObject _tooltip;
    private static RectTransform _tooltipRectTransform;
    private static Text _tooltipText;

    private static Vector3 _tooltipOffset;

    public GameObject Canvas;
    public GameObject TooltipPrefab;

	// Use this for initialization
	public void Start()
    {
	    if (_tooltip != null || Canvas == null || TooltipPrefab == null) return;
	    _tooltip = Instantiate(TooltipPrefab);
        _tooltip.transform.SetParent(Canvas.transform);
	    _tooltipRectTransform = _tooltip.GetComponent<RectTransform>();
        _tooltipText = _tooltip.transform.FindChild("Text").gameObject.GetComponent<Text>();
        SetTooltipText("");
    }
	
	// Update is called once per frame
	public void Update()
	{
	    if (string.IsNullOrEmpty(_tooltipText.text)) return;
	    _tooltip.transform.position = Input.mousePosition + _tooltipOffset;

	}

    public void SetTooltipText(string text)
    {
        if (_tooltipText == null) return;
        _tooltipText.text = text.Replace("<br>", "\n");
        Tooltip.SetActive(!string.IsNullOrEmpty(text));
        _tooltipRectTransform.sizeDelta = new Vector2(_tooltipText.preferredWidth, _tooltipText.preferredHeight);
    }

    public string GetTooltipText()
    {
        return _tooltipText == null ? null : _tooltipText.text;
    }

    public void SetTooltipPivot(string pivot)
    {
        switch (pivot)
        {
            case "BottomLeft":
                _tooltipRectTransform.pivot = new Vector2(0f, 0f);
                _tooltipOffset = Vector3.zero;
                break;
            case "BottomRight":
                _tooltipRectTransform.pivot = new Vector2(1f, 0f);
                _tooltipOffset = Vector3.zero;
                break;
            case "TopLeft":
                _tooltipRectTransform.pivot = new Vector2(0f, 1f);
                _tooltipOffset = new Vector3(10f, 0f, 0f);
                break;
            case "TopRight":
                _tooltipRectTransform.pivot = new Vector2(1f, 1f);
                _tooltipOffset = Vector3.zero;
                break;
            default:
                throw new ArgumentOutOfRangeException("pivot", pivot, null);
        }
    }
}
