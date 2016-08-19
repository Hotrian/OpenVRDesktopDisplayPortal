using System;
using UnityEngine;
using Valve.VR;

public class HOTK_OverlayBase : MonoBehaviour
{
    public ulong Handle { get { return _handle; } }
    protected ulong _handle = OpenVR.k_ulOverlayHandleInvalid;    // caches a reference to our Overlay handle

    public bool AutoUpdateRenderTextures;

    public GameObject RotationTracker
    {
        get
        {
            if (_rotationTracker != null) return _rotationTracker;
            _rotationTracker = new GameObject("Overlay Rotation Tracker" + gameObject.name);// {hideFlags = HideFlags.HideInHierarchy};
            _rotationTracker.transform.parent = transform.parent;
            var com = _rotationTracker.AddComponent<MatchRotationScript>();
            com.Target = this;
            _rotationTracker.SetActive(true);
            return _rotationTracker;
        }
    }
    private GameObject _rotationTracker;

    public GameObject OverlayReference
    {
        get
        {
            return _overlayReference ?? (_overlayReference = new GameObject("Overlay Reference" + GetType()));// { hideFlags = HideFlags.HideInHierarchy };
        }
    }
    private GameObject _overlayReference;

    public HOTK_TrackedDevice HittingTracker;
    public HOTK_TrackedDevice TouchingTracker;

    public Action<HOTK_OverlayBase> OnOverlayAttachmentChanges;
    public Action<HOTK_OverlayBase, HOTK_TrackedDevice, SteamVR_Overlay.IntersectionResults> OnControllerHitsOverlay; // Occurs when either controller aims at this overlay
    public Action<HOTK_OverlayBase, HOTK_TrackedDevice> OnControllerUnhitsOverlay; // Occurs when the currently aiming controller stops aiming at this overlay
    public Action<HOTK_OverlayBase, HOTK_TrackedDevice, SteamVR_Overlay.IntersectionResults> OnControllerTouchesOverlay;
    public Action<HOTK_OverlayBase, HOTK_TrackedDevice> OnControllerStopsTouchingOverlay;

    protected Texture _overlayTexture;                    // These are used to cache values and check for changes
    protected Vector4 _uvOffset = Vector4.zero;
    protected HOTK_Overlay.AttachmentDevice _anchorDevice;             // These are used to cache values and check for changes
    protected HOTK_Overlay.AttachmentPoint _anchorPoint;               // These are used to cache values and check for changes
    protected Vector3 _anchorOffset = Vector3.zero;       // These are used to cache values and check for changes
    protected Vector3 _objectPosition = Vector3.zero;     // These are used to cache values and check for changes
    protected Quaternion _anchorRotation = Quaternion.identity;   // These are used to cache values and check for changes
    protected Quaternion _objectRotation = Quaternion.identity;   // These are used to cache values and check for changes
}
