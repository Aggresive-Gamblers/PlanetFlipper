using UnityEngine;

public class PlanetGravityTrigger : MonoBehaviour
{
    
    public float mult;
    public float multneg;
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            other.GetComponent<PlayerController>().EnterNewGravityField(transform,mult,multneg);
        }
    }
}
