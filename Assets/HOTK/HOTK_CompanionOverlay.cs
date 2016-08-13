using System;
using UnityEngine;
using Valve.VR;

public class HOTK_CompanionOverlay : MonoBehaviour
{
    public HOTK_Overlay Overlay;
    public GameObject OverlayReference;             // Used to get a reference for the Overlay's transform
    public Texture OverlayTexture;
    public VROverlayInputMethod InputMethod = VROverlayInputMethod.None;
    public Vector3 CompanionOffset;

    private bool _subscribed;

    private Texture _overlayTexture;                    // These are used to cache values and check for changes
    private Vector4 _uvOffset = Vector4.zero;
    private HOTK_Overlay.AttachmentDevice _anchorDevice;             // These are used to cache values and check for changes
    private HOTK_Overlay.AttachmentPoint _anchorPoint;               // These are used to cache values and check for changes
    private Vector3 _anchorOffset = Vector3.zero;       // These are used to cache values and check for changes
    private Vector3 _objectPosition = Vector3.zero;     // These are used to cache values and check for changes
    private Quaternion _anchorRotation = Quaternion.identity;   // These are used to cache values and check for changes
    private float _alpha;   // These are used to cache values and check for changes
    private float _scale;   // These are used to cache values and check for changes
    private ulong _handle = OpenVR.k_ulOverlayHandleInvalid;    // caches a reference to our Overlay handle
    private uint _anchor;   // caches a HOTK_TrackedDevice ID for anchoring the Overlay, if applicable

    public void OnEnable()
    {
        #pragma warning disable 0168
        // ReSharper disable once UnusedVariable
        var svr = SteamVR.instance; // Init the SteamVR drivers
        #pragma warning restore 0168
        var overlay = OpenVR.Overlay;
        if (overlay == null) return;
        // Cache the default value on start
        _objectPosition = Vector3.zero;
        var error = overlay.CreateOverlay(HOTK_Overlay.Key + gameObject.GetInstanceID(), gameObject.name, ref _handle);
        if (error != EVROverlayError.None)
        {
            Debug.Log(error.ToString());
            enabled = false;
            return;
        }

        if (!_subscribed && Overlay != null)
        {
            _subscribed = true;
            Overlay.OnOverlayAttachmentChanges += AttachToOverlay;
            Overlay.OnOverlayAlphaChanges += OverlayAlphaChanges;
            Overlay.OnOverlayScaleChanges += OverlayScaleChanges;
            Overlay.OnOverlayRotationChanges += OverlayRotationChanges;
        }
    }

    /// <summary>
    /// When disabled, Destroy the Overlay.
    /// </summary>
    public void OnDisable()
    {
        if (_handle == OpenVR.k_ulOverlayHandleInvalid) return;
        var overlay = OpenVR.Overlay;
        if (overlay != null) overlay.DestroyOverlay(_handle);
        _handle = OpenVR.k_ulOverlayHandleInvalid;
    }

    private bool updateCompanion;

    public void Update()
    {
        // Check if our Overlay's Texture has changed
        CheckOverlayTextureChanged(ref updateCompanion);
        // Check if our Overlay's Anchor has changed
        //CheckOverlayAnchorChanged(ref changed);
        // Check if our Overlay's rotation or position changed
        //CheckOverlayRotationChanged(ref changed);
        //CheckOverlayPositionChanged(ref changed);
        // Check if our Overlay's Alpha or Scale changed
        //CheckOverlayAlphaAndScale(ref changed);
        // Check if our Overlay is being Gazed at, or has been recently and is still animating
        //if (AnimateOnGaze != AnimationType.None) UpdateGaze(ref changed);
        // Check if a controller is aiming at our Overlay
        //UpdateControllers();
        // Check if our Overlay's HighQuality, AntiAlias, or Curved setting changed
        //CheckHighQualityChanged(ref changed);
        // Update our Overlay if anything has changed
        if (updateCompanion)// || _doUpdate)
        {
            updateCompanion = false;
            //_justUpdated = true;
            //_doUpdate = false;
            UpdateOverlay();
        }
        else
        {
            //_justUpdated = false;
            //UpdateTexture();
        }
    }

    private void CheckOverlayTextureChanged(ref bool changed)
    {
        if (_overlayTexture == OverlayTexture && _uvOffset == Overlay.UvOffset) return;
        _overlayTexture = OverlayTexture;
        _uvOffset = Overlay.UvOffset;
        changed = true;

        //if (MeshRenderer != null) // If our texture changes, change our MeshRenderer's texture also. The MeshRenderer is optional.
            //MeshRenderer.material.mainTexture = OverlayTexture;
    }

