using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Collections.Generic;

public static class SceneCleaner
{
    [MenuItem("GunQuest/Clean Scene Hierarchy", false, 100)]
    public static void CleanAllScenes()
    {
        CleanPlayerPrefab("Assets/Prefabs/Player/PlayerFemale.prefab");
        CleanPlayerPrefab("Assets/Prefabs/Player.prefab");

        CleanScene("Assets/Scenes/AutumnValley.unity");
        CleanScene("Assets/Scenes/IceboundPass.unity");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[SceneCleaner] All scenes and player prefabs cleaned successfully!");
    }

    public static void CleanScene(string scenePath)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        int removedCount = 0;

        // 1. Clean Player in scene
        var player = GameObject.Find("Player");
        if (player != null)
        {
            UnpackCompletely(player);

            string[] deadParts = new string[] { "Feet", "Tail", "Wing", "Body", "Root" };
            for (int i = player.transform.childCount - 1; i >= 0; i--)
            {
                var child = player.transform.GetChild(i);
                if (System.Array.IndexOf(deadParts, child.name) >= 0)
                {
                    Debug.Log($"[{scene.name}] Removed dead player part: {child.name}");
                    Object.DestroyImmediate(child.gameObject);
                    removedCount++;
                }
            }
        }

        // 2. Unpack and dissolve NestedParent_Unpack
        var nestedParents = GameObject.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var t in nestedParents)
        {
            if (t != null && t.gameObject.name == "NestedParent_Unpack")
            {
                var go = t.gameObject;
                UnpackCompletely(go);

                // Collect children first
                List<Transform> children = new List<Transform>();
                for (int i = 0; i < go.transform.childCount; i++)
                {
                    children.Add(go.transform.GetChild(i));
                }

                foreach (var child in children)
                {
                    UnpackCompletely(child.gameObject);
                    child.SetParent(null);
                    Debug.Log($"[{scene.name}] Unnested {child.name} to root");
                }

                Object.DestroyImmediate(go);
                removedCount++;
            }
        }

        // 3. Clean legacy/orphan root objects
        var roots = scene.GetRootGameObjects();
        foreach (var root in roots)
        {
            if (root == null) continue;

            // Legacy prototype Canvas (inactive Canvas with HP/Inventory)
            if (root.name == "Canvas" && !root.activeSelf)
            {
                Debug.Log($"[{scene.name}] Removed legacy Canvas");
                Object.DestroyImmediate(root);
                removedCount++;
            }
            // Orphan duplicate ExtractionMarker at root (inactive duplicate)
            else if (root.name == "ExtractionMarker" && !root.activeSelf)
            {
                Debug.Log($"[{scene.name}] Removed orphan root ExtractionMarker");
                Object.DestroyImmediate(root);
                removedCount++;
            }
            // Dead EditorOverview camera
            else if (root.name.EndsWith("_EditorOverview"))
            {
                Debug.Log($"[{scene.name}] Removed editor overview camera: {root.name}");
                Object.DestroyImmediate(root);
                removedCount++;
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[{scene.name}] Cleanup complete. Removed {removedCount} obsolete objects.");
    }

    public static void CleanPlayerPrefab(string prefabPath)
    {
        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        if (root == null) return;

        int removed = 0;
        string[] deadParts = new string[] { "Feet", "Tail", "Wing", "Body", "Root" };
        for (int i = root.transform.childCount - 1; i >= 0; i--)
        {
            var child = root.transform.GetChild(i);
            if (System.Array.IndexOf(deadParts, child.name) >= 0)
            {
                Debug.Log($"[Prefab] Removed dead part from {prefabPath}: {child.name}");
                Object.DestroyImmediate(child.gameObject);
                removed++;
            }
        }

        if (removed > 0)
        {
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Debug.Log($"[Prefab] Saved {prefabPath} with {removed} dead parts removed.");
        }
        PrefabUtility.UnloadPrefabContents(root);
    }

    private static void UnpackCompletely(GameObject go)
    {
        if (go == null) return;

        int safety = 0;
        while (PrefabUtility.IsPartOfPrefabInstance(go) && safety++ < 10)
        {
            var root = PrefabUtility.GetNearestPrefabInstanceRoot(go);
            if (root == null)
            {
                root = PrefabUtility.GetOutermostPrefabInstanceRoot(go);
            }
            if (root == null) break;

            PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        }
    }
}
