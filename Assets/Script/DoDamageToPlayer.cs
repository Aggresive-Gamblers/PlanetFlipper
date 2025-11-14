using Unity.VisualScripting;
using UnityEngine;

public class DoDamageToPlayer : MonoBehaviour
{
    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            other.gameObject.GetComponent<PlayerController>().TakeDamage(7,this.transform);
        }
    }
}
