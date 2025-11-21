using UnityEngine;

public class MainMenuUI : MonoBehaviour
{
    public GameObject binds;

    public void OnenableUI()
    {
        binds.SetActive(true);
    }

    public void OnEchapBinds()
    {
        if (binds.activeSelf)
        {
            binds.SetActive(false);
        }
        else
        {
            binds.SetActive(true);
        }

    }
}
