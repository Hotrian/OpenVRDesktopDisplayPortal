[System.Serializable]
public class WindowSettings
{
    public const uint CurrentSaveVersion = 1;

    public uint SaveFileVersion = 0;
    public bool directMode = true;
    public int offsetX = 0; // SaveFile Version 0 Compat
    public int offsetY = 0; // SaveFile Version 0 Compat
    public int offsetLeft = 0;
    public int offsetTop = 0;
    public int offsetRight = 0;
    public int offsetBottom = 0;
    public int offsetWidth = 0;
    public int offsetHeight = 0;
}