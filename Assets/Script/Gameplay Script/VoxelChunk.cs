using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Représente un chunk voxel local dans la planète avec support du creusage
/// Version MonoBehaviour pour une intégration Unity simplifiée
/// </summary>
public class VoxelChunk : MonoBehaviour
{
    [Header("Chunk Data")]
    public int3 chunkIndex;
    public Vector3 worldCenter;
    public VoxelData voxelData;
    public bool isDirty;

    [Header("Components")]
    public MeshFilter meshFilter;
    public MeshCollider meshCollider;

    // Données privées
    private int resolution;
    private float size;
    private float[,,] densities;

    /// <summary>
    /// Initialise le chunk voxel avec densités sphériques
    /// </summary>
    public void Initialize(int res, float worldSize, float planetRadius, Vector3 planetCenter, Transform chunkTransform)
    {
        resolution = res;
        size = worldSize;
        worldCenter = chunkTransform.position;
        densities = new float[res + 1, res + 1, res + 1];

        // Obtenir les composants
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();

        // Si les composants n'existent pas, les ajouter
        if (meshFilter == null)
            meshFilter = gameObject.AddComponent<MeshFilter>();
        if (meshCollider == null)
            meshCollider = gameObject.AddComponent<MeshCollider>();

        // Générer les densités initiales
        for (int x = 0; x <= res; x++)
        {
            for (int y = 0; y <= res; y++)
            {
                for (int z = 0; z <= res; z++)
                {
                    Vector3 localPos = new Vector3(
                        ((float)x / res - 0.5f) * size,
                        ((float)y / res - 0.5f) * size,
                        ((float)z / res - 0.5f) * size
                    );

                    Vector3 worldPos = chunkTransform.TransformPoint(localPos);

                    // Signed distance field sphérique
                    float dist = Vector3.Distance(worldPos, planetCenter);
                    float density = planetRadius - dist;

                    // Ajout de relief avec bruit Perlin
                    density += Mathf.PerlinNoise(worldPos.x * 0.05f, worldPos.z * 0.05f) * 3f;

                    densities[x, y, z] = density;
                }
            }
        }

        RebuildMesh();
    }

    /// <summary>
    /// Construit le mesh du chunk via Marching Cubes
    /// </summary>
    public void RebuildMesh()
    {
        if (densities == null)
            return;

        Mesh mesh = new Mesh();
        mesh.name = $"VoxelChunk_{worldCenter}";

        // Calcul du voxel size correct
        float voxelSize = size / resolution;
        MarchingCubesJobs.GenerateMeshE(densities, voxelSize, out var verts, out var tris);

        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        if (meshFilter != null)
            meshFilter.sharedMesh = mesh;
        if (meshCollider != null)
            meshCollider.sharedMesh = mesh;

        isDirty = false;
    }

    /// <summary>
    /// Creuse dans le chunk à une position mondiale donnée
    /// </summary>
    public bool DigAtWorldPosition(Vector3 worldPos, float radius, float strength = 1f)
    {
        if (densities == null)
            return false;

        bool modified = false;

        for (int x = 0; x <= resolution; x++)
        {
            for (int y = 0; y <= resolution; y++)
            {
                for (int z = 0; z <= resolution; z++)
                {
                    Vector3 localPos = new Vector3(
                        ((float)x / resolution - 0.5f) * size,
                        ((float)y / resolution - 0.5f) * size,
                        ((float)z / resolution - 0.5f) * size
                    );

                    Vector3 voxelWorldPos = transform.TransformPoint(localPos);
                    float distance = Vector3.Distance(voxelWorldPos, worldPos);

                    if (distance < radius)
                    {
                        // Falloff sphérique lisse
                        float falloff = 1f - (distance / radius);
                        falloff = falloff * falloff; // smooth falloff

                        densities[x, y, z] -= strength * falloff * 10f;
                        modified = true;
                    }
                }
            }
        }

        if (modified)
        {
            isDirty = true;
        }

        return modified;
    }

