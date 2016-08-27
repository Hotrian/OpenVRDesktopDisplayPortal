using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Valve.VR;

public class HOTK_TrackedDeviceManager : MonoBehaviour
{
    public static Action<ETrackedControllerRole, uint> OnControllerIndexChanged; // Called any time a specific controller changes index
    public static Action<HOTK_TrackedDevice> OnControllerTriggerDown;
    public static Action<HOTK_TrackedDevice> OnControllerTriggerUp;
    public static Action<HOTK_TrackedDevice> OnControllerTriggerClicked;
    public static Action<HOTK_TrackedDevice> OnControllerTriggerDoubleClicked;
    public static Action<HOTK_TrackedDevice> OnControllerTouchpadDown;
    public static Action<HOTK_TrackedDevice> OnControllerTouchpadUp;
    public static Action<HOTK_TrackedDevice> OnControllerTouchpadClicked;
    public static Action<HOTK_TrackedDevice> OnControllerGripsDown;
    public static Action<HOTK_TrackedDevice> OnControllerGripsUp;
    public static Action<HOTK_TrackedDevice> OnControllerGripsClicked;
    public static Action<HOTK_TrackedDevice, float, float> OnControllerTouchpadTouchStart;
    public static Action<HOTK_TrackedDevice, float, float> OnControllerTouchpadTouchMove;
    public static Action<HOTK_TrackedDevice> OnControllerTouchpadTouchEnd;
    public static Action OnControllerIndicesUpdated; // Called only when both controllers have been checked/assigned or are swapped

    public static HOTK_TrackedDeviceManager Instance
    {
        get { return _instance ?? (_instance = new GameObject("HOTK_TrackedDeviceManager", typeof(HOTK_TrackedDeviceManager)) {hideFlags = HideFlags.HideInHierarchy}.GetComponent<HOTK_TrackedDeviceManager>()); }
    }

    public HOTK_TrackedDevice LeftTracker
    {
        get
        {
            if (_leftTracker == null)
                FindTracker(ref _leftTracker, HOTK_TrackedDevice.EType.LeftController);
            return _leftTracker;
        }
    }

    public HOTK_TrackedDevice RightTracker
    {
        get
        {
            if (_rightTracker == null)
                FindTracker(ref _rightTracker, HOTK_TrackedDevice.EType.RightController);
            return _rightTracker;
        }
    }

    public uint LeftIndex
    {
        get
        {
            return _leftIndex;
        }
    }
    public uint RightIndex
    {
        get
        {
            return _rightIndex;
        }
    }
    public uint HMDIndex
    {
        get
        {
            return _hmdIndex;
        }
    }

    private HOTK_TrackedDevice _hmdTracker;
    private HOTK_TrackedDevice _leftTracker;
    private HOTK_TrackedDevice _rightTracker;

    private static HOTK_TrackedDeviceManager _instance;

    private uint _leftIndex = OpenVR.k_unTrackedDeviceIndexInvalid;
    private uint _rightIndex = OpenVR.k_unTrackedDeviceIndexInvalid;
    private uint _hmdIndex = OpenVR.k_unTrackedDeviceIndexInvalid;

    private readonly List<HOTK_Overlay> _gazeableOverlays = new List<HOTK_Overlay>();
    private readonly List<HOTK_OverlayBase> _gazeableCompanionOverlays = new List<HOTK_OverlayBase>();
    private readonly List<HOTK_OverlayBase> _interactableOverlays = new List<HOTK_OverlayBase>();

    public void Start()
    {
        InvokeRepeating("CheckForControllers", 10f, 10f);
    }

    public void Update()
    {
        FindHMD();
        FindControllers();
        UpdatePoses();
        UpdateButtons();

        UpdateGaze();
        UpdateAim();
    }

    public void SetOverlayCanGaze(HOTK_Overlay overlay, bool isInteractable = true)
    {
        if (isInteractable)
        {
            if (!_gazeableOverlays.Contains(overlay))
            {
                _gazeableOverlays.Add(overlay);
            }
        }
        else
        {
            if (_gazeableOverlays.Contains(overlay))
            {
                _gazeableOverlays.Remove(overlay);
            }
        }
    }

