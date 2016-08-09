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
    #region Public Variables
    public static DesktopPortalController Instance
    {
        get { return _instance ?? (_instance = FindObjectOfType<DesktopPortalController>()); }
    }
    public Camera RenderCamera;
    public Texture DefaultTexture;
    public DropdownMatchEnumOptions CaptureModeDropdown;
    public DropdownMatchEnumOptions FramerateModeDropdown;
    public DropdownMatchEnumOptions InteractionModeDropdown;
    public Toggle MinimizedToggle;
    public Image SizeLockSprite;
    public Sprite LockSprite;
    public Sprite UnlockSprite;
    public Material OutlineMaterial;
    public InputField OffsetLeftField;
    public InputField OffsetTopField;
    public InputField OffsetRightField;
    public InputField OffsetBottomField;
    public InputField OffsetWidthField;
    public InputField OffsetHeightField;
    public HOTK_Overlay Overlay;
    public Material DisplayMaterial;
    public Dropdown ApplicationDropdown;
    public GameObject DisplayQuad;
    public OffsetMatchInputField OffsetX;
    public OffsetMatchInputField OffsetY;
    public OffsetMatchInputField OffsetZ;
    public RotationMatchSlider OffsetRx;
    public RotationMatchSlider OffsetRy;
    public RotationMatchSlider OffsetRz;
    public RenderTexture RenderTexture
    {
        get { return _renderTexture ?? (_renderTexture = NewRenderTexture()); }
    }
    public GameObject CursorGameObject;
    public SpriteRenderer CursorRenderer // Cache and return the SpriteRenderer for the Cursor if we can
    {
        get
        {
            return _cursorRenderer ??
                   (_cursorRenderer =
                       (CursorGameObject == null ? null : CursorGameObject.GetComponent<SpriteRenderer>()));
        }
    }
    public GameObject OverlayOffsetTracker
    {
        get { return _overlayOffsetTracker ?? (_overlayOffsetTracker = new GameObject("Overlay Offset Tracker")); }
    }
    public Text FpsCounter;
    public Text ResolutionDisplay;
    //string SelectedWindowPath = string.Empty;
    //string SelectedWindowEXE = string.Empty;
    public WindowSettings SelectedWindowSettings;
    [HideInInspector]
    public string SelectedWindowTitle;
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

    // Cache these fractions so they aren't constantly recalculated
    private static readonly float[] FramerateFractions = {
        1f, 1f/2f, 1f/5f, 1f/10f, 1/15f, 1/24f, 1f/30f, 1f/60f, 1f/90f, 1f/120f
    };
    #endregion

    #region Unity Methods
    public void OnEnable()
    {
        Debug.Log("Connected to: " + SteamVR.instance.hmd_TrackingSystemName); // Force SteamVR Plugin to Init
        if (Overlay == null) return;
        SaveLoad.Load();
        _bitmap = CaptureScreen.CaptureDesktop();
        _texture = new Texture2D(_bitmap.Width, _bitmap.Height) {filterMode = FilterMode.Point};

        _stream = new MemoryStream();
        _bitmap.Save(_stream, ImageFormat.Png);
        _stream.Seek(0, SeekOrigin.Begin);

        Overlay.OverlayTexture = RenderTexture;
        DisplayQuad.GetComponent<Renderer>().material.mainTexture = _texture;
        DisplayQuad.transform.localScale = new Vector3(_bitmap.Width, _bitmap.Height, 1f);

        OutlineMaterial.color = new Color(OutlineMaterial.color.r, OutlineMaterial.color.g, OutlineMaterial.color.b, 0f); // Hide Outline on start

        _texture.LoadImage(_stream.ToArray());

        RefreshWindowList();

        StartCoroutine("UpdateEvery1Second");
        StartCoroutine("UpdateEvery10Seconds");
        if (!_subscribed)
        {
            _subscribed = true;
            HOTK_TrackedDeviceManager.OnControllerTriggerClicked += SingleClickApplication;
            HOTK_TrackedDeviceManager.OnControllerTriggerDoubleClicked += DoubleClickApplication;
            HOTK_TrackedDeviceManager.OnControllerTouchpadClicked += RightClickApplication;
            HOTK_TrackedDeviceManager.OnControllerTriggerDown += TriggerDown;
            HOTK_TrackedDeviceManager.OnControllerTriggerUp += TriggerUp;
            HOTK_TrackedDeviceManager.OnControllerTouchpadDown += TouchpadDown;
            Overlay.OnControllerHitsOverlay += AimAtApplication;
            Overlay.OnControllerUnhitsOverlay += UnsetLastHit;
            Overlay.OnControllerTouchesOverlay += TouchOverlay;
            Overlay.OnControllerStopsTouchingOverlay += UnTouchOverlay;
        }
    }

    public void OnDisable()
    {
        StopCoroutine("CaptureWindow");
        _selectedWindow = IntPtr.Zero;
        SelectedWindowTitle = "";
        Overlay.OverlayTexture = DefaultTexture;
        if (DisplayQuad == null) return;
        DisplayQuad.GetComponent<Renderer>().material.mainTexture = DefaultTexture;
        DisplayQuad.transform.localScale = new Vector3(860f, 389f, 1f);
        OutlineMaterial.color = new Color(OutlineMaterial.color.r, OutlineMaterial.color.g, OutlineMaterial.color.b, 0f);
    }

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
        if (_scalingOverlay != null)
        {
            Overlay.Scale = _scalingBaseScale + (Vector3.Distance(_grabbingOverlay.transform.position, _scalingOverlay.transform.position) - _scalingBaseDistance);
        }else if (_grabbingOverlay != null)
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
    private void TouchOverlay(HOTK_Overlay o, HOTK_TrackedDevice tracker, HOTK_Overlay.IntersectionResults result)
    {
        if (_selectedWindow == IntPtr.Zero) return;
        if (Overlay.AnchorDevice == HOTK_Overlay.AttachmentDevice.LeftController && tracker.Type == HOTK_TrackedDevice.EType.LeftController) return;
        if (Overlay.AnchorDevice == HOTK_Overlay.AttachmentDevice.RightController && tracker.Type == HOTK_TrackedDevice.EType.RightController) return;
        if (Overlay.AnchorDevice != HOTK_Overlay.AttachmentDevice.World) return;
        if (_grabbingOverlay != null) return;
        // Hide the cursor from when we were just aiming
        HideCursor();
        _didHitOverlay = false;
        _touchingOverlay = tracker;
        StartCoroutine("GoToGreen");
        StartCoroutine("FadeInOutline");
    }
    private void UnTouchOverlay(HOTK_Overlay o, HOTK_TrackedDevice tracker)
    {
        if (_grabbingOverlay != null) return;
        _touchingOverlay = null;
        if (!_didHitOverlay) StartCoroutine("FadeOutOutline");
    }
    private void AimAtApplication(HOTK_Overlay o, HOTK_TrackedDevice tracker, HOTK_Overlay.IntersectionResults result)
    {
        if (_selectedWindow == IntPtr.Zero) return;
        if (SelectedWindowSettings.interactionMode == MouseInteractionMode.Disabled) return;
        if (Overlay.AnchorDevice == HOTK_Overlay.AttachmentDevice.LeftController && tracker.Type == HOTK_TrackedDevice.EType.LeftController) return;
        if (Overlay.AnchorDevice == HOTK_Overlay.AttachmentDevice.RightController && tracker.Type == HOTK_TrackedDevice.EType.RightController) return;
        if (_touchingOverlay != null || _grabbingOverlay != null) return;
        if (_currentWindowWidth > 0 && _currentWindowHeight > 0)
        {
            var p = new Point((int) ((_currentWindowWidth + _renderTextureMarginWidth) * result.UVs.x),
                              (int) ((_currentWindowHeight + _renderTextureMarginHeight) * result.UVs.y));
            var v1 = new Vector3(-(_currentWindowWidth / 2f) + p.X - (_renderTextureMarginWidth / 2f), (_currentWindowHeight / 2f) - p.Y + (_renderTextureMarginHeight / 2f), -0.5f);
            var v2 = new Vector2((_currentWindowWidth / 2f) + v1.x + SelectedWindowSettings.offsetLeft, (_currentWindowHeight / 2f) - v1.y + SelectedWindowSettings.offsetTop);

            if ((int) v2.x == _lastWindowPosX && (int) v2.y == _lastWindowPosY) return;
            _lastWindowPosX = (int)v2.x;
            _lastWindowPosY = (int)v2.y;

            if (v2.x > 0 && v2.y > 0 && v2.x < _currentWindowWidth + SelectedWindowSettings.offsetLeft && v2.y < _currentWindowHeight + SelectedWindowSettings.offsetTop)
            {
                _aimingAtOverlay = tracker;
                _didHitOverlay = true;
                if (SelectedWindowSettings.interactionMode == MouseInteractionMode.DirectInteraction ||
                    SelectedWindowSettings.interactionMode == MouseInteractionMode.WindowTop)
                    Win32Stuff.BringWindowToTop(_selectedWindow);

                if (SelectedWindowSettings.interactionMode == MouseInteractionMode.DirectInteraction)
                {
                    CursorInteraction.MoveOverWindow(_selectedWindow, new Point((int) v2.x, (int) v2.y));
                }else
                {
                    _localWindowPosX = (int)v2.x;
                    _localWindowPosY = (int)v2.y;
                }
                
                CursorGameObject.transform.localPosition = v1;
                StopCoroutine("GoToGreen");
                StopCoroutine("GoToBlue");
                OutlineMaterial.color = new Color(1f, 0f, 0f, OutlineMaterial.color.a);
                StartCoroutine("FadeInOutline");
            }
            else
            {
                StartCoroutine("FadeOutOutline");
            }
        }
        else
        {
            HideCursor();
            StartCoroutine("FadeOutOutline");
        }
    }
    private void UnsetLastHit(HOTK_Overlay o, HOTK_TrackedDevice tracker)
    {
        if (tracker != _aimingAtOverlay) return;
        _lastWindowPosX = -1;
        _lastWindowPosY = -1;
        _localWindowPosX = -1;
        _localWindowPosY = -1;
        _didHitOverlay = false;
        _isHittingOverlay = false;
        _aimingAtOverlay = null;
        if (_touchingOverlay == null) StartCoroutine("FadeOutOutline");
    }

    private void ShowCursor()
    {
        CursorGameObject.SetActive(true);
    }
    private void HideCursor()
    {
        CursorGameObject.SetActive(false);
    }

    private HOTK_TrackedDevice _scalingOverlay;
    private float _scalingBaseScale;
    private float _scalingBaseDistance;

    private void TriggerDown(HOTK_TrackedDevice tracker)
    {
        if (_touchingOverlay != null)
        {
            if (_grabbingOverlay == null)
            {
                _grabbingOverlay = tracker;
                _lastOverlayParent = Overlay.gameObject.transform.parent;
                _grabbingOffset = Overlay.AnchorOffset - _grabbingOverlay.transform.position;
                OverlayOffsetTracker.transform.parent = _grabbingOverlay.transform;
                OverlayOffsetTracker.transform.localPosition = _grabbingOffset;
                OverlayOffsetTracker.transform.rotation = _grabbingOverlay.transform.rotation;
            }
            else
            {
                _scalingOverlay = tracker;
                _scalingBaseScale = Overlay.Scale;
                _scalingBaseDistance = Vector3.Distance(_grabbingOverlay.transform.position, _scalingOverlay.transform.position);
                StartCoroutine("GoToBlue");
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

    private void TouchpadDown(HOTK_TrackedDevice tracker)
    {
        if (_touchingOverlay == null)
        {
            if (tracker == _aimingAtOverlay)
            {
                if (_didHitOverlay) // Test if we were aiminag at the overlay when the click action started
                    _isHittingOverlay = true;
            }
        }
    }

    // Test if we were aiminag at the overlay when the click action started
    private void TriggerUp(HOTK_TrackedDevice tracker)
    {
        if (_scalingOverlay != null)
        {
            _scalingOverlay = null;
            _scalingBaseScale = 0f;
            _scalingBaseDistance = 0f;
            //if (_grabbingOverlay == null) return;
            //_grabbingOffset = Overlay.AnchorOffset - _grabbingOverlay.transform.position;
            //OverlayOffsetTracker.transform.localPosition = _grabbingOffset;
            StartCoroutine("GoToGreen");
        }
        else if (_grabbingOverlay != null)
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
            
            if (!_didHitOverlay) StartCoroutine("FadeOutOutline");
        }
    }

    // Click the application
    // ReSharper disable once UnusedParameter.Local
    private void ClickApplication(HOTK_TrackedDevice tracker, CursorInteraction.SimulationMode mode)
    {
        if (_selectedWindow == IntPtr.Zero) return;
        if (SelectedWindowSettings.interactionMode == MouseInteractionMode.Disabled) return;
        if (!_isHittingOverlay) return;
        if (tracker != _aimingAtOverlay) return;
        //Debug.Log("Try Click " + tracker.Type);
        if (_selectedWindow == IntPtr.Zero) return;
        //Debug.Log("Clicking" + tracker.Type);
        switch (SelectedWindowSettings.interactionMode)
        {
            case MouseInteractionMode.DirectInteraction:
                CursorInteraction.ClickOnPointAtCursor(_selectedWindow, mode);
                break;
            case MouseInteractionMode.WindowTop:
            case MouseInteractionMode.SendClicksOnly:
                if (_localWindowPosX > -1 && _localWindowPosY > -1)
                    CursorInteraction.ClickOnPoint(_selectedWindow, new Point(_localWindowPosX, _localWindowPosY), mode);
                else
                    Debug.LogWarning("X or Y missing");
                break;
            case MouseInteractionMode.Disabled:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void SingleClickApplication(HOTK_TrackedDevice tracker)
    {
        ClickApplication(tracker, CursorInteraction.SimulationMode.LeftClick);
    }

    private void DoubleClickApplication(HOTK_TrackedDevice tracker)
    {
        ClickApplication(tracker, CursorInteraction.SimulationMode.DoubleClick);
    }

    private void RightClickApplication(HOTK_TrackedDevice tracker)
    {
        ClickApplication(tracker, CursorInteraction.SimulationMode.RightClick);
    }

    #endregion

    #region Coroutines
    // ReSharper disable UnusedMember.Local
    private IEnumerator GoToGreen()
    {
        StopCoroutine("GoToBlue");
        var t = 0f;
        while ((_touchingOverlay != null || _grabbingOverlay != null) && t < 1f)
        {
            t += 0.25f;
            OutlineMaterial.color = Color.Lerp(OutlineMaterial.color, new Color(0f, 1f, 0f, OutlineMaterial.color.a), t);
            yield return new WaitForSeconds(0.025f);
        }
    }
    private IEnumerator GoToBlue()
    {
        StopCoroutine("GoToGreen");
        var t = 0f;
        while ((_touchingOverlay != null || _grabbingOverlay != null) && t < 1f)
        {
            t += 0.25f;
            OutlineMaterial.color = Color.Lerp(OutlineMaterial.color, new Color(0f, 0f, 1f, OutlineMaterial.color.a), t);
            yield return new WaitForSeconds(0.025f);
        }
    }

    private IEnumerator FadeInOutline()
    {
        StopCoroutine("FadeOutOutline");
        if (_touchingOverlay == null && _grabbingOverlay == null) ShowCursor();
        while (OutlineMaterial.color.a < 1f)
        {
            OutlineMaterial.color = new Color(OutlineMaterial.color.r, OutlineMaterial.color.g, OutlineMaterial.color.b, OutlineMaterial.color.a + 0.1f);
            if (CursorRenderer != null) CursorRenderer.color = new Color(CursorRenderer.color.r, CursorRenderer.color.g, CursorRenderer.color.b, OutlineMaterial.color.a);
            yield return new WaitForSeconds(0.025f);
        }
    }
    private IEnumerator FadeOutOutline()
    {
        StopCoroutine("FadeInOutline");
        while (OutlineMaterial.color.a > 0f)
        {
            OutlineMaterial.color = new Color(OutlineMaterial.color.r, OutlineMaterial.color.g, OutlineMaterial.color.b, OutlineMaterial.color.a - 0.1f);
            if (CursorRenderer != null) CursorRenderer.color = new Color(CursorRenderer.color.r, CursorRenderer.color.g, CursorRenderer.color.b, OutlineMaterial.color.a);
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
                MinimizedToggle.isOn = !Win32Stuff.IsIconic(_selectedWindow);
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

    // ReSharper restore UnusedMember.Local

    #endregion

    private RenderTexture NewRenderTexture()
    {
        var r = new RenderTexture((int) DisplayQuad.transform.localScale.x + _renderTextureMarginWidth, (int) DisplayQuad.transform.localScale.y + _renderTextureMarginHeight, 24);
        var previous = RenderCamera.targetTexture;
        RenderCamera.targetTexture = r;
        Overlay.OverlayTexture = r;
        if (previous != null) previous.Release();
        return r;
    }

    public RenderTexture GetNewRenderTexture(int width = 0, int height = 0)
    {
        _renderTextureMarginWidth = width;
        _renderTextureMarginHeight = height;
        _renderTexture = null;
        return RenderTexture;
    }

    #region UI Methods

    public void EnableFpsCounter()
    {
        FpsCounter.gameObject.SetActive(true);
        ResolutionDisplay.gameObject.SetActive(true);
        _showFps = true;
    }

    public void StopRefreshing()
    {
        StopCoroutine("UpdateEvery10Seconds");
    }

    public void StartRefreshing()
    {
        StopCoroutine("UpdateEvery10Seconds");
        StartCoroutine("UpdateEvery10Seconds");
    }

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

        if (!foundCurrent)
        {
            // Attempt to recapture by window PID
            if (_selectedWindow != IntPtr.Zero)
            {
                bool found = false;
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
            if (!foundCurrent)
            {
                _selectedWindow = IntPtr.Zero;
                _selectedWindowFullPath = string.Empty;
                //SelectedWindowPath = string.Empty;
                //SelectedWindowEXE = string.Empty;
                SelectedWindowSettings = null;

                ApplicationDropdown.captionText.text = count + " window(s) detected";
                Debug.Log("Found " + count + " windows");
            }
        }
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
                CaptureModeDropdown.SetToOption(DropdownMatchEnumOptions.CaptureModeNames[(int) SelectedWindowSettings.captureMode]);
            }
            else
            {
                Debug.LogWarning("Failed to grab path info. Settings might not work properly.");
                _selectedWindowFullPath = string.Empty;
                SelectedWindowSettings = LoadConfig(SelectedWindowTitle);
                CaptureModeDropdown.SetToOption(DropdownMatchEnumOptions.CaptureModeNames[(int) SelectedWindowSettings.captureMode]);
            }
            var r = CaptureScreen.GetWindowRect(_selectedWindow);
            _currentCaptureWidth = r.Width;
            _currentCaptureHeight = r.Height;
            OffsetWidthField.text = r.Width.ToString();
            OffsetHeightField.text = r.Height.ToString();
            StartCoroutine("CaptureWindow");
        }
        else
        {
            Debug.LogError("Failed to find Window");
        }
    }

    public void ToggleMinimized()
    {
        if (_selectedWindow == IntPtr.Zero) return;
        Win32Stuff.ShowWindow(_selectedWindow, MinimizedToggle.isOn ? ShowWindowCommands.Restore : ShowWindowCommands.Minimize);
    }

    public void ToggleSizeLocked()
    {
        if (_selectedWindow == IntPtr.Zero) return;
        SelectedWindowSettings.windowSizeLocked = !SelectedWindowSettings.windowSizeLocked;
        SizeLockSprite.sprite = SelectedWindowSettings.windowSizeLocked ? LockSprite : UnlockSprite;
        if (!SelectedWindowSettings.windowSizeLocked) return;
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
            settings = new WindowSettings {SaveFileVersion = WindowSettings.CurrentSaveVersion};
            SaveLoad.SavedSettings.Add(configName, settings);
        }
        //else
        //{
        //    Debug.Log("Loading Config [" + name + "]. Version: " + settings.SaveFileVersion);
        //}

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

        OffsetLeftField.text = settings.offsetLeft.ToString();
        OffsetTopField.text = settings.offsetTop.ToString();
        OffsetRightField.text = settings.offsetRight.ToString();
        OffsetBottomField.text = settings.offsetBottom.ToString();
        SizeLockSprite.sprite = settings.windowSizeLocked ? LockSprite : UnlockSprite;
        CaptureModeDropdown.SetToOption(DropdownMatchEnumOptions.CaptureModeNames[(int) settings.captureMode], true);
        FramerateModeDropdown.SetToOption(DropdownMatchEnumOptions.FramerateModeNames[(int) settings.framerateMode], true);
        InteractionModeDropdown.SetToOption(DropdownMatchEnumOptions.MouseModeNames[(int) settings.interactionMode], true);
        return settings;
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

    #endregion
}

#region Utility Classes

public static class SaveLoad
{
    public static Dictionary<string, WindowSettings> SavedSettings = new Dictionary<string, WindowSettings>();

    public static void Save()
    {
        var bf = new BinaryFormatter();
        var file = File.Create(Application.persistentDataPath + "/savedSettings.gd");
        bf.Serialize(file, SavedSettings);
        file.Close();
        Debug.Log("Saved " + SavedSettings.Count + " config(s).");
    }

    public static void Load()
    {
        if (!File.Exists(Application.persistentDataPath + "/savedSettings.gd")) return;
        var bf = new BinaryFormatter();
        var file = File.Open(Application.persistentDataPath + "/savedSettings.gd", FileMode.Open);
        SavedSettings = (Dictionary<string, WindowSettings>) bf.Deserialize(file);
        file.Close();
        Debug.Log("Loaded " + SavedSettings.Count + " config(s).");
    }
}

#endregion