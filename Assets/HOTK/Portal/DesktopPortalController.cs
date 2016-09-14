using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using ScreenCapture;
using UnityEngine.UI;
using Valve.VR;
using Color = UnityEngine.Color;
using Debug = UnityEngine.Debug;
using Image = UnityEngine.UI.Image;

// ReSharper disable once CheckNamespace
public class DesktopPortalController : MonoBehaviour
{
    #region Unity Variables
    public Camera RenderCamera; // A Render Camera used to capture the texture drawn to the overlay
    public Texture DefaultTexture; // Texture drawn to the overlay when there is nothing else to draw
    public DropdownMatchEnumOptions CaptureModeDropdown; // A Dropdown used to select the current Capture Mode
    public DropdownMatchEnumOptions FramerateModeDropdown; // A Dropdown used to select the current Capture Framerate
    public DropdownMatchEnumOptions FilterModeDropdown;

    // Used to control resolution lock
    public Image SizeLockSprite;
    public Sprite LockSprite;
    public Sprite UnlockSprite;
    public Image AspectLinkedSprite;
    public Sprite AspectLockSprite;
    public Sprite AspectUnlockSprite;

    public Material OutlineMaterial; // Material used for the outline

    // Input Fields used to control cropping and window size
    public InputField OffsetLeftField;
    public InputField OffsetTopField;
    public InputField OffsetRightField;
    public InputField OffsetBottomField;
    public InputField OffsetWidthField;
    public InputField OffsetHeightField;

    public HOTK_Overlay Overlay; // The Overlay we are manipulating
    public Material DisplayMaterial; // The Material we are drawing to
    public Dropdown ApplicationDropdown; // A Dropdown used to select the Target Application
    public GameObject DisplayQuad; // A Quad used to show the output in the Desktop Window, which is captured by a RenderCamera

    // Used to map Position Sliders
    public OffsetMatchInputField OffsetX;
    public OffsetMatchInputField OffsetY;
    public OffsetMatchInputField OffsetZ;

    // Used to map Rotation Sliders
    public RotationMatchSlider OffsetRx;
    public RotationMatchSlider OffsetRy;
    public RotationMatchSlider OffsetRz;

    public GameObject CursorGameObject; // A GameObject which displays our Cursor Sprite

    public Text FpsCounter; // A Text that shows the current FPS
    public Text ResolutionDisplay; // A Text that shows the current Resolution
    public ScaleMatchInputField ScaleField;
    public ScaleMatchInputField Scale2Field;

    public InputField ZInputField;

    public OffsetMatchSlider XSlider;
    public OffsetMatchSlider YSlider;
    public OffsetMatchSlider ZSlider;

    public HOTK_CompanionOverlay Backside;
    public GameObject BacksideDisplayQuad; // A Quad used to show the Backside, which is captured by a RenderCamera
    public Material BacksideDisplayMaterial; // The Material we are drawing to
    public Texture[] BacksideTextures;
    public AutoResizeCameraForRenderTexture BacksideScript;

    public HOTK_CompanionOverlay DodgeGazeDetector;

    public DropdownMatchEnumOptions ClickAPIDropdown;
    public Toggle WindowOnTopToggle;
    public Toggle MoveDesktopCursorToggle;
    public Toggle ScrollEnabledToggle;
    public Toggle ScrollRequiresGripsToggle;
    public Toggle ShowDesktopCursorToggle;
    public Toggle DesktopCursorWindowOnTopToggle;
    public Toggle DesktopCursorAutoHideToggle;
    public DropdownMatchEnumOptions BacksideDropdown;
    public Toggle GrabEnabledToggle;
    public Toggle ScaleEnabledToggle;
    public Toggle HapticsEnabledToggle;
    #endregion

    #region Public Variables
    public static DesktopPortalController Instance
    {
        get { return _instance ?? (_instance = FindObjectOfType<DesktopPortalController>()); }
    }
    public RenderTexture RenderTexture // A Render Texture used to copy the DisplayQuad into VR with an Outline
    {
        get { return _renderTexture ?? (_renderTexture = NewRenderTexture()); }
    }
    public SpriteRenderer CursorRenderer // Cache and return the SpriteRenderer for the Cursor if we can
    {
        get
        {
            return _cursorRenderer ??
                   (_cursorRenderer =
                       (CursorGameObject == null ? null : CursorGameObject.GetComponent<SpriteRenderer>()));
        }
    }
    public GameObject OverlayOffsetTracker // Cache and return a GameObject used to track the relative position of the Overlay when grabbing
    {
        get { return _overlayOffsetTracker ?? (_overlayOffsetTracker = new GameObject("Overlay Offset Tracker") { hideFlags = HideFlags.HideInHierarchy }); }
    }
    [HideInInspector]
    public bool ScreenOffsetPerformed;
    [HideInInspector]
    public string SelectedWindowTitle;

    public Color OutlineColorDefault
    {
        get { return _outlineColorDefault; }
        set
        {
            _outlineColorDefault = value;
            if (_currentMode == OutlineColor.Default)
            {
                StopCoroutine("GoToDefaultColor");
                StartCoroutine("GoToDefaultColor");
            }
        }
    }
    public Color OutlineColorAiming
    {
        get { return _outlineColorAiming; }
        set
        {
            _outlineColorAiming = value;
            if (_currentMode == OutlineColor.Aiming)
            {
                StopCoroutine("GoToAimingColor");
                StartCoroutine("GoToAimingColor");
            }
        }
    }
    public Color OutlineColorTouching
    {
        get { return _outlineColorTouching; }
        set
        {
            _outlineColorTouching = value;
            if (_currentMode == OutlineColor.Touching)
            {
                StopCoroutine("GoToTouchingColor");
                StartCoroutine("GoToTouchingColor");
            }
        }
    }
    public Color OutlineColorScaling
    {
        get { return _outlineColorScaling; }
        set
        {
            _outlineColorScaling = value;
            if (_currentMode == OutlineColor.Scaling)
            {
                StopCoroutine("GoToScalingColor");
                StartCoroutine("GoToScalingColor");
            }
        }
    }

    public BacksideTexture CurrentBacksideTexture
    {
        get { return _currentBacksideTexture; }
        set
        {
            _currentBacksideTexture = value;
            SetBacksideTexture(_currentBacksideTexture);
        }
    }

    public WindowSettings SelectedWindowSettings { get; set; } // The WindowSettings of the current Target Application

    #endregion

    #region Private Variables
    // Getter / Setter vars
    private static DesktopPortalController _instance;
    private RenderTexture _renderTexture;
    private int _renderTextureMarginWidth;
    private int _renderTextureMarginHeight;
    private SpriteRenderer _cursorRenderer;
    private GameObject _overlayOffsetTracker;

    // Internal vars
    private readonly Dictionary<string, IntPtr> _windows = new Dictionary<string, IntPtr>();
    private readonly List<string> _titles = new List<string>();
    private IntPtr _selectedWindow = IntPtr.Zero;
    private string _selectedWindowFullPath = string.Empty;
    private Bitmap _bitmap;
    private Texture2D _texture;
    private MemoryStream _stream;
    private bool _showFps;
    private readonly Stopwatch _fpsTimer = new Stopwatch();
    private int _currentWindowWidth;
    private int _currentWindowHeight;
    // ReSharper disable NotAccessedField.Local
    private int _currentCaptureWidth;
    private int _currentCaptureHeight;
    // ReSharper restore NotAccessedField.Local
    private bool _subscribed;
    private HOTK_TrackedDevice _aimingAtOverlay;
    private HOTK_TrackedDevice _touchingOverlay;
    private HOTK_TrackedDevice _grabbingOverlay;
    private Transform _lastOverlayParent;
    private Vector3 _grabbingOffset;
    private bool _didHitOverlay;
    private bool _isHittingOverlay;

    private bool _touchingTouchpadGripsDown;
    private bool _touchingTouchpad;
    private float _touchingTouchpadX;
    private float _touchingTouchpadY;
    private bool _touchingTouchpadAxisIsVertical;

    private const float TouchingTouchpadRange = 0.5f;
    private const float TouchingTouchpadRange2 = 0.25f;
    private const float TouchingTouchpadTickDistance = 0.1f;
    private const ushort TouchingTouchpadHapticStrength = 500;

    private int _localWindowPosX;
    private int _localWindowPosY;
    private int _lastWindowPosX;
    private int _lastWindowPosY;
    private int _fpsCount;
    private bool _reselecting;
    private CaptureScreen.SIZE _size;
#pragma warning disable 0414
    private Win32Stuff.WINDOWINFO _info;
#pragma warning restore 0414
    private bool _wasDirect;

    private HOTK_TrackedDevice _scalingOverlay;
    private float _scalingBaseScale;
    private float _scalingBaseDistance;
    private bool _scalingScale2;

    private Color _outlineColorDefault = new Color(0f, 0f, 0f, 0f);
    private Color _outlineColorAiming = Color.red;
    private Color _outlineColorTouching = Color.green;
    private Color _outlineColorScaling = Color.blue;
    private OutlineColor _currentMode = OutlineColor.Default;

