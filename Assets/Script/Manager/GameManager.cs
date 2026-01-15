using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Progression par biome")]
    public int forestAnomaliesKilled = 0;
    public int tundraAnomaliesKilled = 0;
    public int desertAnomaliesKilled = 0;

    public string forestbiomeString = "";
    public string tundrabiomeString = "";
    public string desertbiomeString = "";

    [Header("Max Anomalies (Random)")]
    public int maxAnomaliesForest;
    public int maxAnomaliesTundra;
    public int maxAnomaliesDesert;

    public GameObject Jetpackfillbar;

    [Header("Unlock")]
    public bool jetpackUnlocked = false;
    public bool reservoirUpgradeUnlocked = false;
    public bool drillUnlocked = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            maxAnomaliesForest = Random.Range(1, 4);
            maxAnomaliesTundra = Random.Range(1, 4);
            maxAnomaliesDesert = Random.Range(1, 4);

            forestbiomeString = $"{forestAnomaliesKilled}/{maxAnomaliesForest}";
            tundrabiomeString = $"{tundraAnomaliesKilled}/{maxAnomaliesTundra}";
            desertbiomeString = $"{desertAnomaliesKilled}/{maxAnomaliesDesert}";
            
            Jetpackfillbar.SetActive(false);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void OnAnomalyKilled(string biomeType)
    {
        bool objectiveCompleted = false;

        switch (biomeType.ToLower())
        {
            case "forest":
                forestAnomaliesKilled++;
                forestbiomeString = $"{forestAnomaliesKilled}/{maxAnomaliesForest}";

                Debug.Log($"Progression : {forestAnomaliesKilled}/{maxAnomaliesForest}");
                if (forestAnomaliesKilled >= maxAnomaliesForest)
                {
                    objectiveCompleted = true;
                    jetpackUnlocked = true;
                    Jetpackfillbar.SetActive(true);
                    forestAnomaliesKilled = maxAnomaliesForest;
                    forestbiomeString = "You've unlocked the Jetpack Use [Shift] to Fly !";
                }
                break;
            case "tundra":
                tundraAnomaliesKilled++;
                tundrabiomeString = $"{tundraAnomaliesKilled}/{maxAnomaliesTundra}";

                Debug.Log($"Progression : {tundraAnomaliesKilled}/{maxAnomaliesTundra}");
                if (tundraAnomaliesKilled >= maxAnomaliesTundra)
                {
                    objectiveCompleted = true;
                    reservoirUpgradeUnlocked = true;
                    tundraAnomaliesKilled = maxAnomaliesTundra;
                    tundrabiomeString = "You've Double your reservoir capacity !";
                    ApplyDoubleReservoir();
                }
                break;
            case "desert":
                desertAnomaliesKilled++;
                desertbiomeString = $"{desertAnomaliesKilled}/{maxAnomaliesDesert}";

                Debug.Log($"Progression : {desertAnomaliesKilled}/{maxAnomaliesDesert}");
                if (desertAnomaliesKilled >= maxAnomaliesDesert)
                {
                    objectiveCompleted = true;
                    drillUnlocked = true;
                    desertAnomaliesKilled = maxAnomaliesDesert;
                    desertbiomeString = "You've upgraded your drill! It can now drill through even the most resistant anomalies!";
                }
                break;
            
            default:
                Debug.LogWarning($"Type de biome inconnu : {biomeType}");
                break;
        }

        UpdateDiaryUI();

        if (objectiveCompleted)
        {
            DestroyRemainingAnomalies(biomeType);
        }
    }

    private void DestroyRemainingAnomalies(string biomeType)
    {

        Anomaly[] allAnomalies = FindObjectsOfType<Anomaly>();

        int destroyedCount = 0;
        foreach (Anomaly anomaly in allAnomalies)
        {
            PlanetIdentifier planet = anomaly.GetComponentInParent<PlanetIdentifier>();
            if (planet != null && planet.GetBiomeType().ToLower() == biomeType.ToLower())
            {
                Destroy(anomaly.gameObject);
                destroyedCount++;
            }
        }
    }

    private void ApplyDoubleReservoir()
    {
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            player.JetpackReservoir *= 2f;
            player.JetpackFuel = player.JetpackReservoir; 
        }
    }

    private void UpdateDiaryUI()
    {
        Diary diary = FindObjectOfType<Diary>();
        if (diary != null)
        {
            diary.UpdateTexts();
        }
    }
}