    /// <summary>
    /// Update the Overlay's Position and return the resulting HmdMatrix34_t
    /// </summary>
    /// <returns></returns>
    private HmdMatrix34_t GetOverlayPosition()
    {
        if (OverlayReference == null) OverlayReference = new GameObject("Overlay Reference");// { hideFlags = HideFlags.HideInHierarchy };
        if (_anchor == OpenVR.k_unTrackedDeviceIndexInvalid)
        {
            var offset = new SteamVR_Utils.RigidTransform(OverlayReference.transform, transform);
            offset.pos.x /= OverlayReference.transform.localScale.x;
            offset.pos.y /= OverlayReference.transform.localScale.y;
            offset.pos.z /= OverlayReference.transform.localScale.z;
            var t = offset.ToHmdMatrix34();
            return t;
        }
        else
        {
            var offset = new SteamVR_Utils.RigidTransform(HOTK_Overlay.ZeroReference.transform, OverlayReference.transform);
            offset.pos.x /= HOTK_Overlay.ZeroReference.transform.localScale.x;
            offset.pos.y /= HOTK_Overlay.ZeroReference.transform.localScale.y;
            offset.pos.z /= HOTK_Overlay.ZeroReference.transform.localScale.z;
            var t = offset.ToHmdMatrix34();
            return t;
        }
    }

    private void AttachToOverlay(HOTK_Overlay o)
    {
        // Update Overlay Anchor position
        GetOverlayPosition();

        // Update cached values
        _anchorDevice = o.AnchorDevice;
        _anchorPoint = o.AnchorPoint;
        _anchorOffset = o.AnchorOffset;
        _alpha = o.GetCurrentAlpha();
        _scale = o.GetCurrentScale();
        gameObject.transform.parent = o.gameObject.transform;
        gameObject.transform.localPosition = new Vector3(0f, 1f, 0f);
        // Attach Overlay
        switch (_anchorDevice)
        {
            case HOTK_Overlay.AttachmentDevice.Screen:
                _anchor = OpenVR.k_unTrackedDeviceIndexInvalid;
                gameObject.transform.localRotation = Quaternion.identity;
                OverlayReference.transform.localRotation = Quaternion.identity;
                break;
            case HOTK_Overlay.AttachmentDevice.World:
                _anchor = OpenVR.k_unTrackedDeviceIndexInvalid;
                gameObject.transform.localRotation = Quaternion.identity;
                OverlayReference.transform.localRotation = Quaternion.identity;
                break;
            case HOTK_Overlay.AttachmentDevice.LeftController:
                _anchor = HOTK_TrackedDeviceManager.Instance.LeftIndex;
                AttachToController(_anchorPoint, _anchorOffset);
                break;
            case HOTK_Overlay.AttachmentDevice.RightController:
                _anchor = HOTK_TrackedDeviceManager.Instance.RightIndex;
                AttachToController(_anchorPoint, _anchorOffset);
                break;
            default:
                throw new ArgumentOutOfRangeException("device", _anchorDevice, null);
        }

        updateCompanion = true;
    }

    private void AttachToController(HOTK_Overlay.AttachmentPoint point, Vector3 offset)
    {
        float dx = offset.x, dy = offset.y, dz = offset.z;

        Vector3 pos;
        var rot = Quaternion.identity;
        // Apply position and rotation to Overlay anchor
        // Some Axis are flipped here to reorient the offset
        switch (point)
        {
            case HOTK_Overlay.AttachmentPoint.FlatAbove:
            case HOTK_Overlay.AttachmentPoint.FlatBelow:
                pos = new Vector3(dx, dy, dz);
                break;
            case HOTK_Overlay.AttachmentPoint.FlatBelowFlipped:
                pos = new Vector3(dx, -dy, -dz);
                rot = Quaternion.AngleAxis(180f, new Vector3(1f, 0f, 0f));
                break;
            case HOTK_Overlay.AttachmentPoint.Center:
            case HOTK_Overlay.AttachmentPoint.Above:
            case HOTK_Overlay.AttachmentPoint.Below:
                pos = new Vector3(dx, -dz, dy);
                rot = Quaternion.AngleAxis(90f, new Vector3(1f, 0f, 0f));
                break;
            case HOTK_Overlay.AttachmentPoint.Up:
            case HOTK_Overlay.AttachmentPoint.Down:
            case HOTK_Overlay.AttachmentPoint.Left:
            case HOTK_Overlay.AttachmentPoint.Right:
                pos = new Vector3(dx, -dz, dy);
                rot = Quaternion.AngleAxis(90f, new Vector3(1f, 0f, 0f));
                break;
            case HOTK_Overlay.AttachmentPoint.AboveFlipped:
            case HOTK_Overlay.AttachmentPoint.BelowFlipped:
                pos = new Vector3(-dx, dz, dy);
                rot = Quaternion.AngleAxis(90f, new Vector3(1f, 0f, 0f)) * Quaternion.AngleAxis(180f, new Vector3(0f, 1f, 0f));
                break;
            default:
                throw new ArgumentOutOfRangeException("point", point, null);
        }
        //OverlayReference.transform.localPosition = pos;
        _anchorRotation = rot;
    }