    private BacksideTexture _currentBacksideTexture = BacksideTexture.Blue;

    // Cache these fractions so they aren't constantly recalculated
    private static readonly float[] FramerateFractions = {
        1f, 1f/2f, 1f/5f, 1f/10f, 1/15f, 1/24f, 1f/30f, 1f/60f, 1f/90f, 1f/120f
    };

    #endregion

    #region Unity Methods
    public void OnEnable()
    {
        var svr = SteamVR.instance; // Init the SteamVR drivers
        Debug.Log("Connected to: " + svr.hmd_TrackingSystemName);
        if (Overlay == null) return;
        SaveLoad.Load();
        Overlay.OverlayTexture = RenderTexture;
        _bitmap = CaptureScreen.CaptureDesktop();
        _texture = new Texture2D(_bitmap.Width, _bitmap.Height) { filterMode = SelectedWindowSettings != null ? SelectedWindowSettings.filterMode : FilterMode.Point };

        _stream = new MemoryStream();
        _bitmap.Save(_stream, ImageFormat.Png);
        _stream.Seek(0, SeekOrigin.Begin);

        DisplayQuad.GetComponent<Renderer>().material.mainTexture = _texture;
        DisplayQuad.transform.localScale = new Vector3(_bitmap.Width, _bitmap.Height, 1f);

        OutlineMaterial.color = new Color(OutlineMaterial.color.r, OutlineMaterial.color.g, OutlineMaterial.color.b, 0f); // Hide Outline on start

        _texture.LoadImage(_stream.ToArray());

        RefreshWindowList();

        StartCoroutine("UpdateEvery1Second");
        StartCoroutine("UpdateEvery10Seconds");

        if (_selectedWindow != IntPtr.Zero)
        {
            _currentWindowWidth = 0; // Tricks the system into recalculating the size of the Overlay before capturing.
            StartCoroutine("CaptureWindow");
        }
        if (_subscribed) return;
        _subscribed = true;
        HOTK_TrackedDeviceManager.OnControllerTriggerClicked += SingleClickApplication;
        HOTK_TrackedDeviceManager.OnControllerTriggerDoubleClicked += DoubleClickApplication;
        HOTK_TrackedDeviceManager.OnControllerTriggerDown += TriggerDown;
        HOTK_TrackedDeviceManager.OnControllerTriggerHold += StartClickDragApplication;
        HOTK_TrackedDeviceManager.OnControllerTriggerUp += TriggerUp;

        HOTK_TrackedDeviceManager.OnControllerGripsClicked += MiddleClickApplication;
        HOTK_TrackedDeviceManager.OnControllerGripsDown += GripsDown;
        HOTK_TrackedDeviceManager.OnControllerGripsUp += GripsUp;

        HOTK_TrackedDeviceManager.OnControllerTouchpadClicked += RightClickApplication;
        HOTK_TrackedDeviceManager.OnControllerTouchpadDown += TouchpadDown;
        HOTK_TrackedDeviceManager.OnControllerTouchpadTouchStart += TouchStart;
        HOTK_TrackedDeviceManager.OnControllerTouchpadTouchMove += TouchMove;
        HOTK_TrackedDeviceManager.OnControllerTouchpadTouchEnd += TouchEnd;

        Overlay.OnControllerHitsOverlay += AimAtApplication;
        Overlay.OnControllerUnhitsOverlay += UnsetLastHit;
        Overlay.OnControllerTouchesOverlay += TouchOverlay;
        Overlay.OnControllerStopsTouchingOverlay += UnTouchOverlay;
        Overlay.OnOverlayAnimationChanges += AnimationChanges;

        DodgeGazeDetector.OnOverlayGazed += Overlay.GazeDetectorGazed;

        Overlay.OnOverlayEnabled += DodgeGazeDetector.OnParentEnabled;
        Overlay.OnOverlayDisabled += DodgeGazeDetector.OnParentDisabled;
    }

    public void OnDisable()
    {
        SaveLoad.Save();
        StopCoroutine("CaptureWindow");
        //_selectedWindow = IntPtr.Zero;
        //SelectedWindowTitle = "";
        Overlay.OverlayTexture = DefaultTexture;
        if (DisplayQuad == null) return;
        DisplayQuad.GetComponent<Renderer>().material.mainTexture = DefaultTexture;
        DisplayQuad.transform.localScale = new Vector3(860f, 389f, 1f);
        OutlineMaterial.color = new Color(OutlineMaterial.color.r, OutlineMaterial.color.g, OutlineMaterial.color.b, 0f);
    }

    private int _trueCursorRelativeX;
    private int _trueCursorRelativeY;
    private float _lastTrueCursorMoveTime;
    private float _lastScalingHapticPulse;
    public void Update()
    {
        if (_showFps)
        {
            if (_fpsTimer.IsRunning)
            {
                if (_fpsTimer.ElapsedMilliseconds >= 1000)
                {
                    _fpsTimer.Reset();
                    FpsCounter.text = "FPS: " + _fpsCount;
                    var val = Overlay.Framerate != HOTK_Overlay.FramerateMode.AsFastAsPossible ? HOTK_Overlay.FramerateValues[(int)Overlay.Framerate] : -1;
                    if (val != -1)
                    {
                        if (_fpsCount < val)
                        {
                            FpsCounter.color = _fpsCount < val / 2 ? Color.red : Color.yellow;
                        }
                        else FpsCounter.color = Color.white;
                    }
                    else FpsCounter.color = Color.white;

                    _fpsCount = 0;
                    _fpsTimer.Start();
                }
            }
            else
            {
                _fpsTimer.Start();
            }
            _fpsCount++;
        }
        if (SelectedWindowSettings != null)
        {
            if (_selectedWindow != IntPtr.Zero)
            {
                if (SelectedWindowSettings.clickShowDesktopCursor)
                {
                    var p = CursorInteraction.GetCursorPosRelativeWindow(_selectedWindow);
                    if (_trueCursorRelativeX != p.X || _trueCursorRelativeY != p.Y)
                    {
                        _trueCursorRelativeX = p.X;
                        _trueCursorRelativeY = p.Y;
                        _lastTrueCursorMoveTime = Time.time;
                        if (_grabbingOverlay == null && _touchingOverlay == null && _aimingAtOverlay == null)
                        {
                            if (p.X >= 0 && p.X <= _currentWindowWidth && p.Y >= 0 && p.Y <= _currentWindowHeight)
                            {
                                CursorGameObject.transform.localPosition = new Vector3(-(_currentWindowWidth / 2f) + p.X, (_currentWindowHeight / 2f) - p.Y, -0.5f);
                                if (SelectedWindowSettings.clickDesktopCursorForceWindowOnTop)
                                    Win32Stuff.SetForegroundWindow(_selectedWindow);
                                StartCoroutine("GoToAimingColor");
                            }
                            else StartCoroutine("GoToDefaultColor");
                        }
                    }
                    else if (SelectedWindowSettings.clickDesktopCursorAutoHide && Time.time - _lastTrueCursorMoveTime > 3.0f)
                    {
                        if (_grabbingOverlay == null && _touchingOverlay == null && _aimingAtOverlay == null)
                        {
                            StartCoroutine("GoToDefaultColor");
                        }
                    }
                }
                else if (_trueCursorRelativeX != -1 || _trueCursorRelativeY != -1)
                {
                    _trueCursorRelativeX = -1;
                    _trueCursorRelativeY = -1;

                    if (_grabbingOverlay == null && _touchingOverlay == null && _aimingAtOverlay == null)
                    {
                        StartCoroutine("GoToDefaultColor");
                    }
                }
            }
        }
        if (_scalingOverlay != null)
        {
            var scale = Mathf.Max(0.1f, _scalingBaseScale + (Vector3.Distance(_grabbingOverlay.transform.position, _scalingOverlay.transform.position) - _scalingBaseDistance));
            if (HapticsEnabledToggle.isOn && Mathf.Abs(scale - _lastScalingHapticPulse) > 0.1f)
            {
                _lastScalingHapticPulse = scale;
                _grabbingOverlay.TriggerHapticPulse(2000);
                _scalingOverlay.TriggerHapticPulse(2000);
            }
            if (_scalingScale2) Overlay.Scale2 = scale;
            else Overlay.Scale = scale;

        }
        else if (_grabbingOverlay != null)
        {
            Overlay.AnchorOffset = _grabbingOverlay.transform.position + _grabbingOffset;
            Overlay.transform.rotation = OverlayOffsetTracker.transform.rotation;
        }
    }

    public void OnDestroy()
    {
        SaveLoad.Save();
        DisplayMaterial.mainTexture = DefaultTexture;
    }
    #endregion

