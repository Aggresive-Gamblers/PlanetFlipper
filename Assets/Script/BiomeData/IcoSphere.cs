using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(MeshFilter),typeof(MeshCollider), typeof(MeshRenderer))]
public class IcoSphere : MonoBehaviour
{
    [Header("Biome")]
    public BiomeData biome;

    [Header("Planet Settings")]
    [Range(0, 6)]
    public int subdivisions = 3;
    [Range(0.1f, 10f)]
    public float radius = 5f;

    [Header("Seed Settings")]
    public bool randomSeedOnStart = true;
    public int noiseSeed = 1337;

    [Header("Extrusion Settings")]
    public bool applyExtrusion = true;

    [Header("Debug Wireframe")]
    public bool showWireframe;
    public Color wireframeColor = Color.black;

    public bool IsDebugMovement;
    
    private Mesh mesh;
    public List<Vector3> vertices;
    public List<int> triangles;
    private List<Color> vertexColors;
    private Dictionary<long, int> middlePointIndexCache;
    private FastNoiseLite noise;
    private FastNoiseLite treeDensityNoise;
    private FastNoiseLite treeTypeNoise;
    private FastNoiseLite anomalyTypeNoise;
    private FastNoiseLite anomalyDensityNoise;
    private Transform treesContainer;

    private List<TriangleInfo> originalTrianglesInfo = new List<TriangleInfo>();

    private bool lastSpawnTreesState = false;
    private int lastMinElevation = -1;
    private int lastMaxElevation = -1;
    private float lastTreeDensity = -1;
    private float lastTreeDensityNoiseSize = -1;
    private float lastTreeTypeNoiseSize = -1;

    private Transform anomaliesContainer;
    private bool lastSpawnAnomalyState = false;
    private int lastMinAnomaly = -1;
    private int lastMaxAnomaly = -1;
    private float lastAnomalyDensity = -1f;
    private float lastAnomalyDensityNoiseSize = -1f;
    private float lastAnomalyTypeNoiseSize = -1f;

    private int lastSubdivisions = -1;
    private float lastRadius = -1;
    private bool lastApplyExtrusion = false;
    private int lastNumberOfLevels = -1;
    private float lastExtrusionAmount = -1;
    private int lastNoiseSeed = -1;
    private float lastNoiseSize = -1;
    private FastNoiseLite.NoiseType lastNoiseType = FastNoiseLite.NoiseType.OpenSimplex2;
    private FastNoiseLite.FractalType lastFractalType = FastNoiseLite.FractalType.FBm;
    private int lastOctaves = -1;
    private float lastFrequency = -1;
    private float lastLacunarity = -1;
    private float lastGain = -1;

    public class Triangle
    {
        public int v1, v2, v3;
        public int elevationLevel;
        public Vector3 normal;
        public Vector3 center;

        public Triangle(int v1, int v2, int v3)
        {
            this.v1 = v1;
            this.v2 = v2;
            this.v3 = v3;
        }
    }

    private class TriangleInfo
    {
        public Vector3 center;
        public Vector3 v1, v2, v3;
        public int elevationLevel;
        public Vector3 normal;
    }

    void Start()
    {
        if (biome == null)
        {
            Debug.LogError("Aucun biome assigné ! Veuillez assigner un BiomeData.");
            return;
        }

        if (randomSeedOnStart)
        {
            noiseSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            Debug.Log($"Nouvelle seed générée : {noiseSeed}");
        }

        GenerateIcoSphere();

        if (biome.spawnTrees)
        {
            lastSpawnTreesState = biome.spawnTrees;
            lastMinElevation = biome.minElevationForTrees;
            lastMaxElevation = biome.maxElevationForTrees;
            lastTreeDensity = biome.treeDensity;
            lastTreeDensityNoiseSize = biome.treeDensityNoiseSize;
            lastTreeTypeNoiseSize = biome.treeTypeNoiseSize;
        }
        if(biome.spawnAnomalies)
        {
            lastSpawnAnomalyState = biome.spawnAnomalies;
            lastMinAnomaly = biome.minElevationForAnomalies;
            lastMaxAnomaly = biome.maxElevationForAnomalies;
            lastAnomalyDensity = biome.anomalyDensity;
            lastAnomalyDensityNoiseSize = biome.anomalyDensityNoiseSize;
            lastAnomalyTypeNoiseSize = biome.anomalyTypeNoiseSize;
        }

        InitializeTerrainTracking();
    }

