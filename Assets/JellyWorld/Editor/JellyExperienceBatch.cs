using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using JellyWorldGame;

[InitializeOnLoad]
public static class JellyExperienceBatch
{
    const string Key = "JellyWorld.ExperienceQA";
    static double deadline;
    static JellyWorld world;
    static readonly Dictionary<string, string> saved = new Dictionary<string, string>();
    static readonly HashSet<string> present = new HashSet<string>();
    static readonly List<string> intKeys = new List<string>();
    static readonly string[] stringKeys = {"JellyWorld.Campaign.Name", "JellyWorld.Campaign.Ranks"};

    static JellyExperienceBatch()
    {
        EditorApplication.playModeStateChanged += StateChanged;
        EditorApplication.update += Watchdog;
    }
    public static void Run(){StartRun(false);}
    public static void RunVisuals(){StartRun(true);}
    static void StartRun(bool visualOnly)
    {
        SessionState.SetBool(Key+".VisualOnly",visualOnly);
        JellyExperienceVerification.VerifyRules();
        Directory.CreateDirectory("Captures/CandyOpen");
        SessionState.SetInt(Key, 1);
        EditorSceneManager.OpenScene("Assets/Scenes/JellyWorld.unity");
        EditorApplication.isPlaying = true;
    }
    static void StateChanged(PlayModeStateChange state)
    {
        if (SessionState.GetInt(Key, 0) == 0) return;
        if (state == PlayModeStateChange.EnteredPlayMode) {
            deadline = EditorApplication.timeSinceStartup + 240;
            EditorApplication.delayCall += () => {
                world = UnityEngine.Object.FindObjectOfType<JellyWorld>();
                Snapshot();
                new GameObject("Experience QA driver").AddComponent<JellyExperienceDriver>().StartCoroutine(Guard(Exercise()));
            };
        }
        if (state == PlayModeStateChange.EnteredEditMode) {
            int result = SessionState.GetInt(Key, 2) == 3 ? 0 : 1;
            SessionState.SetInt(Key, 0);
            EditorApplication.Exit(result);
        }
    }
    static void Watchdog()
    {
        if (SessionState.GetInt(Key, 0) == 1 && deadline > 0 && EditorApplication.timeSinceStartup > deadline)
            Finish(false, "runtime verification timed out");
    }
    static void Snapshot()
    {
        saved.Clear(); present.Clear(); intKeys.Clear();
        intKeys.AddRange(new[]{"JellyWorld.Best","JellyWorld.InfiniteBest","JellyWorld.Campaign.Unlocked","JellyWorld.Comfort.ReducedEffects"});
        for(int i=1;i<=12;i++) foreach(var field in new[]{"Stars.","Best."}) intKeys.Add("JellyWorld.Campaign."+field+i);
        foreach(string key in intKeys) { if(PlayerPrefs.HasKey(key))present.Add(key); saved[key]=PlayerPrefs.GetInt(key).ToString(); }
        foreach(string key in stringKeys) { if(PlayerPrefs.HasKey(key))present.Add(key); saved[key]=PlayerPrefs.GetString(key); }
    }
    static void ResetProfile(int unlocked)
    {
        foreach(string key in intKeys)PlayerPrefs.DeleteKey(key);
        foreach(string key in stringKeys)PlayerPrefs.DeleteKey(key);
        PlayerPrefs.SetInt("JellyWorld.Campaign.Unlocked",unlocked);
        typeof(JellyProgress).GetField("cache",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,null);
    }
    static void Finish(bool pass,string message)
    {
        SessionState.SetInt(Key, pass?3:2);
        if(world!=null){world.score=0;world.ShowMenu();}
        foreach(string key in intKeys) { if(present.Contains(key))PlayerPrefs.SetInt(key,int.Parse(saved[key]));else PlayerPrefs.DeleteKey(key); }
        foreach(string key in stringKeys) { if(present.Contains(key))PlayerPrefs.SetString(key,saved[key]);else PlayerPrefs.DeleteKey(key); }
        PlayerPrefs.Save();
        File.WriteAllText("Captures/CandyOpen/Verification.txt",(pass?"PASS: ":"FAIL: ")+message+"\n"+DateTime.Now.ToString("s"));
        Debug.Log((pass?"COMFORT JOURNEY PASS: ":"COMFORT JOURNEY FAIL: ")+message);
        EditorApplication.isPlaying=false;
    }
    static IEnumerator Guard(IEnumerator root)
    {
        var stack=new Stack<IEnumerator>();stack.Push(root);
        while(stack.Count>0) {
            object next=null;bool moved=false;Exception failure=null;
            try { moved=stack.Peek().MoveNext();if(moved)next=stack.Peek().Current; }
            catch(Exception e){failure=e;}
            if(failure!=null){while(stack.Count>0){try{(stack.Pop() as IDisposable)?.Dispose();}catch{}}Finish(false,failure.ToString());yield break;}
            if(!moved){(stack.Pop() as IDisposable)?.Dispose();continue;}
            if(next is IEnumerator){stack.Push((IEnumerator)next);continue;}
            yield return next;
        }
        Finish(true,SessionState.GetBool(Key+".VisualOnly",false)?"visual capture and preview resource lifecycle checks":"journey gates, replay stars, chapter navigation, fresh/returning profiles; campaign, buffered input, ice/audio and infinite regressions; rendered screenshots");
    }
    static void Check(bool ok,string message){JellyExperienceVerification.Check(ok,message);}
    static bool Unlocked(int index){return (bool)typeof(JellyWorld).GetMethod("IsToolUnlocked").Invoke(world,new object[]{index});}
    static int Stars(){return (int)typeof(JellyWorld).Assembly.GetType("JellyWorldGame.JellyJourney").GetProperty("TotalStars").GetValue(null);}
    static IEnumerator Exercise()
    {
        yield return null;
        ResetProfile(1);world.ShowMenu();yield return null;
        yield return Capture("01-home");
        yield return Capture("01-home-1280",1280,720);
        var playButton=GameObject.Find("Continue journey");
        var feedback=playButton.GetComponent<JellyButtonFeedback>();var surface=playButton.GetComponent<JellyUISurface>();
        feedback.OnPointerDown(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current));
        yield return new WaitForSecondsRealtime(.15f);
        Check(surface.FaceOffset < -2,"raised button face depresses on pointer down");
        feedback.OnPointerUp(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current));
        yield return new WaitForSecondsRealtime(.3f);
        Check(Mathf.Abs(surface.FaceOffset)<.1f,"button face returns after release");
        var pointer=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
        feedback.OnPointerEnter(pointer);yield return new WaitForSecondsRealtime(.2f);
        Check(surface.FaceOffset>.3f,"hover gently lifts active button");
        yield return Capture("06-home-hover");
        feedback.OnPointerExit(pointer);yield return new WaitForSecondsRealtime(.3f);
        Check(Mathf.Abs(surface.FaceOffset)<.1f,"pointer exit clears elevation");
        feedback.OnPointerEnter(pointer);yield return new WaitForSecondsRealtime(.1f);
        playButton.SetActive(false);Check(Mathf.Abs(surface.FaceOffset)<.1f,"disable clears hover and press state");
        playButton.SetActive(true);yield return null;
        Check(Resources.Load<Cubemap>("StudioLighting/CandyStudio")!=null,"downloaded reflection map imports as cubemap");
        world.ShowLevelSelect();yield return null;
        yield return Capture("02-fresh-locked");yield return Capture("02-fresh-locked-1280",1280,720);
        CheckPreviewTextures();world.ShowMenu();
        GameObject.Find("Continue journey").GetComponent<Button>().onClick.Invoke();yield return null;
        Check(world.mode=="play" && world.level==1,"home continues into first level");
        foreach(var name in new[]{"Journey home card","Objectives card","Tool pool","Compact score bar"}) {
            foreach(var node in world.GetComponentsInChildren<RectTransform>(true))if(node.name==name)
                Check(node.GetComponent<Graphic>()==null,"layout group has no background/raycast graphic: "+name);
        }
        Check(playButton.GetComponent<Image>().material.shader.name=="Jelly World/Soft Button","rounded lighting shader assigned");
        yield return Capture("08-first-level");yield return Capture("08-first-level-1280",1280,720);
        // Button faces catch UI rays; unpainted layout padding does not.
        var toolCard=GameObject.Find("Tool card 0").GetComponent<RectTransform>();
        var rayPointer=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
        rayPointer.position=RectTransformUtility.WorldToScreenPoint(null,toolCard.position);
        var hits=new List<UnityEngine.EventSystems.RaycastResult>();UnityEngine.EventSystems.EventSystem.current.RaycastAll(rayPointer,hits);
        var hitGraphic=toolCard.GetComponent<Image>();
        Debug.Log("UI RAY BEFORE: pointer="+rayPointer.position+" screen="+Screen.width+"x"+Screen.height+" depth="+hitGraphic.depth+" culled="+hitGraphic.canvasRenderer.cull+" hits="+hits.Count);
        Canvas.ForceUpdateCanvases();yield return null;
        rayPointer.position=RectTransformUtility.WorldToScreenPoint(null,toolCard.position);hits.Clear();
        UnityEngine.EventSystems.EventSystem.current.RaycastAll(rayPointer,hits);
        Debug.Log("UI RAY AFTER: pointer="+rayPointer.position+" screen="+Screen.width+"x"+Screen.height+" depth="+hitGraphic.depth+" culled="+hitGraphic.canvasRenderer.cull+" hits="+hits.Count);
        Check(hits.Exists(hit=>hit.gameObject.name=="Tool card 0"),"tool card retains raycast target without outer plate");
        hits.Clear();rayPointer.position=RectTransformUtility.WorldToScreenPoint(null,GameObject.Find("Objectives card").transform.position);
        UnityEngine.EventSystems.EventSystem.current.RaycastAll(rayPointer,hits);
        Check(hits.Count==0,"blank objective-area layout does not intercept pointer");
        Check(playButton.GetComponent<Image>().material.shader.isSupported,"button shader supported");
        for(int i=0;i<5;i++)Check(!Unlocked(i),"fresh profile tools locked");
        int hammer=world.ToolCharges(0),shuffle=world.ToolCharges(2);
        world.ChooseTool(0);world.ChooseTool(2);world.SummonGuardian();world.StartRange();
        Check(world.mode=="play" && !world.busy && !world.ChoosingLanding,"direct entries obey locks");
        Check(world.ToolCharges(0)==hammer && world.ToolCharges(2)==shuffle,"locked tools do not spend charges");
        Check((int)typeof(JellyWorld).GetField("activeTool",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(world)==-1,"locked target tool cannot be selected");
        Check(!world.StartLevel(2),"locked campaign entry rejects");
        JellyProgress.Win(1,1000,2);JellyProgress.Win(1,500,1);
        Check(Stars()==2 && JellyProgress.Stars(1)==2,"replaying lower score cannot farm growth");
        Check(Unlocked(0) && !Unlocked(2),"winning first level unlocks only hammer");
        Vector2Int a,b;world.board.FindMove(out a,out b);var tile=world.tiles[a.x,a.y];
        tile.pressed=tile.hovered=true;
        typeof(JellyWorld).GetField("dragSource",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(world,tile);
        typeof(JellyWorld).GetField("hoverTile",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(world,tile);
        world.TogglePause();Check(!tile.pressed && !tile.hovered,"pause clears tactile pointer state");world.TogglePause();
        world.ShowLevelSelect();yield return null;yield return Capture("02-chapter-one");
        GameObject.Find("Chapter tab 2").GetComponent<Button>().onClick.Invoke();yield return null;
        Check(GameObject.Find("Level 9")!=null && !GameObject.Find("Level 9").GetComponent<Button>().interactable,"late chapter preview stays locked");
        yield return Capture("03-chapter-three");CheckPreviewTextures();
        GameObject.Find("Chapter tab 1").GetComponent<Button>().onClick.Invoke();yield return null;
        yield return Capture("03-chapter-two");CheckPreviewTextures();
        ResetProfile(8);
        PlayerPrefs.SetInt("JellyWorld.Campaign.Stars.1",3);PlayerPrefs.SetInt("JellyWorld.Campaign.Best.1",5678);
        const string priorRanks="{\"rows\":[{\"player\":\"Returning QA\",\"date\":\"2026-10-01\",\"level\":1,\"score\":5678,\"stars\":3}]}";
        PlayerPrefs.SetString("JellyWorld.Campaign.Ranks",priorRanks);world.ShowMenu();
        Check(Stars()==3 && JellyProgress.Best(1)==5678 && JellyProgress.Unlocked==8,"existing profile progress survives home refresh");
        Check(PlayerPrefs.GetString("JellyWorld.Campaign.Ranks")==priorRanks,"existing rankings unchanged");
        world.level=5;world.NewGame();yield return null;yield return Capture("04-board");
        yield return Capture("04-board-1280",1280,720);
        world.ChooseTool(0);yield return null;yield return Capture("07-selected-tool");
        world.ChooseTool(0);
        world.level=12;world.NewGame();yield return null;
        yield return Capture("09-three-objectives");
        world.level=5;world.NewGame();yield return null;

        world.busy=true;
        typeof(JellyWorld).GetMethod("RefreshToolPool",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(world,null);
        yield return new WaitForSecondsRealtime(.15f);yield return Capture("10-busy-controls");
        world.busy=false;
        typeof(JellyWorld).GetMethod("RefreshToolPool",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(world,null);

        var effects=(Transform)typeof(JellyWorld).GetField("effects",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(world);
        for(int i=0;i<100;i++)JellyVfx.Shatter(world,effects,Vector3.zero,i%5,Vector3.back);
        Check(effects.childCount<=120,"large clears respect debris budget");world.ClearGuardianEffects();yield return null;
        world.SetReducedEffects(true);Check(PlayerPrefs.GetInt("JellyWorld.Comfort.ReducedEffects")==1,"comfort preference persisted");
        world.OpenAudioSettings();yield return null;yield return Capture("05-settings");
        world.TogglePause();
        if(SessionState.GetBool(Key+".VisualOnly",false))yield break;
        world.StartInfinite();yield return null;for(int i=0;i<5;i++)Check(Unlocked(i),"infinite opens tools");
        world.score=0;world.ShowMenu();ResetProfile(12);
        var flags=BindingFlags.Static|BindingFlags.NonPublic;
        yield return (IEnumerator)typeof(JellyCampaignVerification).GetMethod("Verify",flags).Invoke(null,new object[]{world});
        yield return (IEnumerator)typeof(JellyInputVerification).GetMethod("Verify",flags).Invoke(null,new object[]{world});
        yield return (IEnumerator)typeof(JellyFeelVerification).GetMethod("Verify",flags).Invoke(null,new object[]{world});
        yield return JellyInfiniteVerification.Run(world);
    }
    static void CheckPreviewTextures()
    {
        var previews=world.GetComponentsInChildren<RawImage>();int count=0;
        foreach(var image in previews)if(image.name=="Map preview"){var texture=image.texture as RenderTexture;Check(texture!=null && texture.IsCreated(),"live 3D map thumbnail");count++;}
        Check(count==4,"one live preview per visible level");
        int owned=0;foreach(var texture in Resources.FindObjectsOfTypeAll<RenderTexture>())if(texture.name.StartsWith("Map diorama ") && texture.IsCreated())owned++;
        Check(owned==4,"chapter switching releases previous render textures");
        Check(GameObject.Find("Temporary map diorama")==null,"preview geometry cleaned up");
    }
    static IEnumerator Capture(string name,int width=1600,int height=900)
    {
        var camera=Camera.main;var canvas=world.GetComponentInChildren<Canvas>();
        var mode=canvas.renderMode;var oldCamera=canvas.worldCamera;float oldDistance=canvas.planeDistance;
        var target=camera.targetTexture;float oldAspect=camera.aspect;var active=RenderTexture.active;
        var rt=new RenderTexture(width,height,24);var image=new Texture2D(width,height,TextureFormat.RGB24,false);
        try {
            camera.targetTexture=rt;camera.aspect=(float)width/height;
            canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
            yield return null;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;
            image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();
            File.WriteAllBytes("Captures/CandyOpen/"+name+".png",image.EncodeToPNG());
        } finally {
            canvas.renderMode=mode;canvas.worldCamera=oldCamera;canvas.planeDistance=oldDistance;
            camera.targetTexture=target;camera.aspect=oldAspect;RenderTexture.active=active;
            rt.Release();UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(image);
        }
    }
}

public sealed class JellyExperienceDriver : MonoBehaviour { }
