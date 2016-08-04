using UnityEngine;

public static class MultiModeController
{
    public static MonoBehaviour CurrentController
    {
        get { return _currentController; }
        set
        {
            if (_currentController != null)
                _currentController.enabled = false;
            _currentController = value;
            _currentController.enabled = true;
        }
    }

    private static MonoBehaviour _currentController;

    public static GameObject CurrentPanel
    {
        get { return _currentPanel; }
        set
        {
            if (_currentPanel != null)
            {
                _currentPanel.SetActive(false);
            }
            _currentPanel = value;
            _currentPanel.SetActive(true);
        }
    }

    private static GameObject _currentPanel;
}
