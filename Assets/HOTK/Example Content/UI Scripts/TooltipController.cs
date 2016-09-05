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
    private static Image _tooltipImage;
    private static Text _tooltipText;

    private static Vector3 _tooltipOffset;

    private static float _tooltipTime;
    private static float _tooltipAlpha;

    public GameObject Canvas;
    public GameObject TooltipPrefab;

    public float TooltipDelay = 1f;
    public float TooltipFadeSpeed = 4f;

	// Use this for initialization
	public void Start()
    {
	    if (_tooltip != null || Canvas == null || TooltipPrefab == null) return;
	    _tooltip = Instantiate(TooltipPrefab);
        _tooltip.transform.SetParent(Canvas.transform);
	    _tooltipRectTransform = _tooltip.GetComponent<RectTransform>();
        _tooltipImage = _tooltip.gameObject.GetComponent<Image>();
        _tooltipText = _tooltip.transform.FindChild("Text").gameObject.GetComponent<Text>();
        SetTooltipText("");
    }
	
	// Update is called once per frame
	public void Update()
	{
	    if (_tooltipText == null || string.IsNullOrEmpty(_tooltipText.text)) return;
	    _tooltip.transform.position = Input.mousePosition + _tooltipOffset;
	    if (Time.time < _tooltipTime) return;
	    if (_tooltipAlpha >= 1f) return;
        SetTooltipAlpha(_tooltipAlpha + (Time.deltaTime * TooltipFadeSpeed));

    }

    public void SetTooltipText(string text)
    {
        if (_tooltipText == null) return;
        _tooltipText.text = text.Replace("<br>", "\n");
        Tooltip.SetActive(!string.IsNullOrEmpty(text));
        _tooltipRectTransform.sizeDelta = new Vector2(_tooltipText.preferredWidth, _tooltipText.preferredHeight);
        _tooltipTime = Time.time + TooltipDelay;
        SetTooltipAlpha(0f);
    }

    private void SetTooltipAlpha(float alpha)
    {
        _tooltipAlpha = Mathf.Clamp01(alpha);
        _tooltipImage.color = new Color(_tooltipImage.color.r, _tooltipImage.color.g, _tooltipImage.color.b, _tooltipAlpha);
        _tooltipText.color = new Color(_tooltipText.color.r, _tooltipText.color.g, _tooltipText.color.b, _tooltipAlpha);
    }

    public string GetTooltipText()
    {
        return _tooltipText == null ? null : _tooltipText.text;
    }

    public void SetTooltipPivot(string pivot)
    {
        if (_tooltipRectTransform == null) return;
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
