using System;
using UnityEngine;

public class MatchRotationScript : MonoBehaviour
{
    public HOTK_OverlayBase Target;
    private Transform root;
    private bool _subbed;
    private Quaternion _baseRotation;
    private bool _matchingController;

	// Use this for initialization
	void OnEnable()
	{
	    if (Target == null)
	    {
	        gameObject.SetActive(false);
            return;
        }
        root = transform.parent;
        var overlay = Target as HOTK_Overlay;
        if (overlay != null)
        {
            _matchingController = false;
            if (!_subbed)
            {
                _subbed = true;
                overlay.OnOverlayAnchorChanges += UpdateAnchor;
                overlay.OnOverlayAnchorRotationChanges += UpdateRotation;
            }
            return;
        }

	    var comp = Target as HOTK_CompanionOverlay;
	    if (comp != null)
	    {
            comp.OnOverlayAttachmentChanges += UpdateCompanionAttachment;
	    }
	}

    private void UpdateCompanionAttachment(HOTK_OverlayBase ov)
    {
        var overlay = Target as HOTK_CompanionOverlay;
        if (overlay == null) return;
        switch (overlay.Overlay.AnchorDevice)
        {
            case HOTK_Overlay.AttachmentDevice.World:
            case HOTK_Overlay.AttachmentDevice.Screen:
                _matchingController = false;
                gameObject.transform.parent = overlay.gameObject.transform;
                gameObject.transform.localPosition = Vector3.zero;
                gameObject.transform.localRotation = Quaternion.identity;
                break;
            case HOTK_Overlay.AttachmentDevice.LeftController:
            case HOTK_Overlay.AttachmentDevice.RightController:
                _matchingController = true;
                gameObject.transform.parent = overlay.Overlay.RotationTracker.transform;
                gameObject.transform.localPosition = Vector3.zero;
                gameObject.transform.localRotation = Quaternion.identity;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void UpdateRotation(HOTK_Overlay o, Quaternion r)
    {
        _baseRotation = r;
    }

    // Update is called once per frame
    public void Update()
    {
        if (!_matchingController)
            gameObject.transform.localRotation = _baseRotation*Target.transform.localRotation;
        else
            gameObject.transform.localRotation = Target.OverlayReference.transform.localRotation;
    }

    private void UpdateAnchor(HOTK_Overlay o, HOTK_Overlay.AttachmentDevice d)
    {
        switch (d)
        {
            case HOTK_Overlay.AttachmentDevice.World:
            case HOTK_Overlay.AttachmentDevice.Screen:
                gameObject.transform.parent = root;
                break;
            case HOTK_Overlay.AttachmentDevice.LeftController:
                gameObject.transform.parent = HOTK_TrackedDeviceManager.Instance.LeftTracker.gameObject.transform;
                break;
            case HOTK_Overlay.AttachmentDevice.RightController:
                gameObject.transform.parent = HOTK_TrackedDeviceManager.Instance.RightTracker.gameObject.transform;
                break;
            default:
                throw new ArgumentOutOfRangeException("d", d, null);
        }
        gameObject.transform.localPosition = Vector3.zero;
        gameObject.transform.localRotation = Quaternion.identity;
    }
}
