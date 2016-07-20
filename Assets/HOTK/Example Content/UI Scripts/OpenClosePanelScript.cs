using UnityEngine;
using UnityEngine.UI;

public class OpenClosePanelScript : MonoBehaviour
{
    public bool isOpenButton;
    public Button MatchingButton;

    public GameObject[] Panels;

    public void DoClicked()
    {
        if (MatchingButton == null) return;
        TooltipController.Instance.SetTooltipText("");
        gameObject.SetActive(false);
        if (Panels != null && Panels.Length > 0)
        {
            foreach (var panel in Panels)
            {
                panel.SetActive(isOpenButton);
            }
        }
        MatchingButton.gameObject.SetActive(true);
    }
}
