using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using ScreenCapture;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;

public class DesktopPortalController : MonoBehaviour
{
    public Texture2D DefaultTexture;
    public Toggle HelpLabelToggle;
    public Toggle DirectCaptureToggle;
    public Toggle MinimizedToggle;

    public Text ToggleHelpTextHelpText;
    public Text DirectCaptureHelpText;
    public Text SelectApplicationHelpText;
    public Text ControlOffsetsHelpText;
    public Text ToggleMinimizedHelpText;
    public Image ToggleHelpTextHelpImage;
    public Image DirectCaptureHelpImage;
    public Image SelectApplicationHelpImage;
    public Image ControlOffsetsHelpImage;
    public Image ToggleMinimizedHelpImage;

    public InputField OffsetXField;
    public InputField OffsetYField;
    public InputField OffsetWidthField;
    public InputField OffsetHeightField;

    public HOTK_Overlay Overlay;
    public Material DisplayMaterial;
    public Dropdown ApplicationDropdown;
    public GameObject DisplayQuad;

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

    public void RefreshWindowList()
    {
        var windows = Win32Stuff.FindWindowsWithSize();
        Windows.Clear();
        Titles.Clear();
        string title;
        int count = 0;
        int copy = 0;
        bool found;
        foreach (var w in windows)
        {
            title = Win32Stuff.GetWindowText(w);
            if (title.Length > 0)
            {
                copy = 0;
                found = false;
                while (!found)
                {
                    try
                    {
                        if (copy == 0)
                            Windows.Add(title, w);
                        else
                            Windows.Add(string.Format("{0} ({1})", title, copy), w);

                        found = true;
                    }
                    catch (ArgumentException e)
                    {
                        copy++;
                    }
                }


                if (copy == 0)
                    Titles.Add(title);
                else
                    Titles.Add(string.Format("{0} ({1})", title, copy));
                count++;
            }
        }

        Debug.Log("Found " + count + " windows");

        ApplicationDropdown.ClearOptions();
        ApplicationDropdown.AddOptions(Titles);

        ApplicationDropdown.captionText.text = count + " window(s) detected";
    }

    public void OptionChanged()
    {
        StopCoroutine("Capture");
        StopCoroutine("CaptureWindow");
        SelectedWindowTitle = ApplicationDropdown.captionText.text;
        Debug.Log("Selected " + SelectedWindowTitle);

        IntPtr window;
        if (Windows.TryGetValue(SelectedWindowTitle, out window))
        {
            SelectedWindow = window;
            SelectedWindowFullPath = Win32Stuff.GetFilePath(SelectedWindow);
            int pos = SelectedWindowFullPath.LastIndexOfAny(new char[] { Path.PathSeparator, Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }) + 1;
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

    public void ToggleHelpLabels()
    {
        if (HelpLabelToggle == null) return;
        if (ToggleHelpTextHelpText != null) ToggleHelpTextHelpText.gameObject.SetActive(HelpLabelToggle.isOn);
        if (DirectCaptureHelpText != null) DirectCaptureHelpText.gameObject.SetActive(HelpLabelToggle.isOn);
        if (SelectApplicationHelpText != null) SelectApplicationHelpText.gameObject.SetActive(HelpLabelToggle.isOn);
        if (ControlOffsetsHelpText != null) ControlOffsetsHelpText.gameObject.SetActive(HelpLabelToggle.isOn);
        if (ToggleMinimizedHelpText != null) ToggleMinimizedHelpText.gameObject.SetActive(HelpLabelToggle.isOn);

        if (ToggleHelpTextHelpImage != null) ToggleHelpTextHelpImage.gameObject.SetActive(HelpLabelToggle.isOn);
        if (DirectCaptureHelpImage != null) DirectCaptureHelpImage.gameObject.SetActive(HelpLabelToggle.isOn);
        if (SelectApplicationHelpImage != null) SelectApplicationHelpImage.gameObject.SetActive(HelpLabelToggle.isOn);
        if (ControlOffsetsHelpImage != null) ControlOffsetsHelpImage.gameObject.SetActive(HelpLabelToggle.isOn);
        if (ToggleMinimizedHelpImage != null) ToggleMinimizedHelpImage.gameObject.SetActive(HelpLabelToggle.isOn);
    }

    // Update is called once per frame
    IEnumerator CaptureDesktop()
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
    }

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
                DisplayQuad.transform.localScale = new Vector3(size.cx / 100f, size.cy / 100f, 1f);
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
                    case HOTK_Overlay.FramerateMode._30FPS:
                        yield return new WaitForSeconds(FramerateFractions[4]);
                        break;
                    case HOTK_Overlay.FramerateMode._60FPS:
                        yield return new WaitForSeconds(FramerateFractions[5]);
                        break;
                    case HOTK_Overlay.FramerateMode._90FPS:
                        yield return new WaitForSeconds(FramerateFractions[6]);
                        break;
                    case HOTK_Overlay.FramerateMode._120FPS:
                        yield return new WaitForSeconds(FramerateFractions[7]);
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
        1f,
        1f/2f,
        1f/5f,
        1f/10f,
        1f/30f,
        1f/60f,
        1f/90f,
        1f/120f
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