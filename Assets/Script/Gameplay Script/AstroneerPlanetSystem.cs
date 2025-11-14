using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using UnityEngine.InputSystem;

public class AstroneerPlanetSystem : MonoBehaviour
{
    [Header("Planet Setup")]
    [Tooltip("IcoSphere de référence (pour la forme initiale)")]
    public IcoSphere referenceIcoSphere;

    [Tooltip("Rayon de la planète")]
    [Range(5f, 100f)]
    public float planetRadius = 15f;

    [Tooltip("Résolution des chunks (voxels par chunk)")]
    [Range(16, 48)]
    public int chunkResolution = 32;

    [Tooltip("Taille d'un chunk en unités world")]
    [Range(4f, 20f)]
    public float chunkSize = 8f;

    [Header("Digging")]
    [Range(0.5f, 10f)]
    public float digRadius = 3f;

    [Range(0.1f, 5f)]
    public float digStrength = 1f;

    public Transform playerCamera;

    [Range(10f, 200f)]
    public float maxReachDistance = 100f;

    [Header("Performance")]
    [Tooltip("Nombre max de chunks à générer par frame")]
    [Range(1, 10)]
    public int maxChunksPerFrame = 3;

    [Tooltip("Distance max pour afficher les chunks")]
    [Range(20f, 200f)]
    public float chunkLoadDistance = 100f;

    [Header("Visual")]
    public Material planetMaterial;

    [Header("Debug")]
    public bool showDebugInfo = true;
    public bool showChunkBounds = false;

    [Tooltip("Rayon de détection du son (recommandé: légèrement plus grand que planetRadius)")]
    [Range(10f, 700f)]
    public float soundTriggerRadius = 20f;

    [Header("Sound")]
    public AudioClip anomalySound;

    private Dictionary<int3, VoxelChunk> chunks = new Dictionary<int3, VoxelChunk>();
    private Queue<VoxelChunk> dirtyChunks = new Queue<VoxelChunk>();
    private Queue<int3> chunksToGenerate = new Queue<int3>();

    private GameObject chunksContainer;
    private bool isInitialized = false;
    private int frameCount = 0;

    public class VoxelChunk
    {
        public int3 chunkIndex;
        public Vector3 worldCenter;
        public NativeArray<float> densities;
        public int resolution;
        public float size;
        public bool isDirty;
        public bool hasMesh;

        public GameObject gameObject;
        public MeshFilter meshFilter;
        public MeshRenderer meshRenderer;
        public MeshCollider meshCollider;

        public VoxelChunk(int3 index, Vector3 center, int res, float chunkSize, Transform parent, Material mat)
        {
            chunkIndex = index;
            worldCenter = center;
            resolution = res;
            size = chunkSize;

            int totalVoxels = (res + 1) * (res + 1) * (res + 1);
            densities = new NativeArray<float>(totalVoxels, Allocator.Persistent);

            gameObject = new GameObject($"Chunk_{index.x}_{index.y}_{index.z}");
            gameObject.transform.parent = parent;
            gameObject.transform.position = center;
            gameObject.layer = 6;

            meshFilter = gameObject.AddComponent<MeshFilter>();
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshCollider = gameObject.AddComponent<MeshCollider>();

            meshRenderer.material = mat;

            isDirty = true;
            hasMesh = false;
        }

        public void Dispose()
        {
            if (densities.IsCreated)
                densities.Dispose();

            if (gameObject != null)
                Object.Destroy(gameObject);
        }
    }

    void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main?.transform;

        if (referenceIcoSphere == null)
        {
            referenceIcoSphere = GetComponent<IcoSphere>();
        }

        chunksContainer = new GameObject("VoxelChunks");
        chunksContainer.transform.parent = transform;
        chunksContainer.transform.localPosition = Vector3.zero;