    #region Controller Interaction
    /// <summary>
    /// Occurs when a controller comes into contact with an Overlay
    /// </summary>
    private void TouchOverlay(HOTK_OverlayBase o, HOTK_TrackedDevice tracker, SteamVR_Overlay.IntersectionResults result)
    {
        if (!Overlay.gameObject.activeSelf) return;
        if (_selectedWindow == IntPtr.Zero) return;
        //if (Overlay.AnchorDevice == HOTK_Overlay.AttachmentDevice.LeftController && tracker.Type == HOTK_TrackedDevice.EType.LeftController) return;
        //if (Overlay.AnchorDevice == HOTK_Overlay.AttachmentDevice.RightController && tracker.Type == HOTK_TrackedDevice.EType.RightController) return;
        if (Overlay.AnimateOnGaze == HOTK_Overlay.AnimationType.DodgeGaze && Overlay.IsDodging) return; // Don't allow touching/grabbing overlays that are dodging
        if (Overlay.AnchorDevice != HOTK_Overlay.AttachmentDevice.World) return;
        if (_grabbingOverlay != null) return;
        // Hide the cursor from when we were just aiming
        _didHitOverlay = false;
        if (HapticsEnabledToggle.isOn && _touchingOverlay != tracker)
            tracker.TriggerHapticPulse(2000);
        _touchingOverlay = tracker;
        StartCoroutine("GoToTouchColor");
    }
    /// <summary>
    /// Occurs when a controller stops touching an Overlay
    /// </summary>
    private void UnTouchOverlay(HOTK_OverlayBase o, HOTK_TrackedDevice tracker)
    {
        if (_grabbingOverlay != null) return;
        _touchingOverlay = null;
        if (!_didHitOverlay) StartCoroutine("GoToDefaultColor");
    }

    private void AnimationChanges(HOTK_Overlay o)
    {
        DodgeGazeDetector.gameObject.SetActive(o.AnimateOnGaze == HOTK_Overlay.AnimationType.DodgeGaze);
        HOTK_TrackedDeviceManager.Instance.SetOverlayCanGaze(o, o.AnimateOnGaze != HOTK_Overlay.AnimationType.DodgeGaze);
        HOTK_TrackedDeviceManager.Instance.SetCompanionCanGaze(DodgeGazeDetector, o.AnimateOnGaze == HOTK_Overlay.AnimationType.DodgeGaze);
    }

    /// <summary>
    /// Occurs when a controller is aiming at an Overlay
    /// </summary>
    private void AimAtApplication(HOTK_OverlayBase o, HOTK_TrackedDevice tracker, SteamVR_Overlay.IntersectionResults result)
    {
        if (!Overlay.gameObject.activeSelf) return;
        if (_selectedWindow == IntPtr.Zero) return;
        if (SelectedWindowSettings.clickAPI == ClickAPI.None) return;
        if (Overlay.AnchorDevice == HOTK_Overlay.AttachmentDevice.LeftController && tracker.Type == HOTK_TrackedDevice.EType.LeftController) return;
        if (Overlay.AnchorDevice == HOTK_Overlay.AttachmentDevice.RightController && tracker.Type == HOTK_TrackedDevice.EType.RightController) return;
        if (_touchingOverlay != null || _grabbingOverlay != null) return;

        if (_currentWindowWidth > 0 && _currentWindowHeight > 0)
        {
            var p = new Point((int)((_currentWindowWidth + _renderTextureMarginWidth) * result.UVs.x),
                              (int)((_currentWindowHeight + _renderTextureMarginHeight) * result.UVs.y));
            var v1 = new Vector3(-(_currentWindowWidth / 2f) + p.X - (_renderTextureMarginWidth / 2f), (_currentWindowHeight / 2f) - p.Y + (_renderTextureMarginHeight / 2f), -0.5f);
            var v2 = new Vector2((_currentWindowWidth / 2f) + v1.x + SelectedWindowSettings.offsetLeft, (_currentWindowHeight / 2f) - v1.y + SelectedWindowSettings.offsetTop);

            if ((int)v2.x == _lastWindowPosX && (int)v2.y == _lastWindowPosY) return;
            _lastWindowPosX = (int)v2.x;
            _lastWindowPosY = (int)v2.y;

            if (v2.x > 0 && v2.y > 0 && v2.x < _currentWindowWidth + SelectedWindowSettings.offsetLeft && v2.y < _currentWindowHeight + SelectedWindowSettings.offsetTop)
            {
                if (HapticsEnabledToggle.isOn && _aimingAtOverlay != tracker)
                    tracker.TriggerHapticPulse(2000);
                _aimingAtOverlay = tracker;
                _didHitOverlay = true;
                if (WindowOnTopToggle.isOn)
                    Win32Stuff.SetForegroundWindow(_selectedWindow);

                if (MoveDesktopCursorToggle.isOn)
                {
                    CursorInteraction.MoveOverWindow(_selectedWindow, new Point((int)v2.x, (int)v2.y));
                }
                _localWindowPosX = (int)v2.x;
                _localWindowPosY = (int)v2.y;

                CursorGameObject.transform.localPosition = v1;
                StopCoroutine("GoToTouchColor");
                StopCoroutine("GoToScalingColor");
                StartCoroutine("GoToAimingColor");
            }
            else
            {
                StartCoroutine("GoToDefaultColor");
            }
        }
        else
        {
            StartCoroutine("GoToDefaultColor");
        }
    }
    /// <summary>
    /// Occurs when a controller stops aiming at an Overlay
    /// </summary>
    private void UnsetLastHit(HOTK_OverlayBase o, HOTK_TrackedDevice tracker)
    {
        if (tracker != _aimingAtOverlay) return;
        if (ClickDragging && tracker == ClickDraggingTracker) EndClickDragApplication(tracker);
        _lastWindowPosX = -1;
        _lastWindowPosY = -1;
        _localWindowPosX = -1;
        _localWindowPosY = -1;
        _didHitOverlay = false;
        _isHittingOverlay = false;
        _aimingAtOverlay = null;
        _touchingTouchpadGripsDown = false;
        if (_touchingOverlay == null) StartCoroutine("GoToDefaultColor");
    }

    private void ShowCursor()
    {
        CursorGameObject.SetActive(true);
    }
    private void HideCursor()
    {
        CursorGameObject.SetActive(false);
    }

    /// <summary>
    /// Occurs when a trigger has been pressed
    /// </summary>
    private void TriggerDown(HOTK_TrackedDevice tracker)
    {
        if (_touchingOverlay != null)
        {
            if (_grabbingOverlay == null)
            {
                if (tracker == _touchingOverlay && GrabEnabledToggle.isOn)
                {
                    _grabbingOverlay = tracker;
                    _lastOverlayParent = Overlay.gameObject.transform.parent;
                    _grabbingOffset = Overlay.AnchorOffset - _grabbingOverlay.transform.position;
                    OverlayOffsetTracker.transform.parent = _grabbingOverlay.transform;
                    OverlayOffsetTracker.transform.localPosition = _grabbingOffset;
                    OverlayOffsetTracker.transform.rotation = _grabbingOverlay.transform.rotation;
                }
            }
            else if (_scalingOverlay == null)
            {
                if (ScaleEnabledToggle.isOn)
                {
                    _scalingOverlay = tracker;
                    _scalingScale2 = Overlay.IsBeingGazed;
                    Overlay.LockGaze(_scalingScale2); // Lock the Gaze Detection on the Overlay to it's current state
                    _scalingBaseScale = _scalingScale2 ? Overlay.Scale2 : Overlay.Scale;
                    _lastScalingHapticPulse = _scalingBaseScale;
                    _scalingBaseDistance = Vector3.Distance(_grabbingOverlay.transform.position, _scalingOverlay.transform.position);
                    StartCoroutine("GoToScalingColor");
                }
            }
        }
        else
        {
            if (tracker == _aimingAtOverlay)
            {
                if (_didHitOverlay) // Test if we were aiminag at the overlay when the click action started
                    _isHittingOverlay = true;
            }
        }
    }
    /// <summary>
    /// Occurs when a trigger stops being pressed
    /// </summary>
    /// <param name="tracker"></param>
    private void TriggerUp(HOTK_TrackedDevice tracker)
    {
        if (EndClickDragApplication(tracker)) return;
        if (_scalingOverlay != null)
        {
            _scalingOverlay = null;
            _scalingBaseScale = 0f;
            _scalingBaseDistance = 0f;
            Overlay.UnlockGaze();
            if (_scalingScale2)
            {
                _scalingScale2 = false;
                Scale2Field.InputField.text = Overlay.Scale2.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                ScaleField.InputField.text = Overlay.Scale.ToString(CultureInfo.InvariantCulture);
            }
            if (tracker == _grabbingOverlay)
            {
                DetachOverlayGrab();
            }
            else StartCoroutine("GoToTouchColor");
        }
        else if (_grabbingOverlay != null)
        {
            DetachOverlayGrab();
        }
    }

