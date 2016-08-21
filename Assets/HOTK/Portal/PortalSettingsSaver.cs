using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

[System.Serializable]
public class ProgramSettings
{
    public const uint CurrentSaveVersion = 1;

    public string LastProfile;
}
/// <summary>
/// This class stores settings for each 'Profile'
/// </summary>
[System.Serializable]
public class PortalSettings
{
    public const uint CurrentSaveVersion = 4;

    public uint SaveFileVersion;
    
    public float X, Y, Z;
    public float RX, RY, RZ;
    public HOTK_Overlay.AttachmentDevice Device;
    public HOTK_Overlay.AttachmentPoint Point;
    public HOTK_Overlay.AnimationType Animation;

    public float AlphaStart, AlphaEnd, AlphaSpeed;
    public float ScaleStart, ScaleEnd, ScaleSpeed;

    public bool ScreenOffsetPerformed;

    public float OutlineDefaultR;
    public float OutlineDefaultG;
    public float OutlineDefaultB;
    public float OutlineDefaultA;
    public float OutlineAimingR;
    public float OutlineAimingG;
    public float OutlineAimingB;
    public float OutlineAimingA;
    public float OutlineTouchingR;
    public float OutlineTouchingG;
    public float OutlineTouchingB;
    public float OutlineTouchingA;
    public float OutlineScalingR;
    public float OutlineScalingG;
    public float OutlineScalingB;
    public float OutlineScalingA;

    public DesktopPortalController.BacksideTexture Backside;
    public bool GrabEnabled;
    public bool ScaleEnabled;
}

public static class PortalSettingsSaver
{
    public static string ProgramSettingsFileName = Application.persistentDataPath + "/programSettings.gd";
    public static string ProfilesFileName = Application.persistentDataPath + "/savedProfiles.gd";

    public static string Current;
    public static ProgramSettings CurrentProgramSettings;
    public static Dictionary<string, PortalSettings> SavedProfiles = new Dictionary<string, PortalSettings>();

    public static void SaveProfiles(int mode = -1)
    {
        var bf = new BinaryFormatter();
        var file = File.Create(ProfilesFileName);
        bf.Serialize(file, SavedProfiles);
        file.Close();
        switch (mode)
        {
            case 1: // Legacy Savefile compatibility
                Debug.Log("Upgrading Legacy Profile Save Data."); // I don't think this is used anymore?
                break;
            case 2:
                Debug.Log("Profile deleted.");
                break;
            default:
                Debug.Log("Saved " + SavedProfiles.Count + " profile(s).");
                break;
        }
    }

    public static void LoadProgramSettings()
    {
        LoadProfiles();
        LoadSettings();
    }
    private static void LoadSettings()
    {
        if (!File.Exists(ProgramSettingsFileName)) return;
        var bf = new BinaryFormatter();
        var file = File.Open(ProgramSettingsFileName, FileMode.Open);
        CurrentProgramSettings = (ProgramSettings)bf.Deserialize(file);
        file.Close();

        if (SavedProfiles != null && SavedProfiles.Count > 0 && SavedProfiles.ContainsKey(CurrentProgramSettings.LastProfile))
        {
            Current = CurrentProgramSettings.LastProfile;
        }
        Debug.Log("Loaded program settings.");
    }

    public static void SaveProgramSettings()
    {
        if (CurrentProgramSettings == null)
        {
            CurrentProgramSettings = new ProgramSettings();
        }
        if (SavedProfiles != null && SavedProfiles.Count > 0 && SavedProfiles.ContainsKey(Current))
        {
            CurrentProgramSettings.LastProfile = Current;
        }

        var bf = new BinaryFormatter();
        var file = File.Create(ProgramSettingsFileName);
        bf.Serialize(file, CurrentProgramSettings);
        file.Close();
        Debug.Log("Saved program settings.");
    }

    public static void LoadProfiles()
    {
        if (!File.Exists(ProfilesFileName)) return;
        var bf = new BinaryFormatter();
        var file = File.Open(ProfilesFileName, FileMode.Open);
        SavedProfiles = (Dictionary<string, PortalSettings>)bf.Deserialize(file);
        file.Close();
        Debug.Log("Loaded " + SavedProfiles.Count + " profile(s).");
    }

    public static void DeleteProfile(string profileName = null)
    {
        if (profileName == null)
            profileName = Current;

        if (!SavedProfiles.ContainsKey(profileName)) return;

        if (!SavedProfiles.Remove(profileName)) return;
        if (profileName == Current)
            Current = null;
        SaveProfiles(2);
    }
}