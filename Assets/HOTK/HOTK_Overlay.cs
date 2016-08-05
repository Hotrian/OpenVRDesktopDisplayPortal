using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Valve.VR;
using Random = System.Random;

public class HOTK_Overlay : MonoBehaviour
{

    #region Custom Inspector Vars
    [NonSerialized] public bool ShowSettingsAppearance = true;
    [NonSerialized] public bool ShowSettingsInput = false;
    [NonSerialized] public bool ShowSettingsAttachment = false;
    #endregion

    #region Settings
    [Tooltip("The texture that will be drawn for the Overlay.")]
    public Texture OverlayTexture;
    [Tooltip("How, if at all, the Overlay is animated when being looked at.")]
    public AnimationType AnimateOnGaze = AnimationType.None;
    [Tooltip("The alpha at which the Overlay will be drawn.")]
    public float Alpha = 1.0f;			// opacity 0..1
    [Tooltip("The alpha at which the Overlay will be drawn.")]
    public float Alpha2 = 1.0f;			// opacity 0..1 - Only used for AnimateOnGaze
    [Tooltip("The speed the Alpha changes at.")]
    public float AlphaSpeed = 0.01f;
    [Tooltip("The scale at which the Overlay will be drawn.")]
    public float Scale = 1.0f;			// size of overlay view
    [Tooltip("The scale at which the Overlay will be drawn.")]
    public float Scale2 = 1.0f;			// size of overlay view - Only used for AnimateOnGaze
    [Tooltip("The speed the Scale changes at.")]
    public float ScaleSpeed = 0.1f;
    [Tooltip("This causes the Overlay to draw directly to the screen, instead of to the VRCompositor.")]
    public bool Highquality;            // Only one Overlay can be HQ at a time
    [Tooltip("This causes the Overlay to draw with Anti-Aliasing. Requires High Quality.")]
    public bool Antialias;
    [Tooltip("This causes the Overlay to draw curved. Requires High Quality.")]
    public bool Curved;

    public Vector4 UvOffset = new Vector4(0, 0, 1, 1);
    public Vector2 MouseScale = Vector3.one;
    public Vector2 CurvedRange = new Vector2(1, 2);
    public VROverlayInputMethod InputMethod = VROverlayInputMethod.None;

    [Tooltip("Controls where the Overlay will be drawn.")]
    public AttachmentDevice AnchorDevice = AttachmentDevice.Screen;
    [Tooltip("Controls the base offset for the Overlay.")]
    public AttachmentPoint AnchorPoint = AttachmentPoint.Center;
    [Tooltip("Controls the offset for the Overlay.")]
    public Vector3 AnchorOffset = Vector3.zero;
    public FramerateMode Framerate = FramerateMode._30FPS;
    #endregion

    public Action<HOTK_Overlay, HOTK_TrackedDevice, IntersectionResults> OnControllerHitsOverlay; // Occurs when either controller aims at this overlay
    public Action<HOTK_Overlay, HOTK_TrackedDevice> OnControllerUnhitsOverlay; // Occurs when the currently aiming controller stops aiming at this overlay

    #region Interal Vars

    public static Random rand = new Random();
    public static HOTK_Overlay HighQualityOverlay;  // Only one Overlay can be HQ at a time
    public static string Key { get { return "unity:" + Application.companyName + "." + Application.productName + "." + rand.Next(); } }
    public static GameObject ZeroReference;         // Used to get a reference to the world 0, 0, 0 point
    public GameObject OverlayReference;             // Used to get a reference for the Overlay's transform
    