    private void DetachOverlayGrab()
    {
        _grabbingOverlay = null;
        _touchingOverlay = null;
        Overlay.gameObject.transform.parent = _lastOverlayParent;
        _lastOverlayParent = null;

        OffsetX.InputField.text = Overlay.AnchorOffset.x.ToString(CultureInfo.InvariantCulture);
        OffsetY.InputField.text = Overlay.AnchorOffset.y.ToString(CultureInfo.InvariantCulture);
        OffsetZ.InputField.text = Overlay.AnchorOffset.z.ToString(CultureInfo.InvariantCulture);
        OffsetX.OnOffsetChanged();
        OffsetY.OnOffsetChanged();
        OffsetZ.OnOffsetChanged();
        var dx = Overlay.gameObject.transform.rotation.eulerAngles.x;
        var dy = Overlay.gameObject.transform.rotation.eulerAngles.y;
        var dz = Overlay.gameObject.transform.rotation.eulerAngles.z;
        OffsetRx.Slider.value = dx;
        OffsetRy.Slider.value = dy;
        OffsetRz.Slider.value = dz;

        if (!_didHitOverlay) StartCoroutine("GoToDefaultColor");
    }
    /// <summary>
    /// Occurs when a touchpad has been pressed down
    /// </summary>
    private void TouchpadDown(HOTK_TrackedDevice tracker)
    {
        if (_touchingOverlay != null) return;
        if (tracker != _aimingAtOverlay) return;
        if (_didHitOverlay) // Test if we were aiminag at the overlay when the click action started
            _isHittingOverlay = true;
    }

    private void GripsDown(HOTK_TrackedDevice tracker)
    {
        if (_selectedWindow == IntPtr.Zero) return;
        if (SelectedWindowSettings.clickAPI == ClickAPI.None) return;
        if (tracker != _aimingAtOverlay) return;
        _touchingTouchpadGripsDown = true;
    }
    private void GripsUp(HOTK_TrackedDevice tracker)
    {
        if (_selectedWindow == IntPtr.Zero) return;
        if (SelectedWindowSettings.clickAPI == ClickAPI.None) return;
        if (tracker != _aimingAtOverlay) return;
        _touchingTouchpadGripsDown = false;
    }

    private void TouchStart(HOTK_TrackedDevice tracker, float dx, float dy)
    {
        if (!ScrollEnabledToggle.isOn) return;
        if (_selectedWindow == IntPtr.Zero) return;
        if (SelectedWindowSettings.clickAPI == ClickAPI.None) return;
        if (!_touchingTouchpadGripsDown && ScrollRequiresGripsToggle.isOn) return;
        if (tracker != _aimingAtOverlay) return;
        if (dy > -TouchingTouchpadRange && dy < TouchingTouchpadRange) // Vertical Range
        {
            if (dx < -TouchingTouchpadRange2 || dx > TouchingTouchpadRange2)
                _touchingTouchpadAxisIsVertical = true;
            else return;
        }
        else if (dx > -TouchingTouchpadRange && dx < TouchingTouchpadRange) // Horizontal Range
        {
            if (dy < -TouchingTouchpadRange2 || dy > TouchingTouchpadRange2)
                _touchingTouchpadAxisIsVertical = false;
            else return;
        }
        else return;
        _touchingTouchpad = true;
        _touchingTouchpadX = dx;
        _touchingTouchpadY = dy;
    }
    private void TouchMove(HOTK_TrackedDevice tracker, float dx, float dy)
    {
        if (!_touchingTouchpad) return;
        if (_selectedWindow == IntPtr.Zero) return;
        if (SelectedWindowSettings.clickAPI == ClickAPI.None) return;
        if ((!_touchingTouchpadGripsDown && ScrollRequiresGripsToggle.isOn && tracker == _aimingAtOverlay) || _aimingAtOverlay == null || !ScrollEnabledToggle.isOn)
        {
            _touchingTouchpad = false;
            return;
        }
        if (tracker != _aimingAtOverlay) return;
        if (_touchingTouchpadAxisIsVertical)
        {
            if (_touchingTouchpadX - dx > TouchingTouchpadTickDistance)
            {
                _touchingTouchpadX = dx;
                if (dy <= -TouchingTouchpadRange || dy >= TouchingTouchpadRange) return;
                if (HapticsEnabledToggle.isOn) tracker.TriggerHapticPulse(TouchingTouchpadHapticStrength);
                ScrollApplication(tracker, CursorInteraction.SimulationMode.ScrollH, -1);
            }
            else if (_touchingTouchpadX - dx < -TouchingTouchpadTickDistance)
            {
                _touchingTouchpadX = dx;
                if (dy <= -TouchingTouchpadRange || dy >= TouchingTouchpadRange) return;
                if (HapticsEnabledToggle.isOn) tracker.TriggerHapticPulse(TouchingTouchpadHapticStrength);
                ScrollApplication(tracker, CursorInteraction.SimulationMode.ScrollH, 1);
            }
        }
        else
        {
            if (_touchingTouchpadY - dy > TouchingTouchpadTickDistance)
            {
                _touchingTouchpadY = dy;
                if (dx <= -TouchingTouchpadRange || dx >= TouchingTouchpadRange) return;
                if (HapticsEnabledToggle.isOn) tracker.TriggerHapticPulse(TouchingTouchpadHapticStrength);
                ScrollApplication(tracker, CursorInteraction.SimulationMode.ScrollV, -1);
            }
            else if (_touchingTouchpadY - dy < -TouchingTouchpadTickDistance)
            {
                _touchingTouchpadY = dy;
                if (dx <= -TouchingTouchpadRange || dx >= TouchingTouchpadRange) return;
                if (HapticsEnabledToggle.isOn) tracker.TriggerHapticPulse(TouchingTouchpadHapticStrength);
                ScrollApplication(tracker, CursorInteraction.SimulationMode.ScrollV, 1);
            }
        }
    }
    private void TouchEnd(HOTK_TrackedDevice tracker)
    {
        if (!ScrollEnabledToggle.isOn) return;
        if (!_touchingTouchpad) return;
        if (tracker != _aimingAtOverlay) return;
        _touchingTouchpad = false;
    }

    private void ClickApplication(HOTK_TrackedDevice tracker, CursorInteraction.SimulationMode mode)
    {
        if (_selectedWindow == IntPtr.Zero) return;
        if (SelectedWindowSettings.clickAPI == ClickAPI.None) return;
        if (!_isHittingOverlay) return;
        if (tracker != _aimingAtOverlay) return;
        CursorInteraction.CursorSendInput(_selectedWindow, mode, SelectedWindowSettings.clickAPI, new Point(_localWindowPosX, _localWindowPosY), 0);
    }
    private void ScrollApplication(HOTK_TrackedDevice tracker, CursorInteraction.SimulationMode mode, int delta = 0)
    {
        if (_selectedWindow == IntPtr.Zero) return;
        if (SelectedWindowSettings.clickAPI == ClickAPI.None) return;
        if (tracker != _aimingAtOverlay) return;
        CursorInteraction.CursorSendInput(_selectedWindow, mode, SelectedWindowSettings.clickAPI, new Point(_localWindowPosX, _localWindowPosY), delta);
    }

    private bool ClickDragging;
    private HOTK_TrackedDevice ClickDraggingTracker;

    private void StartClickDragApplication(HOTK_TrackedDevice tracker, Point p)
    {
        if (_selectedWindow == IntPtr.Zero) return;
        if (SelectedWindowSettings.clickAPI == ClickAPI.None) return;
        if (!_isHittingOverlay) return;
        if (tracker != _aimingAtOverlay) return;
        if (!ClickDragging) System.Windows.Forms.Cursor.Position = p; // Move cursor back to start point when starting
        ClickDragging = true;
        ClickDraggingTracker = tracker;
        ClickApplication(tracker, CursorInteraction.SimulationMode.LeftDown);
    }
    private bool EndClickDragApplication(HOTK_TrackedDevice tracker)
    {
        if (ClickDraggingTracker != tracker) return false;
        if (!ClickDragging) return false;
        ClickDragging = false;
        ClickDraggingTracker = null;
        ClickApplication(tracker, CursorInteraction.SimulationMode.LeftUp);
        return true;
    }

    /// <summary>
    /// Occurs when a trigger has been clicked (pressed and released rapidly)
    /// </summary>
    private void SingleClickApplication(HOTK_TrackedDevice tracker)
    {
        ClickApplication(tracker, CursorInteraction.SimulationMode.LeftClick);
    }

    /// <summary>
    /// Occurs when a trigger has been double clicked (pressed and released rapidly, twice in a row)
    /// </summary>
    private void DoubleClickApplication(HOTK_TrackedDevice tracker)
    {
        ClickApplication(tracker, CursorInteraction.SimulationMode.DoubleClick);
    }

    /// <summary>
    /// Occurs when a touchpad has been clicked (pressed and released rapidly)
    /// </summary>
    private void RightClickApplication(HOTK_TrackedDevice tracker)
    {
        ClickApplication(tracker, CursorInteraction.SimulationMode.RightClick);
    }

    /// <summary>
    /// Occurs when grips has been clicked (pressed and released rapidly)
    /// </summary>
    private void MiddleClickApplication(HOTK_TrackedDevice tracker)
    {
        ClickApplication(tracker, CursorInteraction.SimulationMode.MiddleClick);
    }
    #endregion

    #region Coroutines
    // ReSharper disable UnusedMember.Local
    private IEnumerator GoToDefaultColor()
    {
        StopCoroutine("GoToTouchColor");
        StopCoroutine("GoToAimingColor");
        StopCoroutine("GoToScalingColor");
        _currentMode = OutlineColor.Default;
        StartCoroutine("FadeOutCursor");
        var t = 0f;
        while (t < 1f)
        {
            t += 0.25f;
            OutlineMaterial.color = Color.Lerp(OutlineMaterial.color, OutlineColorDefault, t);
            yield return new WaitForSeconds(0.025f);
        }
    }

