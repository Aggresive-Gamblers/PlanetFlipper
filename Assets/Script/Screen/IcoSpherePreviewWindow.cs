#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public class IcoSpherePreviewWindow : EditorWindow
{
    private GameObject targetObject;
    private int resolution = 2048;
    private bool transparentBackground = true;
    private Color backgroundColor = Color.clear;
    private float padding = 1.1f;
    private string fileName = "icosphere_preview.png";

    [MenuItem("Tools/IcoSphere Preview")]
    public static void ShowWindow() => GetWindow<IcoSpherePreviewWindow>("IcoSphere Preview");

    void OnGUI()
    {
        GUILayout.Label("Capture d'image d'un GameObject (IcoSphere)", EditorStyles.boldLabel);
        targetObject = (GameObject)EditorGUILayout.ObjectField("Target GameObject", targetObject, typeof(GameObject), true);
        resolution = EditorGUILayout.IntField("Résolution (px)", resolution);
        transparentBackground = EditorGUILayout.Toggle("Fond transparent (PNG)", transparentBackground);
        if (!transparentBackground)
            backgroundColor = EditorGUILayout.ColorField("Background Color", backgroundColor);
        padding = EditorGUILayout.FloatField("Padding (zoom)", padding);
        fileName = EditorGUILayout.TextField("Nom fichier", fileName);

        if (GUILayout.Button("Générer et sauvegarder PNG"))
        {
            if (targetObject == null)
            {
                EditorUtility.DisplayDialog("Erreur", "Sélectionne un GameObject contenant le mesh (ou prefab).", "OK");
                return;
            }
            SavePreview(targetObject, resolution, transparentBackground, backgroundColor, padding, fileName);
        }
    }

    static void SavePreview(GameObject go, int res, bool transparent, Color bgColor, float padding, string defaultFileName)
    {
        GameObject temp = (GameObject)PrefabUtility.InstantiatePrefab(go) ?? Object.Instantiate(go);

        foreach (var meshGen in temp.GetComponentsInChildren<IcoSphereGenerator>())
            meshGen.GenerateMesh();

        foreach (var comp in temp.GetComponentsInChildren<MonoBehaviour>())
        {
            if (!(comp is MeshRenderer)) comp.enabled = false;
        }

        var renderers = temp.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            Debug.LogError("Aucun Renderer trouvé !");
            Object.DestroyImmediate(temp);
            return;
        }

        Bounds b = new Bounds(renderers[0].bounds.center, Vector3.zero);
        foreach (var r in renderers) b.Encapsulate(r.bounds);

        // Créer la caméra
        GameObject camGO = new GameObject("TempCamera");
        Camera cam = camGO.AddComponent<Camera>();
        cam.backgroundColor = bgColor;
        cam.clearFlags = transparent ? CameraClearFlags.SolidColor : CameraClearFlags.Color;
        cam.orthographic = false;
        cam.nearClipPlane = 0.01f;
        cam.farClipPlane = b.size.magnitude * 10f;
        cam.cullingMask = ~0;

        Vector3 center = b.center;
        float radius = b.extents.magnitude * padding;
        cam.transform.position = center + new Vector3(0, 0, radius * 2f);
        cam.transform.LookAt(center);

        GameObject lightGO = new GameObject("TempLight");
        Light l = lightGO.AddComponent<Light>();
        l.type = LightType.Directional;
        l.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        l.intensity = 1.2f;

        RenderTexture rt = new RenderTexture(res, res, 24, RenderTextureFormat.ARGB32);
        rt.Create();
        cam.targetTexture = rt;

        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;
        cam.Render();

        Texture2D tex = new Texture2D(res, res, TextureFormat.ARGB32, false);
        tex.ReadPixels(new Rect(0, 0, res, res), 0, 0);
        tex.Apply();

        //Sauvegarder PNG
        string path = EditorUtility.SaveFilePanel("Sauvegarder l'image", Application.dataPath, defaultFileName, "png");
        if (!string.IsNullOrEmpty(path)) File.WriteAllBytes(path, tex.EncodeToPNG());

        cam.targetTexture = null;
        RenderTexture.active = prev;
        rt.Release();
        Object.DestroyImmediate(tex);
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(camGO);
        Object.DestroyImmediate(lightGO);
        Object.DestroyImmediate(temp);

        Debug.Log(" Capture terminée : " + path);
    }
}
#endif
