using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using ScreenCapture;
using UnityEngine.UI;
using Valve.VR;
using Color = UnityEngine.Color;
using Debug = UnityEngine.Debug;
using Image = UnityEngine.UI.Image;

public class DesktopPortalController : MonoBehaviour
{
    public static DesktopPortalController Instance
    {
        get { return _instance ?? (_instance = FindObjectOfType<DesktopPortalController>()); }
    }

    private static DesktopPortalController _instance;

    public Camera RenderCamera;

    public RenderTexture RenderTexture
    {
        get { return _renderTexture ?? (_renderTexture = NewRenderTexture()); }
    }

    private RenderTexture _renderTexture;

    private int _RenderTextureMarginWidth = 0;
    private int _RenderTextureMarginHeight = 0;

    private RenderTexture NewRenderTexture()
    {
        var r = new RenderTexture((int)DisplayQuad.transform.localScale.x + _RenderTextureMarginWidth, (int)DisplayQuad.transform.localScale.y + _RenderTextureMarginHeight, 24);
        var previous = RenderCamera.targetTexture;
        RenderCamera.targetTexture = r;
        Overlay.OverlayTexture = r;
        if (previous != null) previous.Release();
        return r;
    }

    public RenderTexture GetNewRenderTexture(int width = 0, int height = 0)
    {
        _RenderTextureMarginWidth = width;
        _RenderTextureMarginHeight = height;
        _renderTexture = null;
        return RenderTexture;
    }

    public Texture DefaultTexture;
    public DropdownMatchEnumOptions CaptureModeDropdown;
    public DropdownMatchEnumOptions FramerateModeDropdown;
    public DropdownMatchEnumOptions InteractionModeDropdown;
    public Toggle MinimizedToggle;
    public Image SizeLockSprite;
    public Sprite LockSprite;
    public Sprite UnlockSprite;

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
    private SpriteRenderer _cursorRenderer;

    public Material OutlineMaterial;

    public Text FPSCounter;
    public Text ResolutionDisplay;

    Dictionary<string, IntPtr> Windows = new Dictionary<string, IntPtr>();
    List<string> Titles = new List<string>();

    IntPtr SelectedWindow = IntPtr.Zero;
    string SelectedWindowFullPath = string.Empty;
    //string SelectedWindowPath = string.Empty;
    //string SelectedWindowEXE = string.Empty;
    public WindowSettings SelectedWindowSettings = null;

    [HideInInspector]
    public string SelectedWindowTitle;

    Bitmap bitmap;
    Texture2D texture;
    MemoryStream stream;

    private bool _showFPS;
    private Stopwatch FPSTimer = new Stopwatch();
    private int _currentWindowWidth;
    private int _currentWindowHeight;

    private int _currentCaptureWidth;
    private int _currentCaptureHeight;

    private bool _subscribed;

    public void OnEnable()
    {
        // ReSharper disable once UnusedVariable
        #pragma warning disable 0168
        var ins = SteamVR.instance; // Force SteamVR Plugin to Init
        #pragma warning restore 0168
        if (Overlay != null)
        {
            SaveLoad.Load();
            bitmap = CaptureScreen.CaptureDesktop();
            texture = new Texture2D(bitmap.Width, bitmap.Height);
            texture.filterMode = FilterMode.Point;

            stream = new MemoryStream();
            bitmap.Save(stream, ImageFormat.Png);
            stream.Seek(0, SeekOrigin.Begin);

            Overlay.OverlayTexture = RenderTexture;
            DisplayQuad.GetComponent<Renderer>().material.mainTexture = texture;
            DisplayQuad.transform.localScale = new Vector3(bitmap.Width, bitmap.Height, 1f);

            texture.LoadImage(stream.ToArray());

            RefreshWindowList();

            StartCoroutine("UpdateEvery1Second");
            StartCoroutine("UpdateEvery10Seconds");
            if (!_subscribed)
            {
                _subscribed = true;
                HOTK_TrackedDeviceManager.OnControllerTriggerClicked += SingleClickApplication;
                HOTK_TrackedDeviceManager.OnControllerTriggerDoubleClicked += DoubleClickApplication;
                HOTK_TrackedDeviceManager.OnControllerTriggerDown += TestForApplication;
                Overlay.OnControllerHitsOverlay += MoveOverApplication;
                Overlay.OnControllerUnhitsOverlay += UnsetLastHit;
            }
        }
    }

