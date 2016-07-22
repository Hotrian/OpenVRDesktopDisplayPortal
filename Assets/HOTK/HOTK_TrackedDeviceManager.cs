using System;
using UnityEngine;
using Valve.VR;

public class HOTK_TrackedDeviceManager : MonoBehaviour
{
    public static Action<ETrackedControllerRole, uint> OnControllerIndexChanged; // Called any time a specific controller changes index
    public static Action OnControllerIndicesUpdated; // Called only when both controllers have been checked/assigned or are swapped

    public static HOTK_TrackedDeviceManager Instance
    {
        get { return _instance ?? (_instance = new GameObject("HOTK_TrackedDeviceManager", typeof(HOTK_TrackedDeviceManager)) {hideFlags = HideFlags.HideInHierarchy}.GetComponent<HOTK_TrackedDeviceManager>()); }
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

    private static HOTK_TrackedDeviceManager _instance;
    private bool AlertsEnabled = true;

    private uint _leftIndex = OpenVR.k_unTrackedDeviceIndexInvalid;
    private uint _rightIndex = OpenVR.k_unTrackedDeviceIndexInvalid;
    private uint _hmdIndex = OpenVR.k_unTrackedDeviceIndexInvalid;

    public void Start()
    {
        InvokeRepeating("CheckForControllers", 10f, 10f);
    }

    public void Update()
    {
        FindHMD();
        FindControllers();
        UpdatePoses();
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

    private void CheckForControllers()
    {
        if (_noControllersCount >= 10)
        {
            _noControllersCount -= 1;
        }
    }

    private bool _couldntFindControllers;
    private uint _noControllersCount;

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
        DoIndexAssignmentBothControllers(system.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.LeftHand), system.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.RightHand));
        CallControllersUpdated();

        if (_leftIndex != OpenVR.k_unTrackedDeviceIndexInvalid && _rightIndex != OpenVR.k_unTrackedDeviceIndexInvalid) // Both controllers are assigned!
        {
            Log("Found Controller ( Device: {0} ): Right", _rightIndex);
            Log("Found Controller ( Device: {0} ): Left", _leftIndex);
        }
        else if (_leftIndex != OpenVR.k_unTrackedDeviceIndexInvalid && _rightIndex == OpenVR.k_unTrackedDeviceIndexInvalid) // Left controller is assigned but right is missing
        {
            Log("Found Controller ( Device: {0} ): Left", _leftIndex);
            for (uint i = 0; i < OpenVR.k_unMaxTrackedDeviceCount; i++)
            {
                if (i == _leftIndex || system.GetTrackedDeviceClass(i) != ETrackedDeviceClass.Controller)
                {
                    continue;
                }
                DoIndexAssignment(ETrackedControllerRole.RightHand, ref _rightIndex, i);
                Log("Found Controller ( Device: {0} ): Right", _rightIndex);
                break;
            }
            CallControllersUpdated();
        }
        else if (_leftIndex == OpenVR.k_unTrackedDeviceIndexInvalid && _rightIndex != OpenVR.k_unTrackedDeviceIndexInvalid) // Right controller is assigned but left is missing
        {
            Log("Found Controller ( Device: {0} ): Right", _rightIndex);
            for (uint i = 0; i < OpenVR.k_unMaxTrackedDeviceCount; i++)
            {
                if (i == _rightIndex || system.GetTrackedDeviceClass(i) != ETrackedDeviceClass.Controller)
                {
                    continue;
                }
                DoIndexAssignment(ETrackedControllerRole.LeftHand, ref _leftIndex, i);
                Log("Found Controller ( Device: {0} ): Left", _leftIndex);
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
                        Log("Found Controller ( Device: {0} ): Left", _leftIndex);
                        DoIndexAssignment(ETrackedControllerRole.LeftHand, ref _leftIndex, i);
                        break;
                    case ETrackedControllerRole.RightHand:
                        Log("Found Controller ( Device: {0} ): Right", _rightIndex);
                        DoIndexAssignment(ETrackedControllerRole.RightHand, ref _rightIndex, i);
                        break;
                    case ETrackedControllerRole.Invalid:
                        Log("Found Controller ( Device: {0} ): Unassigned", i);
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
                        LogWarning("Only Found One Unassigned Controller, and Right was already assigned! Assigning To Left!");
                        DoIndexAssignment(ETrackedControllerRole.LeftHand, ref _leftIndex, slots[0]);
                        _noControllersCount = 10;
                    }
                    else
                    {
                        LogWarning("Only Found One Unassigned Controller! Assigning To Right!");
                        DoIndexAssignment(ETrackedControllerRole.RightHand, ref _rightIndex, slots[0]);
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
}