    private void OverlayAlphaChanges(HOTK_Overlay o, float alpha)
    {
        _alpha = alpha;
        updateCompanion = true;
    }

    private void OverlayScaleChanges(HOTK_Overlay o, float scale)
    {
        _scale = scale;
        updateCompanion = true;
    }

    private void OverlayPositionChanges(HOTK_Overlay o, Vector3 pos)
    {
        gameObject.transform.localPosition = pos;
        updateCompanion = true;
    }

    private void OverlayRotationChanges(HOTK_Overlay o, Quaternion rot)
    {
        //gameObject.transform.localRotation = rot;
        updateCompanion = true;
    }

    /// <summary>
    /// Push Updates to our Overlay to the OpenVR System
    /// </summary>
    private void UpdateOverlay()
    {
        var overlay = OpenVR.Overlay;
        if (overlay == null) return;

        if (OverlayTexture != null)
        {
            var error = overlay.ShowOverlay(_handle);
            if (error == EVROverlayError.InvalidHandle || error == EVROverlayError.UnknownOverlay)
            {
                if (overlay.FindOverlay(HOTK_Overlay.Key + gameObject.GetInstanceID(), ref _handle) != EVROverlayError.None) return;
            }
            Debug.Log("Drawing Companion");
            var tex = new Texture_t
            {
                handle = OverlayTexture.GetNativeTexturePtr(),
                eType = SteamVR.instance.graphicsAPI,
                eColorSpace = EColorSpace.Auto
            };
            overlay.SetOverlayColor(_handle, 1f, 1f, 1f);
            //overlay.SetOverlayGamma(_handle, 2.2f); // Doesn't exist yet :(
            overlay.SetOverlayTexture(_handle, ref tex);
            overlay.SetOverlayAlpha(_handle, _alpha);
            overlay.SetOverlayWidthInMeters(_handle, _scale);

            var textureBounds = new VRTextureBounds_t
            {
                uMin = (0 + Overlay.UvOffset.x) * Overlay.UvOffset.z,
                vMin = (1 + Overlay.UvOffset.y) * Overlay.UvOffset.w,
                uMax = (1 + Overlay.UvOffset.x) * Overlay.UvOffset.z,
                vMax = (0 + Overlay.UvOffset.y) * Overlay.UvOffset.w
            };
            overlay.SetOverlayTextureBounds(_handle, ref textureBounds);

            var vecMouseScale = new HmdVector2_t
            {
                v0 = 1f,
                v1 = (float)OverlayTexture.height / (float)OverlayTexture.width
            };
            overlay.SetOverlayMouseScale(_handle, ref vecMouseScale);

            if (_anchor != OpenVR.k_unTrackedDeviceIndexInvalid) // Attached to some HOTK_TrackedDevice, used for Controllers
            {
                var t = GetOverlayPosition();
                overlay.SetOverlayTransformTrackedDeviceRelative(_handle, _anchor, ref t);
            }
            else if (_anchorDevice == HOTK_Overlay.AttachmentDevice.World) // Attached to World
            {
                var t = GetOverlayPosition();
                overlay.SetOverlayTransformAbsolute(_handle, SteamVR_Render.instance.trackingSpace, ref t);
            }
            else
            {
                var vrcam = SteamVR_Render.Top();
                if (vrcam != null && vrcam.origin != null) // Attached to Camera (We are Rendering)
                {
                    var offset = new SteamVR_Utils.RigidTransform(vrcam.origin, transform);
                    offset.pos.x /= vrcam.origin.localScale.x;
                    offset.pos.y /= vrcam.origin.localScale.y;
                    offset.pos.z /= vrcam.origin.localScale.z;

                    var t = offset.ToHmdMatrix34();
                    overlay.SetOverlayTransformAbsolute(_handle, SteamVR_Render.instance.trackingSpace, ref t);
                }
                else // Attached to Camera (We are Not Rendering)
                {
                    var t = GetOverlayPosition();
                    overlay.SetOverlayTransformTrackedDeviceRelative(_handle, 0, ref t);
                }
            }

            overlay.SetOverlayInputMethod(_handle, InputMethod);
        }
        else
        {
            overlay.HideOverlay(_handle);
        }
    }

    public enum CompanionMode
    {
        Backside,
        VRInterface
    }
}