    private Texture _overlayTexture;                    // These are used to cache values and check for changes
    private Vector4 _uvOffset = Vector4.zero;
    private AttachmentDevice _anchorDevice;             // These are used to cache values and check for changes
    private AttachmentPoint _anchorPoint;               // These are used to cache values and check for changes
    private Vector3 _anchorOffset = Vector3.zero;       // These are used to cache values and check for changes
    private Vector3 _objectPosition = Vector3.zero;     // These are used to cache values and check for changes
    private Quaternion _anchorRotation = Quaternion.identity;   // These are used to cache values and check for changes
    private Quaternion _objectRotation = Quaternion.identity;   // These are used to cache values and check for changes
    private bool _wasHighQuality;
    private bool _wasAntiAlias;
    private bool _wasCurved;

    private ulong _handle = OpenVR.k_ulOverlayHandleInvalid;    // caches a reference to our Overlay handle
    private HOTK_TrackedDevice _hmdTracker;                     // caches a reference to the HOTK_TrackedDevice that is tracking the HMD
    private HOTK_TrackedDevice _leftTracker;                     // caches a reference to the HOTK_TrackedDevice that is tracking the Left Controller
    private HOTK_TrackedDevice _rightTracker;                     // caches a reference to the HOTK_TrackedDevice that is tracking the Right Controller
    private uint _anchor;   // caches a HOTK_TrackedDevice ID for anchoring the Overlay, if applicable
    private float _alpha;
    private float _scale;

    // Caches our MeshRenderer, if applicable
    private MeshRenderer MeshRenderer
    {
        get { return _meshRenderer ?? (_meshRenderer = GetComponent<MeshRenderer>()); }
    }
    private MeshRenderer _meshRenderer;

    private bool _justUpdated;
    #endregion
    
    /// <summary>
    /// Check if anything has changed with the Overlay, and update the OpenVR system as necessary.
    /// </summary>
    public void Update()
    {
        var changed = false;
        // Check if our Overlay's Texture has changed
        CheckOverlayTextureChanged(ref changed);
        // Check if our Overlay's Anchor has changed
        CheckOverlayAnchorChanged(ref changed);
        // Check if our Overlay's rotation or position changed
        CheckOverlayRotationChanged(ref changed);
        CheckOverlayPositionChanged(ref changed);
        // Check if our Overlay's Alpha or Scale changed
        CheckOverlayAlphaAndScale(ref changed);
        // Check if our Overlay is being Gazed at, or has been recently and is still animating
        if (AnimateOnGaze != AnimationType.None) UpdateGaze(ref changed);
        // Check if a controller is aiming at our Overlay
        if (OnControllerHitsOverlay != null) UpdateControllers();
        // Check if our Overlay's HighQuality, AntiAlias, or Curved setting changed
        CheckHighQualityChanged(ref changed);
        // Update our Overlay if anything has changed
        if (changed)
        {
            _justUpdated = true;
            UpdateOverlay();
        }
        else
        {
            _justUpdated = false;
            UpdateTexture();
        }
    }

    private HOTK_TrackedDevice _lastHit;

    private void UpdateControllers()
    {
        UpdateController(ref _leftTracker, HOTK_TrackedDevice.EType.LeftController);
        UpdateController(ref _rightTracker, HOTK_TrackedDevice.EType.RightController);
    }

    private void UpdateController(ref HOTK_TrackedDevice tracker, HOTK_TrackedDevice.EType role)
    {
        FindDevice(ref tracker, role);
        if (tracker == null || !tracker.IsValid) return;
        if (_lastHit != null && _lastHit != tracker) return;
        var result = new IntersectionResults();
        var hit = ComputeIntersection(tracker.gameObject.transform.position, tracker.gameObject.transform.forward, ref result);
        if (hit)
        {
            //Debug.Log(result.Normal);
            OnControllerHitsOverlay(this, tracker, result);
            _lastHit = tracker;
        }
        else
        {
            if (_lastHit != null && OnControllerUnhitsOverlay != null) OnControllerUnhitsOverlay(this, _lastHit);
            _lastHit = null;
        }
    }

    public bool ClearOverlayTexture()
    {
        var overlay = OpenVR.Overlay;
        if (overlay == null) return false;
        return (overlay.ClearOverlayTexture(_handle) == EVROverlayError.None);
    }

