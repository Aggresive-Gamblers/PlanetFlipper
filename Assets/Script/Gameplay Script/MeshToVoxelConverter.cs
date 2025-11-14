using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using System.Collections.Generic;

public class MeshToVoxelConverter
{
    public static void VoxelizeMesh(IcoSphere icoSphere, AstroneerPlanetSystem.VoxelChunk chunk)
    {
        Mesh mesh = icoSphere.GetComponent<MeshFilter>().sharedMesh;
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;

        var job = new VoxelizeMeshJob
        {
            densities = chunk.densities,
            resolution = chunk.resolution,
            chunkSize = chunk.size,
            chunkCenter = chunk.worldCenter,
            vertices = new NativeArray<float3>(vertices.Length, Allocator.TempJob),
            triangles = new NativeArray<int>(triangles.Length, Allocator.TempJob)
        };

        for (int i = 0; i < vertices.Length; i++)
            job.vertices[i] = vertices[i];

        for (int i = 0; i < triangles.Length; i++)
            job.triangles[i] = triangles[i];

        job.Schedule(chunk.densities.Length, 64).Complete();

        job.vertices.Dispose();
        job.triangles.Dispose();
    }

    [BurstCompile(CompileSynchronously = true)]
    struct VoxelizeMeshJob : IJobParallelFor
    {
        public NativeArray<float> densities;
        public int resolution;
        public float chunkSize;
        public float3 chunkCenter;

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

            // Calculer SDF minimal au mesh
            float minDist = float.MaxValue;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                float3 v0 = vertices[triangles[i]];
                float3 v1 = vertices[triangles[i + 1]];
                float3 v2 = vertices[triangles[i + 2]];

                float dist = DistancePointToTriangle(worldPos, v0, v1, v2);
                if (dist < minDist) minDist = dist;
            }

            // D?terminer si inside/outside avec raycast simple (Y+)
            bool inside = IsInsideMesh(worldPos);

            densities[index] = inside ? 0.0f - minDist : 0.5f + minDist; // isoLevel ~0.5
        }

        private bool IsInsideMesh(float3 point)
        {
            int hits = 0;
            float3 dir = new float3(0, 1, 0);
            for (int t = 0; t < triangles.Length; t += 3)
            {
                float3 v0 = vertices[triangles[t]];
                float3 v1 = vertices[triangles[t + 1]];
                float3 v2 = vertices[triangles[t + 2]];
                if (RayIntersectsTriangle(point, dir, v0, v1, v2))
                    hits++;
            }
            return hits % 2 == 1;
        }

        private bool RayIntersectsTriangle(float3 origin, float3 dir, float3 v0, float3 v1, float3 v2)
        {
            float3 e1 = v1 - v0;
            float3 e2 = v2 - v0;
            float3 h = math.cross(dir, e2);
            float a = math.dot(e1, h);
            if (math.abs(a) < 0.0001f) return false;
            float f = 1.0f / a;
            float3 s = origin - v0;
            float u = f * math.dot(s, h);
            if (u < 0.0f || u > 1.0f) return false;
            float3 q = math.cross(s, e1);
            float v = f * math.dot(dir, q);
            if (v < 0.0f || u + v > 1.0f) return false;
            float t = f * math.dot(e2, q);
            return t > 0.0001f;
        }

        private float DistancePointToTriangle(float3 p, float3 a, float3 b, float3 c)
        {
            // https://stackoverflow.com/questions/9605556/how-to-project-a-point-onto-a-triangle
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
                float v = d1 / (d1 - d3);
                return math.length(ap - v * ab);
            }

            float3 cp = p - c;
            float d5 = math.dot(ab, cp);
            float d6 = math.dot(ac, cp);
            if (d6 >= 0f && d5 <= d6) return math.length(cp);

            float vb = d5 * d2 - d1 * d6;
            if (vb <= 0f && d2 >= 0f && d6 <= 0f)
            {
                float w = d2 / (d2 - d6);
                return math.length(ap - w * ac);
            }

            float va = d3 * d6 - d5 * d4;
            if (va <= 0f && (d4 - d3) >= 0f && (d5 - d6) >= 0f)
            {
                float w = (d4 - d3) / ((d4 - d3) + (d5 - d6));
                return math.length((b + w * (c - b)) - p);
            }

            float3 n = math.cross(b - a, c - a);
            n = math.normalize(n);
            return math.abs(math.dot(p - a, n));
        }
    }
}