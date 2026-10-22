#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class FemaleFirstPersonBuilder
{
    const string Folder = "Assets/Resources/";

    [MenuItem("GunQuest/Female/3 - Build First Person Meshes")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
        Directory.CreateDirectory(Folder);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/DefaultFemalePBR.prefab");
        foreach (var skin in prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (skin.name != "Body01" && skin.name != "Body02") continue;
            var original = skin.sharedMesh;
            var weights = original.boneWeights;
            var handBones = new HashSet<int>();
            for (int i = 0; i < skin.bones.Length; i++)
            {
                string name = skin.bones[i].name;
                if (name.StartsWith("hand_") || name.StartsWith("thumb_") ||
                    name.StartsWith("index_") || name.StartsWith("ring_")) handBones.Add(i);
            }
            Func<int, bool> keep = i =>
            {
                var w = weights[i];
                return (handBones.Contains(w.boneIndex0) ? w.weight0 : 0f) +
                    (handBones.Contains(w.boneIndex1) ? w.weight1 : 0f) +
                    (handBones.Contains(w.boneIndex2) ? w.weight2 : 0f) +
                    (handBones.Contains(w.boneIndex3) ? w.weight3 : 0f) >= 0.8f;
            };
            var mesh = Object.Instantiate(original);
            for (int sub = 0; sub < original.subMeshCount; sub++)
            {
                var triangles = original.GetTriangles(sub);
                var selected = new List<int>();
                for (int i = 0; i < triangles.Length; i += 3)
                    if (keep(triangles[i]) && keep(triangles[i + 1]) && keep(triangles[i + 2]))
                    {
                        selected.Add(triangles[i]);
                        selected.Add(triangles[i + 1]);
                        selected.Add(triangles[i + 2]);
                    }
                mesh.SetTriangles(selected, sub);
            }
            SaveMesh(mesh, "FemaleFirstPerson" + skin.name);
        }
        SaveMesh(Sleeve(), "FemaleFirstPersonSleeve");
        string path = Folder + "FemaleFirstPersonSleeve.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, path);
        }
        material.color = new Color(0.15f, 0.055f, 0.45f);
        material.SetFloat("_Smoothness", 0.15f);
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
        Debug.Log("Female FPS hands and capped low-poly sleeves saved.");
    }

    static void SaveMesh(Mesh mesh, string name)
    {
        mesh.name = name;
        string path = Folder + name + ".asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing == null) AssetDatabase.CreateAsset(mesh, path);
        else
        {
            EditorUtility.CopySerialized(mesh, existing);
            EditorUtility.SetDirty(existing);
            Object.DestroyImmediate(mesh);
        }
    }

    static Mesh Sleeve()
    {
        // Unit length along Z; the wrist end is narrower. Separate face vertices
        // retain the low-poly shading. Both ends are capped.
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        const int sides = 8;
        Action<Vector3, Vector3, Vector3> triangle = (a, b, c) =>
        {
            int index = vertices.Count;
            vertices.Add(a); vertices.Add(b); vertices.Add(c);
            triangles.Add(index); triangles.Add(index + 1); triangles.Add(index + 2);
        };
        for (int i = 0; i < sides; i++)
        {
            float angle = i * Mathf.PI * 2f / sides, next = (i + 1) * Mathf.PI * 2f / sides;
            var a = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
            var b = new Vector3(Mathf.Cos(next), Mathf.Sin(next), 0);
            var c = new Vector3(b.x * 0.7f, b.y * 0.7f, 1);
            var d = new Vector3(a.x * 0.7f, a.y * 0.7f, 1);
            triangle(a, b, c); triangle(a, c, d);
            triangle(Vector3.zero, b, a);
            triangle(Vector3.forward, d, c);
        }
        var mesh = new Mesh();
        mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        return mesh;
    }
}
#endif
