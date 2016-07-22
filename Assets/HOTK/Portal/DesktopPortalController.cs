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
    public Texture2D DefaultTexture;
    public Toggle HelpLabelToggle;
    public Toggle DirectCaptureToggle;
    public Toggle MinimizedToggle;

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

    public Text FPSCounter;
    public Text ResolutionDisplay;

    Dictionary<string, IntPtr> Windows = new Dictionary<string, IntPtr>();
    List<string> Titles = new List<string>();

    IntPtr SelectedWindow = IntPtr.Zero;
    string SelectedWindowFullPath = string.Empty;
    string SelectedWindowPath = string.Empty;
    string SelectedWindowEXE = string.Empty;
    WindowSettings SelectedWindowSettings = null;

    [HideInInspector]
    public string SelectedWindowTitle;

    Bitmap bitmap;
    Texture2D texture;
    MemoryStream stream;

    private bool _showFPS;
    private Stopwatch FPSTimer = new Stopwatch();
    private int _currentWindowWidth;
    private int _currentWindowHeight;

    public void Start ()
    {
        var ins = SteamVR.instance;
        if (Overlay != null)
        {
            bitmap = CaptureScreen.CaptureDesktop();
            texture = new Texture2D(bitmap.Width, bitmap.Height);
            texture.filterMode = FilterMode.Point;

            stream = new MemoryStream();
            bitmap.Save(stream, ImageFormat.Png);
            stream.Seek(0, SeekOrigin.Begin);

            Overlay.OverlayTexture = texture;

            texture.LoadImage(stream.ToArray());

            RefreshWindowList();

            SaveLoad.Load();

            StartCoroutine("UpdateEvery1Second");
            StartCoroutine("UpdateEvery10Seconds");
        }
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
                    var val = Overlay.Framerate != HOTK_Overlay.FramerateMode.AsFastAsPossible ? (int)Overlay.Framerate : -1;
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
        CaptureScreen.DeleteCopyContexts();
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
            if (title.Length > 0)
            {
                var copy = 0;
                var found = false;
                while (!found)
                {
                    try
                    {
                        Windows.Add(copy == 0 ? title : string.Format("{0} ({1})", title, copy), w);
                        found = true;
                    }
                    catch (ArgumentException e)
                    {
                        copy++;
                    }
                }

                Titles.Add(copy == 0 ? title : string.Format("{0} ({1})", title, copy));
                count++;
            }
        }

        ApplicationDropdown.ClearOptions();
        ApplicationDropdown.AddOptions(Titles);
        _reselecting = true;
        ApplicationDropdown.value = 0;

        bool foundCurrent = false;
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
                SelectedWindowPath = string.Empty;
                SelectedWindowEXE = string.Empty;
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
                int pos =
                    SelectedWindowFullPath.LastIndexOfAny(new char[]
                    {Path.PathSeparator, Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar}) + 1;
                SelectedWindowPath = SelectedWindowFullPath.Substring(0, pos);
                SelectedWindowEXE = SelectedWindowFullPath.Substring(pos);
                SelectedWindowSettings = LoadConfig(SelectedWindowFullPath);
            }
            else
            {
                Debug.LogWarning("Failed to grab path info. Settings might not work properly.");
                SelectedWindowFullPath = string.Empty;
                SelectedWindowPath = string.Empty;
                SelectedWindowEXE = string.Empty;
                SelectedWindowSettings = LoadConfig(SelectedWindowTitle);
            }
            var r = CaptureScreen.GetWindowRect(SelectedWindow);
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
    
    public void ToggleDirectMode()
    {
        if (SelectedWindow != IntPtr.Zero)
        {
            SelectedWindowSettings.directMode = DirectCaptureToggle.isOn;
        }
    }

    // Update is called once per frame
    /*IEnumerator CaptureDesktop()
    {
        Overlay.OverlayTexture = texture;
        DisplayMaterial.mainTexture = texture;
        while (Application.isPlaying)
        {
            bitmap = CaptureScreen.CaptureDesktop();
            bitmap.Save(stream, ImageFormat.Png);
            stream.Seek(0, SeekOrigin.Begin);
            texture.LoadImage(stream.ToArray());
            yield return new WaitForEndOfFrame();
        }
    }*/

    CaptureScreen.SIZE size;
    Win32Stuff.WINDOWINFO info;
    private bool wasDirect;

    IEnumerator CaptureWindow()
    {
        Overlay.OverlayTexture = texture;
        DisplayMaterial.mainTexture = texture;
        while (Application.isPlaying && SelectedWindow != IntPtr.Zero)
        {
            if (SelectedWindowSettings.directMode)
            {
                if (!wasDirect)
                {
                    CaptureScreen.DeleteCopyContexts();
                    wasDirect = true;
                }
                bitmap = CaptureScreen.CaptureWindowDirect(SelectedWindow, SelectedWindowSettings, out size, out info);
            }
            else
            {
                if (wasDirect)
                {
                    CaptureScreen.DeleteCopyContexts();
                    wasDirect = false;
                }
                bitmap = CaptureScreen.CaptureWindow(SelectedWindow, SelectedWindowSettings, out size, out info);
            }
            if (bitmap == null)
            {
                SelectedWindow = IntPtr.Zero;
                SelectedWindowFullPath = string.Empty;
                SelectedWindowPath = string.Empty;
                SelectedWindowEXE = string.Empty;
                SelectedWindowSettings = null;
                SelectedWindowTitle = null;
                _currentWindowWidth = 0;
                _currentWindowHeight = 0;
                ResolutionDisplay.text = "";
                DisplayQuad.transform.localScale = new Vector3(0f, 0f, 1f);
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
                DisplayQuad.transform.localScale = new Vector3(size.cx / 100f, size.cy / 100f, 1f);
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
                            SetWindowSize(r);
                        }
                        else
                        {
                            OffsetWidthField.text = r.Width.ToString();
                        }
                    }
                    else
                    {
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
                            SetWindowSize(r);
                        }
                        else
                        {
                            OffsetHeightField.text = r.Height.ToString();
                        }
                    }
                    else
                    {
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
        CaptureScreen.SetWindowRect(SelectedWindow, r, !SelectedWindowSettings.directMode);
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

        OffsetLeftField.text = settings.offsetLeft.ToString();
        OffsetTopField.text = settings.offsetTop.ToString();
        OffsetRightField.text = settings.offsetRight.ToString();
        OffsetBottomField.text = settings.offsetBottom.ToString();
        DirectCaptureToggle.isOn = settings.directMode;
        return settings;
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