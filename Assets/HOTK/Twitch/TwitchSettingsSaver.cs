using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

[System.Serializable]
public class TwitchSettings
{
    public const uint CurrentSaveVersion = 4;

    public uint SaveFileVersion;

    public string Username;
    public string Channel;
    public float X, Y, Z;
    public float RX, RY, RZ;
    public string ChatSound;
    public float Volume, Pitch;
    public string FollowerSound;
    public float FollowerVolume, FollowerPitch;
    public HOTK_Overlay.AttachmentDevice Device;
    public HOTK_Overlay.AttachmentPoint Point;
    public HOTK_Overlay.AnimationType Animation;

    public float BackgroundR, BackgroundG, BackgroundB, BackgroundA;

    public float AlphaStart, AlphaEnd, AlphaSpeed;
    public float ScaleStart, ScaleEnd, ScaleSpeed;
}

public static class TwitchSettingsSaver
{
    public static string ProgramSettingsFileName = Application.persistentDataPath + "/programSettings.gd";
    public static string ProfilesFileName = Application.persistentDataPath + "/savedProfiles.gd";
    public static string OriginalProfilesFileName = Application.persistentDataPath + "/savedSettings.gd"; // Legacy Savefile compatibility

    public static string Current;
    public static ProgramSettings CurrentProgramSettings;
    public static Dictionary<string, TwitchSettings> SavedProfiles = new Dictionary<string, TwitchSettings>();
    
    public static void SaveProfiles(int mode = -1)
    {
        var bf = new BinaryFormatter();
        var file = File.Create(ProfilesFileName);
        bf.Serialize(file, SavedProfiles);
        file.Close();
        switch (mode)
        {
            case 1: // Legacy Savefile compatibility
                TwitchChatTester.Instance.AddSystemNotice("Upgrading Legacy Profile Save Data.");
                break;
            case 2:
                TwitchChatTester.Instance.AddSystemNotice("Profile deleted.");
                break;
            default:
                TwitchChatTester.Instance.AddSystemNotice("Saved " + SavedProfiles.Count + " profile(s).");
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
        TwitchChatTester.Instance.AddSystemNotice("Loaded program settings.");
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
        TwitchChatTester.Instance.AddSystemNotice("Saved program settings.");
    }

    public static void LoadProfiles()
    {
        bool legacy = false;
        var filename = ProfilesFileName;
        if (!File.Exists(filename)) // Legacy Savefile compatibility
        {
            filename = OriginalProfilesFileName;
            if (!File.Exists(filename)) return;
            legacy = true;
            TwitchChatTester.Instance.AddSystemNotice("Found Legacy Profile Save Data: " + filename);
        }
        var bf = new BinaryFormatter();
        var file = File.Open(filename, FileMode.Open);
        SavedProfiles = (Dictionary<string, TwitchSettings>)bf.Deserialize(file);
        file.Close();
        TwitchChatTester.Instance.AddSystemNotice("Loaded " + SavedProfiles.Count + " profile(s).");

        if (!legacy) return; // Legacy Savefile compatibility
        File.Move(OriginalProfilesFileName, OriginalProfilesFileName + ".bak");
        SaveProfiles(1);
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