    public void Start()
    {
        HOTK_TrackedDeviceManager.OnControllerIndexChanged += OnControllerIndexChanged;
    }

    // If the controller we are tracking changes index, update
    private void OnControllerIndexChanged(ETrackedControllerRole role, uint index)
    {
        if (_anchorDevice == AttachmentDevice.LeftController && role == ETrackedControllerRole.LeftHand)
        {
            _anchorDevice = AttachmentDevice.World; // This will trick the system into reattaching the overlay
        }
        else if (_anchorDevice == AttachmentDevice.RightController && role == ETrackedControllerRole.RightHand)
        {
            _anchorDevice = AttachmentDevice.World; // This will trick the system into reattaching the overlay
        }
    }

    /// <summary>
    /// When enabled, Create the Overlay and reset cached values.
    /// </summary>
    public void OnEnable()
    {
        #pragma warning disable 0168
        // ReSharper disable once UnusedVariable
        var svr = SteamVR.instance; // Init the SteamVR drivers
        #pragma warning restore 0168
        var overlay = OpenVR.Overlay;
        if (overlay == null) return;
        // Cache the default value on start
        _scale = Scale;
        _alpha = Alpha;
        _uvOffset = UvOffset;
        _objectRotation = Quaternion.identity;
        _objectPosition = Vector3.zero;
        var error = overlay.CreateOverlay(Key + gameObject.GetInstanceID(), gameObject.name, ref _handle);
        if (error == EVROverlayError.None) return;
        Debug.Log(error.ToString());
        enabled = false;
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

    /// <summary>
    /// Attach the Overlay to [device] at base position [point].
    /// [point] isn't used for HMD or World, and can be ignored.
    /// </summary>
    /// <param name="device"></param>
    /// <param name="point"></param>
    public void AttachTo(AttachmentDevice device, AttachmentPoint point = AttachmentPoint.Center)
    {
        AttachTo(device, 1f, Vector3.zero, point);
    }
    /// <summary>
    /// Attach the Overlay to [device] at [scale], and base position [point].
    /// [point] isn't used for HMD or World, and can be ignored.
    /// </summary>
    /// <param name="device"></param>
    /// <param name="scale"></param>
    /// <param name="point"></param>
    public void AttachTo(AttachmentDevice device, float scale, AttachmentPoint point = AttachmentPoint.Center)
    {
        AttachTo(device, scale, Vector3.zero, point);
    }
    /// <summary>
    /// Attach the Overlay to [device] at [scale] size with offset [offset], and base position [point].
    /// [point] isn't used for HMD or World, and can be ignored.
    /// </summary>
    /// <param name="device"></param>
    /// <param name="scale"></param>
    /// <param name="offset"></param>
    /// <param name="point"></param>
    public void AttachTo(AttachmentDevice device, float scale, Vector3 offset, AttachmentPoint point = AttachmentPoint.Center)
    {
        // Update Overlay Anchor position
        GetOverlayPosition();
        
        // Update cached values
        _anchorDevice = device;
        AnchorDevice = device;
        _anchorPoint = point;
        AnchorPoint = point;
        _anchorOffset = offset;
        AnchorOffset = offset;
        Scale = scale;

        // Attach Overlay
        switch (device)
        {
            case AttachmentDevice.Screen:
                _anchor = OpenVR.k_unTrackedDeviceIndexInvalid;
                OverlayReference.transform.localPosition = -offset;
                OverlayReference.transform.localRotation = Quaternion.identity;
                break;
            case AttachmentDevice.World:
                _anchor = OpenVR.k_unTrackedDeviceIndexInvalid;
                OverlayReference.transform.localPosition = -offset;
                OverlayReference.transform.localRotation = Quaternion.identity;
                break;
            case AttachmentDevice.LeftController:
                _anchor = HOTK_TrackedDeviceManager.Instance.LeftIndex;
                AttachToController(point, offset);
                break;
            case AttachmentDevice.RightController:
                _anchor = HOTK_TrackedDeviceManager.Instance.RightIndex;
                AttachToController(point, offset);
                break;
            default:
                throw new ArgumentOutOfRangeException("device", device, null);
        }
    }

    /// <summary>
    /// Update the Overlay's Position and Rotation, relative to the selected controller, attaching it to [point] with offset [offset]
    /// </summary>
    /// <param name="point"></param>
    /// <param name="offset"></param>
    private void AttachToController(AttachmentPoint point, Vector3 offset)
    {
        float dx = offset.x, dy = offset.y, dz = offset.z;
        // Offset our position based on the Attachment Point
        switch (point)
        {
            case AttachmentPoint.Center:
                break;
            case AttachmentPoint.FlatAbove:
                dz += 0.05f;
                break;
            case AttachmentPoint.FlatBelow:
                dz -= 0.18f;
                break;
            case AttachmentPoint.FlatBelowFlipped:
                dz += 0.18f;
                break;
            case AttachmentPoint.Above:
                dz -= 0.01f;
                break;
            case AttachmentPoint.AboveFlipped:
                dz += 0.01f;
                break;
            case AttachmentPoint.Below:
                dz += 0.1f;
                break;
            case AttachmentPoint.BelowFlipped:
                dz -= 0.1f;
                break;
            case AttachmentPoint.Up:
                dy += 0.5f;
                break;
            case AttachmentPoint.Down:
                dy -= 0.5f;
                break;
            case AttachmentPoint.Left:
                dx -= 0.5f;
                break;
            case AttachmentPoint.Right:
                dx += 0.5f;
                break;
            default:
                throw new ArgumentOutOfRangeException("point", point, null);
        }

        Vector3 pos;
        var rot = Quaternion.identity;
        // Apply position and rotation to Overlay anchor
        // Some Axis are flipped here to reorient the offset
        switch (point)
        {
            case AttachmentPoint.FlatAbove:
            case AttachmentPoint.FlatBelow:
                pos = new Vector3(dx, dy, dz);
                break;
            case AttachmentPoint.FlatBelowFlipped:
                pos = new Vector3(dx, -dy, -dz);
                rot = Quaternion.AngleAxis(180f, new Vector3(1f, 0f, 0f));
                break;
            case AttachmentPoint.Center:
            case AttachmentPoint.Above:
            case AttachmentPoint.Below:
                pos = new Vector3(dx, -dz, dy);
                rot = Quaternion.AngleAxis(90f, new Vector3(1f, 0f, 0f));
                break;
            case AttachmentPoint.Up:
            case AttachmentPoint.Down:
            case AttachmentPoint.Left:
            case AttachmentPoint.Right:
                pos = new Vector3(dx, -dz, dy);
                rot = Quaternion.AngleAxis(90f, new Vector3(1f, 0f, 0f));
                break;
            case AttachmentPoint.AboveFlipped:
            case AttachmentPoint.BelowFlipped:
                pos = new Vector3(-dx, dz, dy);
                rot = Quaternion.AngleAxis(90f, new Vector3(1f, 0f, 0f)) * Quaternion.AngleAxis(180f, new Vector3(0f, 1f, 0f));
                break;
            default:
                throw new ArgumentOutOfRangeException("point", point, null);
        }
        OverlayReference.transform.localPosition = pos;
        _anchorRotation = rot;
        var changed = false;
        CheckOverlayRotationChanged(ref changed, true); // Force rotational update
    }

    private void CheckOverlayAlphaAndScale(ref bool changed)
    {
        if (AnimateOnGaze != AnimationType.Alpha && AnimateOnGaze != AnimationType.AlphaAndScale)
        {
            if (_alpha != Alpha) // Loss of precision but it should work
            {
                _alpha = Alpha;
                changed = true;
            }
        }
        if (AnimateOnGaze != AnimationType.Scale && AnimateOnGaze != AnimationType.AlphaAndScale)
        {
            if (_scale != Scale) // Loss of precision but it should work
            {
                _scale = Scale;
                changed = true;
            }
        }
    }

    /// <summary>
    /// Check if our Overlay's Anchor has changed, and AttachTo it if necessary.
    /// </summary>
    /// <param name="changed"></param>
    private void CheckOverlayAnchorChanged(ref bool changed)
    {
        // If the AnchorDevice changes, or our Attachment Point or Offset changes, reattach the overlay
        if (_anchorDevice == AnchorDevice && _anchorPoint == AnchorPoint && _anchorOffset == AnchorOffset) return;
        AttachTo(AnchorDevice, Scale, AnchorOffset, AnchorPoint);
        changed = true;
    }

    /// <summary>
    /// Update the Overlay's Position if necessary.
    /// </summary>
    /// <returns></returns>
    private void CheckOverlayPositionChanged(ref bool changed)
    {
        if (AnchorDevice == AttachmentDevice.LeftController || AnchorDevice == AttachmentDevice.RightController) return; // Controller overlays do not adjust with gameObject transform
        if (_objectPosition == gameObject.transform.localPosition) return;
        _objectPosition = gameObject.transform.localPosition;
        changed = true;
    }

    /// <summary>
    /// Update the Overlay's Rotation if necessary.
    /// </summary>
    /// <param name="changed"></param>
    /// <param name="force"></param>
    private void CheckOverlayRotationChanged(ref bool changed, bool force = false)
    {
        var gameObjectChanged = _objectRotation != gameObject.transform.localRotation;
        if (gameObjectChanged)
        {
            _objectRotation = gameObject.transform.localRotation;
            changed = true;
        }
        if (_anchor == OpenVR.k_unTrackedDeviceIndexInvalid || OverlayReference == null) return; // This part below is only for Controllers
        if (!force && !gameObjectChanged && OverlayReference.transform.localRotation == _anchorRotation * _objectRotation) return;
        OverlayReference.transform.localRotation = _anchorRotation * _objectRotation;
        changed = true;
    }

    /// <summary>
    /// Update the Overlay's Texture if necessary.
    /// </summary>
    /// <param name="changed"></param>
    private void CheckOverlayTextureChanged(ref bool changed)
    {
        if (_overlayTexture == OverlayTexture && _uvOffset == UvOffset) return;
        _overlayTexture = OverlayTexture;
        _uvOffset = UvOffset;
        changed = true;

        if (MeshRenderer != null) // If our texture changes, change our MeshRenderer's texture also. The MeshRenderer is optional.
            MeshRenderer.material.mainTexture = OverlayTexture;
    }

    private void CheckHighQualityChanged(ref bool changed)
    {
        if (_wasHighQuality == Highquality && _wasAntiAlias == Antialias && _wasCurved == Curved) return;
        _wasHighQuality = Highquality;
        _wasAntiAlias = Antialias;
        _wasCurved = Curved;
        changed = true;
    }

    /// <summary>
    /// Compute a given Ray and determine if it hit an Overlay
    /// </summary>
    /// <param name="source"></param>
    /// <param name="direction"></param>
    /// <param name="results"></param>
    /// <returns></returns>
    private bool ComputeIntersection(Vector3 source, Vector3 direction, ref IntersectionResults results)
    {
        var overlay = OpenVR.Overlay;
        if (overlay == null) return false;

        var input = new VROverlayIntersectionParams_t
        {
            eOrigin = SteamVR_Render.instance.trackingSpace,
            vSource =
            {
                v0 = source.x,
                v1 = source.y,
                v2 = -source.z
            },
            vDirection =
            {
                v0 = direction.x,
                v1 = direction.y,
                v2 = -direction.z
            }
        };

        var output = new VROverlayIntersectionResults_t();
        if (!overlay.ComputeOverlayIntersection(_handle, ref input, ref output)) return false;

        results.Point = new Vector3(output.vPoint.v0, output.vPoint.v1, -output.vPoint.v2);
        results.Normal = new Vector3(output.vNormal.v0, output.vNormal.v1, -output.vNormal.v2);
        results.UVs = new Vector2(output.vUVs.v0, output.vUVs.v1);
        results.Distance = output.fDistance;
        return true;
    }
    private void FindDevice(ref HOTK_TrackedDevice tracker, HOTK_TrackedDevice.EType role)
    {
        if (tracker != null && tracker.IsValid) return;
        // Try to find an HOTK_TrackedDevice that is active and tracking the HMD
        foreach (var g in FindObjectsOfType<HOTK_TrackedDevice>().Where(g => g.enabled && g.Type == role))
        {
            tracker = g;
            break;
        }

        if (tracker != null) return;
        Debug.LogWarning("Couldn't find a " + role + " tracker. Making one up :(");
        var go = new GameObject(role + " Tracker", typeof(HOTK_TrackedDevice)) { hideFlags = HideFlags.HideInHierarchy }.GetComponent<HOTK_TrackedDevice>();
        go.Type = role;
        tracker = go;
    }

    /// <summary>
    /// Update the Overlay's Position and return the resulting HmdMatrix34_t
    /// </summary>
    /// <returns></returns>
    private HmdMatrix34_t GetOverlayPosition()
    {
        if (OverlayReference == null) OverlayReference = new GameObject("Overlay Reference") { hideFlags = HideFlags.HideInHierarchy };
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
            if (ZeroReference == null) ZeroReference = new GameObject("Zero Reference") { hideFlags = HideFlags.HideInHierarchy };
            var offset = new SteamVR_Utils.RigidTransform(ZeroReference.transform, OverlayReference.transform);
            offset.pos.x /= ZeroReference.transform.localScale.x;
            offset.pos.y /= ZeroReference.transform.localScale.y;
            offset.pos.z /= ZeroReference.transform.localScale.z;
            var t = offset.ToHmdMatrix34();
            return t;
        }
    }

