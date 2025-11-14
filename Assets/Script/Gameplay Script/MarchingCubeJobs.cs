using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class MarchingCubesJobs
{
    private VoxelData voxelData;
    private float isoLevel;

    public MarchingCubesJobs(VoxelData voxelData, float isoLevel = 0.5f)
    {
        this.voxelData = voxelData;
        this.isoLevel = isoLevel;
    }

    public void SetVoxelData(VoxelData newVoxelData)
    {
        this.voxelData = newVoxelData;
    }

    public VoxelData GetVoxelData()
    {
        return voxelData;
    }

    public Mesh GenerateMesh()
    {
        int totalCubes = (voxelData.size.x - 1) * (voxelData.size.y - 1) * (voxelData.size.z - 1);

        // Cr?er la table native
        NativeArray<int> triangleTable = new NativeArray<int>(
            MarchingCubesTable.TriangleTable,
            Allocator.TempJob
        );

        // Allouer les buffers
        NativeArray<float> densities = new NativeArray<float>(voxelData.densities, Allocator.TempJob);
        NativeArray<TriangleData> triangleData = new NativeArray<TriangleData>(totalCubes * 5, Allocator.TempJob);
        NativeArray<int> triangleCount = new NativeArray<int>(1, Allocator.TempJob);

        // Job de g?n?ration
        var job = new MarchCubeJob
        {
            densities = densities,
            triangleTable = triangleTable,
            triangleData = triangleData,
            triangleCount = triangleCount,
            sizeX = voxelData.size.x,
            sizeY = voxelData.size.y,
            sizeZ = voxelData.size.z,
            voxelSize = voxelData.voxelSize,
            isoLevel = isoLevel
        };

        // Ex?cuter avec parall?lisation
        JobHandle handle = job.Schedule(totalCubes, 64);
        handle.Complete();

        // Cr?er le mesh
        int finalTriangleCount = triangleCount[0];
        Mesh mesh = CreateMeshFromTriangles(triangleData, finalTriangleCount);

        // Cleanup
        densities.Dispose();
        triangleTable.Dispose();
        triangleData.Dispose();
        triangleCount.Dispose();

        return mesh;
    }
    public static void GenerateMeshE(float[,,] densities, float voxelSize, out List<Vector3> vertices, out List<int> triangles)
    {
        int3 size3 = new int3(densities.GetLength(0), densities.GetLength(1), densities.GetLength(2));
        VoxelData voxelData = new VoxelData(size3, voxelSize);

        // copier densities 3D -> 1D
        for (int x = 0; x < size3.x; x++)
            for (int y = 0; y < size3.y; y++)
                for (int z = 0; z < size3.z; z++)
                    voxelData.densities[x + size3.x * (y + size3.y * z)] = densities[x, y, z];

        MarchingCubesJobs mc = new MarchingCubesJobs(voxelData);
        Mesh mesh = mc.GenerateMesh();

        vertices = new List<Vector3>(mesh.vertices);
        triangles = new List<int>(mesh.triangles);
    }

    private Mesh CreateMeshFromTriangles(NativeArray<TriangleData> triangleData, int count)
    {
        count = math.min(count, triangleData.Length);

        Vector3[] vertices = new Vector3[count * 3];
        int[] triangles = new int[count * 3];

        for (int i = 0; i < count; i++)
        {
            TriangleData tri = triangleData[i];
            int vertexIndex = i * 3;

            vertices[vertexIndex] = tri.v1;
            vertices[vertexIndex + 1] = tri.v2;
            vertices[vertexIndex + 2] = tri.v3;

            triangles[vertexIndex] = vertexIndex;
            triangles[vertexIndex + 1] = vertexIndex + 1;
            triangles[vertexIndex + 2] = vertexIndex + 2;
        }

        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    public struct TriangleData
    {
        public float3 v1;
        public float3 v2;
        public float3 v3;
    }

    [BurstCompile(CompileSynchronously = true, FloatPrecision = FloatPrecision.Standard, FloatMode = FloatMode.Fast)]
    struct MarchCubeJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float> densities;
        [ReadOnly] public NativeArray<int> triangleTable;
        [NativeDisableParallelForRestriction] public NativeArray<TriangleData> triangleData;
        [NativeDisableParallelForRestriction] public NativeArray<int> triangleCount;

        public int sizeX;
        public int sizeY;
        public int sizeZ;
        public float voxelSize;
        public float isoLevel;

        public void Execute(int index)
        {
            // Convertir index en 3D
            int x = index % (sizeX - 1);
            int y = (index / (sizeX - 1)) % (sizeY - 1);
            int z = index / ((sizeX - 1) * (sizeY - 1));

            // 8 coins du cube
            float3 cubeCorners0 = new float3(x + 0, y + 0, z + 0) * voxelSize;
            float3 cubeCorners1 = new float3(x + 1, y + 0, z + 0) * voxelSize;
            float3 cubeCorners2 = new float3(x + 1, y + 1, z + 0) * voxelSize;
            float3 cubeCorners3 = new float3(x + 0, y + 1, z + 0) * voxelSize;
            float3 cubeCorners4 = new float3(x + 0, y + 0, z + 1) * voxelSize;
            float3 cubeCorners5 = new float3(x + 1, y + 0, z + 1) * voxelSize;
            float3 cubeCorners6 = new float3(x + 1, y + 1, z + 1) * voxelSize;
            float3 cubeCorners7 = new float3(x + 0, y + 1, z + 1) * voxelSize;

            // Densit?s
            float val0 = densities[GetIndex(x + 0, y + 0, z + 0)];
            float val1 = densities[GetIndex(x + 1, y + 0, z + 0)];
            float val2 = densities[GetIndex(x + 1, y + 1, z + 0)];
            float val3 = densities[GetIndex(x + 0, y + 1, z + 0)];
            float val4 = densities[GetIndex(x + 0, y + 0, z + 1)];
            float val5 = densities[GetIndex(x + 1, y + 0, z + 1)];
            float val6 = densities[GetIndex(x + 1, y + 1, z + 1)];
            float val7 = densities[GetIndex(x + 0, y + 1, z + 1)];

            // Configuration
            int configIndex = 0;
            if (val0 > isoLevel) configIndex |= 1;
            if (val1 > isoLevel) configIndex |= 2;
            if (val2 > isoLevel) configIndex |= 4;
            if (val3 > isoLevel) configIndex |= 8;
            if (val4 > isoLevel) configIndex |= 16;
            if (val5 > isoLevel) configIndex |= 32;
            if (val6 > isoLevel) configIndex |= 64;
            if (val7 > isoLevel) configIndex |= 128;

            // Skip si vide ou plein
            if (configIndex == 0 || configIndex == 255)
                return;

            // Interpoler les 12 ar?tes
            float3 edge0 = Interpolate(cubeCorners0, cubeCorners1, val0, val1);
            float3 edge1 = Interpolate(cubeCorners1, cubeCorners2, val1, val2);
            float3 edge2 = Interpolate(cubeCorners2, cubeCorners3, val2, val3);
            float3 edge3 = Interpolate(cubeCorners3, cubeCorners0, val3, val0);
            float3 edge4 = Interpolate(cubeCorners4, cubeCorners5, val4, val5);
            float3 edge5 = Interpolate(cubeCorners5, cubeCorners6, val5, val6);
            float3 edge6 = Interpolate(cubeCorners6, cubeCorners7, val6, val7);
            float3 edge7 = Interpolate(cubeCorners7, cubeCorners4, val7, val4);
            float3 edge8 = Interpolate(cubeCorners0, cubeCorners4, val0, val4);
            float3 edge9 = Interpolate(cubeCorners1, cubeCorners5, val1, val5);
            float3 edge10 = Interpolate(cubeCorners2, cubeCorners6, val2, val6);
            float3 edge11 = Interpolate(cubeCorners3, cubeCorners7, val3, val7);

            int tableBase = configIndex * 16;

            for (int i = 0; i < 15; i += 3)
            {
                int edgeIndex1 = triangleTable[tableBase + i];
                if (edgeIndex1 == -1) break;

                int edgeIndex2 = triangleTable[tableBase + i + 1];
                int edgeIndex3 = triangleTable[tableBase + i + 2];

                float3 v1 = GetEdgeVertex(edgeIndex1, edge0, edge1, edge2, edge3, edge4, edge5, edge6, edge7, edge8, edge9, edge10, edge11);
                float3 v2 = GetEdgeVertex(edgeIndex2, edge0, edge1, edge2, edge3, edge4, edge5, edge6, edge7, edge8, edge9, edge10, edge11);
                float3 v3 = GetEdgeVertex(edgeIndex3, edge0, edge1, edge2, edge3, edge4, edge5, edge6, edge7, edge8, edge9, edge10, edge11);

                int triIndex = System.Threading.Interlocked.Increment(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(triangleCount.AsSpan())) - 1;

                if (triIndex < triangleData.Length)
                {
                    triangleData[triIndex] = new TriangleData
                    {
                        v1 = v1,
                        v2 = v2,
                        v3 = v3
                    };
                }
            }
        }

        private float3 Interpolate(float3 v1, float3 v2, float val1, float val2)
        {
            if (math.abs(isoLevel - val1) < 0.00001f) return v1;
            if (math.abs(isoLevel - val2) < 0.00001f) return v2;
            if (math.abs(val1 - val2) < 0.00001f) return v1;

            float t = (isoLevel - val1) / (val2 - val1);
            return v1 + t * (v2 - v1);
        }

        private float3 GetEdgeVertex(int edge, float3 e0, float3 e1, float3 e2, float3 e3, float3 e4, float3 e5, float3 e6, float3 e7, float3 e8, float3 e9, float3 e10, float3 e11)
        {
            switch (edge)
            {
                case 0: return e0;
                case 1: return e1;
                case 2: return e2;
                case 3: return e3;
                case 4: return e4;
                case 5: return e5;
                case 6: return e6;
                case 7: return e7;
                case 8: return e8;
                case 9: return e9;
                case 10: return e10;
                case 11: return e11;
                default: return float3.zero;
            }
        }



        private int GetIndex(int x, int y, int z)
        {
            return x + sizeX * (y + sizeY * z);
        }
    }
}