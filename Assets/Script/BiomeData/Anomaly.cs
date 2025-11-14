using UnityEngine;

public class Anomaly : MonoBehaviour, IDamageable
{
    [Header("Anomaly Health Settings")]
    public float maxHealth = 30f;
    private float currentHealth;

    private string biomeType;

    void Start()
    {
        currentHealth = maxHealth;

        DetectBiomeType();
    }

    void DetectBiomeType()
    {
        PlanetIdentifier planet = GetComponentInParent<PlanetIdentifier>();

        if (planet != null)
        {
            biomeType = planet.GetBiomeType();
            Debug.Log($"{gameObject.name} est sur la planète {biomeType}");
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} : Aucune planète avec PlanetIdentifier trouvée ! Biome par défaut: Forest");
            biomeType = "Forest";
        }
    }

    void Update()
    {

    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        Debug.Log($"{gameObject.name} a reçu {damage} dégâts. Vie restante : {currentHealth:F1}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnAnomalyKilled(biomeType);
        }
        else
        {
            Debug.LogWarning("GameManager introuvable ! Assurez-vous qu'il existe dans la scène.");
        }

        Destroy(gameObject);
    }
}