    /// <summary>
    /// Animate this Overlay, based on it's AnimateOnGaze setting.
    /// </summary>
    /// <param name="hit"></param>
    /// <param name="changed"></param>
    private void HandleAnimateOnGaze(bool hit, ref bool changed)
    {
        if (hit)
        {
            if (AnimateOnGaze == AnimationType.Alpha || AnimateOnGaze == AnimationType.AlphaAndScale)
            {
                if (_alpha < Alpha2)
                {
                    _alpha += AlphaSpeed;
                    changed = true;
                    if (_alpha > Alpha2)
                        _alpha = Alpha2;
                }else if (_alpha > Alpha2)
                {
                    _alpha -= AlphaSpeed;
                    changed = true;
                    if (_alpha < Alpha2)
                        _alpha = Alpha2;
                }
            }
            if (AnimateOnGaze == AnimationType.Scale || AnimateOnGaze == AnimationType.AlphaAndScale)
            {
                if (_scale < Scale2)
                {
                    _scale += ScaleSpeed;
                    changed = true;
                    if (_scale > Scale2)
                        _scale = Scale2;
                }else if (_scale > Scale2)
                {
                    _scale -= ScaleSpeed;
                    changed = true;
                    if (_scale < Scale2)
                        _scale = Scale2;
                }
            }
        }
        else
        {
            if (AnimateOnGaze == AnimationType.Alpha || AnimateOnGaze == AnimationType.AlphaAndScale)
            {
                if (_alpha > Alpha)
                {
                    _alpha -= AlphaSpeed;
                    changed = true;
                    if (_alpha < Alpha)
                        _alpha = Alpha;
                }else if (_alpha < Alpha)
                {
                    _alpha += AlphaSpeed;
                    changed = true;
                    if (_alpha > Alpha)
                        _alpha = Alpha;
                }
            }
            if (AnimateOnGaze == AnimationType.Scale || AnimateOnGaze == AnimationType.AlphaAndScale)
            {
                if (_scale > Scale)
                {
                    _scale -= ScaleSpeed;
                    changed = true;
                    if (_scale < Scale)
                        _scale = Scale;
                }else if (_scale < Scale)
                {
                    _scale += ScaleSpeed;
                    changed = true;
                    if (_scale > Scale)
                        _scale = Scale;
                }
            }
        }
    }

