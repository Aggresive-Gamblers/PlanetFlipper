using TMPro;
using UnityEngine;

public class Diary : MonoBehaviour
{
    [Header("Diary Settings")]
    public TextMeshProUGUI forestBiomeText;
    public TextMeshProUGUI tundraBiomeText;
    public TextMeshProUGUI desertBiomeText;

    public GameObject Canvas;


    void Start()
    {
        UpdateTexts();
    }

    public void UpdateTexts()
    {
        if (GameManager.Instance != null)
        {

            if (forestBiomeText != null)
            {
                forestBiomeText.text = GameManager.Instance.forestbiomeString;
            }

            if (tundraBiomeText != null)
            {
                tundraBiomeText.text = GameManager.Instance.tundrabiomeString;
            }

            if (desertBiomeText != null)
            {
                desertBiomeText.text = GameManager.Instance.desertbiomeString;
            }
        }
        else
        {
            Debug.LogWarning("Les textes ne peuvent pas être mis à jour.");
        }
    }

    public void OnOpenDiary()
    {
        UpdateTexts();
        Canvas.SetActive(true);
    }

    public void OnCloseDiary()
    {
        Canvas.SetActive(false);
    }
}