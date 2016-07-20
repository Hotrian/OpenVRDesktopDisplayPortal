using UnityEngine;

public class DisableOnEventTrigger : MonoBehaviour
{
    public void DoDisable()
    {
        if (gameObject.activeSelf)
            gameObject.SetActive(false);
    }
    public void DoEnable()
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);
    }
}