    private IEnumerator GoToAimingColor()
    {
        StopCoroutine("GoToDefaultColor");
        StopCoroutine("GoToTouchColor");
        StopCoroutine("GoToScalingColor");
        _currentMode = OutlineColor.Aiming;
        StartCoroutine("FadeInCursor");
        var t = 0f;
        while (t < 1f)
        {
            t += 0.25f;
            OutlineMaterial.color = Color.Lerp(OutlineMaterial.color, OutlineColorAiming, t);
            yield return new WaitForSeconds(0.025f);
        }
    }

    private IEnumerator GoToTouchColor()
    {
        StopCoroutine("GoToDefaultColor");
        StopCoroutine("GoToAimingColor");
        StopCoroutine("GoToScalingColor");
        _currentMode = OutlineColor.Touching;
        StartCoroutine("FadeOutCursor");
        var t = 0f;
        while (t < 1f)
        {
            t += 0.25f;
            OutlineMaterial.color = Color.Lerp(OutlineMaterial.color, OutlineColorTouching, t);
            yield return new WaitForSeconds(0.025f);
        }
    }

    private IEnumerator GoToScalingColor()
    {
        StopCoroutine("GoToDefaultColor");
        StopCoroutine("GoToAimingColor");
        StopCoroutine("GoToTouchColor");
        _currentMode = OutlineColor.Scaling;
        StartCoroutine("FadeOutCursor");
        var t = 0f;
        while (t < 1f)
        {
            t += 0.25f;
            OutlineMaterial.color = Color.Lerp(OutlineMaterial.color, OutlineColorScaling, t);
            yield return new WaitForSeconds(0.025f);
        }
    }

    private IEnumerator FadeInCursor()
    {
        StopCoroutine("FadeOutCursor");
        var t = CursorRenderer.color.a;
        ShowCursor();
        while (t < 1f)
        {
            t += 0.1f;
            if (CursorRenderer != null) CursorRenderer.color = new Color(CursorRenderer.color.r, CursorRenderer.color.g, CursorRenderer.color.b, t);
            yield return new WaitForSeconds(0.025f);
        }
    }

    private IEnumerator FadeOutCursor()
    {
        StopCoroutine("FadeInCursor");
        var t = CursorRenderer.color.a;
        while (t > 0)
        {
            t -= 0.1f;
            if (CursorRenderer != null) CursorRenderer.color = new Color(CursorRenderer.color.r, CursorRenderer.color.g, CursorRenderer.color.b, t);
            yield return new WaitForSeconds(0.025f);
        }
        HideCursor();
    }

    private IEnumerator UpdateEvery1Second()
    {
        while (Application.isPlaying)
        {
            if (_selectedWindow != IntPtr.Zero)
            {
                if (!OffsetWidthField.isFocused && !OffsetHeightField.isFocused)
                {
                    var r = CaptureScreen.GetWindowRect(_selectedWindow);
                    if (SelectedWindowSettings.windowSizeLocked && SelectedWindowSettings.offsetWidth > 0 && SelectedWindowSettings.offsetHeight > 0)
                    {
                        r.Width = SelectedWindowSettings.offsetWidth;
                        r.Height = SelectedWindowSettings.offsetHeight;
                        SetWindowSize(r);
                    }
                    _currentCaptureWidth = r.Width;
                    _currentCaptureHeight = r.Height;
                    OffsetWidthField.text = r.Width.ToString();
                    OffsetHeightField.text = r.Height.ToString();
                }
            }
            yield return new WaitForSeconds(1f);
        }
    }

    private IEnumerator UpdateEvery10Seconds()
    {
        while (Application.isPlaying)
        {
            var compositor = OpenVR.Compositor;
            if (compositor != null)
            {
                var trackingSpace = compositor.GetTrackingSpace();
                SteamVR_Render.instance.trackingSpace = trackingSpace;
            }

            RefreshWindowList();
            yield return new WaitForSeconds(10f);
        }
    }

