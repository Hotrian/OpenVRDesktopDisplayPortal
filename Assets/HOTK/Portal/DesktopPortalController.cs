using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using ScreenCapture;
using UnityEngine.UI;
using Color = UnityEngine.Color;
using Debug = UnityEngine.Debug;
using Image = UnityEngine.UI.Image;

public class DesktopPortalController : MonoBehaviour
{
    public Texture2D DefaultTexture;
    public Toggle HelpLabelToggle;
    public Toggle DirectCaptureToggle;
    public Toggle MinimizedToggle;

    public InputField OffsetXField;
    public InputField OffsetYField;
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

            StartCoroutine("UpdateUI");
            StartCoroutine("ReloadWindowList");
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

    void OnDestroy()
    {
        SaveLoad.Save();
        DisplayMaterial.mainTexture = DefaultTexture;
    }

    IEnumerator UpdateUI()
    {
        while (Application.isPlaying)
        {
            if (SelectedWindow != IntPtr.Zero)
            {
                MinimizedToggle.isOn = !Win32Stuff.IsIconic(SelectedWindow);
            }
            yield return new WaitForSeconds(1f);
        }
    }

    IEnumerator ReloadWindowList()
    {
        while (Application.isPlaying)
        {
            RefreshWindowList();
            yield return new WaitForSeconds(10f);
        }
    }

    public void StopRefreshing()
    {
        StopCoroutine("ReloadWindowList");
    }

    public void StartRefreshing()
    {
        StopCoroutine("ReloadWindowList");
        StartCoroutine("ReloadWindowList");
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
        
        if (!foundCurrent)
        {
            ApplicationDropdown.captionText.text = count + " window(s) detected";
            Debug.Log("Found " + count + " windows");
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
        StopCoroutine("Capture");
        StopCoroutine("CaptureWindow");
        SelectedWindowTitle = ApplicationDropdown.captionText.text;
        Debug.Log("Selected " + SelectedWindowTitle);

        IntPtr window;
        if (Windows.TryGetValue(SelectedWindowTitle, out window))
        {
            SelectedWindow = window;
            SelectedWindowFullPath = Win32Stuff.GetFilePath(SelectedWindow);
            int pos = SelectedWindowFullPath.LastIndexOfAny(new char[] {Path.PathSeparator, Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar}) + 1;
            SelectedWindowPath = SelectedWindowFullPath.Substring(0, pos);
            SelectedWindowEXE = SelectedWindowFullPath.Substring(pos);
            SelectedWindowSettings = LoadConfig(SelectedWindowFullPath);
            StartCoroutine("CaptureWindow");
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

    IEnumerator CaptureWindow()
    {
        Overlay.OverlayTexture = texture;
        DisplayMaterial.mainTexture = texture;
        while (Application.isPlaying && SelectedWindow != IntPtr.Zero)
        {
            if (SelectedWindowSettings.directMode)
            {
                bitmap = CaptureScreen.CaptureWindowDirect(SelectedWindow, SelectedWindowSettings, out size, out info);
            }
            else
            {
                bitmap = CaptureScreen.CaptureWindow(SelectedWindow, SelectedWindowSettings, out size, out info);
            }
            if (bitmap == null)
            {
                SelectedWindow = IntPtr.Zero;
                SelectedWindowFullPath = string.Empty;
                SelectedWindowPath = string.Empty;
                SelectedWindowEXE = string.Empty;
                SelectedWindowSettings = null;
                break;
            }
            else
            {
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
            int val;
            switch (Setting)
            {
                case "X":
                    if (int.TryParse(OffsetXField.text, out val))
                    {
                        SelectedWindowSettings.offsetX = val;
                    }
                    else OffsetXField.text = SelectedWindowSettings.offsetX.ToString();
                    break;
                case "Y":
                    if (int.TryParse(OffsetYField.text, out val))
                    {
                        SelectedWindowSettings.offsetY = val;
                    }
                    else OffsetYField.text = SelectedWindowSettings.offsetY.ToString();
                    break;
                case "Width":
                    if (int.TryParse(OffsetWidthField.text, out val))
                    {
                        SelectedWindowSettings.offsetWidth = val;
                    }
                    else OffsetWidthField.text = SelectedWindowSettings.offsetWidth.ToString();
                    break;
                case "Height":
                    if (int.TryParse(OffsetHeightField.text, out val))
                    {
                        SelectedWindowSettings.offsetHeight = val;
                    }
                    else OffsetHeightField.text = SelectedWindowSettings.offsetHeight.ToString();
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
    }

    public WindowSettings LoadConfig(string name)
    {
        WindowSettings settings;
        if (!SaveLoad.savedSettings.TryGetValue(name, out settings))
        {
            Debug.Log("Config not found.");
            settings = new WindowSettings();
            SaveLoad.savedSettings.Add(name, settings);
        }

        DirectCaptureToggle.isOn = settings.directMode;
        OffsetXField.text = settings.offsetX.ToString();
        OffsetYField.text = settings.offsetY.ToString();
        OffsetWidthField.text = settings.offsetWidth.ToString();
        OffsetHeightField.text = settings.offsetHeight.ToString();
        return settings;
    }
}


public static class SaveLoad
{
    public static Dictionary<string, WindowSettings> savedSettings = new Dictionary<string, WindowSettings>();

    //it's static so we can call it from anywhere
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
        if (File.Exists(Application.persistentDataPath + "/savedSettings.gd"))
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open(Application.persistentDataPath + "/savedSettings.gd", FileMode.Open);
            savedSettings = (Dictionary<string, WindowSettings>) bf.Deserialize(file);
            file.Close();
            Debug.Log("Loaded " + savedSettings.Count + " config(s).");
        }
    }
}