    /// <summary>
    /// Attempt to ComputerIntersection the HMD's Gaze and hit an Overlay
    /// </summary>
    /// <param name="changed"></param>
    private void UpdateGaze(ref bool changed)
    {
        FindDevice(ref _hmdTracker, HOTK_TrackedDevice.EType.HMD);
        var hit = false;
        if (_hmdTracker != null && _hmdTracker.IsValid)
        {
            var result = new IntersectionResults();
            hit = ComputeIntersection(_hmdTracker.gameObject.transform.position, _hmdTracker.gameObject.transform.forward, ref result);
            //Debug.Log("Hit! " + gameObject.name);
        }
        HandleAnimateOnGaze(hit, ref changed);
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
                if (overlay.FindOverlay(Key, ref _handle) != EVROverlayError.None) return;
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
            overlay.SetOverlayAlpha(_handle, AnimateOnGaze == AnimationType.Alpha || AnimateOnGaze == AnimationType.AlphaAndScale ? _alpha : Alpha);
            overlay.SetOverlayWidthInMeters(_handle, AnimateOnGaze == AnimationType.Scale || AnimateOnGaze == AnimationType.AlphaAndScale ? _scale : Scale);
            overlay.SetOverlayAutoCurveDistanceRangeInMeters(_handle, CurvedRange.x, CurvedRange.y);

            var textureBounds = new VRTextureBounds_t
            {
                uMin = (0 + UvOffset.x) * UvOffset.z, vMin = (1 + UvOffset.y) * UvOffset.w,
                uMax = (1 + UvOffset.x) * UvOffset.z, vMax = (0 + UvOffset.y) * UvOffset.w
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
            else if (AnchorDevice == AttachmentDevice.World) // Attached to World
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

            if (Highquality)
            {
                if (HighQualityOverlay != this && HighQualityOverlay != null)
                {
                    if (HighQualityOverlay.Highquality)
                    {
                        Debug.LogWarning("Only one Overlay can be in HighQuality mode as per the OpenVR API.");
                        HighQualityOverlay.Highquality = false;
                    }
                    HighQualityOverlay = this;
                }
                else if (HighQualityOverlay == null)
                    HighQualityOverlay = this;

                overlay.SetHighQualityOverlay(_handle);
                overlay.SetOverlayFlag(_handle, VROverlayFlags.Curved, Curved);
                overlay.SetOverlayFlag(_handle, VROverlayFlags.RGSS4X, Antialias);
            }
            else if (overlay.GetHighQualityOverlay() == _handle)
            {
                overlay.SetHighQualityOverlay(OpenVR.k_ulOverlayHandleInvalid);
            }
        }
        else
        {
            overlay.HideOverlay(_handle);
        }
    }