    public void SetCompanionCanGaze(HOTK_OverlayBase overlayBase, bool isInteractable = true)
    {
        if (isInteractable)
        {
            if (!_gazeableCompanionOverlays.Contains(overlayBase))
            {
                _gazeableCompanionOverlays.Add(overlayBase);
            }
        }
        else
        {
            if (_gazeableCompanionOverlays.Contains(overlayBase))
            {
                _gazeableCompanionOverlays.Remove(overlayBase);
            }
        }
    }

    public void SetOverlayCanAim(HOTK_OverlayBase overlay, bool isInteractable = true)
    {
        if (isInteractable)
        {
            if (!_interactableOverlays.Contains(overlay))
            {
                _interactableOverlays.Add(overlay);
            }
        }
        else
        {
            if (_interactableOverlays.Contains(overlay))
            {
                _interactableOverlays.Remove(overlay);
            }
        }
    }

    private void FindTracker(ref HOTK_TrackedDevice tracker, HOTK_TrackedDevice.EType type)
    {
        if (tracker != null && tracker.IsValid) return;
        // Try to find an HOTK_TrackedDevice that is active and tracking the HMD
        foreach (var g in FindObjectsOfType<HOTK_TrackedDevice>().Where(g => g.enabled && g.Type == type))
        {
            tracker = g;
            break;
        }

        if (tracker != null) return;
        Debug.LogWarning("Couldn't find a " + type.ToString() + " tracker. Making one up :(");
        var go = new GameObject(type.ToString() + " Tracker", typeof(HOTK_TrackedDevice)) { hideFlags = HideFlags.HideInHierarchy }.GetComponent<HOTK_TrackedDevice>();
        go.Type = type;
        tracker = go;
    }

    private readonly TrackedDevicePose_t[] _poses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
    private readonly TrackedDevicePose_t[] _gamePoses = new TrackedDevicePose_t[0];

    /// <summary>
    /// Grab the last poses from the compositor and push them to the event listeners.
    /// This method should be disabled if SteamVR_Render.cs is being used, because it is also called there.
    /// </summary>
    private void UpdatePoses()
    {
        var compositor = OpenVR.Compositor;
        if (compositor == null) return;
        compositor.GetLastPoses(_poses, _gamePoses);
        SteamVR_Utils.Event.Send("new_poses", _poses);
        SteamVR_Utils.Event.Send("new_poses_applied");
    }

    private bool _clickedLeft;
    private float _leftButtonDownTimeLeft;
    private float _leftDoubleClickTimeLeft;
    private float _touchpadDownTimeLeft;
    private bool _touchpadClickedLeft;
    private float _gripsDownTimeLeft;
    private bool _gripsClickedLeft;
    private bool _touchpadTouchedLeft;

    private bool _clickedRight;
    private float _leftButtonDownTimeRight;
    private float _leftDoubleClickTimeRight;
    private float _touchpadDownTimeRight;
    private bool _touchpadClickedRight;
    private float _gripsDownTimeRight;
    private bool _gripsClickedRight;
    private bool _touchpadTouchedRight;

    private void UpdateButtons()
    {
        UpdateInput(_leftTracker, ref _clickedLeft, ref _leftButtonDownTimeLeft, ref _leftDoubleClickTimeLeft, ref _touchpadClickedLeft, ref _touchpadDownTimeLeft, ref _gripsClickedLeft, ref _gripsDownTimeLeft, ref _touchpadTouchedLeft, ETrackedControllerRole.LeftHand);
        UpdateInput(_rightTracker, ref _clickedRight, ref _leftButtonDownTimeRight, ref _leftDoubleClickTimeRight, ref _touchpadClickedRight, ref _touchpadDownTimeRight, ref _gripsClickedRight, ref _gripsDownTimeRight, ref _touchpadTouchedRight, ETrackedControllerRole.RightHand);
    }

