using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using JellyWorldGame;

[InitializeOnLoad]
public static class JellyRaceVerification
{
    const string Session="JellyWorld.RaceQA";
    const string Folder="Captures/RaceUI";
    static JellyWorld world;
    static bool running;
    static double deadline;
    static readonly Dictionary<string,int> savedInts=new Dictionary<string,int>();
    static readonly Dictionary<string,string> savedStrings=new Dictionary<string,string>();
    static readonly HashSet<string> present=new HashSet<string>();
    static readonly List<string> checks=new List<string>();

    static JellyRaceVerification()
    {
        EditorApplication.playModeStateChanged+=StateChanged;
        EditorApplication.update+=()=>{if(running && EditorApplication.timeSinceStartup>deadline)Finish(false,"Runtime verification timed out");};
    }
    static void Check(bool condition,string message)
    {
        if(!condition)throw new InvalidOperationException(message);
        checks.Add("PASS: "+message);
    }
    [MenuItem("Jelly World/Verify race rules")]
    public static void VerifyRules()
    {
        var clock=new JellyRaceClock();clock.Start(10);clock.Tick(12);
        Check(clock.Seconds==2,"race clock advances from monotonic time");
        clock.SetPaused(true,12.5);clock.Tick(90);
        Check(clock.Seconds==2.5,"pause excludes all waiting time");
        clock.SetPaused(false,90);clock.Tick(91);clock.Complete(91.25);clock.Tick(500);
        Check(clock.Seconds==3.75 && clock.Completed && !clock.Running,"completion freezes exact target time");
        Check(!clock.Complete(1000) && clock.Seconds==3.75,"completion is idempotent");
        clock.Start(20);clock.Tick(19);clock.Tick(21);clock.Tick(double.NaN);
        Check(clock.Seconds==1 && !clock.Completed,"restart resets the clock and bad timestamps do not corrupt it");
        clock.Cancel();clock.Tick(100);
        Check(clock.Seconds==1,"abandoned races stop ticking");
        Check(JellyRace.Targets.SequenceEqual(new[]{3000,6000,10000}) && !JellyRace.ValidTarget(0),"only supported target distances are accepted");
        Check(JellyRace.FormatTime(65.432,true)=="01:05.432" && JellyRace.FormatTime(65.432)=="01:05.4","time formatting preserves minutes and milliseconds");
        var backups=JellyRace.Targets.ToDictionary(t=>t,t=>PlayerPrefs.GetInt(JellyRace.BestKey(t),0));
        var existing=new HashSet<int>(JellyRace.Targets.Where(t=>PlayerPrefs.HasKey(JellyRace.BestKey(t))));
        try {
            foreach(int target in JellyRace.Targets)PlayerPrefs.DeleteKey(JellyRace.BestKey(target));
            Check(JellyRace.Record(3000,20) && !JellyRace.Record(3000,30),"only a faster completed time replaces the personal best");
            Check(JellyRace.Record(3000,10.2341) && JellyRace.BestMilliseconds(3000)==10235,"saved times round upward to avoid inventing a faster result");
            Check(JellyRace.Record(6000,25) && JellyRace.BestMilliseconds(10000)==0 && JellyRace.BestMilliseconds(3000)==10235,"targets keep independent records");
            Check(!JellyRace.Record(3000,0) && !JellyRace.Record(3000,-1) && !JellyRace.Record(3000,double.NaN) && !JellyRace.Record(3000,double.PositiveInfinity) && !JellyRace.Record(123,1),"invalid results are rejected");
        } finally {
            foreach(int target in JellyRace.Targets)if(existing.Contains(target))PlayerPrefs.SetInt(JellyRace.BestKey(target),backups[target]);else PlayerPrefs.DeleteKey(JellyRace.BestKey(target));
            PlayerPrefs.Save();
        }
        Debug.Log("JELLY RACE RULES PASS: timing, pause, restart, precision and independent bests");
    }
    [MenuItem("Jelly World/Verify race and capture UI")]
    public static void Run()
    {
        if(running)return;
        checks.Clear();VerifyRules();JellyExperienceVerification.VerifyRules();
        Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"/Verification.txt","RUNNING");
        if(EditorApplication.isPlaying){Begin();return;}
        SessionState.SetBool(Session,true);
        if(EditorSceneManager.GetActiveScene().path!="Assets/Scenes/JellyWorld.unity")EditorSceneManager.OpenScene("Assets/Scenes/JellyWorld.unity");
        EditorApplication.isPlaying=true;
    }
    static void StateChanged(PlayModeStateChange state)
    {
        if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Session,false))EditorApplication.delayCall+=Begin;
        if(state==PlayModeStateChange.ExitingPlayMode && running)Finish(false,"Play Mode stopped before verification completed");
    }
    static void Begin()
    {
        SessionState.SetBool(Session,false);
        world=UnityEngine.Object.FindFirstObjectByType<JellyWorld>();
        if(world==null){File.WriteAllText(Folder+"/Verification.txt","FAIL: JellyWorld not loaded");return;}
        Snapshot();running=true;deadline=EditorApplication.timeSinceStartup+240;
        new GameObject("Race UI verification driver").AddComponent<JellyRaceVerificationDriver>().StartCoroutine(Guard(Exercise()));
    }
    static void Snapshot()
    {
        savedInts.Clear();savedStrings.Clear();present.Clear();
        var intKeys=new List<string>{"JellyWorld.Best","JellyWorld.InfiniteBest","JellyWorld.Campaign.Unlocked","JellyWorld.Comfort.ReducedEffects"};
        for(int i=1;i<=12;i++){intKeys.Add("JellyWorld.Campaign.Stars."+i);intKeys.Add("JellyWorld.Campaign.Best."+i);}
        foreach(int target in JellyRace.Targets)intKeys.Add(JellyRace.BestKey(target));
        foreach(string key in intKeys){savedInts[key]=PlayerPrefs.GetInt(key);if(PlayerPrefs.HasKey(key))present.Add(key);}
        foreach(string key in new[]{"JellyWorld.Campaign.Name","JellyWorld.Campaign.Ranks"}){savedStrings[key]=PlayerPrefs.GetString(key);if(PlayerPrefs.HasKey(key))present.Add(key);}
    }
    static void Finish(bool pass,string message)
    {
        if(!running)return;running=false;
        try {if(world!=null){world.score=0;world.ShowMenu();}}
        finally {
            foreach(var pair in savedInts)if(present.Contains(pair.Key))PlayerPrefs.SetInt(pair.Key,pair.Value);else PlayerPrefs.DeleteKey(pair.Key);
            foreach(var pair in savedStrings)if(present.Contains(pair.Key))PlayerPrefs.SetString(pair.Key,pair.Value);else PlayerPrefs.DeleteKey(pair.Key);
            PlayerPrefs.Save();
            typeof(JellyProgress).GetField("cache",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).SetValue(null,null);
            File.WriteAllText(Folder+"/Verification.txt",(pass?"PASS: ":"FAIL: ")+message+"\n"+string.Join("\n",checks));
        }
        Debug.Log((pass?"JELLY RACE UI PASS: ":"JELLY RACE UI FAIL: ")+message);
        EditorApplication.isPlaying=false;
    }
    static IEnumerator Guard(IEnumerator root)
    {
        var stack=new Stack<IEnumerator>();stack.Push(root);
        while(stack.Count>0) {
            object next=null;bool moved=false;Exception failure=null;
            try{moved=stack.Peek().MoveNext();if(moved)next=stack.Peek().Current;}catch(Exception error){failure=error;}
            if(failure!=null){while(stack.Count>0){try{(stack.Pop() as IDisposable)?.Dispose();}catch{}}Finish(false,failure.ToString());yield break;}
            if(!moved){(stack.Pop() as IDisposable)?.Dispose();continue;}
            if(next is IEnumerator){stack.Push((IEnumerator)next);continue;}
            yield return next;
        }
        Finish(true,"real swaps reach target; pause, records, replay, abandoned runs, mode isolation and rendered UI verified");
    }
    static IEnumerator Exercise()
    {
        yield return null;world.ShowMenu();yield return null;
        Check(GameObject.Find("Race mode entry")!=null,"home exposes the race entry");
        yield return Capture("01-home");yield return Capture("01-home-1280",1280,720);
        world.ShowRaceLobby();yield return null;
        foreach(int target in JellyRace.Targets)Check(GameObject.Find("Race start "+target).GetComponent<Button>().interactable,"race target is selectable: "+target);
        yield return Capture("02-race-lobby");yield return Capture("02-race-lobby-1280",1280,720);yield return Capture("02-race-lobby-4x3",1024,768);
        foreach(int target in JellyRace.Targets)PlayerPrefs.DeleteKey(JellyRace.BestKey(target));
        world.StartRace(3000);yield return null;
        Check(world.RaceMode && !world.InfiniteMode && world.BoardSize==8 && world.board.ColorCount==5 && world.FrostRemaining==0,"race starts on its standard board without campaign objectives");
        Check(!GameObject.Find("Tool pool") && GameObject.Find("Race dashboard")!=null,"race dashboard replaces the campaign tool tray");
        int moves=world.moves,charges=world.ToolCharges(0);
        world.ChooseTool(0);world.SummonGuardian();world.StartRange();
        Check(world.mode=="play" && !world.ChoosingLanding && world.ToolCharges(0)==charges,"race rejects all direct tool entry points");
        world.TogglePause();double stopped=world.RaceSeconds;
        yield return new WaitForSecondsRealtime(.3f);
        Check(Math.Abs(world.RaceSeconds-stopped)<.01,"live pause freezes the race clock");
        yield return Capture("04-race-paused");
        world.TogglePause();yield return new WaitForSecondsRealtime(.2f);
        Check(world.RaceSeconds>stopped+.1,"live resume continues timing");
        bool invalid=false;
        for(int x=0;x<7 && !invalid;x++)for(int y=0;y<8 && !invalid;y++) {
            var a=new Vector2Int(x,y);var b=new Vector2Int(x+1,y);
            world.board.Swap(a,b);bool valid=world.board.Matches().Count>0;world.board.Swap(a,b);
            if(!valid){int before=world.score;yield return world.SwapRoutine(a,b);Check(world.score==before && world.moves==moves,"invalid swaps revert without score or move changes");invalid=true;}
        }
        for(int attempt=0;attempt<80 && world.mode=="play";attempt++) {
            if(world.IsPaused)world.TogglePause();
            Vector2Int a,b;Check(world.board.FindMove(out a,out b),"race board retains a valid move");
            yield return world.SwapRoutine(a,b);
            if(attempt==1){yield return Capture("03-race-playing");yield return Capture("03-race-playing-1280",1280,720);yield return Capture("03-race-playing-4x3",1024,768);}
        }
        Check(world.mode=="result" && world.RaceCompleted && world.score>=3000,"real matching and cascades finish the race at its target");
        Check(world.moves==moves,"valid race swaps do not consume moves");
        double finish=world.RaceSeconds;int best=JellyRace.BestMilliseconds(3000);
        yield return new WaitForSecondsRealtime(.25f);
        Check(world.RaceSeconds==finish && best>0 && best==JellyRace.BestMilliseconds(3000),"result time is frozen and completion persists one personal best");
        Check(savedInts.Where(p=>p.Key.StartsWith("JellyWorld.Campaign.")).All(p=>PlayerPrefs.GetInt(p.Key)==p.Value) && savedStrings.All(p=>PlayerPrefs.GetString(p.Key)==p.Value),"race completion preserves campaign progress and rankings");
        yield return Capture("05-race-result");
        GameObject.Find("Dialog primary").GetComponent<Button>().onClick.Invoke();yield return null;
        Check(world.RaceMode && world.score==0 && !world.RaceCompleted && world.RaceSeconds<.5,"result replay creates a fresh race");
        world.ShowMenu();Check(JellyRace.BestMilliseconds(3000)==best,"leaving an unfinished race does not replace its best");
        world.StartRace(6000);yield return null;Check(world.RaceTarget==6000 && world.score==0,"switching target resets score and uses its own distance");
        world.StartInfinite();yield return null;
        Check(!world.RaceMode && world.InfiniteMode && Enumerable.Range(0,5).All(world.IsToolUnlocked),"infinite mode restores all tools after a race");
        world.score=0;world.StartChallenge();yield return null;
        Check(!world.RaceMode && !world.InfiniteMode && world.moves==world.LevelSpec.Moves,"campaign restores its own move and objective rules");
        yield return Capture("06-campaign");world.ShowLevelSelect();yield return null;yield return Capture("07-levels");
    }
    static IEnumerator Capture(string name,int width=1600,int height=900)
    {
        var camera=Camera.main;var canvas=world.GetComponentInChildren<Canvas>();
        var mode=canvas.renderMode;var oldCamera=canvas.worldCamera;float oldDistance=canvas.planeDistance;
        var target=camera.targetTexture;float aspect=camera.aspect;var active=RenderTexture.active;
        var rt=new RenderTexture(width,height,24);var image=new Texture2D(width,height,TextureFormat.RGB24,false);
        try {
            camera.targetTexture=rt;camera.aspect=(float)width/height;
            canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
            yield return null;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;
            image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();File.WriteAllBytes(Folder+"/"+name+".png",image.EncodeToPNG());
        } finally {
            canvas.renderMode=mode;canvas.worldCamera=oldCamera;canvas.planeDistance=oldDistance;
            camera.targetTexture=target;camera.aspect=aspect;RenderTexture.active=active;
            rt.Release();UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(image);
        }
    }
}
public sealed class JellyRaceVerificationDriver : MonoBehaviour { }
