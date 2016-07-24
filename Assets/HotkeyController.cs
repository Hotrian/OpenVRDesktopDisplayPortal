using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class HotkeyController : MonoBehaviour
{
    public InputField NewSaveInputField;

    public Button XNegButton;
    public Button XPosButton;
    public Button YNegButton;
    public Button YPosButton;
    public Button ZNegButton;
    public Button ZPosButton;

    public Button RXNegButton;
    public Button RXPosButton;
    public Button RYNegButton;
    public Button RYPosButton;
    public Button RZNegButton;
    public Button RZPosButton;
    void Update ()
    {
        if (!NewSaveInputField.isFocused)
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                XNegButton.onClick.Invoke();
            }
            else if (Input.GetKeyDown(KeyCode.W))
            {
                XPosButton.onClick.Invoke();
            }
            if (Input.GetKeyDown(KeyCode.A))
            {
                YNegButton.onClick.Invoke();
            }
            else if (Input.GetKeyDown(KeyCode.S))
            {
                YPosButton.onClick.Invoke();
            }
            if (Input.GetKeyDown(KeyCode.Z))
            {
                ZNegButton.onClick.Invoke();
            }
            else if (Input.GetKeyDown(KeyCode.X))
            {
                ZPosButton.onClick.Invoke();
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                RXNegButton.onClick.Invoke();
            }
            else if (Input.GetKeyDown(KeyCode.R))
            {
                RXPosButton.onClick.Invoke();
            }
            if (Input.GetKeyDown(KeyCode.D))
            {
                RYNegButton.onClick.Invoke();
            }
            else if (Input.GetKeyDown(KeyCode.F))
            {
                RYPosButton.onClick.Invoke();
            }
            if (Input.GetKeyDown(KeyCode.C))
            {
                RZNegButton.onClick.Invoke();
            }
            else if (Input.GetKeyDown(KeyCode.V))
            {
                RZPosButton.onClick.Invoke();
            }
        }
    }
}