    private bool GetPress(VRControllerState_t state, EVRButtonId buttonId) { return (state.ulButtonPressed & (1ul << (int)buttonId)) != 0; }

    private void UpdateInput(HOTK_TrackedDevice device, ref bool clicked, ref float clickTime, ref float doubleClickTime, ref bool touchpadClicked, ref float touchpadTime, ref bool grips, ref float gripsTime, ref bool touchpadTouch, ETrackedControllerRole role)
    {
        if (device == null || !device.IsValid) return;
        var svr = SteamVR.instance;
        if (svr == null) return;
        var c = new VRControllerState_t();
        svr.hmd.GetControllerState((uint)device.Index, ref c);
        // c.rAxis0 is Trackpad
        // c.rAxis1 is Trigger

        // Trigger check
        if (c.rAxis1.x >= 0.99f)
        {
            if (!clicked)
            {
                clicked = true;
                clickTime = Time.time;
                if (OnControllerTriggerDown != null) OnControllerTriggerDown(device);
            }
        }
        else
        {
            if (clicked)
            {
                clicked = false;
                if (OnControllerTriggerUp != null) OnControllerTriggerUp(device);
                
                if ((Time.time - doubleClickTime) < 0.25f)
                {
                    if (OnControllerTriggerDoubleClicked != null) OnControllerTriggerDoubleClicked(device);
                    return;
                }
                doubleClickTime = Time.time;

                if (!((Time.time - clickTime) < 0.25f)) return;
                if (OnControllerTriggerClicked != null) OnControllerTriggerClicked(device);
            }
        }

        // Touchpad Check
        if (GetPress(c, EVRButtonId.k_EButton_SteamVR_Touchpad))
        {
            if (!touchpadClicked)
            {
                touchpadClicked = true;
                touchpadTime = Time.time;
                if (OnControllerTouchpadDown != null) OnControllerTouchpadDown(device);
            }
        }
        else
        {
            if (touchpadClicked)
            {
                touchpadClicked = false;
                if (OnControllerTouchpadUp != null) OnControllerTouchpadUp(device);
                if (!((Time.time - touchpadTime) < 0.25f)) return;
                if (OnControllerTouchpadClicked != null) OnControllerTouchpadClicked(device);
            }
        }

        // Grips Check
        if (GetPress(c, EVRButtonId.k_EButton_Grip))
        {
            if (!grips)
            {
                grips = true;
                gripsTime = Time.time;
                if (OnControllerGripsDown != null) OnControllerGripsDown(device);
            }
        }
        else
        {
            if (grips)
            {
                grips = false;
                if (OnControllerGripsUp != null) OnControllerGripsUp(device);
                if (!((Time.time - gripsTime) < 0.25f)) return;
                if (OnControllerGripsClicked != null) OnControllerGripsClicked(device);
            }
        }

        if (c.rAxis0.x != 0f && c.rAxis0.y != 0f)
        {
            if (!touchpadTouch)
            {
                touchpadTouch = true;
                if (OnControllerTouchpadTouchStart != null) OnControllerTouchpadTouchStart(device, c.rAxis0.x, c.rAxis0.y);
            }else if (OnControllerTouchpadTouchMove != null) OnControllerTouchpadTouchMove(device, c.rAxis0.x, c.rAxis0.y);
        }
        else
        {
            if (touchpadTouch)
            {
                touchpadTouch = false;
                if (OnControllerTouchpadTouchEnd != null) OnControllerTouchpadTouchEnd(device);
            }
        }
    }
    
    /// <summary>
    /// Attempt to find the HMD.
    /// </summary>
    public void FindHMD()
    {
        var system = OpenVR.System;
        if (system == null)
        {
            LogError("OpenVR System not found.");
            return;
        }

        if (_hmdIndex != OpenVR.k_unTrackedDeviceIndexInvalid &&
            system.GetTrackedDeviceClass(_hmdIndex) == ETrackedDeviceClass.HMD)
        {
            // Assume we as still connected to the HMD..
            return;
        }

        for (uint i = 0; i < OpenVR.k_unMaxTrackedDeviceCount; i++)
        {
            if (system.GetTrackedDeviceClass(i) != ETrackedDeviceClass.HMD) continue;
            _hmdIndex = i;
            break;
        }
        if (_hmdIndex != OpenVR.k_unTrackedDeviceIndexInvalid)
        {
            Log("Found HMD ( Device: {0} )", _hmdIndex);
        }
    }