    private IEnumerator CaptureWindow()
    {
        Overlay.OverlayTexture = RenderTexture;
        DisplayMaterial.mainTexture = _texture;
        while (Application.isPlaying && _selectedWindow != IntPtr.Zero)
        {
            if (SelectedWindowSettings.captureMode == CaptureMode.GdiDirect)
            {
                if (!_wasDirect)
                {
                    _wasDirect = true;
                }
                _bitmap = CaptureScreen.CaptureWindowDirect(_selectedWindow, SelectedWindowSettings, out _size, out _info);
            }
            else if (SelectedWindowSettings.captureMode == CaptureMode.GdiIndirect)
            {
                if (_wasDirect)
                {
                    _wasDirect = false;
                }
                _bitmap = CaptureScreen.CaptureWindow(_selectedWindow, SelectedWindowSettings, out _size, out _info);
            }
            if (_bitmap == null)
            {
                _selectedWindow = IntPtr.Zero;
                _selectedWindowFullPath = string.Empty;
                SelectedWindowSettings = null;
                SelectedWindowTitle = null;
                _currentWindowWidth = 0;
                _currentWindowHeight = 0;
                ResolutionDisplay.text = "";
                DisplayQuad.transform.localScale = new Vector3(0f, 0f, 1f);
                Overlay.ClearOverlayTexture();
                StartRefreshing();
                break;
            }
            _bitmap.Save(_stream, ImageFormat.Png);
            _stream.Seek(0, SeekOrigin.Begin);
            _texture.LoadImage(_stream.ToArray());
            if (_currentWindowWidth != _size.cx || _currentWindowHeight != _size.cy)
            {
                _currentWindowWidth = _size.cx;
                _currentWindowHeight = _size.cy;
                ResolutionDisplay.text = string.Format("( {0} x {1} )", _size.cx, _size.cy);
                DisplayQuad.transform.localScale = new Vector3(_size.cx, _size.cy, 1f);
            }
            Overlay.RefreshTexture();
            switch (Overlay.Framerate)
            {
                case HOTK_Overlay.FramerateMode._1FPS:
                    yield return new WaitForSeconds(FramerateFractions[0]);
                    break;
                case HOTK_Overlay.FramerateMode._2FPS:
                    yield return new WaitForSeconds(FramerateFractions[1]);
                    break;
                case HOTK_Overlay.FramerateMode._5FPS:
                    yield return new WaitForSeconds(FramerateFractions[2]);
                    break;
                case HOTK_Overlay.FramerateMode._10FPS:
                    yield return new WaitForSeconds(FramerateFractions[3]);
                    break;
                case HOTK_Overlay.FramerateMode._15FPS:
                    yield return new WaitForSeconds(FramerateFractions[4]);
                    break;
                case HOTK_Overlay.FramerateMode._24FPS:
                    yield return new WaitForSeconds(FramerateFractions[5]);
                    break;
                case HOTK_Overlay.FramerateMode._30FPS:
                    yield return new WaitForSeconds(FramerateFractions[6]);
                    break;
                case HOTK_Overlay.FramerateMode._60FPS:
                    yield return new WaitForSeconds(FramerateFractions[7]);
                    break;
                case HOTK_Overlay.FramerateMode._90FPS:
                    yield return new WaitForSeconds(FramerateFractions[8]);
                    break;
                case HOTK_Overlay.FramerateMode._120FPS:
                    yield return new WaitForSeconds(FramerateFractions[9]);
                    break;
                case HOTK_Overlay.FramerateMode.AsFastAsPossible:
                    yield return new WaitForEndOfFrame();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    private IEnumerator UpdateBacksideAfterFrame()
    {
        yield return new WaitForEndOfFrame();
        Backside.DoUpdateOverlay();
        BacksideScript.Camera.enabled = false;
    }

    // ReSharper restore UnusedMember.Local

    #endregion

    /// <summary>
    /// Generates a new RenderTexture and assigns it
    /// </summary>
    private RenderTexture NewRenderTexture()
    {
        var r = new RenderTexture(((int)DisplayQuad.transform.localScale.x + _renderTextureMarginWidth) * 2, ((int)DisplayQuad.transform.localScale.y + _renderTextureMarginHeight) * 2, 24);
        var previous = RenderCamera.targetTexture;
        if (_selectedWindow != IntPtr.Zero)
        {
            r.filterMode = SelectedWindowSettings.filterMode;
        }
        RenderCamera.targetTexture = r;
        Overlay.OverlayTexture = r;
        if (previous != null) previous.Release();
        return r;
    }

    /// <summary>
    /// Automatically replaces the existing RenderTexture with an appropriately sized one using the Getter/Setter
    /// </summary>
    public RenderTexture GetNewRenderTexture(int width = 0, int height = 0)
    {
        Overlay.StopDodging();
        _renderTextureMarginWidth = width;
        _renderTextureMarginHeight = height;
        _renderTexture = null;
        return RenderTexture;
    }

    public void CheckOverlayOffsetPerformed()
    {
        if (Overlay.AnchorDevice == HOTK_Overlay.AttachmentDevice.Screen)
        {
            if (ScreenOffsetPerformed) return;
            ScreenOffsetPerformed = true;
            var v = Overlay.AnchorOffset.z + 1;
            ZSlider.Slider.minValue = v - 2;
            ZSlider.Slider.maxValue = v + 2;
            ZSlider.Slider.value = v;
            ZSlider.OnOffsetChanged();
            ZInputField.text = v.ToString(CultureInfo.InvariantCulture);
        }
        else
        {
            if (!ScreenOffsetPerformed) return;
            ScreenOffsetPerformed = false;
            var v = Overlay.AnchorOffset.z - 1;
            ZSlider.Slider.minValue = v - 2;
            ZSlider.Slider.maxValue = v + 2;
            ZSlider.Slider.value = v;
            ZSlider.OnOffsetChanged();
            ZInputField.text = v.ToString(CultureInfo.InvariantCulture);
        }
    }

    private void SetBacksideTexture(BacksideTexture texture)
    {
        if (BacksideDropdown != null)
            BacksideDropdown.SetToOption(texture.ToString(), true);
        if (texture == BacksideTexture.None)
        {
            Backside.gameObject.SetActive(false);
            BacksideDisplayQuad.gameObject.SetActive(false);
            return;
        }
        BacksideDisplayMaterial.mainTexture = BacksideTextures[(int)texture - 1];
        BacksideDisplayQuad.gameObject.SetActive(true);
        BacksideScript.Camera.enabled = true;
        if (Backside.gameObject.activeSelf)
            StartCoroutine(UpdateBacksideAfterFrame());
        else
            Backside.gameObject.SetActive(true);
    }

    #region UI Methods

    public void EnableFpsCounter()
    {
        FpsCounter.gameObject.SetActive(true);
        ResolutionDisplay.gameObject.SetActive(true);
        _showFps = true;
    }

    public void DoChangeFilterMode(FilterMode mode)
    {
        if (_selectedWindow == IntPtr.Zero) return;
        switch (mode)
        {
            case FilterMode.Point:
            case FilterMode.Bilinear:
            case FilterMode.Trilinear:
                SelectedWindowSettings.filterMode = mode;
                _texture.filterMode = SelectedWindowSettings.filterMode;
                RenderTexture.filterMode = SelectedWindowSettings.filterMode;
                break;
            default:
                throw new NotImplementedException();
        }
    }

    public void StopRefreshing()
    {
        if (!Overlay.gameObject.activeSelf) return;
        StopCoroutine("UpdateEvery10Seconds");
    }

    public void StartRefreshing()
    {
        if (!Overlay.gameObject.activeSelf) return;
        StopCoroutine("UpdateEvery10Seconds");
        StartCoroutine("UpdateEvery10Seconds");
    }

    /// <summary>
    /// Refreshes the Window List with a list of the current windows
    /// </summary>
    private void RefreshWindowList()
    {
        var windows = Win32Stuff.FindWindowsWithSize();
        _windows.Clear();
        _titles.Clear();
        var count = 0;
        foreach (var w in windows)
        {
            var title = Win32Stuff.GetWindowText(w);
            if (title.Length <= 0) continue;
            var copy = 0;
            var found = false;
            while (!found)
            {
                try
                {
                    _windows.Add(copy == 0 ? title : string.Format("{0} ({1})", title, copy), w);
                    found = true;
                }
                catch (ArgumentException)
                {
                    copy++;
                }
            }

            _titles.Add(copy == 0 ? title : string.Format("{0} ({1})", title, copy));
            count++;
        }

        ApplicationDropdown.ClearOptions();
        ApplicationDropdown.AddOptions(_titles);
        _reselecting = true;
        ApplicationDropdown.value = 0;

        var foundCurrent = false;
        if (!string.IsNullOrEmpty(SelectedWindowTitle))
        {
            for (var i = 0; i < ApplicationDropdown.options.Count; i++)
            {
                if (ApplicationDropdown.options[i].text != SelectedWindowTitle) continue;
                _reselecting = true;
                ApplicationDropdown.value = i;
                foundCurrent = true;
                break;
            }
        }
        _reselecting = false;

        if (foundCurrent) return;
        // Attempt to recapture by window PID
        if (_selectedWindow != IntPtr.Zero)
        {
            var found = false;
            string windowName = null;

            foreach (var entry in _windows.Where(entry => entry.Value == _selectedWindow))
            {
                windowName = entry.Key;
                found = true;
                break;
            }
            if (found)
            {
                if (!string.IsNullOrEmpty(windowName))
                {
                    for (var i = 0; i < ApplicationDropdown.options.Count; i++)
                    {
                        if (ApplicationDropdown.options[i].text != windowName) continue;
                        Debug.Log("Found " + SelectedWindowTitle + " by new name " + windowName);
                        _reselecting = true;
                        SelectedWindowTitle = windowName;
                        ApplicationDropdown.value = i;
                        foundCurrent = true;
                        break;
                    }
                }
            }
        }
        // Windows must be closed or otherwise lost to cyberspace
        if (foundCurrent) return;
        _selectedWindow = IntPtr.Zero;
        _selectedWindowFullPath = string.Empty;
        SelectedWindowSettings = null;

        ApplicationDropdown.captionText.text = count + " window(s) detected";
        Debug.Log("Found " + count + " windows");
    }

    public void OptionChanged()
    {
        if (_reselecting)
        {
            _reselecting = false;
            return;
        }
        StopCoroutine("CaptureWindow");
        SelectedWindowTitle = ApplicationDropdown.captionText.text;
        Debug.Log("Selected " + SelectedWindowTitle);

        IntPtr window;
        if (_windows.TryGetValue(SelectedWindowTitle, out window))
        {
            _selectedWindow = window;
            _selectedWindowFullPath = Win32Stuff.GetFilePath(_selectedWindow);
            if (!string.IsNullOrEmpty(_selectedWindowFullPath))
            {
                SelectedWindowSettings = LoadConfig(_selectedWindowFullPath);
                CaptureModeDropdown.SetToOption(DropdownMatchEnumOptions.CaptureModeNames[(int)SelectedWindowSettings.captureMode]);
            }
            else
            {
                Debug.LogWarning("Failed to grab path info. Settings might not work properly.");
                _selectedWindowFullPath = string.Empty;
                SelectedWindowSettings = LoadConfig(SelectedWindowTitle);
                CaptureModeDropdown.SetToOption(DropdownMatchEnumOptions.CaptureModeNames[(int)SelectedWindowSettings.captureMode]);
            }
            var r = CaptureScreen.GetWindowRect(_selectedWindow);
            _currentCaptureWidth = r.Width;
            _currentCaptureHeight = r.Height;
            OffsetWidthField.text = r.Width.ToString();
            OffsetHeightField.text = r.Height.ToString();
            if (!Overlay.gameObject.activeSelf) return;
            StartCoroutine("CaptureWindow");
        }
        else
        {
            Debug.LogError("Failed to find Window");
        }
    }

    public void ToggleSizeLocked()
    {
        if (_selectedWindow == IntPtr.Zero) return;
        SelectedWindowSettings.windowSizeLocked = !SelectedWindowSettings.windowSizeLocked;
        SizeLockSprite.sprite = SelectedWindowSettings.windowSizeLocked ? LockSprite : UnlockSprite;
        if (SelectedWindowSettings.windowSizeLocked)
        {
            int v;
            if (int.TryParse(OffsetWidthField.text, out v))
            {
                if (v > 0)
                {
                    SelectedWindowSettings.offsetWidth = v;
                }
            }
            if (int.TryParse(OffsetHeightField.text, out v))
            {
                if (v > 0)
                {
                    SelectedWindowSettings.offsetHeight = v;
                }
            }
        }
        SaveLoad.Save();
    }

    public void ToggleAspectLocked()
    {
        if (_selectedWindow == IntPtr.Zero) return;
        SelectedWindowSettings.windowAspectLocked = !SelectedWindowSettings.windowAspectLocked;
        var r = CaptureScreen.GetWindowRect(_selectedWindow);
        SelectedWindowSettings.windowAspectW = (float)r.Height / (float)r.Width;
        SelectedWindowSettings.windowAspectH = (float)r.Width / (float)r.Height;
        AspectLinkedSprite.sprite = SelectedWindowSettings.windowAspectLocked ? AspectLockSprite : AspectUnlockSprite;

        SaveLoad.Save();
    }

    public void WindowSettingConfirmed(string setting)
    {
        if (_selectedWindow == IntPtr.Zero) return;
        RECT r;
        int v;
        int val;
        switch (setting)
        {
            case "Left":
                if (int.TryParse(OffsetLeftField.text, out val))
                {
                    SelectedWindowSettings.offsetLeft = val;
                }
                else OffsetLeftField.text = SelectedWindowSettings.offsetLeft.ToString();
                break;
            case "Top":
                if (int.TryParse(OffsetTopField.text, out val))
                {
                    SelectedWindowSettings.offsetTop = val;
                }
                else OffsetTopField.text = SelectedWindowSettings.offsetTop.ToString();
                break;
            case "Right":
                if (int.TryParse(OffsetRightField.text, out val))
                {
                    SelectedWindowSettings.offsetRight = val;
                }
                else OffsetRightField.text = SelectedWindowSettings.offsetRight.ToString();
                break;
            case "Bottom":
                if (int.TryParse(OffsetBottomField.text, out val))
                {
                    SelectedWindowSettings.offsetBottom = val;
                }
                else OffsetBottomField.text = SelectedWindowSettings.offsetBottom.ToString();
                break;
            case "Width":
                r = CaptureScreen.GetWindowRect(_selectedWindow);
                if (int.TryParse(OffsetWidthField.text, out v))
                {
                    if (v > 0)
                    {
                        r.Width = v;
                        SelectedWindowSettings.offsetWidth = v;
                        if (SelectedWindowSettings.windowAspectLocked)
                        {
                            r.Height = (int)(r.Width * SelectedWindowSettings.windowAspectW);
                            OffsetHeightField.text = r.Height.ToString();
                            SelectedWindowSettings.offsetHeight = r.Height;
                        }
                        if (SelectedWindowSettings.offsetHeight <= 0) SelectedWindowSettings.offsetHeight = r.Height;
                        SetWindowSize(r);
                    }
                    else
                    {
                        _currentCaptureWidth = r.Width;
                        OffsetWidthField.text = r.Width.ToString();
                    }
                }
                else
                {
                    _currentCaptureWidth = r.Width;
                    OffsetWidthField.text = r.Width.ToString();
                }
                break;
            case "Height":
                r = CaptureScreen.GetWindowRect(_selectedWindow);
                if (int.TryParse(OffsetHeightField.text, out v))
                {
                    if (v > 0)
                    {
                        r.Height = v;
                        SelectedWindowSettings.offsetHeight = v;
                        if (SelectedWindowSettings.windowAspectLocked)
                        {
                            r.Width = (int)(r.Height * SelectedWindowSettings.windowAspectH);
                            OffsetWidthField.text = r.Width.ToString();
                            SelectedWindowSettings.offsetWidth = r.Width;
                        }
                        if (SelectedWindowSettings.offsetWidth <= 0) SelectedWindowSettings.offsetWidth = r.Width;
                        SetWindowSize(r);
                    }
                    else
                    {
                        _currentCaptureHeight = r.Height;
                        OffsetHeightField.text = r.Height.ToString();
                    }
                }
                else
                {
                    _currentCaptureHeight = r.Height;
                    OffsetHeightField.text = r.Height.ToString();
                }
                break;
            default:
                throw new NotImplementedException();
        }
    }

    public void SetWindowSize(RECT r)
    {
        CaptureScreen.SetWindowRect(_selectedWindow, r, SelectedWindowSettings.captureMode == CaptureMode.GdiIndirect);
    }

    public WindowSettings LoadConfig(string configName)
    {
        WindowSettings settings;
        if (!SaveLoad.SavedSettings.TryGetValue(configName, out settings))
        {
            Debug.Log("Config [" + configName + "] not found.");
            settings = new WindowSettings { SaveFileVersion = WindowSettings.CurrentSaveVersion };
            SaveLoad.SavedSettings.Add(configName, settings);
        }
        else Debug.Log("Config [" + configName + "] Loaded.");

        if (settings.SaveFileVersion == 0)
        {
            Debug.Log("Upgrading [" + configName + "] to SaveFileVersion 1.");
            settings.offsetLeft = settings.offsetX;
            settings.offsetTop = settings.offsetY;
            settings.offsetRight = settings.offsetWidth;
            settings.offsetBottom = settings.offsetHeight;
            settings.offsetX = 0;
            settings.offsetY = 0;
            settings.offsetWidth = 0;
            settings.offsetHeight = 0;
            settings.SaveFileVersion = 1;
        }

        if (settings.SaveFileVersion == 1)
        {
            Debug.Log("Upgrading [" + configName + "] to SaveFileVersion 2.");
            settings.captureMode = settings.directMode ? CaptureMode.GdiDirect : CaptureMode.GdiIndirect;
            settings.interactionMode = MouseInteractionMode.DirectInteraction;
            settings.directMode = false;
            settings.windowSizeLocked = false;
            settings.SaveFileVersion = 2;
        }
        if (settings.SaveFileVersion == 2)
        {
            Debug.Log("Upgrading [" + configName + "] to SaveFileVersion 3.");
            settings.framerateMode = HOTK_Overlay.FramerateMode._24FPS; // Compatibility because these values changed significantly. Default to 24FPS.
            settings.SaveFileVersion = 3;
        }
        if (settings.SaveFileVersion == 3)
        {
            Debug.Log("Upgrading [" + configName + "] to SaveFileVersion 4.");
            settings.filterMode = FilterMode.Point;
            settings.SaveFileVersion = 4;
        }
        if (settings.SaveFileVersion == 4)
        {
            Debug.Log("Upgrading [" + configName + "] to SaveFileVersion 5.");
            settings.interactionMode = MouseInteractionMode.Disabled;
            settings.clickAPI = ClickAPI.SendInput;
            settings.clickForceWindowOnTop = true;
            settings.clickMoveDesktopCursor = true;
            settings.clickShowDesktopCursor = false;
            settings.SaveFileVersion = 5;
        }
        if (settings.SaveFileVersion == 5)
        {
            Debug.Log("Upgrading [" + configName + "] to SaveFileVersion 6.");
            settings.clickScrollEnabled = true;
            settings.clickScrollRequiresGrips = true;
            settings.SaveFileVersion = 6;
        }

        if (_texture != null)
            _texture.filterMode = settings.filterMode;

        OffsetLeftField.text = settings.offsetLeft.ToString();
        OffsetTopField.text = settings.offsetTop.ToString();
        OffsetRightField.text = settings.offsetRight.ToString();
        OffsetBottomField.text = settings.offsetBottom.ToString();
        SizeLockSprite.sprite = settings.windowSizeLocked ? LockSprite : UnlockSprite;
        AspectLinkedSprite.sprite = settings.windowAspectLocked ? AspectLockSprite : AspectUnlockSprite;
        CaptureModeDropdown.SetToOption(DropdownMatchEnumOptions.CaptureModeNames[(int)settings.captureMode], true);
        FramerateModeDropdown.SetToOption(DropdownMatchEnumOptions.FramerateModeNames[(int)settings.framerateMode], true);

        Overlay.Framerate = settings.framerateMode;

        FilterModeDropdown.Dropdown.interactable = true;
        FilterModeDropdown.SetToOption(settings.filterMode.ToString(), true);

        ClickAPIDropdown.Dropdown.interactable = true;
        ClickAPIDropdown.SetToOption(settings.clickAPI.ToString(), true);
        SetClickAPIInternal(settings);

        return settings;
    }

    public Color GetOutlineColor(OutlineColor mode)
    {
        switch (mode)
        {
            case OutlineColor.Default:
                return OutlineColorDefault;
            case OutlineColor.Aiming:
                return OutlineColorAiming;
            case OutlineColor.Touching:
                return OutlineColorTouching;
            case OutlineColor.Scaling:
                return OutlineColorScaling;
            default:
                throw new ArgumentOutOfRangeException("mode", mode, null);
        }
    }

    public void SetOutlineColor(OutlineColor mode, Color color)
    {
        switch (mode)
        {
            case OutlineColor.Default:
                OutlineColorDefault = color;
                break;
            case OutlineColor.Aiming:
                OutlineColorAiming = color;
                break;
            case OutlineColor.Touching:
                OutlineColorTouching = color;
                break;
            case OutlineColor.Scaling:
                OutlineColorScaling = color;
                break;
            default:
                throw new ArgumentOutOfRangeException("mode", mode, null);
        }
    }

    public void SetClickAPI(ClickAPI api)
    {
        if (SelectedWindowSettings == null) return;
        SelectedWindowSettings.clickAPI = api;
        SetClickAPIInternal(SelectedWindowSettings);
    }

    private void SetClickAPIInternal(WindowSettings settings)
    {
        if (settings == null) return;
        switch (settings.clickAPI)
        {
            case ClickAPI.None:
                WindowOnTopToggle.interactable = false;
                MoveDesktopCursorToggle.interactable = false;
                ShowDesktopCursorToggle.interactable = _selectedWindow != IntPtr.Zero;
                DesktopCursorWindowOnTopToggle.interactable = _selectedWindow != IntPtr.Zero && settings.clickShowDesktopCursor;
                DesktopCursorAutoHideToggle.interactable = _selectedWindow != IntPtr.Zero && settings.clickShowDesktopCursor;

                WindowOnTopToggle.isOn = false;
                MoveDesktopCursorToggle.isOn = false;
                ScrollEnabledToggle.isOn = false;
                ScrollRequiresGripsToggle.isOn = false;
                ShowDesktopCursorToggle.isOn = settings.clickShowDesktopCursor;
                DesktopCursorWindowOnTopToggle.isOn = settings.clickDesktopCursorForceWindowOnTop;
                DesktopCursorAutoHideToggle.isOn = settings.clickDesktopCursorAutoHide;

                ScrollEnabledToggle.interactable = false;
                ScrollRequiresGripsToggle.interactable = false;
                break;
            case ClickAPI.SendInput:
                WindowOnTopToggle.interactable = false;
                MoveDesktopCursorToggle.interactable = false;
                ShowDesktopCursorToggle.interactable = true;
                DesktopCursorWindowOnTopToggle.interactable = settings.clickShowDesktopCursor;
                DesktopCursorAutoHideToggle.interactable = settings.clickShowDesktopCursor;

                WindowOnTopToggle.isOn = true;
                MoveDesktopCursorToggle.isOn = true;
                ShowDesktopCursorToggle.isOn = settings.clickShowDesktopCursor;
                DesktopCursorWindowOnTopToggle.isOn = settings.clickDesktopCursorForceWindowOnTop;
                DesktopCursorAutoHideToggle.isOn = settings.clickDesktopCursorAutoHide;

                SetScrollWheelSettings(settings);
                break;
            case ClickAPI.SendMessage:
                WindowOnTopToggle.interactable = true;
                MoveDesktopCursorToggle.interactable = true;
                ShowDesktopCursorToggle.interactable = true;
                DesktopCursorWindowOnTopToggle.interactable = settings.clickShowDesktopCursor;
                DesktopCursorAutoHideToggle.interactable = settings.clickShowDesktopCursor;

                WindowOnTopToggle.isOn = settings.clickForceWindowOnTop;
                MoveDesktopCursorToggle.isOn = settings.clickMoveDesktopCursor;
                ShowDesktopCursorToggle.isOn = settings.clickShowDesktopCursor;
                DesktopCursorWindowOnTopToggle.isOn = settings.clickDesktopCursorForceWindowOnTop;
                DesktopCursorAutoHideToggle.isOn = settings.clickDesktopCursorAutoHide;

                SetScrollWheelSettings(settings);
                break;
            case ClickAPI.SendNotifyMessage:
                WindowOnTopToggle.interactable = true;
                MoveDesktopCursorToggle.interactable = true;
                ShowDesktopCursorToggle.interactable = true;
                DesktopCursorWindowOnTopToggle.interactable = settings.clickShowDesktopCursor;
                DesktopCursorAutoHideToggle.interactable = settings.clickShowDesktopCursor;

                WindowOnTopToggle.isOn = settings.clickForceWindowOnTop;
                MoveDesktopCursorToggle.isOn = settings.clickMoveDesktopCursor;
                ShowDesktopCursorToggle.isOn = settings.clickShowDesktopCursor;
                DesktopCursorWindowOnTopToggle.isOn = settings.clickDesktopCursorForceWindowOnTop;
                DesktopCursorAutoHideToggle.isOn = settings.clickDesktopCursorAutoHide;

                SetScrollWheelSettings(settings);
                break;
            default:
                throw new ArgumentOutOfRangeException("api", settings.clickAPI, null);
        }
    }

    private void SetScrollWheelSettings(WindowSettings settings)
    {
        ScrollEnabledToggle.interactable = WindowOnTopToggle.isOn && MoveDesktopCursorToggle.isOn;
        ScrollEnabledToggle.isOn = ScrollEnabledToggle.interactable && settings.clickScrollEnabled;

        ScrollRequiresGripsToggle.interactable = ScrollEnabledToggle.interactable && ScrollEnabledToggle.isOn;
        ScrollRequiresGripsToggle.isOn = ScrollRequiresGripsToggle.interactable && settings.clickScrollRequiresGrips;
    }

    public void ToggleWindowOnTop()
    {
        if (!WindowOnTopToggle.IsInteractable()) return;
        SelectedWindowSettings.clickForceWindowOnTop = WindowOnTopToggle.isOn;
        SetScrollWheelSettings(SelectedWindowSettings);
    }

    public void ToggleMoveCursor()
    {
        if (!MoveDesktopCursorToggle.IsInteractable()) return;
        SelectedWindowSettings.clickMoveDesktopCursor = MoveDesktopCursorToggle.isOn;
        SetScrollWheelSettings(SelectedWindowSettings);
    }

    public void ToggleShowCursor()
    {
        if (!ShowDesktopCursorToggle.IsInteractable()) return;
        SelectedWindowSettings.clickShowDesktopCursor = ShowDesktopCursorToggle.isOn;
        DesktopCursorWindowOnTopToggle.interactable = ShowDesktopCursorToggle.isOn;
        DesktopCursorAutoHideToggle.interactable = ShowDesktopCursorToggle.isOn;
    }

    public void ToggleWindowOnTopWithDesktopCursor()
    {
        if (!DesktopCursorWindowOnTopToggle.IsInteractable()) return;
        SelectedWindowSettings.clickDesktopCursorForceWindowOnTop = DesktopCursorWindowOnTopToggle.isOn;
    }

    public void ToggleAutoHideWithDesktopCursor()
    {
        if (!DesktopCursorAutoHideToggle.IsInteractable()) return;
        SelectedWindowSettings.clickDesktopCursorAutoHide = DesktopCursorAutoHideToggle.isOn;
    }

    public void ToggleGrabEnabled()
    {
        if (!GrabEnabledToggle.IsInteractable()) return;
    }

    public void ToggleScaleEnabled()
    {
        if (!ScaleEnabledToggle.IsInteractable()) return;
    }

    public void ToggleHapticsEnabled()
    {
        if (!HapticsEnabledToggle.IsInteractable()) return;
    }

    public void ToggleScrollEnabled()
    {
        if (!ScrollEnabledToggle.IsInteractable()) return;
        SelectedWindowSettings.clickScrollEnabled = ScrollEnabledToggle.isOn;
        SetScrollWheelSettings(SelectedWindowSettings);
    }

    public void ToggleScrollRequiresGrips()
    {
        if (!ScrollRequiresGripsToggle.IsInteractable()) return;
        SelectedWindowSettings.clickScrollRequiresGrips = ScrollRequiresGripsToggle.isOn;
    }

    #endregion

    #region Enums

    public enum CaptureMode
    {
        GdiDirect = 0,
        GdiIndirect = 1,
        ReplicationApi = 2,
    }

    public enum MouseInteractionMode
    {
        DirectInteraction = 0, // Keep Window on top, Move Cursor
        WindowTop = 1, // Keep Window on top only, Send Mouse Clicks Only (No Move)
        SendClicksOnly = 2, // Only Send Mouse Clicks
        Disabled = 3
    }

    public enum ClickAPI
    {
        None = 0,
        SendInput = 1,
        SendMessage = 2,
        SendNotifyMessage = 3,
    }

    public enum BacksideTexture
    {
        None = 0,
        Green = 1,
        Blue = 2,
        Purple = 3,
        Red = 4,
        Orange = 5,
        Yellow = 6,
    }

    public enum OutlineColor
    {
        Default = 0,
        Aiming = 1,
        Touching = 2,
        Scaling,
    }

    #endregion
}

#region Utility Classes

public static class SaveLoad
{
    public static Dictionary<string, WindowSettings> SavedSettings = new Dictionary<string, WindowSettings>();

    public static readonly string TargetSettingsFile = Application.persistentDataPath + "/savedSettings.gd";
    public static readonly string TargetSettingsBackupFile = Application.persistentDataPath + "/savedSettings.bak";

    public static void Save()
    {
        var bf = new BinaryFormatter();
        var file = File.Create(TargetSettingsFile);
        bf.Serialize(file, SavedSettings);
        file.Close();
        Debug.Log("Saved " + SavedSettings.Count + " config(s).");
        Debug.Log("Testing Settings File");
        var settings = LoadSettingsFile(TargetSettingsFile);
        if (settings != null && settings.Count > 0)
        {
            Debug.Log("Settings File valid; Backing up");
            if (File.Exists(TargetSettingsBackupFile))
                File.Delete(TargetSettingsBackupFile);
            File.Copy(TargetSettingsFile, TargetSettingsBackupFile);
        }
        else Debug.LogError("Settings File Invalid!");
    }

    public static void Load()
    {
        var settings = LoadSettingsFile(TargetSettingsFile);
        if (settings != null && settings.Count > 0)
        {
            SavedSettings = settings;
            Debug.Log("Loaded " + SavedSettings.Count + " config(s).");
            return;
        }
        settings = LoadSettingsFile(TargetSettingsBackupFile);
        if (settings != null && settings.Count > 0)
        {
            SavedSettings = settings;
            Debug.LogWarning("Target Application Settings not found, but Backup found.");
            Debug.Log("Loaded " + SavedSettings.Count + " config(s) from Backup.");
            return;
        }
        Debug.Log("Target Application Settings not found.");
    }

    public static Dictionary<string, WindowSettings> LoadSettingsFile(string filename)
    {
        if (!File.Exists(filename)) return null;
        var bf = new BinaryFormatter();
        var file = File.Open(filename, FileMode.Open);
        var settings = (Dictionary<string, WindowSettings>)bf.Deserialize(file);
        file.Close();
        return settings;
    }
}

#endregion