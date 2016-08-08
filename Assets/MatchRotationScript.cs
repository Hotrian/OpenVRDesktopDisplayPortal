using System;
using UnityEngine;

public class MatchRotationScript : MonoBehaviour
{
    public HOTK_Overlay Target;
    private Transform root;
    private bool _subbed;
    private Quaternion _baseRotation;

	// Use this for initialization
	void OnEnable()
	{
	    if (Target == null)
	    {
	        gameObject.SetActive(false);
            return;
        }
        root = transform.parent;
        if (!_subbed)
	    {
	        _subbed = true;
	        Target.OnOverlayAnchorChanges += UpdateAnchor;
	        Target.OnOverlayAnchorRotationChanges += UpdateRotation;
	    }
	}

    private void UpdateRotation(HOTK_Overlay o, Quaternion r)
    {
        _baseRotation = r;
    }

    // Update is called once per frame
	public void Update()
	{
	    gameObject.transform.localRotation = _baseRotation * Target.transform.localRotation;
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