        StartCoroutine(InitializeSystem());
    }

    System.Collections.IEnumerator InitializeSystem()
    {
        yield return null;
        yield return null;

        Debug.Log(" Initializing Astroneer-style planet system...");

        GenerateChunkGrid();

        Debug.Log($" Planet system initialized! {chunksToGenerate.Count} chunks to generate");
        isInitialized = true;
    }

    void GenerateChunkGrid()
    {
        float diameter = planetRadius * 2f;
        int chunksPerSide = Mathf.CeilToInt(diameter / chunkSize) + 2; 
        int offset = chunksPerSide / 2;

        for (int x = -offset; x <= offset; x++)
        {
            for (int y = -offset; y <= offset; y++)
            {
                for (int z = -offset; z <= offset; z++)
                {
                    int3 chunkIndex = new int3(x, y, z);
                    Vector3 chunkCenter = new Vector3(x, y, z) * chunkSize;

                    float distanceToCenter = chunkCenter.magnitude;
                    float chunkDiagonal = chunkSize * 1.732f; 

                    if (distanceToCenter < planetRadius + chunkDiagonal * 0.5f)
                    {
                        chunksToGenerate.Enqueue(chunkIndex);
                    }
                }
            }
        }
    }

    void Update()
    {
        CheckPlayerProximity();
        if (!isInitialized) return;

        frameCount++;

        GenerateChunksProgressively();

        HandleDigging();

     

        if (frameCount % 2 == 0)
        {
            RebuildDirtyChunks();
        }
    }
    
    private bool isPlayerOnPlanet = false;
    private float distanceToPlanet;
    void CheckPlayerProximity()
    {
        if (playerCamera == null || anomalySound == null) return;

         distanceToPlanet = Vector3.Distance(playerCamera.position, transform.position);
        bool shouldPlaySound = distanceToPlanet <= soundTriggerRadius;

        if (shouldPlaySound && !isPlayerOnPlanet)
        {
            isPlayerOnPlanet = true;
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayMusic(anomalySound,2);
                
                SoundManager.Instance.StopMusic(true,0);
                SoundManager.Instance.StopMusic(true,1);
                
                
                Debug.Log("Player entered planet zone, playing anomaly sound.");
            }
        }
       
    }

    void GenerateChunksProgressively()
    {
        int generated = 0;

        while (chunksToGenerate.Count > 0 && generated < maxChunksPerFrame)
        {
            int3 chunkIndex = chunksToGenerate.Dequeue();

            if (!chunks.ContainsKey(chunkIndex))
            {
                CreateChunk(chunkIndex);
                generated++;
            }
        }

        if (chunksToGenerate.Count == 0 && showDebugInfo && generated > 0)
        {
            Debug.Log($"All chunks generated! Total: {chunks.Count}");
        }
    }

    void CreateChunk(int3 chunkIndex)
    {
        Vector3 chunkCenter = new Vector3(chunkIndex.x, chunkIndex.y, chunkIndex.z) * chunkSize;

        VoxelChunk chunk = new VoxelChunk(
            chunkIndex,
            chunkCenter,
            chunkResolution,
            chunkSize,
            chunksContainer.transform,
            planetMaterial
        );

        InitializeChunkDensities(chunk);
        //InitializeChunkDensitiesFromMesh(chunk);

        chunks[chunkIndex] = chunk;
        dirtyChunks.Enqueue(chunk);
    }

    void InitializeChunkDensities(VoxelChunk chunk)
    {
        if (referenceIcoSphere == null)
        {
            Debug.LogError(" referenceIcoSphere is null!");
            return;
        }

        Mesh mesh = referenceIcoSphere.GetComponent<MeshFilter>()?.sharedMesh;
        if (mesh == null)
        {
            Debug.LogError(" IcoSphere mesh is null!");
            return;
        }

        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;

        NativeArray<float3> nativeVertices = new NativeArray<float3>(vertices.Length, Allocator.TempJob);
        NativeArray<int> nativeTriangles = new NativeArray<int>(triangles.Length, Allocator.TempJob);

        for (int i = 0; i < vertices.Length; i++)
            nativeVertices[i] = vertices[i];

        for (int i = 0; i < triangles.Length; i++)
            nativeTriangles[i] = triangles[i];

        var job = new InitializeChunkFromMeshJob
        {
            densities = chunk.densities,
            resolution = chunk.resolution,
            chunkSize = chunk.size,
            chunkCenter = chunk.worldCenter,
            vertices = nativeVertices,
            triangles = nativeTriangles,
            planetCenter = transform.position
        };

        job.Schedule(chunk.densities.Length, 64).Complete();

        nativeVertices.Dispose();
        nativeTriangles.Dispose();
    }

    [BurstCompile(CompileSynchronously = true)]
    struct InitializeChunkFromMeshJob : IJobParallelFor
    {
        [WriteOnly] public NativeArray<float> densities;
        public int resolution;
        public float chunkSize;
        public float3 chunkCenter;
        public float3 planetCenter;

        [ReadOnly] public NativeArray<float3> vertices;
        [ReadOnly] public NativeArray<int> triangles;

        public void Execute(int index)
        {
            int res1 = resolution + 1;
            int x = index % res1;
            int temp = index / res1;
            int y = temp % res1;
            int z = temp / res1;

            float fx = ((float)x / resolution) * chunkSize - chunkSize * 0.5f;
            float fy = ((float)y / resolution) * chunkSize - chunkSize * 0.5f;
            float fz = ((float)z / resolution) * chunkSize - chunkSize * 0.5f;

            float3 worldPos = chunkCenter + new float3(fx, fy, fz);

            float minDistance = float.MaxValue;

            for (int i = 0; i < triangles.Length; i += 3)
            {
                float3 v0 = vertices[triangles[i]];
                float3 v1 = vertices[triangles[i + 1]];
                float3 v2 = vertices[triangles[i + 2]];

                float dist = DistancePointToTriangle(worldPos, v0, v1, v2);
                minDistance = math.min(minDistance, dist);
            }

            float3 toPoint = worldPos - planetCenter;
            float distToCenter = math.length(toPoint);

            float3 dirToCenter = math.normalize(-toPoint);

            int intersections = 0;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                float3 v0 = vertices[triangles[i]];
                float3 v1 = vertices[triangles[i + 1]];
                float3 v2 = vertices[triangles[i + 2]];

                if (RayIntersectsTriangle(worldPos, dirToCenter, v0, v1, v2))
                    intersections++;
            }

            bool isInside = (intersections % 2) == 1;

            float signedDist = isInside ? -minDistance : minDistance;

            float densityScale = 0.3f;
            densities[index] = 0.5f + signedDist * densityScale;
        }

        private static float DistancePointToTriangle(float3 p, float3 a, float3 b, float3 c)
        {
            float3 ab = b - a;
            float3 ac = c - a;
            float3 ap = p - a;

            float d1 = math.dot(ab, ap);
            float d2 = math.dot(ac, ap);
            if (d1 <= 0f && d2 <= 0f) return math.length(ap);

            float3 bp = p - b;
            float d3 = math.dot(ab, bp);
            float d4 = math.dot(ac, bp);
            if (d3 >= 0f && d4 <= d3) return math.length(bp);

            float vc = d1 * d4 - d3 * d2;
            if (vc <= 0f && d1 >= 0f && d3 <= 0f)
            {
                float v2 = d1 / (d1 - d3);
                return math.length(ap - v2 * ab);
            }

            float3 cp = p - c;
            float d5 = math.dot(ab, cp);
            float d6 = math.dot(ac, cp);
            if (d6 >= 0f && d5 <= d6) return math.length(cp);

            float vb = d5 * d2 - d1 * d6;
            if (vb <= 0f && d2 >= 0f && d6 <= 0f)
            {
                float w3 = d2 / (d2 - d6);
                return math.length(ap - w3 * ac);
            }

            float va = d3 * d6 - d5 * d4;
            if (va <= 0f && (d4 - d3) >= 0f && (d5 - d6) >= 0f)
            {
                float w2 = (d4 - d3) / ((d4 - d3) + (d5 - d6));
                return math.length((b + w2 * (c - b)) - p);
            }

            float denom = d1 * d4 - d3 * d2;
            if (math.abs(denom) < 0.0001f)
            {
                float3 n = math.cross(ab, ac);
                if (math.lengthsq(n) < 0.0001f)
                    return math.min(math.min(math.length(ap), math.length(bp)), math.length(cp));
                n = math.normalize(n);
                return math.abs(math.dot(ap, n));
            }

            float v = (d4 * d1 - d2 * d3) / denom;
            float w = (d1 * d4 - d3 * d2) / denom;
            float u = 1f - v - w;

            float3 closest = u * a + v * b + w * c;
            return math.length(p - closest);
        }

        private static bool RayIntersectsTriangle(float3 origin, float3 dir, float3 v0, float3 v1, float3 v2)
        {
            const float EPSILON = 0.0000001f;

            float3 edge1 = v1 - v0;
            float3 edge2 = v2 - v0;
            float3 h = math.cross(dir, edge2);
            float a = math.dot(edge1, h);

            if (math.abs(a) < EPSILON)
                return false;

            float f = 1f / a;
            float3 s = origin - v0;
            float u = f * math.dot(s, h);

            if (u < 0f || u > 1f)
                return false;

            float3 q = math.cross(s, edge1);
            float v = f * math.dot(dir, q);

            if (v < 0f || u + v > 1f)
                return false;

            float t = f * math.dot(edge2, q);
            return t > EPSILON;
        }
    }

    void RebuildDirtyChunks()
    {
        int rebuilt = 0;
        int maxRebuilds = 5;

        while (dirtyChunks.Count > 0 && rebuilt < maxRebuilds)
        {
            VoxelChunk chunk = dirtyChunks.Dequeue();

            if (chunk != null && chunk.isDirty)
            {
                RebuildChunkMesh(chunk);
                rebuilt++;
            }
        }
    }

    void RebuildChunkMesh(VoxelChunk chunk)
    {
        float[,,] densities3D = new float[chunk.resolution + 1, chunk.resolution + 1, chunk.resolution + 1];

        for (int x = 0; x <= chunk.resolution; x++)
        {
            for (int y = 0; y <= chunk.resolution; y++)
            {
                for (int z = 0; z <= chunk.resolution; z++)
                {
                    int index = x + (chunk.resolution + 1) * (y + (chunk.resolution + 1) * z);
                    densities3D[x, y, z] = chunk.densities[index];
                }
            }
        }

        float voxelSize = chunk.size / chunk.resolution;
        MarchingCubesJobs.GenerateMeshE(densities3D, voxelSize, out List<Vector3> vertices, out List<int> triangles);

        Mesh mesh = chunk.meshFilter.sharedMesh;
        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.name = $"ChunkMesh_{chunk.chunkIndex}";
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }
        else
        {
            mesh.Clear();
        }

        if (vertices.Count > 0)
        {
            for (int i = 0; i < vertices.Count; i++)
            {
                vertices[i] -= new Vector3(chunk.size * 0.5f, chunk.size * 0.5f, chunk.size * 0.5f);
            }

            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            chunk.meshFilter.sharedMesh = mesh;
            chunk.meshCollider.sharedMesh = mesh;

            if (!chunk.hasMesh && showDebugInfo)
            {
                Debug.Log($"✨ Chunk {chunk.chunkIndex} generated: {vertices.Count} verts, {triangles.Count / 3} tris");
            }

            chunk.hasMesh = true;
        }
        else
        {
            chunk.meshFilter.sharedMesh = null;
            chunk.meshCollider.sharedMesh = null;
            chunk.hasMesh = false;
        }

        chunk.isDirty = false;
    }

    void HandleDigging()
    {
        if(GameManager.Instance.drillUnlocked)
        {
            if (playerCamera == null) return;

            bool leftButton = Mouse.current?.leftButton.isPressed ?? false;

            if (leftButton)
            {
                Ray ray = new Ray(playerCamera.position, playerCamera.forward);

                if (Physics.Raycast(ray, out RaycastHit hit, maxReachDistance))
                {
                    DigAtPosition(hit.point, leftButton);
                }
            }

        }
    }

    void DigAtPosition(Vector3 worldPos, bool isDigging)
    {
        List<VoxelChunk> affectedChunks = GetChunksInRadius(worldPos, digRadius);

        foreach (VoxelChunk chunk in affectedChunks)
        {
            ModifyChunkDensities(chunk, worldPos, digRadius, digStrength * Time.deltaTime, isDigging);
        }
    }

    void ModifyChunkDensities(VoxelChunk chunk, Vector3 worldPos, float radius, float strength, bool isDigging)
    {
        var job = new ModifyDensitiesJob
        {
            densities = chunk.densities,
            resolution = chunk.resolution,
            chunkSize = chunk.size,
            chunkCenter = chunk.worldCenter,
            modifyPoint = worldPos,
            modifyRadius = radius,
            strength = strength,
            isDigging = isDigging
        };

        job.Schedule(chunk.densities.Length, 32).Complete();

        chunk.isDirty = true;
        if (!dirtyChunks.Contains(chunk))
        {
            dirtyChunks.Enqueue(chunk);
        }
    }

    [BurstCompile(CompileSynchronously = true)]
    struct ModifyDensitiesJob : IJobParallelFor
    {
        public NativeArray<float> densities;
        public int resolution;
        public float chunkSize;
        public float3 chunkCenter;
        public float3 modifyPoint;
        public float modifyRadius;
        public float strength;
        public bool isDigging;

        public void Execute(int index)
        {
            int res1 = resolution + 1;
            int x = index % res1;
            int temp = index / res1;
            int y = temp % res1;
            int z = temp / res1;

            float fx = ((float)x / resolution) * chunkSize - chunkSize * 0.5f;
            float fy = ((float)y / resolution) * chunkSize - chunkSize * 0.5f;
            float fz = ((float)z / resolution) * chunkSize - chunkSize * 0.5f;

            float3 worldPos = chunkCenter + new float3(fx, fy, fz);
            float distance = math.distance(worldPos, modifyPoint);

            if (distance < modifyRadius)
            {
                float falloff = 1f - (distance / modifyRadius);
                falloff = falloff * falloff;

                float change = strength * falloff * 2f;

                if (isDigging)
                    densities[index] += change;
            }
        }
    }

    List<VoxelChunk> GetChunksInRadius(Vector3 worldPos, float radius)
    {
        List<VoxelChunk> result = new List<VoxelChunk>();
        float checkRadius = radius + chunkSize * 1.0f;

        foreach (var kvp in chunks)
        {
            VoxelChunk chunk = kvp.Value;
            if (Vector3.Distance(chunk.worldCenter, worldPos) < checkRadius)
            {
                result.Add(chunk);
            }
        }

        return result;
    }

    void ResetPlanet()
    {
        Debug.Log("🔄 Resetting planet...");

        foreach (var kvp in chunks)
        {
            VoxelChunk chunk = kvp.Value;
            InitializeChunkDensities(chunk);
            chunk.isDirty = true;
            dirtyChunks.Enqueue(chunk);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = new Color(0, 1, 0, 0.2f);
        Gizmos.DrawWireSphere(transform.position, planetRadius);

        if (showChunkBounds && chunks != null)
        {
            foreach (var kvp in chunks)
            {
                VoxelChunk chunk = kvp.Value;
                Gizmos.color = chunk.hasMesh ? new Color(0, 1, 1, 0.3f) : new Color(1, 0, 0, 0.1f);
                Gizmos.DrawWireCube(chunk.worldCenter, Vector3.one * chunk.size);
            }
        }

        if (playerCamera != null)
        {
            Ray ray = new Ray(playerCamera.position, playerCamera.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, maxReachDistance))
            {
                Gizmos.color = new Color(1, 0.5f, 0, 0.5f);
                Gizmos.DrawWireSphere(hit.point, digRadius);
            }
        }
    }

    void OnDestroy()
    {
        foreach (var kvp in chunks)
        {
            kvp.Value.Dispose();
        }
        chunks.Clear();
    }
}