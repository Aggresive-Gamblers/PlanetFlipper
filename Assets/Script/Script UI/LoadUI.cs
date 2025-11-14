using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class LoadUI : MonoBehaviour
{
    public string levelSceneName = "Level";
    public string UISceneName = "UI";

    [Tooltip("Temps minimum de chargement en secondes")]
    public float minimumLoadTime = 2f;

    void Start()
    {
        levelSceneName = "Level" + LevelSelectionState.selectedLevelIndex.ToString();
        StartCoroutine(LoadScenesWithMinimumTime());
    }

    IEnumerator LoadScenesWithMinimumTime()
    {
        float startTime = Time.time;

        AsyncOperation asyncLoadLevel = SceneManager.LoadSceneAsync(levelSceneName, LoadSceneMode.Additive);
        AsyncOperation asyncLoadUI = SceneManager.LoadSceneAsync(UISceneName, LoadSceneMode.Additive);

        asyncLoadLevel.allowSceneActivation = false;
        asyncLoadUI.allowSceneActivation = false;

        while (asyncLoadLevel.progress < 0.9f || asyncLoadUI.progress < 0.9f)
        {
            float totalProgress = (asyncLoadLevel.progress + asyncLoadUI.progress) / 2f;
            yield return null;
        }

        float elapsedTime = Time.time - startTime;
        if (elapsedTime < minimumLoadTime)
        {
            yield return new WaitForSeconds(minimumLoadTime - elapsedTime);
        }

        asyncLoadLevel.allowSceneActivation = true;
        asyncLoadUI.allowSceneActivation = true;

        while (!asyncLoadLevel.isDone || !asyncLoadUI.isDone)
        {
            yield return null;
        }

        yield return new WaitForSeconds(0.1f);
        yield return SceneManager.UnloadSceneAsync(gameObject.scene);
    }
}