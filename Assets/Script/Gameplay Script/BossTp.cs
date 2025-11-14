using UnityEngine;
using UnityEngine.SceneManagement;

public class BossTp : MonoBehaviour
{
    public void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            SceneManager.LoadScene("Boss");
        }
    }
}
