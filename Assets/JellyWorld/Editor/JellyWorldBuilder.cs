using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using Newtonsoft.Json.Linq;
using JellyWorldGame;

public static class JellyWorldBuilder
{
    const string Root="Assets/JellyWorld";
    [MenuItem("Jelly World/Build playable world")]
    public static void Build()
    {
        Directory.CreateDirectory(Root+"/Art");
        Directory.CreateDirectory(Root+"/Prefabs");
        AssetDatabase.Refresh();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var go=new GameObject("Jelly World");
        var world=go.AddComponent<JellyWorld>();
        world.jellyMesh=ImportMesh();
        string[] hex={"F875A6","72CFF5","61DDB5","FFCF66","B895F3","FF9147","526CE0"};
        world.candy=new Material[7];
        for(int i=0;i<7;i++)world.candy[i]=Mat("Jelly "+i,hex[i],.82f);
        world.toolBeam=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Art/Sugar beam.mat");
        if(world.toolBeam==null){world.toolBeam=new Material(Shader.Find("Jelly World/Sugar Beam"));AssetDatabase.CreateAsset(world.toolBeam,Root+"/Art/Sugar beam.mat");}
        world.cream=Mat("Vanilla","FFF4D9",.35f);
        world.plum=Mat("Deep grape","584276",.50f);
        world.white=Mat("Sugar","FFFFFF",.60f);
        world.blush=Mat("Rose milk","F5C7DB",.45f);
        world.mint=Mat("Pistachio","A1DECE",.3f);
        world.stone=Mat("Guardian stone","8C9FAF",.28f);
        world.stone.shader=Shader.Find("Jelly World/Sugar Stone");
        world.stoneDark=Mat("Guardian joints","455467",.35f);
        world.rune=Mat("Living mint crystal","69FFD1",.88f);
        world.rune.SetColor("_EmissionColor",new Color(.13f,.60f,.38f));
        world.gold=Mat("Guardian gold","F3CF87",.65f);world.gold.SetFloat("_Metallic",.3f);
        world.glow=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Art/Guardian glow.mat");
        if(world.glow==null) {
            world.glow=new Material(Shader.Find("Jelly World/Guardian Glow"));
            AssetDatabase.CreateAsset(world.glow,Root+"/Art/Guardian glow.mat");
        }
        world.fractureMesh=ShardMesh();
        world.iceShell=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Art/Downloaded/Ice001/IceShell.mat");
        if(world.iceShell==null){world.iceShell=new Material(Shader.Find("Jelly World/Downloaded Ice Shell"));AssetDatabase.CreateAsset(world.iceShell,Root+"/Art/Downloaded/Ice001/IceShell.mat");}
        string iceRoot=Root+"/Art/Downloaded/Ice001/Ice001_1K-JPG_";
        foreach(string suffix in new[]{"Color","NormalGL","Roughness"}) {
            string path=iceRoot+suffix+".jpg";var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer!=null){importer.sRGBTexture=suffix=="Color";importer.maxTextureSize=512;importer.wrapMode=TextureWrapMode.Repeat;importer.SaveAndReimport();}
        }
        world.iceShell.SetTexture("_IceTex",AssetDatabase.LoadAssetAtPath<Texture2D>(iceRoot+"Color.jpg"));
        world.iceShell.SetTexture("_IceNormal",AssetDatabase.LoadAssetAtPath<Texture2D>(iceRoot+"NormalGL.jpg"));
        world.iceShell.SetTexture("_Roughness",AssetDatabase.LoadAssetAtPath<Texture2D>(iceRoot+"Roughness.jpg"));
        world.toolStarMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"/Art/ToolStar.asset");
        if(world.toolStarMesh==null){world.toolStarMesh=JellyToolModels.CreateStarMesh();AssetDatabase.CreateAsset(world.toolStarMesh,Root+"/Art/ToolStar.asset");}
        world.toolArrowMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"/Art/ToolArrow.asset");
        if(world.toolArrowMesh==null){world.toolArrowMesh=JellyToolModels.CreateArrowMesh();AssetDatabase.CreateAsset(world.toolArrowMesh,Root+"/Art/ToolArrow.asset");}
        world.toolGlass=Mat("Tool capsule glass","D4FFF0",.92f);
        world.toolGlass.color=new Color(.82f,1,.94f,.22f);world.toolGlass.SetFloat("_Mode",3);
        world.toolGlass.SetInt("_SrcBlend",(int)BlendMode.SrcAlpha);world.toolGlass.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);
        world.toolGlass.SetInt("_ZWrite",0);world.toolGlass.EnableKeyword("_ALPHABLEND_ON");world.toolGlass.renderQueue=3000;
        world.uiFont=AssetDatabase.LoadAssetAtPath<Font>(Root+"/Fonts/JellyChinese.ttf");
        world.titleFont=AssetDatabase.LoadAssetAtPath<Font>(Root+"/Fonts/JellyTitle.ttf");
        world.roundSprite=RoundSprite();
        string audioRoot=Root+"/Audio/Downloaded/";
        string[] sounds={
            "interface-sounds/click_003.ogg","interface-sounds/tick_001.ogg","interface-sounds/switch_002.ogg",
            "interface-sounds/drop_002.ogg","interface-sounds/error_002.ogg","ice-spells/ice.wav",
            "impact-sounds/impactSoft_medium_000.ogg","impact-sounds/impactSoft_heavy_000.ogg",
            "interface-sounds/pluck_002.ogg","interface-sounds/maximize_003.ogg",
            "interface-sounds/confirmation_004.ogg","interface-sounds/error_003.ogg",
            "digital-audio/phaseJump1.ogg","digital-audio/phaserUp1.ogg"
        };
        world.soundClips=new AudioClip[sounds.Length];
        for(int i=0;i<sounds.Length;i++) {
            var importer=AssetImporter.GetAtPath(audioRoot+sounds[i]) as AudioImporter;
            if(importer!=null){var settings=importer.defaultSampleSettings;settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=.75f;importer.defaultSampleSettings=settings;importer.forceToMono=true;importer.SaveAndReimport();}
            world.soundClips[i]=AssetDatabase.LoadAssetAtPath<AudioClip>(audioRoot+sounds[i]);
        }
        string[] iconNames={"hammer","cross","shuffle","guardian","blaster"};
        world.toolIcons=new Sprite[iconNames.Length];
        for(int i=0;i<iconNames.Length;i++) {
            string iconPath=Root+"/Art/ToolIcons/"+iconNames[i]+".png";
            var importer=AssetImporter.GetAtPath(iconPath) as TextureImporter;
            if(importer!=null) {
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
                importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.maxTextureSize=512;
                importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
                world.toolIcons[i]=AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            }
        }
        var key=new GameObject("Sun • softbox").AddComponent<Light>();
        key.type=LightType.Directional;key.intensity=.85f;key.color=new Color(1,.94f,.88f);
        key.transform.rotation=Quaternion.Euler(35,-32,0);key.shadows=LightShadows.Soft;
        var fill=new GameObject("Sky • fill").AddComponent<Light>();
        fill.type=LightType.Directional;fill.intensity=.28f;fill.color=new Color(.78f,.86f,1);
        fill.transform.rotation=Quaternion.Euler(-20,145,0);
        RenderSettings.ambientMode=AmbientMode.Flat;
        RenderSettings.ambientLight=new Color(.58f,.60f,.68f);
        RenderSettings.fog=false;
        QualitySettings.antiAliasing=4;
        world.BuildPresentation();
        var prefab=new GameObject("Jelly piece");
        prefab.AddComponent<MeshFilter>().sharedMesh=world.jellyMesh;
        prefab.AddComponent<MeshRenderer>().sharedMaterial=world.candy[0];
        prefab.AddComponent<BoxCollider>();
        PrefabUtility.SaveAsPrefabAsset(prefab,Root+"/Prefabs/JellyPiece.prefab");
        UnityEngine.Object.DestroyImmediate(prefab);
        var guardian=new GameObject("Moss Sugar Crystal Guardian");
        guardian.AddComponent<JellyGuardianVisual>().Build(world);
        PrefabUtility.SaveAsPrefabAsset(guardian,Root+"/Prefabs/SugarCrystalGuardian.prefab");
        UnityEngine.Object.DestroyImmediate(guardian);
        for(int i=0;i<5;i++) {
            var model=JellyToolModels.Create(world,i,null);
            PrefabUtility.SaveAsPrefabAsset(model.gameObject,Root+"/Prefabs/Tool"+i+".prefab");
            UnityEngine.Object.DestroyImmediate(model.gameObject);
        }
        EditorSceneManager.SaveScene(scene,"Assets/Scenes/JellyWorld.unity");
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/JellyWorld.unity",true)};
        PlayerSettings.productName="Jelly World";
        PlayerSettings.companyName="Jelly World Studio";
        PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=900;
        PlayerSettings.runInBackground=true;
        AssetDatabase.SaveAssets();
        Selection.activeGameObject=go;
        Debug.Log("JELLY WORLD: scene built successfully using rounded_cube.glb.");
    }
    static Material Mat(string name,string hex,float gloss)
    {
        string path=Root+"/Art/"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(m,path);}
        Color c;ColorUtility.TryParseHtmlString("#"+hex,out c);
        m.color=c;m.SetFloat("_Glossiness",gloss);m.SetFloat("_Metallic",.03f);
        m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",c*.035f);
        return m;
    }
    static Mesh ImportMesh()
    {
        string path=Root+"/Art/RoundedJelly.asset";
        var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing!=null)return existing;
        byte[] bytes=File.ReadAllBytes("Assets/Scenes/rounded_cube.glb");
        int jsonLen=BitConverter.ToInt32(bytes,12);
        var json=JObject.Parse(System.Text.Encoding.UTF8.GetString(bytes,20,jsonLen));
        int bin=20+jsonLen+8;
        var accessors=(JArray)json["accessors"];var views=(JArray)json["bufferViews"];
        var primitive=json["meshes"][0]["primitives"][0];
        int pa=(int)primitive["attributes"]["POSITION"],na=(int)primitive["attributes"]["NORMAL"],ia=(int)primitive["indices"];
        var verts=ReadVectors(bytes,bin,accessors,views,pa,true);
        var normals=ReadVectors(bytes,bin,accessors,views,na,false);
        var acc=accessors[ia];var view=views[(int)acc["bufferView"]];
        int start=bin+(view["byteOffset"]==null?0:(int)view["byteOffset"])+(acc["byteOffset"]==null?0:(int)acc["byteOffset"]);
        int count=(int)acc["count"],ctype=(int)acc["componentType"];
        int[] indices=new int[count];
        for(int i=0;i<count;i++)indices[i]=ctype==5125?(int)BitConverter.ToUInt32(bytes,start+i*4):BitConverter.ToUInt16(bytes,start+i*2);
        for(int i=0;i<count;i+=3){int t=indices[i];indices[i]=indices[i+2];indices[i+2]=t;}
        Mesh mesh=new Mesh();mesh.name="Rounded jelly • from provided GLB";
        mesh.vertices=verts;mesh.normals=normals;mesh.triangles=indices;mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
    static Mesh ShardMesh()
    {
        string path=Root+"/Art/SugarShard.asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh!=null)return mesh;
        Vector3[] corners={new Vector3(0,.65f,0),new Vector3(.48f,0,0),new Vector3(0,0,.46f),
            new Vector3(-.48f,0,0),new Vector3(0,0,-.46f),new Vector3(0,-.5f,0)};
        int[] faces={0,2,1,0,3,2,0,4,3,0,1,4,5,1,2,5,2,3,5,3,4,5,4,1};
        var vertices=new Vector3[24];var indices=new int[24];
        for(int i=0;i<24;i++){vertices[i]=corners[faces[i]];indices[i]=i;}
        mesh=new Mesh();mesh.name="Faceted sugar crystal and fracture shard";
        mesh.vertices=vertices;mesh.triangles=indices;mesh.RecalculateNormals();mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
    static Vector3[] ReadVectors(byte[] bytes,int bin,JArray accessors,JArray views,int a,bool position)
    {
        var ac=accessors[a];var v=views[(int)ac["bufferView"]];
        int start=bin+(v["byteOffset"]==null?0:(int)v["byteOffset"])+(ac["byteOffset"]==null?0:(int)ac["byteOffset"]);
        int stride=v["byteStride"]==null?12:(int)v["byteStride"];int n=(int)ac["count"];
        var result=new Vector3[n];
        for(int i=0;i<n;i++) {
            int k=start+i*stride;
            result[i]=new Vector3(-BitConverter.ToSingle(bytes,k),BitConverter.ToSingle(bytes,k+4),BitConverter.ToSingle(bytes,k+8))*(position?.5f:1);
        }
        return result;
    }
    static Sprite RoundSprite()
    {
        string path=Root+"/Art/RoundedPanel.png";
        var tex=new Texture2D(128,128,TextureFormat.RGBA32,false);
        for(int y=0;y<128;y++)for(int x=0;x<128;x++){
            float dx=Mathf.Max(Mathf.Abs(x-63.5f)-35.5f,0),dy=Mathf.Max(Mathf.Abs(y-63.5f)-35.5f,0);
            float alpha=Mathf.Clamp01(28-Mathf.Sqrt(dx*dx+dy*dy));
            tex.SetPixel(x,y,new Color(1,1,1,alpha));
        }
        tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite;importer.spriteBorder=new Vector4(32,32,32,32);
        importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    [MenuItem("Jelly World/Verify board rules")]
    public static void Verify()
    {
        int checkedMoves=0;
        for(int seed=0;seed<200;seed++) {
            var b=new JellyBoard(seed);if(b.Matches().Count!=0)throw new Exception("Initial match at seed "+seed);
            Vector2Int a,c;if(!b.FindMove(out a,out c))throw new Exception("Dead board");
            int av=b.Cells[a.x,a.y],cv=b.Cells[c.x,c.y];
            b.Swap(a,c);if(b.Matches().Count<3)throw new Exception("Invalid hint");
            b.Swap(a,c);if(b.Cells[a.x,a.y]!=av||b.Cells[c.x,c.y]!=cv)throw new Exception("Swap restore failed");
            checkedMoves++;
        }
        var cross=new JellyBoard(7);
        for(int x=0;x<8;x++)for(int y=0;y<8;y++)cross.Cells[x,y]=(x+y)%5;
        for(int k=1;k<=3;k++){cross.Cells[k,2]=4;cross.Cells[2,k]=4;}
        if(cross.Matches().Count!=5)throw new Exception("Cross should count each cell once");
        Debug.Log("JELLY RULES PASS: 200 seeds, match-free opening, legal hint, reversible swap, cross union. Moves: "+checkedMoves);
    }
}
