using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.AI.Navigation;
using Object = UnityEngine.Object;

// One-time Editor conversion. No runtime generation or imported asset changes.
public static class CampingTerrainConverter
{
    const string Folder = "Assets/Maps/PalmovCamping";
    const string Backup = "Assets/Scenes/Scene1_BeforeTerrain_20261006.unity";
    const int N = 513;
    [MenuItem("GunQuest/Terrain/Convert Camping Ground")]
    public static void Convert()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.path != "Assets/Scenes/Scene1.unity") throw new InvalidOperationException("Open Scene1 in Edit Mode.");
        if (GameObject.Find("CampingTerrain") != null || AssetDatabase.LoadAssetAtPath<TerrainData>(Folder + "/CampingTerrain.asset") != null)
            throw new InvalidOperationException("Converted terrain already exists. Refusing to overwrite sculpted terrain.");
        MeshFilter source = null;
        foreach (var f in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None)) if (f.name == "land camping forest") source = f;
        if (source == null) throw new InvalidOperationException("Ground source not found.");
        EditorSceneManager.SaveScene(scene);
        if (!File.Exists(Backup) && !AssetDatabase.CopyAsset(scene.path, Backup)) throw new IOException("Scene backup failed.");
        var renderer = source.GetComponent<MeshRenderer>();
        var mesh = source.sharedMesh;
        var vertices = mesh.vertices; var uv = mesh.uv; var indices = mesh.triangles;
        var bounds = renderer.bounds;
        for (int i = 0; i < vertices.Length; i++) vertices[i] = source.transform.TransformPoint(vertices[i]);
        float baseY = bounds.min.y - 0.05f, rangeY = Mathf.Max(1f, bounds.size.y + 0.1f);
        var heights = new float[N,N]; var coverage = new bool[N,N]; var colors = new Color[N*N];
        var palette = Readable(renderer.sharedMaterial.mainTexture);
        var cliffs = new List<int>(); int surfaces = 0;
        for (int t = 0; t < indices.Length; t += 3)
        {
            int ia = indices[t], ib = indices[t+1], ic = indices[t+2];
            Vector3 a = vertices[ia], b = vertices[ib], c = vertices[ic];
            if (Vector3.Cross(b-a,c-a).normalized.y < 0.25f) { cliffs.Add(ia); cliffs.Add(ib); cliffs.Add(ic); continue; }
            surfaces++;
            Vector2 pa = Grid(a,bounds), pb = Grid(b,bounds), pc = Grid(c,bounds);
            float d = (pb.y-pc.y)*(pa.x-pc.x)+(pc.x-pb.x)*(pa.y-pc.y);
            if (Mathf.Abs(d) < 0.000001f) continue;
            int minX = Mathf.Max(0,Mathf.FloorToInt(Mathf.Min(pa.x,Mathf.Min(pb.x,pc.x))));
            int maxX = Mathf.Min(N-1,Mathf.CeilToInt(Mathf.Max(pa.x,Mathf.Max(pb.x,pc.x))));
            int minZ = Mathf.Max(0,Mathf.FloorToInt(Mathf.Min(pa.y,Mathf.Min(pb.y,pc.y))));
            int maxZ = Mathf.Min(N-1,Mathf.CeilToInt(Mathf.Max(pa.y,Mathf.Max(pb.y,pc.y))));
            for (int z=minZ;z<=maxZ;z++) for(int x=minX;x<=maxX;x++)
            {
                float wa=((pb.y-pc.y)*(x-pc.x)+(pc.x-pb.x)*(z-pc.y))/d;
                float wb=((pc.y-pa.y)*(x-pc.x)+(pa.x-pc.x)*(z-pc.y))/d, wc=1-wa-wb;
                if(wa < -0.0001f || wb < -0.0001f || wc < -0.0001f) continue;
                float h=(a.y*wa+b.y*wb+c.y*wc-baseY)/rangeY;
                if(coverage[z,x] && h<heights[z,x]) continue;
                coverage[z,x]=true; heights[z,x]=h;
                Vector2 texUV=Vector2.Scale(uv[ia]*wa+uv[ib]*wb+uv[ic]*wc,renderer.sharedMaterial.mainTextureScale)+renderer.sharedMaterial.mainTextureOffset;
                colors[z*N+x]=palette.GetPixel(Mathf.Clamp(Mathf.FloorToInt(texUV.x*palette.width),0,palette.width-1),Mathf.Clamp(Mathf.FloorToInt(texUV.y*palette.height),0,palette.height-1))*renderer.sharedMaterial.color;
            }
        }
        Object.DestroyImmediate(palette);
        int missing=0;
        for(int z=0;z<N;z++) for(int x=0;x<N;x++)
        {
            if(coverage[z,x]) continue;
            missing++; bool found=false;
            for(int radius=1;radius<=8 && !found;radius++) for(int dz=-radius;dz<=radius && !found;dz++) for(int dx=-radius;dx<=radius && !found;dx++)
            {
                int nx=x+dx,nz=z+dz;
                if(nx<0||nz<0||nx>=N||nz>=N||!coverage[nz,nx]) continue;
                heights[z,x]=heights[nz,nx]; colors[z*N+x]=colors[nz*N+nx]; found=true;
            }
            if(!found) colors[z*N+x]=new Color(0.38f,0.52f,0.2f);
        }
        var baked=new Texture2D(N,N,TextureFormat.RGB24,false); baked.SetPixels(colors); baked.Apply();
        string texPath=Folder+"/CampingGroundAlbedo.png"; File.WriteAllBytes(texPath,baked.EncodeToPNG()); Object.DestroyImmediate(baked);
        AssetDatabase.ImportAsset(texPath);
        var importer=(TextureImporter)AssetImporter.GetAtPath(texPath);
        importer.wrapMode=TextureWrapMode.Clamp; importer.filterMode=FilterMode.Point;
        importer.textureCompression=TextureImporterCompression.Uncompressed; importer.mipmapEnabled=false; importer.SaveAndReimport();
        var layers=new TerrainLayer[] {
            Layer("OriginalGround",AssetDatabase.LoadAssetAtPath<Texture2D>(texPath),new Vector2(bounds.size.x,bounds.size.z)),
            Layer("Grass",Solid("Grass",new Color(0.45f,0.62f,0.23f)),new Vector2(8,8)),
            Layer("Dirt",Solid("Dirt",new Color(0.82f,0.65f,0.36f)),new Vector2(8,8)),
            Layer("Rock",Solid("Rock",new Color(0.43f,0.44f,0.4f)),new Vector2(8,8)) };
        var data=new TerrainData {name="CampingTerrain",heightmapResolution=N,size=new Vector3(bounds.size.x,rangeY,bounds.size.z),alphamapResolution=512,baseMapResolution=1024,terrainLayers=layers};
        data.SetHeights(0,0,heights);
        var alpha=new float[512,512,4]; var holes=new bool[512,512];
        for(int z=0;z<512;z++) for(int x=0;x<512;x++) {alpha[z,x,0]=1;holes[z,x]=coverage[z,x]||coverage[z+1,x]||coverage[z,x+1]||coverage[z+1,x+1];}
        data.SetAlphamaps(0,0,alpha); data.SetHoles(0,0,holes); AssetDatabase.CreateAsset(data,Folder+"/CampingTerrain.asset");
        var go=Terrain.CreateTerrainGameObject(data); Undo.RegisterCreatedObjectUndo(go,"Convert camping terrain"); go.name="CampingTerrain";
        // Parent to map root with identity world scale, so Terrain world dimensions remain correct.
        var map=GameObject.Find("PalmovMap_CampingOutpost"); go.transform.SetParent(map.transform,true);
        go.transform.position=new Vector3(bounds.min.x,baseY,bounds.min.z);
        var terrain=go.GetComponent<Terrain>(); terrain.heightmapPixelError=1;terrain.basemapDistance=1000;terrain.drawInstanced=true;
        var shader=Shader.Find("Universal Render Pipeline/Terrain/Lit");
        if(shader==null) throw new InvalidOperationException("URP Terrain shader missing.");
        terrain.materialTemplate=new Material(shader); AssetDatabase.CreateAsset(terrain.materialTemplate,Folder+"/CampingTerrainMaterial.mat");
        if(cliffs.Count>0)
        {
            var cliffMesh=Object.Instantiate(mesh);cliffMesh.name="CampingCliffs";cliffMesh.triangles=cliffs.ToArray();cliffMesh.RecalculateBounds();
            AssetDatabase.CreateAsset(cliffMesh,Folder+"/CampingCliffs.asset");
            var cliffGO=new GameObject("CampingCliffs");Undo.RegisterCreatedObjectUndo(cliffGO,"Preserve cliffs");cliffGO.transform.SetParent(source.transform,false);
            cliffGO.AddComponent<MeshFilter>().sharedMesh=cliffMesh;cliffGO.AddComponent<MeshRenderer>().sharedMaterials=renderer.sharedMaterials;cliffGO.AddComponent<MeshCollider>().sharedMesh=cliffMesh;
        }
        Undo.RecordObject(renderer,"Disable original ground");renderer.enabled=false;PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
        foreach(var collider in source.GetComponents<Collider>()) {Undo.RecordObject(collider,"Disable original ground collider");collider.enabled=false;PrefabUtility.RecordPrefabInstancePropertyModifications(collider);}
        BuildBridgeApproaches(terrain);
        Physics.SyncTransforms();
        foreach(var surface in Object.FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None))
        {
            Undo.RecordObject(surface,"Rebuild terrain navigation");surface.BuildNavMesh();
            if(surface.navMeshData!=null && !AssetDatabase.Contains(surface.navMeshData)) AssetDatabase.CreateAsset(surface.navMeshData,Folder+"/CampingTerrain_NavMesh.asset");
            EditorUtility.SetDirty(surface);
        }
        EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);Selection.activeGameObject=go;
        Debug.Log($"Terrain converted: {surfaces} surface triangles, {cliffs.Count/3} preserved faces, {missing} uncovered samples. Backup: {Backup}");
    }
    static void BuildBridgeApproaches(Terrain terrain)
    {
        var bridge = GameObject.Find("BridgeDeckCollision").GetComponent<MeshCollider>();
        var original = bridge.sharedMesh.vertices;
        Vector3 first = bridge.transform.TransformPoint(original[0]);
        Vector3 second = bridge.transform.TransformPoint(original[1]);
        Vector3 last = bridge.transform.TransformPoint(original[original.Length - 2]);
        float[] zs = { first.z - 1.6f, first.z + 0.001f, first.z + 0.94f,
            last.z - 0.94f, last.z - 0.001f, last.z + 1.6f };
        var vertices = new Vector3[12];
        for (int row = 0; row < 6; row++) for (int side = 0; side < 2; side++)
        {
            float x = side == 0 ? first.x : second.x;
            float y;
            if (row == 0 || row == 5)
                y = terrain.SampleHeight(new Vector3(x, 0, zs[row])) + terrain.transform.position.y + 0.015f;
            else
            {
                if (!bridge.Raycast(new Ray(new Vector3(x + (side == 0 ? 0.01f : -0.01f), 20, zs[row]), Vector3.down), out var hit, 30))
                    throw new InvalidOperationException("Bridge height sample failed.");
                y = hit.point.y + 0.04f;
            }
            vertices[row * 2 + side] = new Vector3(x, y, zs[row]);
        }
        var mesh = new Mesh { name = "BridgeTerrainApproaches", vertices = vertices,
            triangles = new int[] {0,2,1,1,2,3,2,4,3,3,4,5,6,8,7,7,8,9,8,10,9,9,10,11} };
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, Folder + "/BridgeTerrainApproaches.asset");
        var go = new GameObject("BridgeTerrainApproaches");
        Undo.RegisterCreatedObjectUndo(go, "Bridge terrain transitions");
        go.transform.SetParent(terrain.transform.parent, true);
        go.AddComponent<MeshCollider>().sharedMesh = mesh;
    }
    static Vector2 Grid(Vector3 v,Bounds b)=>new Vector2((v.x-b.min.x)/b.size.x*(N-1),(v.z-b.min.z)/b.size.z*(N-1));
    static Texture2D Readable(Texture texture)
    {
        var previous=RenderTexture.active;var rt=RenderTexture.GetTemporary(texture.width,texture.height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
        try {Graphics.Blit(texture,rt);RenderTexture.active=rt;var copy=new Texture2D(texture.width,texture.height,TextureFormat.RGBA32,false);copy.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0);copy.Apply();return copy;}
        finally {RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);}
    }
    static Texture2D Solid(string name,Color color)
    {
        var texture=new Texture2D(4,4,TextureFormat.RGB24,false);var pixels=new Color[16];for(int i=0;i<16;i++) pixels[i]=color;texture.SetPixels(pixels);texture.Apply();
        string path=Folder+"/Terrain"+name+".png";File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path);return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static TerrainLayer Layer(string name,Texture2D texture,Vector2 tile)
    {
        var layer=new TerrainLayer {name=name,diffuseTexture=texture,tileSize=tile,metallic=0,smoothness=0};AssetDatabase.CreateAsset(layer,Folder+"/Terrain"+name+".terrainlayer");return layer;
    }
}

