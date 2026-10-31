#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

// Editor-only helper: renders prefab previews and game-view shots for the presentation.
// Output folder: <project>/Documentation/Slides
public static class SlideCapture
{
    const string OutDir = "Documentation/Slides";
    static readonly string[] PrefabPaths =
    {
        "Assets/Prefabs/Enemy/BlueSlime.prefab", "Assets/Prefabs/Enemy/Swarm09.prefab",
        "Assets/Prefabs/Enemy/SlimePBR.prefab", "Assets/Prefabs/Enemy/StarFish.prefab",
        "Assets/Prefabs/Enemy/CactusPBR.prefab", "Assets/Prefabs/Enemy/ChestMonsterPBRDefault.prefab",
        "Assets/Prefabs/Enemy/P01.prefab", "Assets/Prefabs/Enemy/TurtleShellPBR.prefab",
        "Assets/Prefabs/Enemy/P02.prefab", "Assets/Prefabs/Enemy/Swarm08.prefab",
        "Assets/Prefabs/Enemy/MushroomAngryPBR.prefab", "Assets/Prefabs/Enemy/BeholderPBRDefault.prefab",
        "Assets/Prefabs/Monster1.prefab", "Assets/Prefabs/Monster2.prefab", "Assets/Prefabs/Monster3.prefab",
        "Assets/Prefabs/Monster4.prefab", "Assets/Prefabs/Monster5.prefab",
        "Assets/Prefabs/Player/PlayerFemale.prefab",
        "Assets/Prefabs/Weapon/AssaultRiflePBR.prefab", "Assets/Prefabs/Weapon/PistolPBR.prefab",
        "Assets/Prefabs/Weapon/BaseBallBat.prefab",
        "Assets/Prefabs/Items/HealthPotionModel.prefab", "Assets/Prefabs/Items/GrenadeModel.prefab",
    };

    static string OutPath(string file)
    {
        string dir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", OutDir));
        Directory.CreateDirectory(dir);
        return System.IO.Path.Combine(dir, file);
    }

    [MenuItem("GunQuest/Slides/Capture Game View _F9")]
    static void CaptureGameView()
    {
        string file = "shot_" + System.DateTime.Now.ToString("HHmmss") + ".png";
        ScreenCapture.CaptureScreenshot(OutPath(file), 2);
        Debug.Log("[SlideCapture] Saved " + file);
    }

    [MenuItem("GunQuest/Slides/Render Prefab Previews")]
    static void RenderPrefabPreviews()
    {
        var pr = new PreviewRenderUtility();
        int done = 0;
        try
        {
            foreach (string path in PrefabPaths)
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (asset == null) { Debug.LogWarning("[SlideCapture] Missing " + path); continue; }
                var go = Object.Instantiate(asset);
                pr.AddSingleGO(go);
                if (Render(pr, go, System.IO.Path.GetFileNameWithoutExtension(path) + ".png")) done++;
                Object.DestroyImmediate(go);
            }
        }
        finally { pr.Cleanup(); }
        Debug.Log("[SlideCapture] Rendered " + done + " previews into " + OutDir);
    }

    static bool Render(PreviewRenderUtility pr, GameObject go, string file)
    {
        Bounds b = default; bool has = false;
        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            if (!(r is MeshRenderer) && !(r is SkinnedMeshRenderer)) continue;
            if (!has) { b = r.bounds; has = true; } else b.Encapsulate(r.bounds);
        }
        if (!has) return false;
        const int W = 800, H = 800;
        var cam = pr.camera;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color32(0x4A, 0x5C, 0x82, 255);
        cam.fieldOfView = 30; cam.nearClipPlane = .05f; cam.farClipPlane = 500;
        float dist = b.extents.magnitude / Mathf.Sin(cam.fieldOfView * .5f * Mathf.Deg2Rad) * 1.05f;
        Vector3 dir = Quaternion.Euler(12, 28, 0) * Vector3.forward;
        cam.transform.position = b.center + dir * dist;
        cam.transform.LookAt(b.center);
        pr.lights[0].intensity = 2.6f; pr.lights[0].transform.rotation = Quaternion.Euler(40, 150, 0);
        pr.lights[1].intensity = 1.6f; pr.lights[1].transform.rotation = Quaternion.Euler(340, 218, 177);
        pr.ambientColor = new Color(.55f, .55f, .6f);
        pr.BeginPreview(new Rect(0, 0, W, H), GUIStyle.none);
        pr.Render(true, false);
        var rt = (RenderTexture)pr.EndPreview();
        var prev = RenderTexture.active; RenderTexture.active = rt;
        var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); tex.Apply();
        RenderTexture.active = prev;
        File.WriteAllBytes(OutPath(file), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        return true;
    }
}
#endif
