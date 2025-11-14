using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class OpenLevelSelector : MonoBehaviour
{
    public string sceneName;
    public LoadSceneMode loadMode;

    void Update()
    {
        if (Keyboard.current.lKey.wasPressedThisFrame)
        {
            Scene scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.isLoaded)
            {
                SceneManager.LoadScene(sceneName, loadMode);
            }
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Scene scene = SceneManager.GetSceneByName(sceneName);
            if (scene.isLoaded)
            {
                SceneManager.UnloadSceneAsync(scene);
            }
        }
    }
}


