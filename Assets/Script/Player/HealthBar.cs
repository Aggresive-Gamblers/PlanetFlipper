using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class HealthBar : MonoBehaviour
{
    public Image HealthFill; 
    public float maxHealth = 100;
    public float currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();
    }

    void Update()
    {
        CheckDamage();
        UpdateHealthUI();

        if(Keyboard.current.hKey.wasPressedThisFrame)
        {
            TakeDamage(10);
        }
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        Debug.Log("Damage Taken");

        if (currentHealth > maxHealth)
            currentHealth = maxHealth;

        if (currentHealth < 0)
            currentHealth = 0;

        UpdateHealthUI();
    }

    public void CheckDamage()
    {
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        Debug.Log("Dying");
    }

    public void ResetHP()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();
    }

    private void UpdateHealthUI()
    {
        if (HealthFill != null)
        {
            HealthFill.fillAmount = currentHealth / maxHealth;
        }
    }
}