    /// <summary>
    /// Ajoute de la matière (inverse de creuser)
    /// </summary>
    public bool AddAtWorldPosition(Vector3 worldPos, float radius, float strength = 1f)
    {
        if (densities == null)
            return false;

        bool modified = false;

        for (int x = 0; x <= resolution; x++)
        {
            for (int y = 0; y <= resolution; y++)
            {
                for (int z = 0; z <= resolution; z++)
                {
                    Vector3 localPos = new Vector3(
                        ((float)x / resolution - 0.5f) * size,
                        ((float)y / resolution - 0.5f) * size,
                        ((float)z / resolution - 0.5f) * size
                    );

                    Vector3 voxelWorldPos = transform.TransformPoint(localPos);
                    float distance = Vector3.Distance(voxelWorldPos, worldPos);

                    if (distance < radius)
                    {
                        float falloff = 1f - (distance / radius);
                        falloff = falloff * falloff;

                        densities[x, y, z] += strength * falloff * 10f;
                        modified = true;
                    }
                }
            }
        }

        if (modified)
        {
            isDirty = true;
        }

        return modified;
    }

    /// <summary>
    /// Lisse les densités dans une zone (utile pour adoucir les modifications)
    /// </summary>
    public bool SmoothAtWorldPosition(Vector3 worldPos, float radius, float strength = 0.5f)
    {
        if (densities == null)
            return false;

        bool modified = false;
        float[,,] newDensities = (float[,,])densities.Clone();

        for (int x = 1; x < resolution; x++)
        {
            for (int y = 1; y < resolution; y++)
            {
                for (int z = 1; z < resolution; z++)
                {
                    Vector3 localPos = new Vector3(
                        ((float)x / resolution - 0.5f) * size,
                        ((float)y / resolution - 0.5f) * size,
                        ((float)z / resolution - 0.5f) * size
                    );

                    Vector3 voxelWorldPos = transform.TransformPoint(localPos);
                    float distance = Vector3.Distance(voxelWorldPos, worldPos);

                    if (distance < radius)
                    {
                        // Moyenne des voisins
                        float avg = (
                            densities[x - 1, y, z] + densities[x + 1, y, z] +
                            densities[x, y - 1, z] + densities[x, y + 1, z] +
                            densities[x, y, z - 1] + densities[x, y, z + 1]
                        ) / 6f;

                        float falloff = 1f - (distance / radius);
                        falloff = falloff * falloff;

                        newDensities[x, y, z] = Mathf.Lerp(densities[x, y, z], avg, strength * falloff);
                        modified = true;
                    }
                }
            }
        }

        if (modified)
        {
            densities = newDensities;
            isDirty = true;
        }

        return modified;
    }

    /// <summary>
    /// Obtient la densité à une position mondiale (avec interpolation)
    /// </summary>
    public float GetDensityAtWorldPosition(Vector3 worldPos)
    {
        if (densities == null)
            return 0f;

        Vector3 localPos = transform.InverseTransformPoint(worldPos);

        // Convertir en coordonnées voxel
        float fx = (localPos.x / size + 0.5f) * resolution;
        float fy = (localPos.y / size + 0.5f) * resolution;
        float fz = (localPos.z / size + 0.5f) * resolution;

        int x0 = Mathf.FloorToInt(fx);
        int y0 = Mathf.FloorToInt(fy);
        int z0 = Mathf.FloorToInt(fz);

        if (x0 < 0 || x0 >= resolution || y0 < 0 || y0 >= resolution || z0 < 0 || z0 >= resolution)
            return 0f;

        return densities[x0, y0, z0];
    }

    /// <summary>
    /// Vérifie si le chunk a besoin d'être régénéré
    /// </summary>
    public bool NeedsRebuild()
    {
        return isDirty;
    }

    /// <summary>
    /// Indique si un point du monde est à l'intérieur de ce chunk
    /// </summary>
    public bool ContainsWorldPoint(Vector3 worldPoint, float chunkWorldSize)
    {
        float halfSize = chunkWorldSize * 0.5f;
        return Mathf.Abs(worldPoint.x - worldCenter.x) <= halfSize &&
               Mathf.Abs(worldPoint.y - worldCenter.y) <= halfSize &&
               Mathf.Abs(worldPoint.z - worldCenter.z) <= halfSize;
    }

    /// <summary>
    /// Obtient les densités (pour le système de sauvegarde)
    /// </summary>
    public float[,,] GetDensities()
    {
        return densities;
    }

    /// <summary>
    /// Définit les densités (pour le système de sauvegarde)
    /// </summary>
    public void SetDensities(float[,,] newDensities)
    {
        if (newDensities != null)
        {
            densities = newDensities;
            isDirty = true;
        }
    }

    /// <summary>
    /// Obtient la résolution du chunk
    /// </summary>
    public int GetResolution()
    {
        return resolution;
    }

    /// <summary>
    /// Visualisation dans l'éditeur
    /// </summary>
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(worldCenter, Vector3.one * size);
    }
}