using UnityEngine;
using UnityEngine.InputSystem;

public class Drill : MonoBehaviour
{
    [Header("Drill Damage Settings")]
    public float damagePerSecond = 4f;
    public float tickRate = 0.25f;

    [Header("Optional Settings")]
    public bool isActive = true;

    private float nextTickTime = 0f;    
    private bool isButtonHeld = false;

    public GameObject Collidingwith;

    void OnTriggerEnter(Collider other)
    {
        Collidingwith = other.gameObject;
    }

    void OnTriggerExit(Collider other)
    {
        Collidingwith = null;
    }

    void Update()
    {
        if (Mouse.current.leftButton.isPressed)
        {
            isButtonHeld = true;
        }
        else
        {
            isButtonHeld = false;
        }

        if (isActive && isButtonHeld && Time.time >= nextTickTime && Collidingwith != null)
        {
            float damagePerTick = damagePerSecond * tickRate;
            ApplyDamage(Collidingwith, damagePerTick);
            nextTickTime = Time.time + tickRate;
        }
    }

    protected void ApplyDamage(GameObject target, float damage)
    {
        IDamageable damageable = target.GetComponent<IDamageable>();
        if (damageable == null)
        {
            damageable = target.GetComponentInParent<IDamageable>();
        }

        if (damageable != null)
        {
            damageable.TakeDamage(damage);
        }
    }

    public void Activate()
    {
        isActive = true;
    }

    public void Deactivate()
    {
        isActive = false;
    }
}
public interface IDamageable
{
    void TakeDamage(float damage);
}