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

// One-time Editor authoring. All geometry, paint and navigation are saved.
public static class IceboundPassMapBuilder
{
    public const string ScenePath="Assets/Scenes/IceboundPass.unity";
    const string Folder="Assets/Maps/IceboundPass";
    const string Pack="Assets/DL/Palmov Island/Low Poly Atmospheric Locations Pack/Prefabs/";
    static Terrain terrain;
    static Transform map,forest,structures;
    static Material timber,stone,ice,crack,flag,snow;
    static readonly Vector2[] ShoreRoute={
        new Vector2(0,-57),new Vector2(22,-40),new Vector2(36,-24),
        new Vector2(38,6),new Vector2(28,30),new Vector2(2,48)};
    static readonly Vector2[] IceRoute={
        new Vector2(0,-57),new Vector2(-12,-34),new Vector2(-12,-10),
        new Vector2(-10,12),new Vector2(-11,29),new Vector2(2,48)};

    [MenuItem("GunQuest/Maps/Create Icebound Pass")]
    public static void Build()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode first.");
        if(File.Exists(ScenePath)||Directory.Exists(Folder))
            throw new InvalidOperationException("Icebound Pass already exists; refusing to overwrite authored terrain.");
        for(int i=0;i<EditorSceneManager.sceneCount;i++)
            if(EditorSceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Save your open scenes first.");
        Directory.CreateDirectory(Folder);
        AssetDatabase.Refresh();
        if(!AssetDatabase.CopyAsset("Assets/Scenes/Scene1.unity",ScenePath))
            throw new IOException("Could not copy gameplay scene.");
        var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
        var game=Object.FindFirstObjectByType<ContractGame>();
        var settings=new SerializedObject(game);
        var boss=(EnemyHealth)settings.FindProperty("contractTarget").objectReferenceValue;
        var player=Object.FindFirstObjectByType<PlayerMotor>();
        foreach(var name in new[]{"PalmovMap_CampingOutpost","GameplaySpawns","MapBoundary"})
        {
            var old=GameObject.Find(name);
            if(old!=null)Object.DestroyImmediate(old);
        }
        foreach(var root in scene.GetRootGameObjects())
            if(root.name=="ExtractionMarker")Object.DestroyImmediate(root);
        map=new GameObject("IceboundPass_Map").transform;
        forest=new GameObject("SnowForest_RockRidges").transform;forest.SetParent(map,false);
        structures=new GameObject("FrozenLake_Villages_Outpost").transform;structures.SetParent(map,false);
        timber=Mat("DarkWinterTimber",new Color(.22f,.18f,.15f));
        stone=Mat("FrostedStone",new Color(.55f,.66f,.73f));
        snow=Mat("FreshSnow",new Color(.89f,.94f,.97f));
        ice=Mat("GlacierBlue",new Color(.24f,.61f,.78f));
        ice.SetFloat("_Smoothness",.68f);ice.SetFloat("_Metallic",.05f);
        crack=Mat("FrostLines",new Color(.72f,.94f,1));
        flag=Mat("OutpostBlue",new Color(.055f,.21f,.44f));
        MakeTerrain();
        MakeFrozenLake();
        DressSettlements();
        ScatterWinter();
        Boundary();
        var surface=map.gameObject.AddComponent<NavMeshSurface>();
        surface.collectObjects=CollectObjects.Children;
        surface.useGeometry=NavMeshCollectGeometry.PhysicsColliders;
        surface.overrideVoxelSize=true;surface.voxelSize=.15f;surface.minRegionArea=3;
        Physics.SyncTransforms();surface.BuildNavMesh();
        if(surface.navMeshData==null)throw new InvalidOperationException("Navigation bake failed.");
        surface.navMeshData.name="IceboundPass_NavMesh";
        AssetDatabase.CreateAsset(surface.navMeshData,Folder+"/IceboundPass_NavMesh.asset");
        if(PrefabUtility.IsPartOfPrefabInstance(player.gameObject))
            PrefabUtility.UnpackPrefabInstance(PrefabUtility.GetOutermostPrefabInstanceRoot(player.gameObject),
                PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
        player.transform.SetParent(null,true);player.transform.position=Ground(0,-57)+Vector3.up*.08f;
        player.transform.rotation=Quaternion.identity;
        var enemies=new GameObject("EnemyGroup").transform;
        boss.transform.SetParent(enemies,true);PlaceOnNav(boss.transform,2,48);
        boss.transform.rotation=Quaternion.Euler(0,180,0);
        var guards=new[]{new Vector2(24,-31),new Vector2(35,14),new Vector2(23,33),
            new Vector2(-12,-18),new Vector2(-10,26),new Vector2(-7,40)};
        var enemyTypes=new[]{ "TurtleShellPBR", "Swarm08", "StarFish", "ChestMonsterPBRDefault", "CactusPBR", "SlimePBR" };
        for(int i=0;i<guards.Length;i++)
        {
            var pPath = $"Assets/Prefabs/Enemy/{enemyTypes[i % enemyTypes.Length]}.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(pPath);
            if (prefab == null) prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Monster4.prefab");
            var enemy=(GameObject)PrefabUtility.InstantiatePrefab(prefab,enemies);
            enemy.name=$"Enemy_FrostGuard_{enemyTypes[i % enemyTypes.Length]}_{i+1}";
            PlaceOnNav(enemy.transform,guards[i].x,guards[i].y);
        }
        var extraction=new GameObject("IceboundExtraction").transform;
        extraction.position=Ground(0,-57)+Vector3.up*.03f;
        var marker=GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.name="ExtractionMarker";marker.transform.SetParent(extraction,false);
        marker.transform.localScale=new Vector3(4.6f,.035f,4.6f);
        marker.GetComponent<Renderer>().sharedMaterial=Mat("ExtractionJade",new Color(.10f,.73f,.47f));
        Object.DestroyImmediate(marker.GetComponent<Collider>());marker.SetActive(false);
        settings=new SerializedObject(game);
        settings.FindProperty("contractTarget").objectReferenceValue=boss;
        settings.FindProperty("extractionPoint").objectReferenceValue=extraction;
        settings.FindProperty("extractionMarker").objectReferenceValue=marker;
        settings.FindProperty("reward").intValue=350;
        settings.FindProperty("missionTitle").stringValue="ĐÈO BĂNG GIÁ";
        settings.FindProperty("missionObjective").stringValue="Hạ quái laser tại tiền đồn cờ xanh phía bắc.";
        settings.FindProperty("missionBriefing").stringValue=
            "Băng qua hồ đóng băng hoặc theo đường tuyết ven núi phía đông.\nHạ quái laser tại tiền đồn cờ xanh, rồi quay về vòng xanh ở làng xuất phát.";
        settings.ApplyModifiedPropertiesWithoutUndo();
        WinterLight();
        var camera=Camera.main;
        foreach(var b in camera.GetComponents<MonoBehaviour>())
            if(b.GetType().Namespace!=null&&b.GetType().Namespace.Contains("Cinemachine"))
            {b.enabled=false;PrefabUtility.RecordPrefabInstancePropertyModifications(b);}
        camera.fieldOfView=75;
        camera.transform.SetPositionAndRotation(player.transform.position+Vector3.up*1.65f,Quaternion.identity);
        PrefabUtility.RecordPrefabInstancePropertyModifications(camera);
        PrefabUtility.RecordPrefabInstancePropertyModifications(camera.transform);
        var preview=new GameObject("IceboundPass_EditorOverview",typeof(Camera));
        preview.tag="EditorOnly";var pc=preview.GetComponent<Camera>();
        pc.enabled=false;pc.farClipPlane=450;pc.fieldOfView=63;
        preview.transform.position=new Vector3(95,125,-112);preview.transform.LookAt(new Vector3(0,3,0));
        var builds=EditorBuildSettings.scenes.ToList();
        if(!builds.Any(s=>s.path==ScenePath))builds.Add(new EditorBuildSettingsScene(ScenePath,true));
        EditorBuildSettings.scenes=builds.ToArray();
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        if(SceneView.lastActiveSceneView!=null)
        {
            SceneView.lastActiveSceneView.pivot=new Vector3(0,3,0);
            SceneView.lastActiveSceneView.rotation=Quaternion.Euler(50,-30,0);
            SceneView.lastActiveSceneView.size=105;SceneView.lastActiveSceneView.Show();
        }
        Selection.activeObject=null;
        Debug.Log("Icebound Pass saved: snow Terrain, solid frozen lake, shoreline route, winter villages and contract.");
    }

    static float LakeRadius(float x,float z)
    {
        float dx=(x+10)/28,dz=(z+2)/25;return Mathf.Sqrt(dx*dx+dz*dz);
    }
    static float DistanceSegment(Vector2 p,Vector2 a,Vector2 b)
    {
        Vector2 d=b-a;return Vector2.Distance(p,a+d*Mathf.Clamp01(Vector2.Dot(p-a,d)/d.sqrMagnitude));
    }
    static float RouteDistance(Vector2 p)
    {
        float d=float.MaxValue;
        foreach(var route in new[]{ShoreRoute,IceRoute})
            for(int i=1;i<route.Length;i++)d=Mathf.Min(d,DistanceSegment(p,route[i-1],route[i]));
        return d;
    }
    static float Height(float x,float z)
    {
        float h=3+.8f*Mathf.PerlinNoise((x+230)*.037f,(z+180)*.037f);
        h+=9*Mathf.Exp(-((x+50)*(x+50)+(z-18)*(z-18))/320);
        h+=10*Mathf.Exp(-((x-53)*(x-53)+(z-24)*(z-24))/320);
        float edge=Mathf.Clamp01((Mathf.Max(Mathf.Abs(x),Mathf.Abs(z))-56)/24);
        h+=19*edge*edge;
        float route=RouteDistance(new Vector2(x,z));
        h=Mathf.Lerp(3,h,Mathf.SmoothStep(0,1,(route-3.5f)/6));
        float camp=Mathf.Min(Vector2.Distance(new Vector2(x,z),new Vector2(0,-57)),
            Vector2.Distance(new Vector2(x,z),new Vector2(2,48)));
        h=Mathf.Lerp(3,h,Mathf.SmoothStep(0,1,(camp-14)/8));
        float lake=LakeRadius(x,z);
        if(lake<1.15f)h=Mathf.Lerp(2.82f,h,Mathf.SmoothStep(0,1,(lake-.96f)/.19f));
        return h;
    }
    static Vector3 Ground(float x,float z)=>new Vector3(x,terrain.SampleHeight(new Vector3(x,0,z)),z);
    static void MakeTerrain()
    {
        var data=new TerrainData{name="IceboundPass_Terrain",heightmapResolution=513,
            size=new Vector3(160,36,160),alphamapResolution=512,baseMapResolution=1024};
        var heights=new float[513,513];
        for(int z=0;z<513;z++)for(int x=0;x<513;x++)heights[z,x]=Height(x*160f/512-80,z*160f/512-80)/36;
        data.SetHeights(0,0,heights);
        string[] names={"PowderSnow","PackedTrail","FrostRock","Glacier"};
        Color[] colors={new Color(.83f,.90f,.95f),new Color(.52f,.67f,.76f),
            new Color(.43f,.54f,.64f),new Color(.31f,.65f,.80f)};
        var layers=new TerrainLayer[4];
        for(int i=0;i<4;i++)
        {
            var texture=new Texture2D(4,4,TextureFormat.RGB24,false);
            texture.SetPixels(Enumerable.Repeat(colors[i],16).ToArray());texture.Apply();
            string path=Folder+"/"+names[i]+".png";File.WriteAllBytes(path,texture.EncodeToPNG());
            Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            layers[i]=new TerrainLayer{name=names[i],diffuseTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(path),tileSize=new Vector2(8,8)};
            AssetDatabase.CreateAsset(layers[i],Folder+"/"+names[i]+".terrainlayer");
        }
        data.terrainLayers=layers;
        AssetDatabase.CreateAsset(data,Folder+"/IceboundPass_Terrain.asset");
        PaintTerrain(data);
        var go=Terrain.CreateTerrainGameObject(data);go.name="IceboundPass_Terrain";
        go.transform.SetParent(map,false);go.transform.position=new Vector3(-80,0,-80);
        terrain=go.GetComponent<Terrain>();terrain.heightmapPixelError=2;terrain.drawInstanced=true;terrain.basemapDistance=1000;
        terrain.materialTemplate=Mat("SnowTerrain",Color.white,"Universal Render Pipeline/Terrain/Lit");
    }
    public static void PaintTerrain(TerrainData data)
    {
        var alpha=new float[512,512,4];
        for(int z=0;z<512;z++)for(int x=0;x<512;x++)
        {
            float wx=x*160f/511-80,wz=z*160f/511-80;
            float camp=Mathf.Min(Vector2.Distance(new Vector2(wx,wz),new Vector2(0,-57)),
                Vector2.Distance(new Vector2(wx,wz),new Vector2(2,48)));
            float trail=1-Mathf.SmoothStep(0,1,(Mathf.Min(RouteDistance(new Vector2(wx,wz)),camp-8)-2)/1.4f);
            float rock=Mathf.Clamp01((Height(wx,wz)-8)/15)*.6f;
            float glacier=1-Mathf.SmoothStep(0,1,(LakeRadius(wx,wz)-.96f)/.13f);
            alpha[z,x,3]=glacier;alpha[z,x,2]=rock*(1-glacier);
            alpha[z,x,1]=trail*(1-glacier)*(1-rock);
            alpha[z,x,0]=Mathf.Max(0,1-alpha[z,x,1]-alpha[z,x,2]-glacier);
        }
        data.SetAlphamaps(0,0,alpha);data.SetBaseMapDirty();EditorUtility.SetDirty(data);
    }
    static Material Mat(string name,Color color,string shader="Universal Render Pipeline/Lit")
    {
        var material=new Material(Shader.Find(shader)){name=name,color=color};
        AssetDatabase.CreateAsset(material,Folder+"/"+name+".mat");return material;
    }
    static GameObject Block(string name,Transform parent,Vector3 pos,Vector3 size,Material material,bool collision=true)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,true);
        g.transform.position=pos;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=material;
        if(!collision)Object.DestroyImmediate(g.GetComponent<Collider>());
        return g;
    }
    static GameObject Prop(string path,Transform parent,float x,float z,float scale=1,float yaw=0)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Pack+path+".prefab");
        if(prefab==null)throw new FileNotFoundException(path);
        var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);
        g.transform.SetPositionAndRotation(Ground(x,z),Quaternion.Euler(0,yaw,0));
        g.transform.localScale=Vector3.one*scale;return g;
    }
    static void MakeFrozenLake()
    {
        var vertices=new List<Vector3>{new Vector3(-10,3.04f,-2)};
        var triangles=new List<int>();const int sides=24;
        for(int i=0;i<sides;i++)
        {
            float a=i*Mathf.PI*2/sides;
            vertices.Add(new Vector3(-10+28*Mathf.Cos(a),3.04f,-2+25*Mathf.Sin(a)));
        }
        for(int i=0;i<sides;i++)triangles.AddRange(new[]{0,(i+1)%sides+1,i+1});
        var mesh=new Mesh{name="FrozenLakeSolidSurface"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);
        mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Folder+"/FrozenLake.asset");
        var lake=new GameObject("FrozenLake_Walkable",typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));
        lake.transform.SetParent(structures,false);lake.GetComponent<MeshFilter>().sharedMesh=mesh;
        lake.GetComponent<MeshRenderer>().sharedMaterial=ice;lake.GetComponent<MeshCollider>().sharedMesh=mesh;
        // Visible frost veins are decoration, with no gaps in the solid ice.
        Vector2[][] veins={
            new[]{new Vector2(-32,-11),new Vector2(-22,-5),new Vector2(-14,-3),new Vector2(-7,4),new Vector2(6,10)},
            new[]{new Vector2(-14,-3),new Vector2(-9,-13),new Vector2(-2,-23)},
            new[]{new Vector2(-7,4),new Vector2(-17,13),new Vector2(-20,19)},
            new[]{new Vector2(-22,-5),new Vector2(-26,3),new Vector2(-31,6)}};
        foreach(var vein in veins)for(int i=1;i<vein.Length;i++)
        {
            Vector2 d=vein[i]-vein[i-1],mid=(vein[i]+vein[i-1])*.5f;
            var line=Block("SurfaceFrostVein",lake.transform,new Vector3(mid.x,3.052f,mid.y),new Vector3(.09f,.012f,d.magnitude),crack,false);
            line.transform.rotation=Quaternion.LookRotation(new Vector3(d.x,0,d.y));
        }
        foreach(var p in new[]{new Vector2(-34,-7),new Vector2(-30,14),new Vector2(9,13),new Vector2(12,-13)})
            CrystalCluster(p.x,p.y,1);
    }
    static void CrystalCluster(float x,float z,float scale)
    {
        var root=new GameObject("GlacierCrystalCluster").transform;root.SetParent(structures,false);
        for(int n=0;n<3;n++)
        {
            float radius=(n==0?1.5f:.95f)*scale,h=(n==0?7:4.5f)*scale;
            var v=new List<Vector3>();var tris=new List<int>();
            for(int i=0;i<6;i++){float a=i*Mathf.PI/3;v.Add(new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius));}
            for(int i=0;i<6;i++){float a=i*Mathf.PI/3;v.Add(new Vector3(Mathf.Cos(a)*radius*.72f,h*.78f,Mathf.Sin(a)*radius*.72f));}
            v.Add(new Vector3(radius*.15f,h,0));
            for(int i=0;i<6;i++){int j=(i+1)%6;tris.AddRange(new[]{i,i+6,j,j,i+6,j+6,i+6,12,j+6});}
            var m=new Mesh{name="GlacierCrystal_"+x+"_"+z+"_"+n};m.SetVertices(tris.Select(index=>v[index]).ToList());m.SetTriangles(Enumerable.Range(0,tris.Count).ToArray(),0);m.RecalculateNormals();m.RecalculateBounds();
            AssetDatabase.CreateAsset(m,Folder+"/Crystal_"+x+"_"+z+"_"+n+".asset");
            var g=new GameObject("FacetedIce",typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));
            g.transform.SetParent(root,false);g.transform.position=Ground(x+(n-1)*1.8f*scale,z+n*.9f*scale);
            g.GetComponent<MeshFilter>().sharedMesh=m;g.GetComponent<MeshRenderer>().sharedMaterial=ice;g.GetComponent<MeshCollider>().sharedMesh=m;
        }
    }
    static void DressSettlements()
    {
        Prop("Houses/Wooden winter houses/wooden winter house",structures,-10,-59,1,25);
        Prop("Houses/Wooden winter houses/wooden winter house 2",structures,10,-60,1,-25);
        Prop("Environment/Winter environment/winter well",structures,-7,-50,.9f);
        Prop("Environment/Winter environment/snowman",structures,8,-51,1.2f,-10);
        Prop("Environment/Camping environment/bonfire",structures,0,-48,1);
        Prop("Houses/Wooden winter houses/wooden winter house 3",structures,-11,54,1.2f,155);
        Prop("Houses/Wooden winter houses/wooden winter house",structures,15,53,1.2f,200);
        Prop("Environment/Winter environment/winter well",structures,14,43,1);
        foreach(float x in new[]{-6f,10f})
        {
            Vector3 p=Ground(x,39);
            Block("OutpostStonePillar",structures,p+Vector3.up*3.6f,new Vector3(1.8f,7.2f,1.8f),stone);
            Block("PillarSnowCap",structures,p+Vector3.up*7.3f,new Vector3(2.4f,.45f,2.4f),snow);
            Block("BlueSignalFlag",structures,p+new Vector3(.9f,5.4f,0),new Vector3(1.8f,2.2f,.08f),flag,false);
        }
        Block("OutpostLintel",structures,Ground(2,39)+Vector3.up*7.3f,new Vector3(17.5f,.7f,1.7f),stone);
        Block("GateSnow",structures,Ground(2,39)+Vector3.up*7.75f,new Vector3(18,.2f,2.1f),snow,false);
        CrystalCluster(2,60,1.2f);
        foreach(var p in new[]{new Vector2(29,-32),new Vector2(44,-6),new Vector2(31,23),
            new Vector2(-22,29),new Vector2(14,34),new Vector2(-17,-32)})
            Prop("Stones/stones group winter",structures,p.x,p.y,.65f,p.x*8);
        for(int side=-1;side<=1;side+=2)for(int i=0;i<3;i++)
        {
            Vector3 p=Ground(2+side*11,43+i*2);
            Block("SupplyCrate",structures,p+Vector3.up*.65f,new Vector3(1.4f,1.3f,1.4f),timber);
            Block("CrateSnow",structures,p+Vector3.up*1.34f,new Vector3(1.45f,.1f,1.45f),snow,false);
        }
        foreach(float x in new[]{-17f,-5f})
        {
            var p=Ground(x,28);
            Block("IceTrailGate",structures,p+Vector3.up*3,new Vector3(1.2f,6,1.2f),ice);
        }
        Block("IceTrailArch",structures,Ground(-11,28)+Vector3.up*6,new Vector3(13,.8f,1.5f),ice);
    }
    static void ScatterWinter()
    {
        var rng=new System.Random(1023);var occupied=new List<Vector2>();int count=0;
        for(int attempt=0;attempt<7000&&count<230;attempt++)
        {
            var p=new Vector2((float)rng.NextDouble()*146-73,(float)rng.NextDouble()*146-73);
            if(RouteDistance(p)<6||LakeRadius(p.x,p.y)<1.24f||
               Vector2.Distance(p,new Vector2(0,-57))<16||Vector2.Distance(p,new Vector2(2,48))<20||
               occupied.Any(o=>Vector2.Distance(o,p)<4.8f))continue;
            occupied.Add(p);string size=count%4==0?"large":count%4==1?"small":"medium";
            var tree=Prop("Trees/Fir trees winter/fir tree winter "+size,forest,p.x,p.y,.9f+(float)rng.NextDouble()*.55f,count*73);
            foreach(var c in tree.GetComponentsInChildren<Collider>(true))
            {c.enabled=false;PrefabUtility.RecordPrefabInstancePropertyModifications(c);}
            var trunk=tree.AddComponent<CapsuleCollider>();trunk.radius=.28f;trunk.height=3;trunk.center=Vector3.up*1.5f;count++;
        }
        for(int i=0;i<42;i++)
        {
            float angle=i*Mathf.PI*2/42,x=Mathf.Cos(angle)*69,z=Mathf.Sin(angle)*72;
            Prop("Stones/stones group winter",forest,x,z,1.3f+(i%3)*.35f,i*47);
        }
        for(int i=0;i<65;i++)
        {
            float x=(float)rng.NextDouble()*124-62,z=(float)rng.NextDouble()*128-64;
            if(RouteDistance(new Vector2(x,z))<4||LakeRadius(x,z)<1.18f||
                Vector2.Distance(new Vector2(x,z),new Vector2(0,-57))<12||
                Vector2.Distance(new Vector2(x,z),new Vector2(2,48))<15)continue;
            Prop("Stones/Stones winter small/stone winter small",forest,x,z,2.2f+(float)rng.NextDouble()*2,i*53);
        }
    }
    static void Boundary()
    {
        var bounds=new GameObject("PlayableBoundary").transform;bounds.SetParent(map,false);
        foreach(int side in new[]{-1,1})
        {
            var x=Block("BoundaryEW",bounds,new Vector3(side*66,19,0),new Vector3(1,44,138),stone);
            var z=Block("BoundaryNS",bounds,new Vector3(0,19,side*68),new Vector3(134,44,1),stone);
            x.GetComponent<Renderer>().enabled=false;z.GetComponent<Renderer>().enabled=false;
        }
    }
    static void PlaceOnNav(Transform t,float x,float z)
    {
        if(!NavMesh.SamplePosition(Ground(x,z)+Vector3.up*.3f,out var hit,3,NavMesh.AllAreas))
            throw new InvalidOperationException("No navigation near "+x+","+z);
        t.position=hit.position+Vector3.up*.04f;
    }
    static void WinterLight()
    {
        var sun=Object.FindObjectsByType<Light>(FindObjectsSortMode.None).First(l=>l.type==LightType.Directional);
        sun.transform.rotation=Quaternion.Euler(35,-28,0);sun.color=new Color(.83f,.91f,1);sun.intensity=1.05f;sun.shadows=LightShadows.Soft;
        RenderSettings.ambientMode=AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=new Color(.64f,.74f,.88f);
        RenderSettings.ambientEquatorColor=new Color(.51f,.62f,.74f);
        RenderSettings.ambientGroundColor=new Color(.37f,.44f,.56f);
        RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogColor=new Color(.70f,.81f,.90f);
        RenderSettings.fogStartDistance=155;RenderSettings.fogEndDistance=380;
    }
}
#endif
