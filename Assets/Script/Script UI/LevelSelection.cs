using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelSelection : MonoBehaviour
{
    public void LoadScene(string sceneName)
    { 
        SceneManager.LoadScene(sceneName);
    }

    public void SelectedLevelInt(int Level)
    {
        LevelSelectionState.selectedLevelIndex = Level;
    }

    public void ApplicationQuit()
    {
        Application.Quit();
    }
}
