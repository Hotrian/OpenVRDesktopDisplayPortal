/// <summary>
/// This class stores settings for each Target Application
/// </summary>
[System.Serializable]
public class WindowSettings
{
    public const uint CurrentSaveVersion = 5;

    public uint SaveFileVersion = 0;
    public bool directMode = true; // SaveFile Version 1 Compat
    public DesktopPortalController.CaptureMode captureMode = DesktopPortalController.CaptureMode.GdiDirect;
    public HOTK_Overlay.FramerateMode framerateMode = HOTK_Overlay.FramerateMode._24FPS;
    public UnityEngine.FilterMode filterMode = UnityEngine.FilterMode.Point;

    public DesktopPortalController.MouseInteractionMode interactionMode = DesktopPortalController.MouseInteractionMode.Disabled; // SaveFile Version <4 Compat
    public DesktopPortalController.ClickAPI clickAPI = DesktopPortalController.ClickAPI.SendInput;
    public bool clickForceWindowOnTop = true;
    public bool clickMoveDesktopCursor = true;
    public bool clickShowDesktopCursor = false;
    public bool clickDesktopCursorForceWindowOnTop = true;
    public bool clickDesktopCursorAutoHide = true;

    public DesktopPortalController.BacksideTexture backsideTexture = DesktopPortalController.BacksideTexture.Purple;

    public bool windowSizeLocked = false;
    public int offsetX = 0; // SaveFile Version 0 Compat
    public int offsetY = 0; // SaveFile Version 0 Compat
    public int offsetLeft = 0;
    public int offsetTop = 0;
    public int offsetRight = 0;
    public int offsetBottom = 0;
    public int offsetWidth = 0;
    public int offsetHeight = 0;
}