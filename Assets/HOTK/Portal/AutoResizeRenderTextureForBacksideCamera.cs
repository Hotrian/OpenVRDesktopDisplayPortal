using System.Collections;
using UnityEngine;

public class AutoResizeRenderTextureForBacksideCamera : MonoBehaviour
{
    public RenderTexture Texture;
    public AutoResizeCameraForRenderTexture script;
    public HOTK_CompanionOverlay BacksideOverlay;

    private int _width;
    private int _height;

    public void Update()
    {
        if (_width == DesktopPortalController.Instance.RenderTexture.width && _height == DesktopPortalController.Instance.RenderTexture.height) return;
        _width = DesktopPortalController.Instance.RenderTexture.width;
        _height = DesktopPortalController.Instance.RenderTexture.height;
        var aspect = (float)_height / (float)_width;
        ResizeRenderTexture((int) gameObject.transform.localScale.x, (int)(gameObject.transform.localScale.x * aspect));
    }

    public void ResizeRenderTexture(int width, int height)
    {
        var tex = script.Camera.targetTexture;
        Texture = new RenderTexture(width, height, 0);
        script.Camera.targetTexture = Texture;
        script.ResizeCamera(Texture, 2f);
        if (tex != null) tex.Release();
        script.Camera.enabled = true;
        BacksideOverlay.OverlayTexture = Texture;
        StartCoroutine(UpdateAfterFrame());
    }

    private IEnumerator UpdateAfterFrame()
    {
        yield return new WaitForEndOfFrame();
        BacksideOverlay.DoUpdateOverlay();
        script.Camera.enabled = false;
    }
}