using System;
using UnityEngine;
using Valve.VR;

public class HOTK_CompanionOverlay : HOTK_OverlayBase
{
    public HOTK_Overlay Overlay;
    public Texture OverlayTexture;
    public VROverlayInputMethod InputMethod = VROverlayInputMethod.None;

    public CompanionMode OverlayMode;
    private CompanionMode _overlayMode;

    public Action<HOTK_OverlayBase, bool> OnOverlayGazed;

    private bool _subscribed;
    
    private float _alpha;   // These are used to cache values and check for changes
    private float _scale;   // These are used to cache values and check for changes
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
        AutoUpdateRenderTextures = false;
        var error = overlay.CreateOverlay(HOTK_Overlay.Key + gameObject.GetInstanceID(), gameObject.name, ref _handle);
        #pragma warning disable 0168
        // ReSharper disable once UnusedVariable
        var rt = RotationTracker; // Spawn RotationTracker
        #pragma warning restore 0168
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
            Overlay.OnOverlayPositionChanges += OverlayPositionChanges;
            Overlay.OnOverlayRotationChanges += OverlayRotationChanges;
        }
        updateCompanion = true;
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

    public void OnParentEnabled(HOTK_Overlay o)
    {
        gameObject.SetActive(OverlayMode != CompanionMode.DodgeOnGaze || Overlay.AnimateOnGaze == HOTK_Overlay.AnimationType.DodgeGaze);
    }

    public void OnParentDisabled(HOTK_Overlay o)
    {
        gameObject.SetActive(false);
    }

    private bool updateCompanion;

    public void Update()
    {
        CheckCompanionModeChanged();
        // Check if our Overlay's Texture has changed
        CheckOverlayTextureChanged();
        // Check if our Overlay's Anchor has changed
        //CheckOverlayAnchorChanged(ref changed);
        // Check if our Overlay's rotation or position changed
        //CheckOverlayRotationChanged(ref changed);
        CheckOverlayPositionChanged();
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

    public void DoUpdateOverlay()
    {
        updateCompanion = true;
    }

    private void CheckCompanionModeChanged()
    {
        if (_overlayMode == OverlayMode) return;
        _overlayMode = OverlayMode;

        if (Overlay != null)
            AttachToOverlay(Overlay);
    }

    private void CheckOverlayTextureChanged()
    {
        if (((OverlayTexture is RenderTexture || OverlayTexture is MovieTexture) && !AutoUpdateRenderTextures) && _overlayTexture == OverlayTexture && _uvOffset == Overlay.UvOffset) return;
        _overlayTexture = OverlayTexture;
        _uvOffset = Overlay.UvOffset;
        updateCompanion = true;

        //if (MeshRenderer != null) // If our texture changes, change our MeshRenderer's texture also. The MeshRenderer is optional.
            //MeshRenderer.material.mainTexture = OverlayTexture;
    }

    private void CheckOverlayPositionChanged()
    {
        if (_objectPosition == gameObject.transform.localPosition) return;
        if (OverlayMode == CompanionMode.Backside) gameObject.transform.localPosition = Vector3.zero;
        _objectPosition = gameObject.transform.localPosition;
        updateCompanion = true;
    }

    public override void UpdateGaze(bool wasHit)
    {
        if (OnOverlayGazed != null)
            OnOverlayGazed(this, wasHit);
    }

    /// <summary>
    /// Update the Overlay's Position and return the resulting HmdMatrix34_t
    /// </summary>
    /// <returns></returns>
    private HmdMatrix34_t GetOverlayPosition()
    {
        if (_anchor == OpenVR.k_unTrackedDeviceIndexInvalid)
        {
            //Debug.Log(OverlayReference.transform.position + " " + transform.position);
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

    private void AttachToOverlay(HOTK_OverlayBase o)
    {
        var overlay = o as HOTK_Overlay;
        if (overlay == null) return;
        // Update Overlay Anchor position
        GetOverlayPosition();

        // Update cached values
        _anchorDevice = overlay.AnchorDevice;
        _anchorPoint = overlay.AnchorPoint;
        _anchorOffset = overlay.AnchorOffset;
        _alpha = overlay.GetCurrentAlpha();
        _scale = overlay.GetCurrentScale();
        gameObject.transform.parent = o.gameObject.transform;
        gameObject.transform.localPosition = Vector3.zero;
        OverlayReference.transform.parent = Overlay.OverlayReference.transform;
        OverlayReference.transform.localPosition = Vector3.zero;
        // Attach Overlay
        switch (_anchorDevice)
        {
            case HOTK_Overlay.AttachmentDevice.Screen:
                _anchor = OpenVR.k_unTrackedDeviceIndexInvalid;
                gameObject.transform.localRotation = OverlayMode == CompanionMode.Backside ? Quaternion.AngleAxis(180f, Vector3.up) : Quaternion.identity;
                OverlayReference.transform.localRotation = Quaternion.identity;
                break;
            case HOTK_Overlay.AttachmentDevice.World:
                _anchor = OpenVR.k_unTrackedDeviceIndexInvalid;
                gameObject.transform.parent = OverlayMode == CompanionMode.DodgeOnGaze ? o.gameObject.transform.parent : o.gameObject.transform;
                gameObject.transform.localPosition = OverlayMode == CompanionMode.DodgeOnGaze ? o.gameObject.transform.localPosition : Vector3.zero;
                gameObject.transform.localRotation = OverlayMode == CompanionMode.Backside ? Quaternion.AngleAxis(180f, Vector3.up) : OverlayMode == CompanionMode.DodgeOnGaze ? o.gameObject.transform.localRotation : Quaternion.identity;
                OverlayReference.transform.localRotation = Quaternion.identity;
                break;
            case HOTK_Overlay.AttachmentDevice.LeftController:
                _anchor = HOTK_TrackedDeviceManager.Instance.LeftIndex;
                gameObject.transform.localRotation = Quaternion.identity;
                OverlayReference.transform.localRotation = OverlayMode == CompanionMode.Backside ? Quaternion.AngleAxis(180f, Vector3.up) : Quaternion.identity;
                OverlayRotationChanges(); // Force rotational update
                break;
            case HOTK_Overlay.AttachmentDevice.RightController:
                _anchor = HOTK_TrackedDeviceManager.Instance.RightIndex;
                gameObject.transform.localRotation = Quaternion.identity;
                OverlayReference.transform.localRotation = OverlayMode == CompanionMode.Backside ? Quaternion.AngleAxis(180f, Vector3.up) : Quaternion.identity;
                OverlayRotationChanges(); // Force rotational update
                break;
            default:
                throw new ArgumentOutOfRangeException("device", _anchorDevice, null);
        }

        if (OnOverlayAttachmentChanges != null)
            OnOverlayAttachmentChanges(this);

        updateCompanion = true;
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
        if (OverlayMode != CompanionMode.DodgeOnGaze) return;
        if (_anchorDevice == HOTK_Overlay.AttachmentDevice.World)
            gameObject.transform.localPosition = pos;
        updateCompanion = true;
    }

    private void OverlayRotationChanges(HOTK_Overlay o, Quaternion rot)
    {
        if (OverlayMode == CompanionMode.DodgeOnGaze)
        {
            if (_anchorDevice == HOTK_Overlay.AttachmentDevice.World)
                gameObject.transform.localRotation = rot;
        }
        OverlayRotationChanges();
    }

    private void OverlayRotationChanges()
    {
        _objectRotation = gameObject.transform.localRotation;
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
        VRInterface,
        DodgeOnGaze
    }
}