    private bool _didHitOverlay;
    private bool _didHitOverlay2;
    private int _localWindowPosX;
    private int _localWindowPosY;
    private int _lastWindowPosX;
    private int _lastWindowPosY;

    private void MoveOverApplication(HOTK_Overlay o, HOTK_TrackedDevice tracker, HOTK_Overlay.IntersectionResults result)
    {
        if (SelectedWindow == IntPtr.Zero) return;
        if (SelectedWindowSettings.interactionMode == MouseInteractionMode.Disabled) return;
        if (Overlay.AnchorDevice == HOTK_Overlay.AttachmentDevice.LeftController && tracker.Type == HOTK_TrackedDevice.EType.LeftController) return;
        if (Overlay.AnchorDevice == HOTK_Overlay.AttachmentDevice.RightController && tracker.Type == HOTK_TrackedDevice.EType.RightController) return;
        CancelInvoke("HideCursor");
        if (_currentWindowWidth > 0 && _currentWindowHeight > 0)
        {
            var p = new Point((int) ((_currentWindowWidth + _RenderTextureMarginWidth) * result.UVs.x),
                              (int) ((_currentWindowHeight + _RenderTextureMarginHeight) * result.UVs.y));
            var v1 = new Vector3(-(_currentWindowWidth / 2f) + p.X - (_RenderTextureMarginWidth / 2f), (_currentWindowHeight / 2f) - p.Y + (_RenderTextureMarginHeight / 2f), -0.5f);
            var v2 = new Vector2((_currentWindowWidth / 2f) + v1.x + SelectedWindowSettings.offsetLeft, (_currentWindowHeight / 2f) - v1.y + SelectedWindowSettings.offsetTop);

            if ((int) v2.x == _lastWindowPosX && (int) v2.y == _lastWindowPosY) return;
            _lastWindowPosX = (int)v2.x;
            _lastWindowPosY = (int)v2.y;
            //Debug.Log("( " + v2.x + " / " + _currentWindowWidth + " ) x ( " + v2.y + " / " + _currentWindowHeight + " )");

            if (v2.x > 0 && v2.y > 0 && v2.x < _currentWindowWidth + SelectedWindowSettings.offsetLeft && v2.y < _currentWindowHeight + SelectedWindowSettings.offsetTop)
            {
                _didHitOverlay = true;
                if (SelectedWindowSettings.interactionMode == MouseInteractionMode.DirectInteraction ||
                    SelectedWindowSettings.interactionMode == MouseInteractionMode.WindowTop)
                    Win32Stuff.BringWindowToTop(SelectedWindow);

                if (SelectedWindowSettings.interactionMode == MouseInteractionMode.DirectInteraction)
                {
                    CursorInteraction.MoveOverWindow(SelectedWindow, new Point((int) v2.x, (int) v2.y));
                }else
                {
                    _localWindowPosX = (int)v2.x;
                    _localWindowPosY = (int)v2.y;
                }
                
                CursorGameObject.transform.localPosition = v1;
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

    IEnumerator FadeInOutline()
    {
        StopCoroutine("FadeOutOutline");
        ShowCursor();
        while (OutlineMaterial.color.a < 1f)
        {
            OutlineMaterial.color = new Color(OutlineMaterial.color.r, OutlineMaterial.color.b, OutlineMaterial.color.g, OutlineMaterial.color.a + 0.1f);
            if (CursorRenderer != null) CursorRenderer.color = new Color(CursorRenderer.color.r, CursorRenderer.color.b, CursorRenderer.color.g, OutlineMaterial.color.a);
            yield return new WaitForSeconds(0.025f);
        }
    }

    IEnumerator FadeOutOutline()
    {
        StopCoroutine("FadeInOutline");
        while (OutlineMaterial.color.a > 0f)
        {
            OutlineMaterial.color = new Color(OutlineMaterial.color.r, OutlineMaterial.color.b, OutlineMaterial.color.g, OutlineMaterial.color.a - 0.1f);
            if (CursorRenderer != null) CursorRenderer.color = new Color(CursorRenderer.color.r, CursorRenderer.color.b, CursorRenderer.color.g, OutlineMaterial.color.a);
            yield return new WaitForSeconds(0.025f);
        }
        HideCursor();
    }

    private void UnsetLastHit(HOTK_Overlay o, HOTK_TrackedDevice tracker)
    {
        _lastWindowPosX = -1;
        _lastWindowPosY = -1;
        _localWindowPosX = -1;
        _localWindowPosY = -1;
        _didHitOverlay = false;
        _didHitOverlay2 = false;
        StartCoroutine("FadeOutOutline");
    }

    private void ShowCursor()
    {
        CursorGameObject.SetActive(true);
    }

    private void HideCursor()
    {
        CursorGameObject.SetActive(false);
    }

    private void TestForApplication(HOTK_TrackedDevice tracker)
    {
        if (_didHitOverlay)
            _didHitOverlay2 = true;
    }

    private void ClickApplication(HOTK_TrackedDevice tracker, bool doubleClick)
    {
        if (SelectedWindow == IntPtr.Zero) return;
        if (SelectedWindowSettings.interactionMode == MouseInteractionMode.Disabled) return;
        if (!_didHitOverlay2) return;
        //Debug.Log("Try Click " + tracker.Type);
        if (SelectedWindow == IntPtr.Zero) return;
        //Debug.Log("Clicking" + tracker.Type);
        switch (SelectedWindowSettings.interactionMode)
        {
            case MouseInteractionMode.DirectInteraction:
                CursorInteraction.ClickOnPointAtCursor(SelectedWindow, doubleClick);
                break;
            case MouseInteractionMode.WindowTop:
            case MouseInteractionMode.SendClicksOnly:
                if (_localWindowPosX > -1 && _localWindowPosY > -1)
                    CursorInteraction.ClickOnPoint(SelectedWindow, new Point(_localWindowPosX, _localWindowPosY), doubleClick);
                else
                    Debug.LogWarning("X or Y missing");
                break;
        }
    }

    private void SingleClickApplication(HOTK_TrackedDevice tracker)
    {
        Debug.Log("Single Click");
        ClickApplication(tracker, false);
    }
    private void DoubleClickApplication(HOTK_TrackedDevice tracker)
    {
        Debug.Log("Double Click");
        ClickApplication(tracker, true);
    }

    public void ReleaseApplication(HOTK_TrackedDevice tracker)
    {
        Debug.Log("Try Release" + tracker.Type);
        if (SelectedWindow == IntPtr.Zero) return;
        CursorInteraction.ReleaseClick(SelectedWindow);
    }

    public void OnDisable()
    {
        StopCoroutine("CaptureWindow");
        SelectedWindow = IntPtr.Zero;
        SelectedWindowTitle = "";
        Overlay.OverlayTexture = DefaultTexture;
        if (DisplayQuad == null) return;
        DisplayQuad.GetComponent<Renderer>().material.mainTexture = DefaultTexture;
        DisplayQuad.transform.localScale = new Vector3(860f, 389f, 1f);
    }

    public void EnableFPSCounter()
    {
        FPSCounter.gameObject.SetActive(true);
        ResolutionDisplay.gameObject.SetActive(true);
       _showFPS = true;
    }

    private int _FPSCount;
    public void Update()
    {
        if (_showFPS)
        {
            if (FPSTimer.IsRunning)
            {
                if (FPSTimer.ElapsedMilliseconds >= 1000)
                {
                    FPSTimer.Reset();
                    FPSCounter.text = "FPS: " + _FPSCount;
                    var val = Overlay.Framerate != HOTK_Overlay.FramerateMode.AsFastAsPossible ? HOTK_Overlay.FramerateValues[(int)Overlay.Framerate] : -1;
                    if (val != -1)
                    {
                        if (_FPSCount < val)
                        {
                            FPSCounter.color = _FPSCount < val/2 ? Color.red : Color.yellow;
                        }
                        else FPSCounter.color = Color.white;
                    }
                    else FPSCounter.color = Color.white;

                    _FPSCount = 0;
                    FPSTimer.Start();
                }
            }
            else
            {
                FPSTimer.Start();
            }
            _FPSCount++;
        }
    }

    public void OnDestroy()
    {
        SaveLoad.Save();
        DisplayMaterial.mainTexture = DefaultTexture;
        //CaptureScreen.DeleteCopyContexts();
    }

    IEnumerator UpdateEvery1Second()
    {
        while (Application.isPlaying)
        {
            if (SelectedWindow != IntPtr.Zero)
            {
                MinimizedToggle.isOn = !Win32Stuff.IsIconic(SelectedWindow);
                if (!OffsetWidthField.isFocused && !OffsetHeightField.isFocused)
                {
                    var r = CaptureScreen.GetWindowRect(SelectedWindow);
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

    IEnumerator UpdateEvery10Seconds()
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
        Windows.Clear();
        Titles.Clear();
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
                    Windows.Add(copy == 0 ? title : string.Format("{0} ({1})", title, copy), w);
                    found = true;
                }
                catch (ArgumentException)
                {
                    copy++;
                }
            }

            Titles.Add(copy == 0 ? title : string.Format("{0} ({1})", title, copy));
            count++;
        }

        ApplicationDropdown.ClearOptions();
        ApplicationDropdown.AddOptions(Titles);
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
            if (SelectedWindow != IntPtr.Zero)
            {
                bool found = false;
                string name = null;

                foreach (var entry in Windows.Where(entry => entry.Value == SelectedWindow))
                {
                    name = entry.Key;
                    found = true;
                    break;
                }
                if (found)
                {
                    if (!string.IsNullOrEmpty(name))
                    {
                        for (var i = 0; i < ApplicationDropdown.options.Count; i++)
                        {
                            if (ApplicationDropdown.options[i].text != name) continue;
                            Debug.Log("Found " + SelectedWindowTitle + " by new name " + name);
                            _reselecting = true;
                            SelectedWindowTitle = name;
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
                SelectedWindow = IntPtr.Zero;
                SelectedWindowFullPath = string.Empty;
                //SelectedWindowPath = string.Empty;
                //SelectedWindowEXE = string.Empty;
                SelectedWindowSettings = null;

                ApplicationDropdown.captionText.text = count + " window(s) detected";
                Debug.Log("Found " + count + " windows");
            }
        }
    }

    private bool _reselecting;
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
        if (Windows.TryGetValue(SelectedWindowTitle, out window))
        {
            SelectedWindow = window;
            SelectedWindowFullPath = Win32Stuff.GetFilePath(SelectedWindow);
            if (!string.IsNullOrEmpty(SelectedWindowFullPath))
            {
                //int pos = SelectedWindowFullPath.LastIndexOfAny(new char[]{Path.PathSeparator, Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar}) + 1;
                //SelectedWindowPath = SelectedWindowFullPath.Substring(0, pos);
                //SelectedWindowEXE = SelectedWindowFullPath.Substring(pos);
                SelectedWindowSettings = LoadConfig(SelectedWindowFullPath);
                CaptureModeDropdown.SetToOption(DropdownMatchEnumOptions.CaptureModeNames[(int)SelectedWindowSettings.captureMode]);
            }
            else
            {
                Debug.LogWarning("Failed to grab path info. Settings might not work properly.");
                SelectedWindowFullPath = string.Empty;
                //SelectedWindowPath = string.Empty;
                //SelectedWindowEXE = string.Empty;
                SelectedWindowSettings = LoadConfig(SelectedWindowTitle);
                CaptureModeDropdown.SetToOption(DropdownMatchEnumOptions.CaptureModeNames[(int)SelectedWindowSettings.captureMode]);
            }
            //Debug.Log("Begin Capture " + SelectedWindowTitle);
            var r = CaptureScreen.GetWindowRect(SelectedWindow);
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
        if (SelectedWindow != IntPtr.Zero)
        {
            if (MinimizedToggle.isOn)
            {
                Win32Stuff.ShowWindow(SelectedWindow, ShowWindowCommands.Restore);
            }
            else
                Win32Stuff.ShowWindow(SelectedWindow, ShowWindowCommands.Minimize);
        }
    }

    public void ToggleSizeLocked()
    {
        if (SelectedWindow != IntPtr.Zero)
        {
            SelectedWindowSettings.windowSizeLocked = !SelectedWindowSettings.windowSizeLocked;
            SizeLockSprite.sprite = SelectedWindowSettings.windowSizeLocked ? LockSprite : UnlockSprite;
            if (SelectedWindowSettings.windowSizeLocked)
            {
                int v;
                if (int.TryParse(OffsetWidthField.text, out v)) { if (v > 0) { SelectedWindowSettings.offsetWidth = v;} }
                if (int.TryParse(OffsetHeightField.text, out v)) { if (v > 0) { SelectedWindowSettings.offsetHeight = v;} }
            }
        }
    }

    CaptureScreen.SIZE size;
    #pragma warning disable 0414
    Win32Stuff.WINDOWINFO info;
    #pragma warning restore 0414
    private bool wasDirect;

    IEnumerator CaptureWindow()
    {
        Overlay.OverlayTexture = RenderTexture;
        DisplayMaterial.mainTexture = texture;
        while (Application.isPlaying && SelectedWindow != IntPtr.Zero)
        {
            if (SelectedWindowSettings.captureMode == CaptureMode.GDIDirect)
            {
                if (!wasDirect)
                {
                    //CaptureScreen.DeleteCopyContexts();
                    wasDirect = true;
                }
                bitmap = CaptureScreen.CaptureWindowDirect(SelectedWindow, SelectedWindowSettings, out size, out info);
            }
            else if (SelectedWindowSettings.captureMode == CaptureMode.GDIIndirect)
            {
                if (wasDirect)
                {
                    //CaptureScreen.DeleteCopyContexts();
                    wasDirect = false;
                }
                bitmap = CaptureScreen.CaptureWindow(SelectedWindow, SelectedWindowSettings, out size, out info);
            }
            if (bitmap == null)
            {
                SelectedWindow = IntPtr.Zero;
                SelectedWindowFullPath = string.Empty;
                //SelectedWindowPath = string.Empty;
                //SelectedWindowEXE = string.Empty;
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
            bitmap.Save(stream, ImageFormat.Png);
            stream.Seek(0, SeekOrigin.Begin);
            texture.LoadImage(stream.ToArray());
            if (_currentWindowWidth != size.cx || _currentWindowHeight != size.cy)
            {
                _currentWindowWidth = size.cx;
                _currentWindowHeight = size.cy;
                ResolutionDisplay.text = string.Format("( {0} x {1} )", size.cx, size.cy);
                DisplayQuad.transform.localScale = new Vector3(size.cx, size.cy, 1f);
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

    // Cache these fractions so they aren't constantly recalculated
    private static readonly float[] FramerateFractions = new[]
    {
        1f, 1f/2f, 1f/5f, 1f/10f, 1/15f, 1/24f, 1f/30f, 1f/60f, 1f/90f, 1f/120f
    };

    public void WindowSettingConfirmed(string Setting)
    {
        if (SelectedWindow != IntPtr.Zero)
        {
            RECT r;
            int v;
            int val;
            switch (Setting)
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
                    r = CaptureScreen.GetWindowRect(SelectedWindow);
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
                    r = CaptureScreen.GetWindowRect(SelectedWindow);
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
    }

    public void SetWindowSize(RECT r)
    {
        CaptureScreen.SetWindowRect(SelectedWindow, r, SelectedWindowSettings.captureMode == CaptureMode.GDIIndirect);
    }

    public WindowSettings LoadConfig(string name)
    {
        WindowSettings settings;
        if (!SaveLoad.savedSettings.TryGetValue(name, out settings))
        {
            Debug.Log("Config [" + name + "] not found.");
            settings = new WindowSettings {SaveFileVersion = WindowSettings.CurrentSaveVersion};
            SaveLoad.savedSettings.Add(name, settings);
        }
        //else
        //{
        //    Debug.Log("Loading Config [" + name + "]. Version: " + settings.SaveFileVersion);
        //}

        if (settings.SaveFileVersion == 0)
        {
            Debug.Log("Upgrading [" + name + "] to SaveFileVersion 1.");
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
            Debug.Log("Upgrading [" + name + "] to SaveFileVersion 2.");
            settings.captureMode = settings.directMode ? CaptureMode.GDIDirect : CaptureMode.GDIIndirect;
            settings.interactionMode = MouseInteractionMode.DirectInteraction;
            settings.directMode = false;
            settings.windowSizeLocked = false;
            settings.SaveFileVersion = 2;
        }
        if (settings.SaveFileVersion == 2)
        {
            Debug.Log("Upgrading [" + name + "] to SaveFileVersion 3.");
            settings.framerateMode = HOTK_Overlay.FramerateMode._24FPS; // Compatibility because these values changed significantly. Default to 24FPS.
            settings.SaveFileVersion = 3;
        }

        OffsetLeftField.text = settings.offsetLeft.ToString();
        OffsetTopField.text = settings.offsetTop.ToString();
        OffsetRightField.text = settings.offsetRight.ToString();
        OffsetBottomField.text = settings.offsetBottom.ToString();
        SizeLockSprite.sprite = settings.windowSizeLocked ? LockSprite : UnlockSprite;
        CaptureModeDropdown.SetToOption(DropdownMatchEnumOptions.CaptureModeNames[(int)settings.captureMode], true);
        FramerateModeDropdown.SetToOption(DropdownMatchEnumOptions.FramerateModeNames[(int)settings.framerateMode], true);
        InteractionModeDropdown.SetToOption(DropdownMatchEnumOptions.MouseModeNames[(int)settings.interactionMode], true);
        return settings;
    }

    public enum CaptureMode
    {
        GDIDirect = 0,
        GDIIndirect = 1,
        ReplicationAPI = 2,
    }

    public enum MouseInteractionMode
    {
        DirectInteraction = 0,   // Keep Window on top, Move Cursor
        WindowTop = 1,           // Keep Window on top only, Send Mouse Clicks Only (No Move)
        SendClicksOnly = 2,      // Only Send Mouse Clicks
        Disabled = 3
    }
}


public static class SaveLoad
{
    public static Dictionary<string, WindowSettings> savedSettings = new Dictionary<string, WindowSettings>();
    
    public static void Save()
    {
        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Create(Application.persistentDataPath + "/savedSettings.gd");
        bf.Serialize(file, savedSettings);
        file.Close();
        Debug.Log("Saved " + savedSettings.Count + " config(s).");
    }

    public static void Load()
    {
        if (!File.Exists(Application.persistentDataPath + "/savedSettings.gd")) return;
        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Open(Application.persistentDataPath + "/savedSettings.gd", FileMode.Open);
        savedSettings = (Dictionary<string, WindowSettings>) bf.Deserialize(file);
        file.Close();
        Debug.Log("Loaded " + savedSettings.Count + " config(s).");
    }
}