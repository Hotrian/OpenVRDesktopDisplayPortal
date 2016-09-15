using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class VRSettingsPanelController : MonoBehaviour
{
    public HOTK_CompanionOverlay Overlay;

    public Button SettingsButton;
    public Text SettingsButtonText;

    public string ButtonOpenText = "Open Settings";
    public string ButtonCloseText = "Close Settings";
    public string ButtonOpeningText = "Opening..";
    public string ButtonClosingText = "Closing..";

    public float OpenPivotX;
    public Vector3 OpenPosition;
    public float OpenTime = 1f;
    public float OpenPivotTime = 1f;

    public float ClosedPivotX;
    public Vector3 ClosedPosition;
    public float ClosedTime = 1f;
    public float ClosedPivotTime = 1f;

    public bool IsOpen { get; private set; }
    public bool IsClosed { get; private set; }

    private bool _busy;
    
	public void Start()
	{
	    IsOpen = false;
	    IsClosed = true;
	    _busy = false;

	    SettingsButtonText.text = ButtonOpenText;
	}

    public void TogglePanel()
    {
        if (_busy) return;
        _busy = true;
        if (IsOpen)
        {
            IsOpen = false;
            SettingsButton.interactable = false;
            SettingsButtonText.text = ButtonClosingText;
            StartCoroutine(DoAnimate(OpenPosition, ClosedPosition, ClosedTime, true, Quaternion.Euler(OpenPivotX, 0f, 0f), Quaternion.Euler(ClosedPivotX, 0f, 0f), ClosedPivotTime));
        }
        else if (IsClosed)
        {
            IsClosed = false;
            SettingsButton.interactable = false;
            SettingsButtonText.text = ButtonOpeningText;
            StartCoroutine(DoAnimate(ClosedPosition, OpenPosition, OpenTime, false, Quaternion.Euler(ClosedPivotX, 0f, 0f), Quaternion.Euler(OpenPivotX, 0f, 0f), OpenPivotTime));
        }
    }

    private IEnumerator DoAnimate(Vector3 start, Vector3 end, float time, bool closing, Quaternion startPivot, Quaternion endPivot, float pivotTime)
    {
        if (time <= 0) yield break;
        float t;
        float ti;

        // Pivot first if opening
        if (!closing)
        {
            t = 0f;
            ti = 0f;
            Overlay.Pivot.transform.localRotation = startPivot;
            while (t < 1f)
            {
                Overlay.Pivot.transform.localRotation = Quaternion.Lerp(startPivot, endPivot, t);
                yield return new WaitForEndOfFrame();
                ti += Time.deltaTime;
                t = Mathf.Min(ti / pivotTime, 1f);
            }
            Overlay.Pivot.transform.localRotation = endPivot;
        }

        t = 0f;
        ti = 0f;
        // Slide animation
        gameObject.transform.localPosition = start;
        while (t < 1f)
        {
            gameObject.transform.localPosition = Vector3.Lerp(start, end, t);
            yield return new WaitForEndOfFrame();
            ti += Time.deltaTime;
            t = Mathf.Min(ti / time, 1f);
        }
        gameObject.transform.localPosition = end;

        // Pivot after if closing
        if (closing)
        {
            t = 0f;
            ti = 0f;
            Overlay.Pivot.transform.localRotation = startPivot;
            while (t < 1f)
            {
                Overlay.Pivot.transform.localRotation = Quaternion.Lerp(startPivot, endPivot, t);
                yield return new WaitForEndOfFrame();
                ti += Time.deltaTime;
                t = Mathf.Min(ti / pivotTime, 1f);
            }
            Overlay.Pivot.transform.localRotation = endPivot;
        }

        if (closing)
        {
            SettingsButtonText.text = ButtonCloseText;
            IsClosed = true;
        }
        else
        {
            SettingsButtonText.text = ButtonOpenText;
            IsOpen = true;
        }
        SettingsButton.interactable = true;
        _busy = false;
    }
}
