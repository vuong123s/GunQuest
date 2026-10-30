#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

// Authored once in Editor. Terrain, props, navigation and gameplay references
// are saved assets; nothing is regenerated when the game starts.
public static class AutumnValleyMapBuilder
{
    public const string ScenePath = "Assets/Scenes/AutumnValley.unity";
    const string Folder = "Assets/Maps/AutumnValley";
    const string Pack = "Assets/DL/Palmov Island/Low Poly Atmospheric Locations Pack/Prefabs/";
    static Terrain terrain;
    static Transform map, vegetation, structures;
    static Material wood, darkWood, stone, brass, banner;
    static readonly Vector2[] MainRoute = {
        new Vector2(0,-48), new Vector2(-9,-36), new Vector2(-23,-23),
        new Vector2(-23,12), new Vector2(-15,25), new Vector2(2,38) };
    static readonly Vector2[] FlankRoute = {
        new Vector2(0,-48), new Vector2(22,-35), new Vector2(26,-20),
        new Vector2(26,15), new Vector2(22,29), new Vector2(2,38) };

    [MenuItem("GunQuest/Maps/Create Autumn Valley")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
        if (File.Exists(ScenePath) || Directory.Exists(Folder))
            throw new InvalidOperationException("Autumn Valley already exists. Refusing to overwrite authored map.");
        for (int i=0;i<EditorSceneManager.sceneCount;i++)
            if (EditorSceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Save your open scenes before building.");
        Directory.CreateDirectory(Folder);
        AssetDatabase.Refresh();
        if (!AssetDatabase.CopyAsset("Assets/Scenes/Scene1.unity", ScenePath))
            throw new IOException("Could not create gameplay scene copy.");
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var game = Object.FindFirstObjectByType<ContractGame>();
        var gameSO = new SerializedObject(game);
        var boss = (EnemyHealth)gameSO.FindProperty("contractTarget").objectReferenceValue;
        var player = Object.FindFirstObjectByType<PlayerMotor>();
        foreach (var name in new[]{"PalmovMap_CampingOutpost","GameplaySpawns","MapBoundary"})
        {
            var old = GameObject.Find(name);
            if (old != null) Object.DestroyImmediate(old);
        }
        var marker = GameObject.Find("ExtractionMarker");
        if(marker != null) Object.DestroyImmediate(marker);
        map = new GameObject("AutumnValley_Map").transform;
        vegetation = new GameObject("AutumnForest").transform;
        vegetation.SetParent(map);
        structures = new GameObject("Trails_Camps_Bridges").transform;
        structures.SetParent(map);
        wood = Mat("WeatheredWood",new Color(.31f,.16f,.075f));
        darkWood = Mat("DarkTimber",new Color(.15f,.095f,.06f));
        stone = Mat("AncientStone",new Color(.43f,.46f,.41f));
        brass = Mat("WarmMetal",new Color(.8f,.56f,.22f));
        banner = Mat("OutpostBanner",new Color(.6f,.16f,.11f));
        MakeTerrain();
        MakeRiver();
        MakeBridge(-23);
        MakeBridge(26);
        DressCamps();
        ScatterForest();
        MakeBoundary();
        var surface=map.gameObject.AddComponent<NavMeshSurface>();
        surface.collectObjects=CollectObjects.Children;
        surface.useGeometry=NavMeshCollectGeometry.PhysicsColliders;
        surface.overrideVoxelSize=true;
        surface.voxelSize=.15f;
        surface.minRegionArea=3;
        Physics.SyncTransforms();
        surface.BuildNavMesh();
        if (surface.navMeshData == null) throw new InvalidOperationException("Navigation bake failed.");
        AssetDatabase.CreateAsset(surface.navMeshData,Folder+"/AutumnValley_NavMesh.asset");

        // Keep existing controller, weapons, UI and target identity.
        if(PrefabUtility.IsPartOfPrefabInstance(player.gameObject))
            PrefabUtility.UnpackPrefabInstance(PrefabUtility.GetOutermostPrefabInstanceRoot(player.gameObject),
                PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
        player.transform.SetParent(null,true);
        player.transform.position=OnGround(0,-48)+Vector3.up*.08f;
        player.transform.rotation=Quaternion.Euler(0,-24,0);
        var enemyGroup=new GameObject("EnemyGroup").transform;
        boss.transform.SetParent(enemyGroup,true);
        PlaceOnNav(boss.transform,2,38);
        boss.transform.rotation=Quaternion.Euler(0,180,0);
        EnemyAnimatorAndPrefabBuilder.PopulateAutumnValley();
        var extraction = new GameObject("ValleyExtraction").transform;
        extraction.position=OnGround(0,-48)+Vector3.up*.03f;
        var extractMat=Mat("ExtractionJade",new Color(.14f,.77f,.61f));
        var disc=GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disc.name="ExtractionMarker";disc.transform.SetParent(extraction,false);
        disc.transform.localScale=new Vector3(4.6f,.035f,4.6f);
        disc.GetComponent<Renderer>().sharedMaterial=extractMat;
        Object.DestroyImmediate(disc.GetComponent<Collider>());
        disc.SetActive(false);
        gameSO=new SerializedObject(game);
        gameSO.FindProperty("contractTarget").objectReferenceValue=boss;
        gameSO.FindProperty("extractionPoint").objectReferenceValue=extraction;
        gameSO.FindProperty("extractionMarker").objectReferenceValue=disc;
        gameSO.FindProperty("reward").intValue=300;
        gameSO.FindProperty("missionTitle").stringValue="THUNG LŨNG LÁ ĐỎ";
        gameSO.FindProperty("missionObjective").stringValue="Vượt suối, hạ quái laser tại trại phía bắc.";
        gameSO.FindProperty("missionBriefing").stringValue=
            "Theo đường đất qua cầu phía tây hoặc đi vòng qua cầu phía đông.\nHạ quái laser tại trại cờ đỏ, rồi quay về vòng xanh ở trại xuất phát.";
        gameSO.ApplyModifiedPropertiesWithoutUndo();
        Lighting();
        var cam=Camera.main;
        foreach(var behaviour in cam.GetComponents<MonoBehaviour>())
            if(behaviour.GetType().Namespace!=null && behaviour.GetType().Namespace.Contains("Cinemachine"))
                behaviour.enabled=false;
        cam.fieldOfView=75;
        cam.transform.position=player.transform.position+Vector3.up*1.65f;
        cam.transform.rotation=Quaternion.Euler(0,-24,0);
        var preview=new GameObject("AutumnValley_EditorOverview",typeof(Camera));
        preview.tag="EditorOnly";
        var pc=preview.GetComponent<Camera>();pc.enabled=false;pc.farClipPlane=400;
        pc.fieldOfView=63;
        preview.transform.position=new Vector3(78,96,-90);
        preview.transform.LookAt(new Vector3(0,2,0));
        var builds=EditorBuildSettings.scenes.ToList();
        if(!builds.Any(s=>s.path==ScenePath))builds.Add(new EditorBuildSettingsScene(ScenePath,true));
        EditorBuildSettings.scenes=builds.ToArray();
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        if(SceneView.lastActiveSceneView!=null)
        {
            SceneView.lastActiveSceneView.pivot=new Vector3(0,2,0);
            SceneView.lastActiveSceneView.rotation=Quaternion.Euler(52,-30,0);
            SceneView.lastActiveSceneView.size=90;
        }
        Selection.activeGameObject=terrain.gameObject;
        Debug.Log("Autumn Valley saved: editable Terrain, two routes/bridges, five guards, laser target and extraction.");
    }

    static float RiverZ(float x)=>-2+3*Mathf.Sin(x*.045f);
    static float SegmentDistance(Vector2 p,Vector2 a,Vector2 b)
    {
        Vector2 d=b-a;
        return Vector2.Distance(p,a+d*Mathf.Clamp01(Vector2.Dot(p-a,d)/d.sqrMagnitude));
    }
    static float RouteDistance(Vector2 p)
    {
        float d=float.MaxValue;
        foreach(var route in new[]{MainRoute,FlankRoute})
            for(int i=1;i<route.Length;i++)d=Mathf.Min(d,SegmentDistance(p,route[i-1],route[i]));
        return d;
    }
    static float Height(float x,float z)
    {
        float h=2.8f+.45f*Mathf.Sin(x*.1f)*Mathf.Cos(z*.08f)+
            1.2f*Mathf.PerlinNoise((x+200)*.035f,(z+200)*.035f);
        h+=8*Mathf.Exp(-((x-43)*(x-43)+(z-28)*(z-28))/240);
        h+=6*Mathf.Exp(-((x+43)*(x+43)+(z-15)*(z-15))/230);
        float edge=Mathf.Clamp01((Mathf.Max(Mathf.Abs(x),Mathf.Abs(z))-47)/25);
        h+=17*edge*edge;
        float path=RouteDistance(new Vector2(x,z));
        h=Mathf.Lerp(2.9f,h,Mathf.SmoothStep(0,1,(path-3)/5));
        float camp=Mathf.Min(Vector2.Distance(new Vector2(x,z),new Vector2(2,38)),
            Vector2.Distance(new Vector2(x,z),new Vector2(0,-48)));
        h=Mathf.Lerp(2.9f,h,Mathf.SmoothStep(0,1,(camp-12)/7));
        float river=Mathf.Abs(z-RiverZ(x));
        if(river<7)h=Mathf.Lerp(.35f,h,Mathf.SmoothStep(0,1,(river-2.5f)/4.5f));
        return h;
    }
    static void MakeTerrain()
    {
        var data=new TerrainData {name="AutumnValley_Terrain",heightmapResolution=513,
            size=new Vector3(144,32,144),alphamapResolution=512,baseMapResolution=1024};
        var heights=new float[513,513];
        for(int z=0;z<513;z++)for(int x=0;x<513;x++)
            heights[z,x]=Height(x*144f/512-72,z*144f/512-72)/32;
        data.SetHeights(0,0,heights);
        Color[] colors={new Color(.42f,.47f,.20f),new Color(.69f,.53f,.31f),
            new Color(.40f,.42f,.35f),new Color(.58f,.37f,.15f)};
        string[] names={"Meadow","Trail","Rock","FallenLeaves"};
        var layers=new TerrainLayer[4];
        for(int i=0;i<4;i++)
        {
            var texture=new Texture2D(4,4,TextureFormat.RGB24,false);
            texture.SetPixels(Enumerable.Repeat(colors[i],16).ToArray());texture.Apply();
            string path=Folder+"/"+names[i]+".png";
            File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            layers[i]=new TerrainLayer {name=names[i],diffuseTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(path),
                tileSize=new Vector2(8,8)};
            AssetDatabase.CreateAsset(layers[i],Folder+"/"+names[i]+".terrainlayer");
        }
        data.terrainLayers=layers;
        AssetDatabase.CreateAsset(data,Folder+"/AutumnValley_Terrain.asset");
        PaintTerrain(data);
        var go=Terrain.CreateTerrainGameObject(data);go.name="AutumnValley_Terrain";
        go.transform.SetParent(map,false);go.transform.position=new Vector3(-72,0,-72);
        terrain=go.GetComponent<Terrain>();terrain.heightmapPixelError=2;terrain.basemapDistance=1000;terrain.drawInstanced=true;
        terrain.materialTemplate=Mat("AutumnTerrain",Color.white,"Universal Render Pipeline/Terrain/Lit");
    }
    public static void PaintTerrain(TerrainData data)
    {
        var splat=new float[512,512,4];
        for(int z=0;z<512;z++)for(int x=0;x<512;x++)
        {
            float wx=x*144f/511-72,wz=z*144f/511-72;
            float d=RouteDistance(new Vector2(wx,wz));
            float camp=Mathf.Min(Vector2.Distance(new Vector2(wx,wz),new Vector2(2,38)),
                Vector2.Distance(new Vector2(wx,wz),new Vector2(0,-48)));
            float trail=1-Mathf.SmoothStep(0,1,(Mathf.Min(d,camp-7)-1.8f)/1.3f);
            float rock=Mathf.Clamp01((Height(wx,wz)-6)/10);
            if(Mathf.Abs(wz-RiverZ(wx))<5.8f){rock=.7f;trail=0;}
            float leaves=(1-rock)*(1-trail)*Mathf.Clamp01((Mathf.PerlinNoise(wx*.08f+100,wz*.08f+100)-.35f)*1.4f);
            splat[z,x,1]=trail*(1-rock);splat[z,x,2]=rock;splat[z,x,3]=leaves;
            splat[z,x,0]=Mathf.Max(0,1-splat[z,x,1]-rock-leaves);
        }
        data.SetAlphamaps(0,0,splat);

        data.SetBaseMapDirty();
        EditorUtility.SetDirty(data);
    }
    static Vector3 OnGround(float x,float z)=>new Vector3(x,terrain.SampleHeight(new Vector3(x,0,z)),z);

    static Material Mat(string name,Color color,string shader="Universal Render Pipeline/Lit")
    {
        var m=new Material(Shader.Find(shader)){name=name,color=color};
        AssetDatabase.CreateAsset(m,Folder+"/"+name+".mat");return m;
    }
    static GameObject Block(string name,Transform parent,Vector3 position,Vector3 scale,Material mat,bool collision=true)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;
        g.transform.SetParent(parent,true);g.transform.position=position;g.transform.localScale=scale;
        g.GetComponent<Renderer>().sharedMaterial=mat;
        if(!collision)Object.DestroyImmediate(g.GetComponent<Collider>());
        return g;
    }
    static GameObject Prop(string path,Transform parent,float x,float z,float scale=1,float yaw=0)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Pack+path+".prefab");
        if(prefab==null)throw new FileNotFoundException(path);
        var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);
        var groundPos = OnGround(x,z);

        // Slope sinking to eliminate floating gaps on uneven terrain
        var tData = terrain != null ? terrain.terrainData : null;
        float steepness = 0f;
        if (tData != null)
        {
            float normX = (x - terrain.transform.position.x) / tData.size.x;
            float normZ = (z - terrain.transform.position.z) / tData.size.z;
            steepness = tData.GetSteepness(normX, normZ);
        }

        if (path.Contains("stone") || path.Contains("Stone"))
        {
            float sink = Mathf.Tan(steepness * Mathf.Deg2Rad) * (2.6f * scale) + 0.45f;
            groundPos.y -= sink;
        }
        else if (path.Contains("tree") || path.Contains("Tree"))
        {
            groundPos.y -= 0.45f;
        }

        g.transform.SetPositionAndRotation(groundPos,Quaternion.Euler(0,yaw,0));
        g.transform.localScale=Vector3.one*scale;

        // Accurate colliders for rocks, trees, and tents
        if (path.Contains("stone") || path.Contains("Stone"))
        {
            foreach(var bc in g.GetComponentsInChildren<BoxCollider>(true)) Object.DestroyImmediate(bc);
            foreach(var mesh in g.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mesh.GetComponent<Collider>() == null)
                    mesh.gameObject.AddComponent<MeshCollider>().sharedMesh = mesh.sharedMesh;
            }
        }
        else if(path.Contains("camping tent") && g.GetComponentsInChildren<Collider>(true).Length==0)
        {
            foreach(var mesh in g.GetComponentsInChildren<MeshFilter>(true))
                mesh.gameObject.AddComponent<MeshCollider>().sharedMesh=mesh.sharedMesh;
        }
        return g;
    }
    static void MakeRiver()
    {
        var vertices=new List<Vector3>();var tris=new List<int>();
        for(int i=0;i<=72;i++)
        {
            float x=-72+i*2;float c=RiverZ(x);
            vertices.Add(new Vector3(x,.95f,c-4.2f));
            vertices.Add(new Vector3(x,.95f,c+4.2f));
            if(i==0)continue;
            int b=(i-1)*2;tris.AddRange(new[]{b,b+1,b+2,b+1,b+3,b+2});
        }
        var mesh=new Mesh {name="ValleyStream"};mesh.SetVertices(vertices);mesh.SetTriangles(tris,0);
        mesh.RecalculateNormals();mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh,Folder+"/ValleyStream.asset");
        var g=new GameObject("ShallowValleyStream",typeof(MeshFilter),typeof(MeshRenderer));
        g.transform.SetParent(map,false);g.GetComponent<MeshFilter>().sharedMesh=mesh;
        g.GetComponent<MeshRenderer>().sharedMaterial=Mat("RiverJade",new Color(.12f,.48f,.48f));
        for(int i=-68;i<70;i+=4)
        {
            var volume=new GameObject("RiverNavigationExclusion").AddComponent<NavMeshModifierVolume>();
            volume.transform.SetParent(map,false);volume.transform.position=new Vector3(i,0,RiverZ(i));
            volume.size=new Vector3(4.1f,3,8.5f);volume.center=new Vector3(0,0,0);volume.area=1;
        }
        // Shallow channel: player can safely wade; agents use the two bridges.
    }
    static void MakeBridge(float x)
    {
        float z=RiverZ(x);
        var root=new GameObject(x<0?"WestBridge":"EastBridge").transform;
        root.SetParent(structures,false);
        Block("ContinuousWalkableDeck",root,new Vector3(x,2.95f,z),new Vector3(5,.4f,18),wood);
        for(int i=0;i<24;i++)
            Block("TimberPlank",root,new Vector3(x,3.16f,z-8.6f+i*.75f),new Vector3(5.1f,.06f,.67f),i%4==0?darkWood:wood,false);
        for(int side=-1;side<=1;side+=2)
        {
            for(int i=0;i<5;i++)
                Block("RailingPost",root,new Vector3(x+side*2.5f,3.7f,z-8+i*4),new Vector3(.18f,1.4f,.18f),darkWood);
            for(int rail=0;rail<2;rail++)
                Block("BridgeRail",root,new Vector3(x+side*2.5f,3.5f+rail*.55f,z),new Vector3(.14f,.13f,17),wood);
            Block("UnderBridgeBeam",root,new Vector3(x+side*1.5f,2.45f,z),new Vector3(.28f,.65f,18),darkWood,false);
        }
    }
    static void DressCamps()
    {
        Prop("Environment/Camping environment/camping tent yellow",structures,-6,-48,1.25f,65);
        Prop("Environment/Camping environment/camping tent blue",structures,6,-51,1.1f,-35);
        Prop("Environment/Camping environment/bonfire",structures,-2,-42,1);
        Prop("Environment/information post",structures,-4,-39,1.2f,180);
        Prop("Environment/chest",structures,8,-47,1);
        Prop("Environment/Camping environment/camping tent red",structures,-7,43,1.65f,145);
        Prop("Environment/Camping environment/camping tent yellow",structures,11,43,1.3f,210);
        Prop("Environment/Camping environment/bonfire",structures,2,44,1.4f);
        Prop("Environment/chest",structures,8,39,1.1f);
        // A broken stone gateway makes the objective readable from both trails.
        foreach(float x in new[]{-4f,8f})
        {
            var p=OnGround(x,31);
            Block("RuinFooting",structures,p+Vector3.up*.35f,new Vector3(2.5f,.7f,2.5f),stone);
            Block("RuinPillar",structures,p+Vector3.up*3.2f,new Vector3(1.5f,5.7f,1.5f),stone);
            Block("RuinCapital",structures,p+Vector3.up*6.1f,new Vector3(2.1f,.6f,2.1f),stone);
        }
        Block("AncientLintel",structures,OnGround(2,31)+Vector3.up*6.5f,new Vector3(15,.85f,2),stone);
        foreach(float x in new[]{-7f,11f})
        {
            var p=OnGround(x,36);
            Block("BannerPole",structures,p+Vector3.up*3.2f,new Vector3(.13f,6.4f,.13f),darkWood);
            Block("RedOutpostFlag",structures,p+new Vector3(.75f,5.25f,0),new Vector3(1.4f,1.8f,.07f),banner,false);
            Block("FlagFinial",structures,p+Vector3.up*6.45f,Vector3.one*.25f,brass,false);
        }
        // Cover around encounters, leaving a broad aisle through the gateway.
        foreach(var p in new[]{new Vector2(-12,31),new Vector2(13,28),new Vector2(-29,-20),
            new Vector2(-17,16),new Vector2(30,17),new Vector2(20,-28)})
            Prop("Stones/Stones large/stone large",structures,p.x,p.y,1.3f,30);
        for(int side=-1;side<=1;side+=2)
            for(int i=0;i<3;i++)
                Block("CampSupplyCrate",structures,OnGround(2+side*10,37+i*2)+Vector3.up*.6f,new Vector3(1.3f,1.2f,1.2f),wood);
    }
    static void ScatterForest()
    {
        var rng=new System.Random(231023);
        var occupied=new List<Vector2>();
        int placed=0;
        var tData = terrain != null ? terrain.terrainData : null;
        for(int attempt=0;attempt<6000&&placed<180;attempt++)
        {
            var p=new Vector2((float)rng.NextDouble()*96-48,(float)rng.NextDouble()*96-48);
            if(RouteDistance(p)<6||Mathf.Abs(p.y-RiverZ(p.x))<9||
                Vector2.Distance(p,new Vector2(0,-48))<14||
                Vector2.Distance(p,new Vector2(2,38))<16||
                occupied.Any(o=>Vector2.Distance(o,p)<5.0f))continue;

            if (tData != null)
            {
                float normX = (p.x - terrain.transform.position.x) / tData.size.x;
                float normZ = (p.y - terrain.transform.position.z) / tData.size.z;
                if (tData.GetSteepness(normX, normZ) > 16f) continue; // Keep trees on walkable ground, not steep cliffs
            }

            occupied.Add(p);
            bool pine=placed%4==0;
            string asset=pine?"Trees/Autumn fir trees/autumn pine tree medium":
                "Trees/Autumn trees/autumn tree"+(placed%5==0?"":" "+(placed%5+1));
            float scale=.85f+(float)rng.NextDouble()*.5f;
            var tree=Prop(asset,vegetation,p.x,p.y,scale,(float)rng.NextDouble()*360);

            // Clean trunk collider, remove oversized box
            foreach(var c in tree.GetComponentsInChildren<BoxCollider>(true)) Object.DestroyImmediate(c);
            var trunk=tree.AddComponent<CapsuleCollider>();
            trunk.radius=.45f;trunk.height=4.5f;trunk.center=Vector3.up*2.25f;
            placed++;
        }
        // Border boulders placed at the foot of mountains (r = 46), not on 50-degree cliffs!
        for(int i=0;i<28;i++)
        {
            float angle=i*Mathf.PI*2/28;
            float x=Mathf.Cos(angle)*46f,z=Mathf.Sin(angle)*47f;
            if(Mathf.Abs(z-RiverZ(x))<9)continue;
            Prop("Stones/Stones large/stone large",vegetation,x,z,0.85f+(i%3)*.2f,i*51);
        }
        for(int i=0;i<80;i++)
        {
            float x=(float)rng.NextDouble()*96-48,z=(float)rng.NextDouble()*96-48;
            if(RouteDistance(new Vector2(x,z))<4||Mathf.Abs(z-RiverZ(x))<8||
               Vector2.Distance(new Vector2(x,z),new Vector2(0,-48))<10||
               Vector2.Distance(new Vector2(x,z),new Vector2(2,38))<12)continue;

            if (tData != null)
            {
                float normX = (x - terrain.transform.position.x) / tData.size.x;
                float normZ = (z - terrain.transform.position.z) / tData.size.z;
                if (tData.GetSteepness(normX, normZ) > 16f) continue;
            }

            var shrub=Prop("Plants/Autumn shrubs/autumn shrub",vegetation,x,z,.55f+(float)rng.NextDouble()*.4f,i*79);
            foreach(var c in shrub.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
        }
    }
    static void MakeBoundary()
    {
        var root=new GameObject("PlayableBoundary").transform;root.SetParent(map,false);
        foreach(int side in new[]{-1,1})
        {
            var x=Block("Boundary_EW",root,new Vector3(side*59,16,0),new Vector3(1,40,120),stone);
            x.GetComponent<Renderer>().enabled=false;
            var z=Block("Boundary_NS",root,new Vector3(0,16,side*61),new Vector3(120,40,1),stone);
            z.GetComponent<Renderer>().enabled=false;
        }
    }
    static void PlaceOnNav(Transform t,float x,float z)
    {
        if(!NavMesh.SamplePosition(OnGround(x,z)+Vector3.up*.3f,out var hit,3,NavMesh.AllAreas))
            throw new InvalidOperationException("No navigation at "+x+","+z);
        t.position=hit.position+Vector3.up*.04f;
    }
    static void Lighting()
    {
        var sun=Object.FindObjectsByType<Light>(FindObjectsSortMode.None).First(l=>l.type==LightType.Directional);
        sun.transform.rotation=Quaternion.Euler(42,-32,0);
        sun.color=new Color(1,.87f,.68f);sun.intensity=1.35f;sun.shadows=LightShadows.Soft;
        RenderSettings.ambientMode=AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=new Color(.56f,.65f,.72f);
        RenderSettings.ambientEquatorColor=new Color(.48f,.44f,.32f);
        RenderSettings.ambientGroundColor=new Color(.24f,.27f,.19f);
        RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;
        RenderSettings.fogColor=new Color(.68f,.72f,.63f);RenderSettings.fogStartDistance=135;RenderSettings.fogEndDistance=330;
    }
}
#endif
