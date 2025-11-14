using Unity.Mathematics;
using UnityEngine;

public class VoxelData
{
    public int3 size;
    public float[] densities;
    public float voxelSize = 1f;

    public VoxelData(int3 size, float voxelSize = 1f)
    {
        this.size = size;
        this.voxelSize = voxelSize;
        this.densities = new float[size.x * size.y * size.z];
    }

    public int GetIndex(int x, int y, int z)
    {
        return x + size.x * (y + size.y * z);
    }

    public float GetDensity(int x, int y, int z)
    {
        if (x < 0 || x >= size.x || y < 0 || y >= size.y || z < 0 || z >= size.z)
            return 0f;

        return densities[GetIndex(x, y, z)];
    }

    public void SetDensity(int x, int y, int z, float value)
    {
        if (x < 0 || x >= size.x || y < 0 || y >= size.y || z < 0 || z >= size.z)
            return;

        densities[GetIndex(x, y, z)] = value;
    }

    public float3 IndexToWorldPosition(int x, int y, int z)
    {
        return new float3(x, y, z) * voxelSize;
    }

    public int3 WorldToVoxelPosition(float3 worldPos)
    {
        return new int3(
            Mathf.RoundToInt(worldPos.x / voxelSize),
            Mathf.RoundToInt(worldPos.y / voxelSize),
            Mathf.RoundToInt(worldPos.z / voxelSize)
        );
    }
}