    public void ResetControllerFindAttemptCount()
    {
        _noControllersCount = 0;
    }

    public void CheckForControllers()
    {
        if (_noControllersCount >= 10)
        {
            _noControllersCount -= 1;
        }
    }

    private bool _couldntFindControllers;
    private uint _noControllersCount;
    private bool _alreadyFoundLeft;
    private bool _alreadyFoundRight;

    /// <summary>
    /// Attempt to find both controllers.
    /// </summary>
    public void FindControllers()
    {
        var system = OpenVR.System;
        if (system == null)
        {
            LogError("OpenVR System not found.");
            return;
        }
        FindTracker(ref _leftTracker, HOTK_TrackedDevice.EType.LeftController);
        FindTracker(ref _rightTracker, HOTK_TrackedDevice.EType.RightController);
        if (_noControllersCount >= 10)
        {
            return;
        }

        if (_leftIndex != OpenVR.k_unTrackedDeviceIndexInvalid && system.GetTrackedDeviceClass(_leftIndex) == ETrackedDeviceClass.Controller &&
            _rightIndex != OpenVR.k_unTrackedDeviceIndexInvalid && system.GetTrackedDeviceClass(_rightIndex) == ETrackedDeviceClass.Controller)
        {
            // Assume we are still connected to the controllers..
            _noControllersCount = 10;
            return;
        }

        if (_noControllersCount == 0) Log("Searching for Controllers..");

        // Check if either controller has already been found
        var l = system.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.LeftHand);
        var r = system.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.RightHand);
        if (_leftIndex == OpenVR.k_unTrackedDeviceIndexInvalid && _rightIndex == OpenVR.k_unTrackedDeviceIndexInvalid)
        {
            DoIndexAssignmentBothControllers(l, r);
            CallControllersUpdated();
        }
        else
        {
            // If either controller hasn't been assigned and it can be, assign it now
            if (_leftIndex == OpenVR.k_unTrackedDeviceIndexInvalid && l != OpenVR.k_unTrackedDeviceIndexInvalid)
            {
                DoIndexAssignment(ETrackedControllerRole.LeftHand, ref _leftIndex, l);
            }
            else if (_rightIndex == OpenVR.k_unTrackedDeviceIndexInvalid && r != OpenVR.k_unTrackedDeviceIndexInvalid)
            {
                DoIndexAssignment(ETrackedControllerRole.RightHand, ref _rightIndex, r);
            }

            // If both controllers are now found, trigger the action that occurs when both controllers update
            if (_leftIndex != OpenVR.k_unTrackedDeviceIndexInvalid && _rightIndex != OpenVR.k_unTrackedDeviceIndexInvalid)
                CallControllersUpdated();
        }

        // Track down the remaining controller
        if (_leftIndex != OpenVR.k_unTrackedDeviceIndexInvalid && _rightIndex != OpenVR.k_unTrackedDeviceIndexInvalid) // Both controllers are assigned!
        {
            ReportControllerFound(ETrackedControllerRole.RightHand, _rightIndex);
            ReportControllerFound(ETrackedControllerRole.LeftHand, _leftIndex);
        }
        else if (_leftIndex != OpenVR.k_unTrackedDeviceIndexInvalid && _rightIndex == OpenVR.k_unTrackedDeviceIndexInvalid) // Left controller is assigned but right is missing
        {
            ReportControllerFound(ETrackedControllerRole.LeftHand, _leftIndex);
            for (uint i = 0; i < OpenVR.k_unMaxTrackedDeviceCount; i++)
            {
                if (i == _leftIndex || system.GetTrackedDeviceClass(i) != ETrackedDeviceClass.Controller)
                {
                    continue;
                }
                DoIndexAssignment(ETrackedControllerRole.RightHand, ref _rightIndex, i);
                ReportControllerFound(ETrackedControllerRole.RightHand, _rightIndex);
                break;
            }
            CallControllersUpdated();
        }
        else if (_leftIndex == OpenVR.k_unTrackedDeviceIndexInvalid && _rightIndex != OpenVR.k_unTrackedDeviceIndexInvalid) // Right controller is assigned but left is missing
        {
            ReportControllerFound(ETrackedControllerRole.RightHand, _rightIndex);
            for (uint i = 0; i < OpenVR.k_unMaxTrackedDeviceCount; i++)
            {
                if (i == _rightIndex || system.GetTrackedDeviceClass(i) != ETrackedDeviceClass.Controller)
                {
                    continue;
                }
                DoIndexAssignment(ETrackedControllerRole.LeftHand, ref _leftIndex, i);
                ReportControllerFound(ETrackedControllerRole.LeftHand, _leftIndex);
                break;
            }
            CallControllersUpdated();
        }
        else if (_leftIndex == OpenVR.k_unTrackedDeviceIndexInvalid && _rightIndex == OpenVR.k_unTrackedDeviceIndexInvalid) // Both controllers are unassigned
        {
            if (_noControllersCount == 0) LogWarning("SteamVR Reports No Assigned Controllers..! Searching..");
            var foundUnassigned = 0;
            var slots = new uint[2];
            // Sort through all the devices until we find two controllers
            for (uint i = 0; i < OpenVR.k_unMaxTrackedDeviceCount; i++)
            {
                if (system.GetTrackedDeviceClass(i) != ETrackedDeviceClass.Controller)
                {
                    continue; // This device isn't a controller, skip it
                }
                switch (system.GetControllerRoleForTrackedDeviceIndex(i))
                {
                    case ETrackedControllerRole.LeftHand:
                        ReportControllerFound(ETrackedControllerRole.LeftHand, _leftIndex);
                        DoIndexAssignment(ETrackedControllerRole.LeftHand, ref _leftIndex, i);
                        break;
                    case ETrackedControllerRole.RightHand:
                        ReportControllerFound(ETrackedControllerRole.RightHand, _rightIndex);
                        DoIndexAssignment(ETrackedControllerRole.RightHand, ref _rightIndex, i);
                        break;
                    case ETrackedControllerRole.Invalid:
                        ReportControllerFound(ETrackedControllerRole.Invalid, i);
                        if (foundUnassigned <= 1)
                            slots[foundUnassigned++] = i;
                        break;
                }

                if (foundUnassigned == 2)
                {
                    break; // We have two controllers, stop searching
                }
            }
            switch (foundUnassigned)
            {
                case 2:
                    LogWarning("Found Two Unassigned Controllers! Randomly Assigning!");
                    DoIndexAssignmentBothControllers(slots[1], slots[0]);
                    break;
                case 1:
                    if (_leftIndex == OpenVR.k_unTrackedDeviceIndexInvalid &&
                       _rightIndex != OpenVR.k_unTrackedDeviceIndexInvalid)
                    {
                        if (slots[0] != _leftIndex)
                        {
                            LogWarning("Only Found One Unassigned Controller, and Right was already assigned! Assigning To Left!");
                            DoIndexAssignment(ETrackedControllerRole.LeftHand, ref _leftIndex, slots[0]);
                            _alreadyFoundLeft = true;
                        }
                        _noControllersCount = 10;
                    }
                    else
                    {
                        if (slots[0] != _rightIndex)
                        {
                            LogWarning("Only Found One Unassigned Controller! Assigning To Right!");
                            DoIndexAssignment(ETrackedControllerRole.RightHand, ref _rightIndex, slots[0]);
                            _alreadyFoundRight = true;
                        }
                        _noControllersCount = 10;
                    }
                    break;
                case 0:
                    if (_noControllersCount == 0) LogWarning("Couldn't Find Any Unassigned Controllers!");
                    _noControllersCount++;
                    if (!_couldntFindControllers && _noControllersCount >= 10)
                    {
                        _couldntFindControllers = true;
                        LogError("Controllers not found!");
                        LogError("Please connect the controllers and restart!");
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            CallControllersUpdated();
        }
    }

    private void ReportControllerFound(ETrackedControllerRole role, uint index)
    {
        switch (role)
        {
            case ETrackedControllerRole.LeftHand:
                if (!_alreadyFoundLeft) Log("Found Controller ( Device: {0} ): Left", index);
                _alreadyFoundLeft = true;
                break;
            case ETrackedControllerRole.RightHand:
                if (!_alreadyFoundRight) Log("Found Controller ( Device: {0} ): Right", index);
                _alreadyFoundRight = true;
                break;
            case ETrackedControllerRole.Invalid:
                Log("Found Controller ( Device: {0} ): Unassigned", index);
                break;
        }
    }

    private void DoIndexAssignment(ETrackedControllerRole role, ref uint index, uint newIndex)
    {
        if (index == newIndex) return;
        index = newIndex;
        if (OnControllerIndexChanged != null)
            OnControllerIndexChanged(role, index);
    }

    private void DoIndexAssignmentBothControllers(uint leftNew, uint rightNew)
    {
        bool leftChanged;
        bool rightChanged;

        if (_leftIndex != leftNew)
        {
            _leftIndex = leftNew;
            leftChanged = true;
        }else leftChanged = false;
        if (_rightIndex != rightNew)
        {
            _rightIndex = rightNew;
            rightChanged = true;
        }else rightChanged = false;

        if (OnControllerIndexChanged == null) return;
        if (leftChanged) OnControllerIndexChanged(ETrackedControllerRole.LeftHand, _leftIndex);
        if (rightChanged) OnControllerIndexChanged(ETrackedControllerRole.RightHand, _rightIndex);
    }

    private void CallControllersUpdated()
    {
        if (OnControllerIndicesUpdated != null)
            OnControllerIndicesUpdated();
    }

    public void SwapControllers()
    {
        DoIndexAssignmentBothControllers(_rightIndex, _leftIndex);
        CallControllersUpdated();
    }

    /// <summary>
    /// This is just used to quickly enable/disable Log messages.
    /// </summary>
    /// <param name="text"></param>
    void Log(string text, params object[] vars)
    {
        Debug.Log(vars == null ? text : string.Format(text, vars));
    }

    /// <summary>
    /// This is just used to quickly enable/disable LogWarning messages.
    /// </summary>
    /// <param name="text"></param>
    void LogWarning(string text, params object[] vars)
    {
        Debug.LogWarning(vars == null ? text : string.Format(text, vars));
    }

    /// <summary>
    /// This is just used to quickly enable/disable LogError messages.
    /// </summary>
    /// <param name="text"></param>
    void LogError(string text, params object[] vars)
    {
        Debug.LogError(vars == null ? text : string.Format(text, vars));
    }

    /// <summary>
    /// Compute a given Ray and determine if it hit an Overlay
    /// </summary>
    /// <param name="source"></param>
    /// <param name="direction"></param>
    /// <param name="results"></param>
    /// <returns></returns>
    private bool ComputeIntersection(HOTK_OverlayBase hotkOverlay, Vector3 source, Vector3 direction, ref SteamVR_Overlay.IntersectionResults results)
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
        if (!overlay.ComputeOverlayIntersection(hotkOverlay.Handle, ref input, ref output)) return false;

        results.point = new Vector3(output.vPoint.v0, output.vPoint.v1, -output.vPoint.v2);
        results.normal = new Vector3(output.vNormal.v0, output.vNormal.v1, -output.vNormal.v2);
        results.UVs = new Vector2(output.vUVs.v0, output.vUVs.v1);
        results.distance = output.fDistance;
        return true;
    }

    private void UpdateGaze()
    {
        FindTracker(ref _hmdTracker, HOTK_TrackedDevice.EType.HMD);
        HOTK_Overlay hitOverlay = null;
        HOTK_OverlayBase hitOverlayBase = null;
        SteamVR_Overlay.IntersectionResults? hitResult = null;

        // Test Overlays
        foreach (var overlay in _gazeableOverlays)
        {
            if (overlay.AnimateOnGaze == HOTK_Overlay.AnimationType.None) continue;
            if (overlay.GazeLocked || _hmdTracker == null || !_hmdTracker.IsValid) continue;
            if (!(Vector3.Angle(_hmdTracker.transform.forward, overlay.RotationTracker.transform.forward) <= 90f))
                continue;
            var result = new SteamVR_Overlay.IntersectionResults();
            var hit = ComputeIntersection(overlay, _hmdTracker.gameObject.transform.position, _hmdTracker.gameObject.transform.forward, ref result);
            if (!hit || (hitResult != null && !(result.distance < hitResult.Value.distance))) continue;
            hitOverlay = overlay;
            hitResult = result;
        }
        // Test Companions
        foreach (var overlay in _gazeableCompanionOverlays)
        {
            if (_hmdTracker == null || !_hmdTracker.IsValid) continue;
            if (!(Vector3.Angle(_hmdTracker.transform.forward, overlay.RotationTracker.transform.forward) <= 90f))
                continue;
            var result = new SteamVR_Overlay.IntersectionResults();
            var hit = ComputeIntersection(overlay, _hmdTracker.gameObject.transform.position, _hmdTracker.gameObject.transform.forward, ref result);
            if (!hit || (hitResult != null && !(result.distance < hitResult.Value.distance))) continue;
            hitOverlay = null;
            hitOverlayBase = overlay;
            hitResult = result;
        }

        if (hitOverlay != null)
        {
            foreach (var overlay in _gazeableOverlays)
            {
                overlay.UpdateGaze((overlay.GazeLocked && overlay.GazeLockedOn) || (!overlay.GazeLocked && overlay == hitOverlay));
            }
            foreach (var companion in _gazeableCompanionOverlays)
                companion.UpdateGaze(false);
        }
        else if (hitOverlayBase != null)
        {
            foreach (var overlay in _gazeableOverlays)
                overlay.UpdateGaze(overlay.GazeLocked && overlay.GazeLockedOn);
            foreach (var companion in _gazeableCompanionOverlays.Where(o => o != hitOverlayBase))
                companion.UpdateGaze(false);
            hitOverlayBase.UpdateGaze(true);
        }
        else
        {
            foreach (var overlay in _gazeableOverlays)
                overlay.UpdateGaze(overlay.GazeLocked && overlay.GazeLockedOn);
            foreach (var companion in _gazeableCompanionOverlays)
                companion.UpdateGaze(false);
        }
    }

    private HOTK_OverlayBase lastAimed;
    private HOTK_OverlayBase lastTouched;
    private void UpdateAim()
    {
        HOTK_OverlayBase hitBase = null;
        HOTK_TrackedDevice hitTracker = null;
        SteamVR_Overlay.IntersectionResults? hitResults = null;

        // Check if we are touching an overlay
        foreach (var overlay in _interactableOverlays.Where(overlay => overlay.OnControllerTouchesOverlay != null))
        {
            TestControllerTouchesOverlay(overlay, ref _leftTracker, HOTK_TrackedDevice.EType.LeftController, ref hitBase, ref hitTracker, ref hitResults);
            TestControllerTouchesOverlay(overlay, ref _rightTracker, HOTK_TrackedDevice.EType.RightController, ref hitBase, ref hitTracker, ref hitResults);
        }
        if (hitBase == null && lastTouched != null) StopTouching();
        if (hitBase != null && lastTouched != hitBase) StopTouching();
        if (hitBase != null && hitResults != null)
        {
            StopAiming();
            hitBase.OnControllerTouchesOverlay(hitBase, hitTracker, hitResults.Value);
            hitBase.TouchingTracker = hitTracker;
            lastTouched = hitBase;
            return;
        }

        // Check if we are aiming at an overlay
        foreach (var overlay in _interactableOverlays.Where(overlay => overlay.OnControllerHitsOverlay != null))
        {
            TestControllerAimsAtOverlay(overlay, ref _leftTracker, HOTK_TrackedDevice.EType.LeftController, ref hitBase, ref hitTracker, ref hitResults);
            TestControllerAimsAtOverlay(overlay, ref _rightTracker, HOTK_TrackedDevice.EType.RightController, ref hitBase, ref hitTracker, ref hitResults);
        }
        if (hitBase == null && lastAimed != null) StopAiming();
        if (hitBase != null && lastAimed != hitBase) StopAiming();
        if (hitBase != null && hitResults != null)
        {
            hitBase.OnControllerHitsOverlay(hitBase, hitTracker, hitResults.Value);
            hitBase.HittingTracker = hitTracker;
            lastAimed = hitBase;
        }
    }

    private void StopTouching()
    {
        if (lastTouched == null) return;
        if (lastTouched.TouchingTracker != null && lastTouched.OnControllerStopsTouchingOverlay != null) lastTouched.OnControllerStopsTouchingOverlay(lastTouched, lastTouched.TouchingTracker);
        lastTouched.TouchingTracker = null;
        lastTouched = null;
    }

    private void StopAiming()
    {
        if (lastAimed == null) return;
        if (lastAimed.HittingTracker != null && lastAimed.OnControllerUnhitsOverlay != null) lastAimed.OnControllerUnhitsOverlay(lastAimed, lastAimed.HittingTracker);
        lastAimed.HittingTracker = null;
        lastAimed = null;
    }

    private void TestControllerAimsAtOverlay(HOTK_OverlayBase overlay, ref HOTK_TrackedDevice tracker, HOTK_TrackedDevice.EType role, ref HOTK_OverlayBase target, ref HOTK_TrackedDevice hitTracker, ref SteamVR_Overlay.IntersectionResults? results)
    {
        FindTracker(ref tracker, role);
        if (tracker == null || !tracker.IsValid) return;
        if (overlay.HittingTracker != null && overlay.HittingTracker != tracker) return;
        var result = new SteamVR_Overlay.IntersectionResults();
        var hit = !(Vector3.Angle(tracker.transform.forward, overlay.RotationTracker.transform.forward) > 90f) && ComputeIntersection(overlay, tracker.gameObject.transform.position, tracker.gameObject.transform.forward, ref result);
        if (!hit || (results != null && (result.distance > results.Value.distance))) return;
        target = overlay;
        hitTracker = tracker;
        results = result;
    }

    private void TestControllerTouchesOverlay(HOTK_OverlayBase overlay, ref HOTK_TrackedDevice tracker, HOTK_TrackedDevice.EType role, ref HOTK_OverlayBase target, ref HOTK_TrackedDevice hitTracker, ref SteamVR_Overlay.IntersectionResults? results)
    {
        FindTracker(ref tracker, role);
        if (tracker == null || !tracker.IsValid) return;
        if (overlay.TouchingTracker != null && overlay.TouchingTracker != tracker) return;
        var result = new SteamVR_Overlay.IntersectionResults();
        var hit = !(Vector3.Angle(tracker.transform.forward, overlay.RotationTracker.transform.forward) > 90f) && ComputeIntersection(overlay, tracker.gameObject.transform.position - (tracker.gameObject.transform.forward * 0.1f), tracker.gameObject.transform.forward, ref result);
        if (!hit || result.distance >= 0.15f || (results != null && !(result.distance < results.Value.distance))) return;
        target = overlay;
        hitTracker = tracker;
        results = result;
    }

    public void TriggerHapticPulse(HOTK_TrackedDevice dev, ushort duration)
    {
        if (dev.Index == HOTK_TrackedDevice.EIndex.None) return;
        var system = OpenVR.System;
        system.TriggerHapticPulse((uint)dev.Index, 0, (char)duration);
    }
}