    private void InitializeTerrainTracking()
    {
        if (biome == null) return;

        lastSubdivisions = subdivisions;
        lastRadius = radius;
        lastApplyExtrusion = applyExtrusion;
        lastNumberOfLevels = biome.numberOfLevels;
        lastExtrusionAmount = biome.extrusionAmount;
        lastNoiseSeed = noiseSeed;
        lastNoiseSize = biome.noiseSize;
        lastNoiseType = biome.noiseType;
        lastFractalType = biome.fractalType;
        lastOctaves = biome.octaves;
        lastFrequency = biome.frequency;
        lastLacunarity = biome.lacunarity;
        lastGain = biome.gain;
    }

    private void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.isPressed && IsDebugMovement)
        {
            Vector2 delta = Mouse.current.delta.ReadValue();
            transform.Rotate(Vector3.up, -delta.x * 0.5f, Space.World);
            transform.Rotate(Vector3.right, delta.y * 0.5f, Space.World);
        }

        if (biome != null)
        {
            CheckTreeParametersChanged();
        }
    }

    private bool HasTerrainParametersChanged()
    {
        if (biome == null) return false;

        return subdivisions != lastSubdivisions ||
               Mathf.Abs(radius - lastRadius) > 0.001f ||
               applyExtrusion != lastApplyExtrusion ||
               biome.numberOfLevels != lastNumberOfLevels ||
               Mathf.Abs(biome.extrusionAmount - lastExtrusionAmount) > 0.001f ||
               noiseSeed != lastNoiseSeed ||
               Mathf.Abs(biome.noiseSize - lastNoiseSize) > 0.001f ||
               biome.noiseType != lastNoiseType ||
               biome.fractalType != lastFractalType ||
               biome.octaves != lastOctaves ||
               Mathf.Abs(biome.frequency - lastFrequency) > 0.001f ||
               Mathf.Abs(biome.lacunarity - lastLacunarity) > 0.001f ||
               Mathf.Abs(biome.gain - lastGain) > 0.001f;
    }

    private void CheckTreeParametersChanged()
    {
        if (biome == null) return;

        bool parametersChanged = false;
        bool needToRespawn = false;
        bool needToClear = false;

        if (biome.spawnTrees != lastSpawnTreesState)
        {
            if (biome.spawnTrees)
            {
                needToRespawn = true;
            }
            else
            {
                needToClear = true;
            }
            lastSpawnTreesState = biome.spawnTrees;
            parametersChanged = true;
        }

        if (biome.spawnTrees && !parametersChanged)
        {
            if (biome.minElevationForTrees != lastMinElevation ||
                biome.maxElevationForTrees != lastMaxElevation ||
                Mathf.Abs(biome.treeDensity - lastTreeDensity) > 0.001f ||
                Mathf.Abs(biome.treeDensityNoiseSize - lastTreeDensityNoiseSize) > 0.001f ||
                Mathf.Abs(biome.treeTypeNoiseSize - lastTreeTypeNoiseSize) > 0.001f)
            {
                needToRespawn = true;
                lastMinElevation = biome.minElevationForTrees;
                lastMaxElevation = biome.maxElevationForTrees;
                lastTreeDensity = biome.treeDensity;
                lastTreeDensityNoiseSize = biome.treeDensityNoiseSize;
                lastTreeTypeNoiseSize = biome.treeTypeNoiseSize;
            }
        }

        if (needToClear)
        {
            ClearTrees();
        }
        else if (needToRespawn && biome.treePrefabs != null && biome.treePrefabs.Length > 0)
        {
            SpawnTrees();
        }
    }

    private void ClearTrees()
    {
        List<Transform> oldContainers = new List<Transform>();
        foreach (Transform child in transform)
        {
            if (child.name == "Trees")
                oldContainers.Add(child);
        }

        foreach (Transform old in oldContainers)
        {
            if (Application.isPlaying)
                Destroy(old.gameObject);
            else
                DestroyImmediate(old.gameObject);
        }

        treesContainer = null;
    }

    public FastNoiseLite GenerateNoise()
    {
        if (biome == null) return null;

        noise = new FastNoiseLite(noiseSeed);
        noise.SetNoiseType(biome.noiseType);
        noise.SetFractalType(biome.fractalType);
        noise.SetFractalOctaves(biome.octaves);
        noise.SetFrequency(biome.frequency);
        noise.SetFractalLacunarity(biome.lacunarity);
        noise.SetFractalGain(biome.gain);

        return noise;
    }

    public void GenerateIcoSphere()
    {
        if (biome == null)
        {
            Debug.LogError("Aucun biome assigné !");
            return;
        }

        noise = GenerateNoise();

        treeDensityNoise = new FastNoiseLite(noiseSeed + 1000);
        treeDensityNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        treeDensityNoise.SetFrequency(0.05f);

        treeTypeNoise = new FastNoiseLite(noiseSeed + 2000);
        treeTypeNoise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
        treeTypeNoise.SetFrequency(0.03f);

        anomalyDensityNoise = new FastNoiseLite(noiseSeed + 3000);
        anomalyDensityNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        anomalyDensityNoise.SetFrequency(0.05f);

        anomalyTypeNoise = new FastNoiseLite(noiseSeed + 4000);
        anomalyTypeNoise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
        anomalyTypeNoise.SetFrequency(0.03f);

        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshCollider>().sharedMesh = mesh;
        

        vertices = new List<Vector3>();
        triangles = new List<int>();
        vertexColors = new List<Color>();
        middlePointIndexCache = new Dictionary<long, int>();
        originalTrianglesInfo.Clear();

        GenerateIcosahedron();

        for (int i = 0; i < subdivisions; i++)
        {
            SubdivideTriangles();
        }

        for (int i = 0; i < vertices.Count; i++)
        {
            vertices[i] = vertices[i].normalized * radius;
        }

        if (applyExtrusion)
        {
            ApplyExtrusion();
        }

        Debug.Log($"Vertices: {vertices.Count}, VertexColors: {vertexColors.Count}");

        UpdateMesh();

        if (biome.spawnTrees)
        {
            SpawnTrees();
        }

        if (biome.spawnAnomalies)
        {
            SpawnAnomalies();
        }
    }

    private void ApplyExtrusion()
    {
        if (biome == null) return;

        List<Triangle> triangleList = new List<Triangle>();
        Dictionary<int, int> levelCount = new Dictionary<int, int>();

        for (int i = 0; i < triangles.Count; i += 3)
        {
            Triangle tri = new Triangle(triangles[i], triangles[i + 1], triangles[i + 2]);
            tri.center = (vertices[tri.v1] + vertices[tri.v2] + vertices[tri.v3]) / 3f;

            Vector3 n1 = vertices[tri.v1].normalized;
            Vector3 n2 = vertices[tri.v2].normalized;
            Vector3 n3 = vertices[tri.v3].normalized;
            tri.normal = ((n1 + n2 + n3) / 3f).normalized;

            Vector3 localPos = tri.center;

            float seedOffset = noiseSeed * 0.12345f;

            float noiseValue = SampleSphericalNoise(localPos, biome.noiseSize, noiseSeed);
            noiseValue = (noiseValue + 1f) * 0.5f;

            tri.elevationLevel = Mathf.RoundToInt(noiseValue * biome.numberOfLevels);
            tri.elevationLevel = Mathf.Clamp(tri.elevationLevel, 0, biome.numberOfLevels);

            if (!levelCount.ContainsKey(tri.elevationLevel))
                levelCount[tri.elevationLevel] = 0;
            levelCount[tri.elevationLevel]++;

            triangleList.Add(tri);

            TriangleInfo info = new TriangleInfo
            {
                center = tri.center,
                v1 = vertices[tri.v1],
                v2 = vertices[tri.v2],
                v3 = vertices[tri.v3],
                elevationLevel = tri.elevationLevel,
                normal = tri.normal
            };
            originalTrianglesInfo.Add(info);
        }

        string distribution = "Elevation distribution: ";
        foreach (var kvp in levelCount.OrderBy(x => x.Key))
            distribution += $"Level {kvp.Key}: {kvp.Value} triangles | ";
        Debug.Log(distribution);

        Dictionary<int, List<Triangle>> vertexToTriangles = new Dictionary<int, List<Triangle>>();
        foreach (Triangle tri in triangleList)
        {
            if (!vertexToTriangles.ContainsKey(tri.v1)) vertexToTriangles[tri.v1] = new List<Triangle>();
            if (!vertexToTriangles.ContainsKey(tri.v2)) vertexToTriangles[tri.v2] = new List<Triangle>();
            if (!vertexToTriangles.ContainsKey(tri.v3)) vertexToTriangles[tri.v3] = new List<Triangle>();

            vertexToTriangles[tri.v1].Add(tri);
            vertexToTriangles[tri.v2].Add(tri);
            vertexToTriangles[tri.v3].Add(tri);
        }

        Dictionary<long, int> extrudedVertexCache = new Dictionary<long, int>();
        List<Vector3> newVertices = new List<Vector3>();
        List<Color> newColors = new List<Color>();

        System.Func<int, int, int> GetOrCreateExtrudedVertex = (int originalVertexIndex, int elevationLevel) =>
        {
            long key = ((long)originalVertexIndex << 32) | (long)elevationLevel;
            if (extrudedVertexCache.TryGetValue(key, out int cached))
                return cached;

            Vector3 originalPos = vertices[originalVertexIndex];
            List<Triangle> adjacentSameLevel = vertexToTriangles[originalVertexIndex]
                .Where(t => t.elevationLevel == elevationLevel)
                .ToList();

            Vector3 averageNormal = Vector3.zero;
            foreach (Triangle adjTri in adjacentSameLevel)
                averageNormal += adjTri.normal;

            if (averageNormal == Vector3.zero)
                averageNormal = originalPos.normalized;
            else
                averageNormal.Normalize();

            float extrusionDistance = elevationLevel * biome.extrusionAmount;
            Vector3 newPos = originalPos + averageNormal * extrusionDistance;

            // Utiliser le gradient du biome au lieu du grayscale
            float normalizedElevation = biome.numberOfLevels > 0 ? (float)elevationLevel / (float)biome.numberOfLevels : 0f;
            Color vertexColor = biome.colorGradient.Evaluate(normalizedElevation);

            int newIndex = newVertices.Count;
            newVertices.Add(newPos);
            newColors.Add(vertexColor);
            extrudedVertexCache[key] = newIndex;
            return newIndex;
        };

        List<int> newTriangles = new List<int>();

        foreach (Triangle tri in triangleList)
        {
            int idx1 = GetOrCreateExtrudedVertex(tri.v1, tri.elevationLevel);
            int idx2 = GetOrCreateExtrudedVertex(tri.v2, tri.elevationLevel);
            int idx3 = GetOrCreateExtrudedVertex(tri.v3, tri.elevationLevel);

            newTriangles.Add(idx1);
            newTriangles.Add(idx3);
            newTriangles.Add(idx2);
        }

        Dictionary<long, List<(Triangle tri, int v1, int v2)>> edgeToTriangles = new Dictionary<long, List<(Triangle, int, int)>>();

        foreach (Triangle tri in triangleList)
        {
            int[][] edges = new int[][]
            {
                new int[] { tri.v1, tri.v2 },
                new int[] { tri.v2, tri.v3 },
                new int[] { tri.v3, tri.v1 }
            };

            foreach (int[] edge in edges)
            {
                long edgeKey = ((long)Mathf.Min(edge[0], edge[1]) << 32) | (long)Mathf.Max(edge[0], edge[1]);
                if (!edgeToTriangles.ContainsKey(edgeKey))
                    edgeToTriangles[edgeKey] = new List<(Triangle, int, int)>();

                edgeToTriangles[edgeKey].Add((tri, edge[0], edge[1]));
            }
        }

        HashSet<(int, int, int, int)> addedWalls = new HashSet<(int, int, int, int)>();

        foreach (var kvp in edgeToTriangles)
        {
            var trisOnEdge = kvp.Value;
            if (trisOnEdge.Count < 2)
                continue;

            var (tri1, v1a, v2a) = trisOnEdge[0];
            var (tri2, v1b, v2b) = trisOnEdge[1];
            if (tri1.elevationLevel == tri2.elevationLevel)
                continue;

            Triangle lowTri = tri1.elevationLevel < tri2.elevationLevel ? tri1 : tri2;
            Triangle highTri = tri1.elevationLevel < tri2.elevationLevel ? tri2 : tri1;

            int v1 = (lowTri == tri1) ? v1a : v1b;
            int v2 = (lowTri == tri1) ? v2a : v2b;

            int lowLevel = lowTri.elevationLevel;
            int highLevel = highTri.elevationLevel;

            for (int level = lowLevel; level < highLevel; level++)
            {
                var wallKey = (Mathf.Min(v1a, v2a), Mathf.Max(v1a, v2a), level, level + 1);
                if (addedWalls.Contains(wallKey))
                    continue;
                addedWalls.Add(wallKey);

                int idxA_low = GetOrCreateExtrudedVertex(v1, level);
                int idxB_low = GetOrCreateExtrudedVertex(v2, level);
                int idxA_high = GetOrCreateExtrudedVertex(v1, level + 1);
                int idxB_high = GetOrCreateExtrudedVertex(v2, level + 1);

                float normalizedElevation = biome.numberOfLevels > 0 ? (float)level / (float)biome.numberOfLevels : 0f;
                Color wallColor = biome.colorGradient.Evaluate(normalizedElevation);

                int quadV1 = newVertices.Count;
                newVertices.Add(newVertices[idxA_low]);
                newColors.Add(wallColor);

                int quadV2 = newVertices.Count;
                newVertices.Add(newVertices[idxB_low]);
                newColors.Add(wallColor);

                int quadV3 = newVertices.Count;
                newVertices.Add(newVertices[idxA_high]);
                newColors.Add(wallColor);

                int quadV4 = newVertices.Count;
                newVertices.Add(newVertices[idxB_high]);
                newColors.Add(wallColor);

                newTriangles.Add(quadV1);
                newTriangles.Add(quadV2);
                newTriangles.Add(quadV3);

                newTriangles.Add(quadV2);
                newTriangles.Add(quadV4);
                newTriangles.Add(quadV3);
            }
        }

        vertices = newVertices;
        triangles = newTriangles;
        vertexColors = newColors;

        mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetColors(vertexColors);
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
    }

    private void SpawnTrees()
    {
        if (biome == null) return;

        ClearTrees();

        if (biome.treePrefabs == null || biome.treePrefabs.Length == 0)
        {
            Debug.LogWarning("Aucun prefab d'arbre dans ce biome !");
            return;
        }

        if (originalTrianglesInfo == null || originalTrianglesInfo.Count == 0)
        {
            Debug.LogWarning("Pas de données de triangles pour le spawn d'arbres !");
            return;
        }

        treesContainer = new GameObject("Trees").transform;
        treesContainer.SetParent(transform);
        treesContainer.localPosition = Vector3.zero;
        treesContainer.localRotation = Quaternion.identity;

        int treeCount = 0;
        int trianglesInElevationRange = 0;

        foreach (TriangleInfo triInfo in originalTrianglesInfo)
        {
            if (triInfo.elevationLevel < biome.minElevationForTrees ||
                triInfo.elevationLevel > biome.maxElevationForTrees)
                continue;

            trianglesInElevationRange++;

            Vector3 localCenter = triInfo.center;

            float densityNoise = treeDensityNoise.GetNoise(
                localCenter.x * biome.treeDensityNoiseSize,
                localCenter.y * biome.treeDensityNoiseSize,
                localCenter.z * biome.treeDensityNoiseSize
            );
            densityNoise = (densityNoise + 1f) * 0.5f;

            float combinedDensity = densityNoise * biome.treeDensity;
            int treeCountInTriangle = Mathf.FloorToInt(combinedDensity * 3.99f);

            if (treeCountInTriangle == 0)
                continue;

            float treeTypeNoise01 = treeTypeNoise.GetNoise(
                localCenter.x * biome.treeTypeNoiseSize,
                localCenter.y * biome.treeTypeNoiseSize,
                localCenter.z * biome.treeTypeNoiseSize
            );
            treeTypeNoise01 = (treeTypeNoise01 + 1f) * 0.5f;

            Vector3 extrudedV1 = triInfo.v1 + triInfo.v1.normalized * (triInfo.elevationLevel * biome.extrusionAmount);
            Vector3 extrudedV2 = triInfo.v2 + triInfo.v2.normalized * (triInfo.elevationLevel * biome.extrusionAmount);
            Vector3 extrudedV3 = triInfo.v3 + triInfo.v3.normalized * (triInfo.elevationLevel * biome.extrusionAmount);

            Vector3[] treePositions = new Vector3[]
            {
                (extrudedV1 + extrudedV2) * 0.5f,
                (extrudedV2 + extrudedV3) * 0.5f,
                (extrudedV3 + extrudedV1) * 0.5f
            };

            for (int j = 0; j < treeCountInTriangle; j++)
            {
                Vector3 localPos = treePositions[j];
                Vector3 worldPos = transform.TransformPoint(localPos);

                int prefabIndex = Mathf.FloorToInt(treeTypeNoise01 * biome.treePrefabs.Length);
                prefabIndex = Mathf.Clamp(prefabIndex, 0, biome.treePrefabs.Length - 1);
                GameObject prefabToUse = biome.treePrefabs[prefabIndex];

                if (prefabToUse == null)
                    continue;

                GameObject tree = Instantiate(prefabToUse, worldPos, Quaternion.identity, treesContainer);

                Vector3 normal = localPos.normalized;
                tree.transform.up = transform.TransformDirection(normal);

                tree.transform.Rotate(tree.transform.up, UnityEngine.Random.Range(0f, 360f), Space.World);

                treeCount++;
            }
        }

        Debug.Log($"Spawned {treeCount} trees on {trianglesInElevationRange} eligible triangles (elevation {biome.minElevationForTrees}-{biome.maxElevationForTrees})");
    }

    public void GenerateIcosahedron()
    {
        float phi = (1f + Mathf.Sqrt(5f)) / 2f;
        float a = 1f;
        float b = 1f / phi;

        float scale = 1f / Mathf.Sqrt(a * a + b * b);
        a *= scale;
        b *= scale;

        AddVertex(new Vector3(0, b, -a));
        AddVertex(new Vector3(b, a, 0));
        AddVertex(new Vector3(-b, a, 0));
        AddVertex(new Vector3(0, b, a));
        AddVertex(new Vector3(0, -b, a));
        AddVertex(new Vector3(-a, 0, b));
        AddVertex(new Vector3(0, -b, -a));
        AddVertex(new Vector3(a, 0, -b));
        AddVertex(new Vector3(a, 0, b));
        AddVertex(new Vector3(-a, 0, -b));
        AddVertex(new Vector3(b, -a, 0));
        AddVertex(new Vector3(-b, -a, 0));

        int[][] faces = new int[][]
        {
            new int[] {0, 1, 2}, new int[] {3, 2, 1}, new int[] {3, 4, 5}, new int[] {3, 8, 4},
            new int[] {0, 6, 7}, new int[] {0, 9, 6}, new int[] {4, 10, 11}, new int[] {6, 11, 10},
            new int[] {2, 5, 9}, new int[] {11, 9, 5}, new int[] {1, 7, 8}, new int[] {10, 8, 7},
            new int[] {3, 5, 2}, new int[] {3, 1, 8}, new int[] {0, 2, 9}, new int[] {0, 7, 1},
            new int[] {6, 9, 11}, new int[] {6, 10, 7}, new int[] {4, 11, 5}, new int[] {4, 8, 10}
        };

        foreach (int[] face in faces)
        {
            triangles.Add(face[0]);
            triangles.Add(face[1]);
            triangles.Add(face[2]);
        }
    }

    public void SubdivideTriangles()
    {
        List<int> newTriangles = new List<int>();

        for (int i = 0; i < triangles.Count; i += 3)
        {
            int v1 = triangles[i];
            int v2 = triangles[i + 1];
            int v3 = triangles[i + 2];

            int a = GetMiddlePoint(v1, v2);
            int b = GetMiddlePoint(v2, v3);
            int c = GetMiddlePoint(v3, v1);

            newTriangles.Add(v1); newTriangles.Add(a); newTriangles.Add(c);
            newTriangles.Add(v2); newTriangles.Add(b); newTriangles.Add(a);
            newTriangles.Add(v3); newTriangles.Add(c); newTriangles.Add(b);
            newTriangles.Add(a); newTriangles.Add(b); newTriangles.Add(c);
        }
        triangles = newTriangles;
        middlePointIndexCache.Clear();
    }

    int GetMiddlePoint(int p1, int p2)
    {
        long smallerIndex = Mathf.Min(p1, p2);
        long greaterIndex = Mathf.Max(p1, p2);
        long key = (smallerIndex << 32) + greaterIndex;

        if (middlePointIndexCache.ContainsKey(key))
            return middlePointIndexCache[key];

        Vector3 point1 = vertices[p1];
        Vector3 point2 = vertices[p2];
        Vector3 middle = (point1 + point2) / 2f;
        middle = middle.normalized;

        int index = AddVertex(middle);
        middlePointIndexCache.Add(key, index);
        return index;
    }

    int AddVertex(Vector3 vertex)
    {
        vertices.Add(vertex);
        return vertices.Count - 1;
    }

    public void UpdateMesh()
    {
        mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);

        if (vertexColors != null && vertexColors.Count == vertices.Count)
            mesh.SetColors(vertexColors);

        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();

        
        MeshCollider meshCollider = GetComponent<MeshCollider>();
        if (meshCollider != null)
        {
            meshCollider.sharedMesh = null; 
            meshCollider.sharedMesh = mesh; 
            Debug.Log("✓ MeshCollider mis à jour avec le nouveau mesh");
        }

        //GetComponent<MeshCollider>().convex = false;
    }

    void OnValidate()
    {
        if (Application.isPlaying && mesh != null && biome != null)
        {
            CheckTreeParametersChanged();
            CheckAnomalyParametersChanged();

            if (HasTerrainParametersChanged())
            {
                ClearTrees();
                ClearAnomalies();

                bool shouldRespawnTrees = biome.spawnTrees && biome.treePrefabs != null && biome.treePrefabs.Length > 0;
                bool shouldRespawnAnomalies = biome.spawnAnomalies && biome.anomalyPrefab != null && biome.anomalyPrefab.Length > 0;

                GenerateIcoSphere();
                InitializeTerrainTracking();

                if (shouldRespawnTrees)
                {
                    SpawnTrees();
                }
                if (shouldRespawnAnomalies)
                {
                    SpawnAnomalies();
                }
            }
        }
    }

    private float SampleSphericalNoise(Vector3 position, float scale, int seed)
    {
        Vector3 p = position.normalized;

        float seedOffset = seed * 0.12345f;

        float noise1 = noise.GetNoise(
            p.x * scale + seedOffset,
            p.y * scale + seedOffset,
            p.z * scale + seedOffset
        );

        float cos120 = -0.5f;
        float sin120 = 0.866f;
        float x2 = p.x * cos120 - p.z * sin120;
        float z2 = p.x * sin120 + p.z * cos120;
        float noise2 = noise.GetNoise(
            x2 * scale + seedOffset + 100f,
            p.y * scale + seedOffset + 100f,
            z2 * scale + seedOffset + 100f
        );

        float cos240 = -0.5f;
        float sin240 = -0.866f;
        float x3 = p.x * cos240 - p.z * sin240;
        float z3 = p.x * sin240 + p.z * cos240;
        float noise3 = noise.GetNoise(
            x3 * scale + seedOffset + 200f,
            p.y * scale + seedOffset + 200f,
            z3 * scale + seedOffset + 200f
        );

        return (noise1 + noise2 + noise3) / 3f;
    }

    void OnRenderObject()
    {
        if (!showWireframe || mesh == null || !Application.isPlaying) return;

        Material lineMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));
        lineMaterial.SetPass(0);

        GL.PushMatrix();
        GL.MultMatrix(transform.localToWorldMatrix);
        GL.Begin(GL.LINES);
        GL.Color(wireframeColor);

        for (int i = 0; i < triangles.Count; i += 3)
        {
            Vector3 v1 = vertices[triangles[i]];
            Vector3 v2 = vertices[triangles[i + 1]];
            Vector3 v3 = vertices[triangles[i + 2]];

            GL.Vertex(v1); GL.Vertex(v2);
            GL.Vertex(v2); GL.Vertex(v3);
            GL.Vertex(v3); GL.Vertex(v1);
        }

        GL.End();
        GL.PopMatrix();
    }

    // ANOMALIES MANAGEMENT
    private void ClearAnomalies()
    {
        List<Transform> old = new List<Transform>();
        foreach (Transform child in transform)
            if (child.name == "Anomalies") old.Add(child);

        foreach (var o in old)
        {
            if (Application.isPlaying) Destroy(o.gameObject); else DestroyImmediate(o.gameObject);
        }
        anomaliesContainer = null;
    }

    private void SpawnAnomalies()
    {
        if (biome == null || biome.anomalyPrefab == null || biome.anomalyPrefab.Length == 0) return;
        if (originalTrianglesInfo == null || originalTrianglesInfo.Count == 0) return;

        ClearAnomalies();

        anomaliesContainer = new GameObject("Anomalies").transform;
        anomaliesContainer.SetParent(transform);
        anomaliesContainer.localPosition = Vector3.zero;
        anomaliesContainer.localRotation = Quaternion.identity;

        int anomaliesSpawned = 0;
        foreach (var triInfo in originalTrianglesInfo)
        {
            if (!biome.spawnAnomalies) break;
            if (triInfo.elevationLevel < biome.minElevationForAnomalies || triInfo.elevationLevel > biome.maxElevationForAnomalies) continue;

            float dn = anomalyDensityNoise.GetNoise(triInfo.center.x * biome.anomalyDensityNoiseSize,
                                                  triInfo.center.y * biome.anomalyDensityNoiseSize,
                                                  triInfo.center.z * biome.anomalyDensityNoiseSize);
            dn = (dn + 1f) * 0.5f;
            if (dn > biome.anomalyDensity) continue;

            float tn = anomalyTypeNoise.GetNoise(triInfo.center.x * biome.anomalyTypeNoiseSize,
                                              triInfo.center.y * biome.anomalyTypeNoiseSize,
                                              triInfo.center.z * biome.anomalyTypeNoiseSize);
            tn = (tn + 1f) * 0.5f;
            int prefabIndex = Mathf.Clamp(Mathf.FloorToInt(tn * biome.anomalyPrefab.Length), 0, biome.anomalyPrefab.Length - 1);
            GameObject prefab = biome.anomalyPrefab[prefabIndex];
            if (prefab == null) continue;

            Vector3 extruded = triInfo.center + triInfo.center.normalized * (triInfo.elevationLevel * biome.extrusionAmount);
            Vector3 world = transform.TransformPoint(extruded);
            GameObject inst = Instantiate(prefab, world, Quaternion.identity, anomaliesContainer);

            inst.transform.up = transform.TransformDirection(triInfo.normal);
            inst.transform.Rotate(inst.transform.up, Random.Range(0f, 360f), Space.World);

            anomaliesSpawned++;
        }

        Debug.Log($"Spawned {anomaliesSpawned} anomalies");
    }

    private void CheckAnomalyParametersChanged()
    {
        if (biome == null) return;
        bool parametersChanged = false;
        bool needToRespawn = false;
        bool needToClear = false;
        if (biome.spawnAnomalies != lastSpawnAnomalyState)
        {
            if (biome.spawnAnomalies)
            {
                needToRespawn = true;
            }
            else
            {
                needToClear = true;
            }
            lastSpawnAnomalyState = biome.spawnAnomalies;
            parametersChanged = true;
        }
        if (biome.spawnAnomalies && !parametersChanged)
        {
            if (biome.minElevationForAnomalies != lastMinAnomaly ||
                biome.maxElevationForAnomalies != lastMaxAnomaly ||
                Mathf.Abs(biome.anomalyDensity - lastAnomalyDensity) > 0.001f ||
                Mathf.Abs(biome.anomalyDensityNoiseSize - lastAnomalyDensityNoiseSize) > 0.001f ||
                Mathf.Abs(biome.anomalyTypeNoiseSize - lastAnomalyTypeNoiseSize) > 0.001f)
            {
                needToRespawn = true;
                lastMinAnomaly = biome.minElevationForAnomalies;
                lastMaxAnomaly = biome.maxElevationForAnomalies;
                lastAnomalyDensity = biome.anomalyDensity;
                lastAnomalyDensityNoiseSize = biome.anomalyDensityNoiseSize;
                lastAnomalyTypeNoiseSize = biome.anomalyTypeNoiseSize;
            }
        }
        if (needToClear)
        {
            ClearAnomalies();
        }
        else if (needToRespawn && biome.anomalyPrefab != null && biome.anomalyPrefab.Length > 0)
        {
            SpawnAnomalies();
        }
    }

}