    public void RefreshTexture()
    {
        UpdateTexture(true);
    }

    /// <summary>
    /// Update our texture if we are a RenderTexture.
    /// This is called every frame where nothing else changes, so that we still push RenderTexture updates if needed.
    /// </summary>
    private void UpdateTexture(bool refresh = false)
    {
        if (!(OverlayTexture is RenderTexture) && !(OverlayTexture is MovieTexture) && !refresh) return; // This covers the null check for OverlayTexture
        if (_justUpdated) return;
        if (refresh && OverlayTexture == null) return;
        var overlay = OpenVR.Overlay;
        if (overlay == null) return;

        var tex = new Texture_t
        {
            handle = OverlayTexture.GetNativeTexturePtr(),
            eType = SteamVR.instance.graphicsAPI,
            eColorSpace = EColorSpace.Auto
        };

        var vecMouseScale = new HmdVector2_t
        {
            v0 = 1f,
            v1 = (float)OverlayTexture.height / (float)OverlayTexture.width
        };
        overlay.SetOverlayMouseScale(_handle, ref vecMouseScale);

        overlay.SetOverlayTexture(_handle, ref tex);
    }

    #region Structs and Enums
    public struct IntersectionResults
    {
        public Vector3 Point;
        public Vector3 Normal;
        public Vector2 UVs;
        public float Distance;
    }

