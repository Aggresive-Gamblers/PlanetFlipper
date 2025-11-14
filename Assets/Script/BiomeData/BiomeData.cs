using UnityEngine;

[CreateAssetMenu(fileName = "BiomeData", menuName = "Scriptable Objects/BiomeData")]
public class BiomeData : ScriptableObject
{
    [Header("Identification")]
    public string biomeName;

    [Header("Apparence")]
    public Gradient colorGradient;

    [Header("Terrain Parameters")]
    public int numberOfLevels;
    public float extrusionAmount;

    [Header("FastNoise Settings")]
    [Range(0.1f, 20f)]
    public float noiseSize;
    public FastNoiseLite.NoiseType noiseType;
    public FastNoiseLite.FractalType fractalType;
    [Range(1, 8)]
    public int octaves;
    [Range(0.001f, 0.1f)]
    public float frequency;
    [Range(1f, 4f)]
    public float lacunarity;
    [Range(0f, 1f)]
    public float gain;

    [Header("Vegetation")]
    public GameObject[] treePrefabs; 
    public bool spawnTrees = false;
    [Range(0, 10)]
    public int minElevationForTrees;
    [Range(0, 10)]
    public int maxElevationForTrees;
    [Range(0f, 1f)]
    public float treeDensity;
    [Range(0.1f, 20f)]
    public float treeDensityNoiseSize;
    [Range(0.1f, 20f)]
    public float treeTypeNoiseSize;

    [Header("anomaly")]
    public GameObject[] anomalyPrefab;
    public bool spawnAnomalies = false;
    [Range(0, 10)]
    public int minElevationForAnomalies;
    [Range(0, 10)]
    public int maxElevationForAnomalies;
    [Range(0f, 1f)]
    public float anomalyDensity;
    [Range(0.1f, 20f)]
    public float anomalyDensityNoiseSize;
    [Range(0.1f, 20f)]
    public float anomalyTypeNoiseSize;  
}
