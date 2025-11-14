using UnityEngine;

public class PlanetIdentifier : MonoBehaviour
{
    [Header("Planet Type")]
    public string biomeType = "Forest"; 

    public string GetBiomeType()
    {
        return biomeType;
    }
}