    /// <summary>
    /// Used to determine where an Overlay should be attached.
    /// </summary>
    public enum AttachmentDevice
    {
        /// <summary>
        /// Attempts to attach the Overlay to the World
        /// </summary>
        World,
        /// <summary>
        /// Attempts to attach the Overlay to the Screen / HMD
        /// </summary>
        Screen,
        /// <summary>
        /// Attempts to attach the Overlay to the Left Controller
        /// </summary>
        LeftController,
        /// <summary>
        /// Attempts to attach the Overlay to the Right Controller
        /// </summary>
        RightController,
    }

    /// <summary>
    /// Used when attaching Overlays to Controllers, to determine the base attachment offset.
    /// </summary>
    public enum AttachmentPoint
    {
        /// <summary>
        /// Directly in the center at (0, 0, 0), facing upwards through the Trackpad.
        /// </summary>
        Center,
        /// <summary>
        /// At the end of the controller, like a staff ornament, facing towards the center.
        /// </summary>
        FlatAbove,
        /// <summary>
        /// At the bottom of the controller, facing away from the center.
        /// </summary>
        FlatBelow,
        /// <summary>
        /// At the bottom of the controller, facing towards the center.
        /// </summary>
        FlatBelowFlipped,
        /// <summary>
        /// Just above the Trackpad, facing away from the center.
        /// </summary>
        Above,
        /// <summary>
        /// Just above thr Trackpad, facing the center.
        /// </summary>
        AboveFlipped,
        /// <summary>
        /// Just below the Trigger, facing the center.
        /// </summary>
        Below,
        /// <summary>
        /// Just below the Trigger, facing away from the center.
        /// </summary>
        BelowFlipped,
        /// <summary>
        /// When holding the controller out vertically, Like "Center", but "Up", above the controller.
        /// </summary>
        Up,
        /// <summary>
        /// When holding the controller out vertically, Like "Center", but "Down", below the controller.
        /// </summary>
        Down,
        /// <summary>
        /// When holding the controller out vertically, Like "Center", but "Left", to the side of the controller.
        /// </summary>
        Left,
        /// <summary>
        /// When holding the controller out vertically, Like "Center", but "Right", to the side of the controller.
        /// </summary>
        Right,
    }

    public enum AnimationType
    {
        /// <summary>
        /// Don't animate this Overlay.
        /// </summary>
        None,
        /// <summary>
        /// Animate this Overlay by changing its Alpha.
        /// </summary>
        Alpha,
        /// <summary>
        /// Animate this Overlay by scaling it.
        /// </summary>
        Scale,
        /// <summary>
        /// Animate this Overlay by changing its Alpha and scaling it.
        /// </summary>
        AlphaAndScale,
    }

    public enum FramerateMode
    {
        _1FPS = 0,
        _2FPS = 1,
        _5FPS = 2,
        _10FPS = 3,
        _15FPS = 4,
        _24FPS = 5,
        _30FPS = 6,
        _60FPS = 7,
        _90FPS = 8,
        _120FPS = 9,
        AsFastAsPossible = 10
    }

    public static readonly List<int> FramerateValues = new List<int>()
    {
        1,
        2,
        5,
        10,
        15,
        24,
        30,
        60,
        90,
        120,
        9999
    };

    #endregion
}