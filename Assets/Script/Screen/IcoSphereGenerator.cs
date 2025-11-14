using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class IcoSphereGenerator : MonoBehaviour
{
    [Range(0, 5)]
    public int subdivisions = 2;
    private MeshFilter mf;

    private void Awake() => GenerateMesh();
    private void OnEnable() => GenerateMesh();
    private void OnValidate() => GenerateMesh();

    public void GenerateMesh()
    {
        if (mf == null) mf = GetComponent<MeshFilter>();
        if (mf == null) return;

        mf.sharedMesh = IcoSphereBuilder.Build(subdivisions);
    }
}

public static class IcoSphereBuilder
{
    private struct Triangle { public int v1, v2, v3; public Triangle(int a, int b, int c) { v1 = a; v2 = b; v3 = c; } }

    public static Mesh Build(int recursion)
    {
        List<Vector3> vertices = new List<Vector3>();
        Dictionary<long, int> middleCache = new Dictionary<long, int>();
        List<Triangle> faces = new List<Triangle>();
        int index = 0;

        float t = (1f + Mathf.Sqrt(5f)) / 2f;

        vertices.Add(new Vector3(-1, t, 0).normalized);
        vertices.Add(new Vector3(1, t, 0).normalized);
        vertices.Add(new Vector3(-1, -t, 0).normalized);
        vertices.Add(new Vector3(1, -t, 0).normalized);
        vertices.Add(new Vector3(0, -1, t).normalized);
        vertices.Add(new Vector3(0, 1, t).normalized);
        vertices.Add(new Vector3(0, -1, -t).normalized);
        vertices.Add(new Vector3(0, 1, -t).normalized);
        vertices.Add(new Vector3(t, 0, -1).normalized);
        vertices.Add(new Vector3(t, 0, 1).normalized);
        vertices.Add(new Vector3(-t, 0, -1).normalized);
        vertices.Add(new Vector3(-t, 0, 1).normalized);
        index = vertices.Count;

        faces.AddRange(new Triangle[] {
            new Triangle(0,11,5), new Triangle(0,5,1), new Triangle(0,1,7), new Triangle(0,7,10), new Triangle(0,10,11),
            new Triangle(1,5,9), new Triangle(5,11,4), new Triangle(11,10,2), new Triangle(10,7,6), new Triangle(7,1,8),
            new Triangle(3,9,4), new Triangle(3,4,2), new Triangle(3,2,6), new Triangle(3,6,8), new Triangle(3,8,9),
            new Triangle(4,9,5), new Triangle(2,4,11), new Triangle(6,2,10), new Triangle(8,6,7), new Triangle(9,8,1)
        });

        for (int i = 0; i < recursion; i++)
        {
            List<Triangle> faces2 = new List<Triangle>();
            foreach (var tri in faces)
            {
                int a = GetMiddlePoint(tri.v1, tri.v2, ref vertices, ref middleCache, ref index);
                int b = GetMiddlePoint(tri.v2, tri.v3, ref vertices, ref middleCache, ref index);
                int c = GetMiddlePoint(tri.v3, tri.v1, ref vertices, ref middleCache, ref index);

                faces2.Add(new Triangle(tri.v1, a, c));
                faces2.Add(new Triangle(tri.v2, b, a));
                faces2.Add(new Triangle(tri.v3, c, b));
                faces2.Add(new Triangle(a, b, c));
            }
            faces = faces2;
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();

        List<int> triangles = new List<int>();
        foreach (var tri in faces)
        {
            triangles.Add(tri.v1);
            triangles.Add(tri.v2);
            triangles.Add(tri.v3);
        }
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static int GetMiddlePoint(int p1, int p2, ref List<Vector3> vertices, ref Dictionary<long, int> cache, ref int index)
    {
        long smaller = Mathf.Min(p1, p2);
        long greater = Mathf.Max(p1, p2);
        long key = (smaller << 32) + greater;

        if (cache.TryGetValue(key, out int ret)) return ret;

        Vector3 middle = ((vertices[p1] + vertices[p2]) * 0.5f).normalized;
        vertices.Add(middle);
        cache.Add(key, index);
        return index++;
    }
}
