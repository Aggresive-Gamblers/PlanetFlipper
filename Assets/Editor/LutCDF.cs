using UnityEngine;
using UnityEditor;
using System.Linq;

public class LinearizeFunctionLUT
{
    const int N = 256;

    [MenuItem("Tools/Generate Linearization LUT")]
    static void Generate()
    {
        var samples = new (float x, float y)[N];

        // 1. Sample f(x)
        for (int i = 0; i < N; i++)
        {
            float x = i / (float)(N - 1);

            // 🔴 TA FONCTION NON LINÉAIRE ICI
            float y = Mathf.SmoothStep(0.52f, 0.63f, x);

            samples[i] = (x, y);
        }

        // 2. Sort by y
        var sorted = samples.OrderBy(s => s.y).ToArray();

        // 3. Create texture
        var tex = new Texture2D(N, 1, TextureFormat.RFloat, false, true);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        for (int i = 0; i < N; i++)
        {
            float linearX = i / (float)(N - 1);
            tex.SetPixel(i, 0, new Color(linearX, 0, 0, 1));
        }

        tex.Apply();

        AssetDatabase.CreateAsset(tex, "Assets/LinearizeLUT.asset");
        Debug.Log("LUT generated");
    }
}
