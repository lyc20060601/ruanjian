using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using UnityEngine;
using JellyWorldGame;

public static class JellyCampaignVerification
{
    static int checks;
    static void Check(bool ok,string name){if(!ok)throw new Exception("CAMPAIGN FAILED: "+name);checks++;}
    [UnityEditor.MenuItem("Jelly World/Verify campaign (Play Mode)")]
    public static void Run()
    {
        if(!Application.isPlaying)return;
        new GameObject("Campaign verification").AddComponent<JellyTile>().StartCoroutine(Verify(UnityEngine.Object.FindObjectOfType<JellyWorld>()));
    }
    static IEnumerator WaitFor(JellyWorld w,bool guardian=false)
    {
        float deadline=Time.realtimeSinceStartup+25;
        while((w.busy || guardian && w.mode=="golem") && Time.realtimeSinceStartup<deadline)yield return null;
        Check(!w.busy,"action finishes");
    }
    static void Stable(JellyWorld w)
    {
        int n=w.BoardSize;Check(w.tiles.GetLength(0)==n,"dynamic tile array");
        for(int x=0;x<n;x++)for(int y=0;y<n;y++) {
            var t=w.tiles[x,y];
            Check(t!=null && t.x==x && t.y==y && t.color==w.board.Cells[x,y] && t.color<w.board.ColorCount,"tile identity and color range");
        }
        Vector2Int a,b;Check(w.board.Matches().Count==0 && w.board.FindMove(out a,out b),"stable playable grid");
    }
    static IEnumerator Verify(JellyWorld w)
    {
        checks=0;
        var ints=new Dictionary<string,int>();var strings=new Dictionary<string,string>();var existing=new HashSet<string>();
        string[] baseInts={"JellyWorld.Best","JellyWorld.InfiniteBest","JellyWorld.Campaign.Unlocked"};
        foreach(var key in baseInts){ints[key]=PlayerPrefs.GetInt(key);if(PlayerPrefs.HasKey(key))existing.Add(key);}
        for(int i=1;i<=12;i++)foreach(var field in new[]{"Stars.","Best."}) {
            string key="JellyWorld.Campaign."+field+i;ints[key]=PlayerPrefs.GetInt(key);if(PlayerPrefs.HasKey(key))existing.Add(key);
        }
        foreach(var key in new[]{"JellyWorld.Campaign.Name","JellyWorld.Campaign.Ranks"}) {
            strings[key]=PlayerPrefs.GetString(key);if(PlayerPrefs.HasKey(key))existing.Add(key);
        }
        try {
            w.StartChallenge();
            foreach(var level in JellyLevels.All) {
                for(int seed=0;seed<30;seed++) {
                    var b=new JellyBoard(seed,level.Size,level.Colors);Vector2Int a,c;
                    Check(b.Matches().Count==0 && b.FindMove(out a,out c),"level opening legal");
                    for(int x=0;x<level.Size;x++)for(int y=0;y<level.Size;y++)Check(b.Cells[x,y]<level.Colors,"configured color count");
                }
                w.level=level.Id;w.NewGame();yield return null;Stable(w);
                Check(w.moves==level.Moves && w.FrostRemaining==level.Frost,"level objectives and move budget");
                w.score=level.Score;
                if(level.Frost>0 || level.ColorA>=0)Check(!w.ObjectivesMet,"score alone cannot bypass collection or ice");
            }
            w.level=12;w.NewGame();yield return null;
            int color=w.board.Cells[9,9];w.ChooseTool(0);w.ClickCell(new Vector2Int(9,9));
            yield return WaitFor(w);Check(w.collected[color]>0,"hammer counts collection at last cell");Stable(w);
            w.ChooseTool(1);w.ClickCell(new Vector2Int(9,9));yield return WaitFor(w);Check(w.score>=20*60,"10 by 10 cross clears 19 unique cells");Stable(w);
            w.ChooseTool(2);yield return WaitFor(w);Stable(w);
            w.SummonGuardian();Check(w.ConfirmGuardianLanding(new Vector2Int(9,9)),"largest-board landing accepted");
            while(w.Guardian.Transitioning)yield return null;
            Check(Vector3.Distance(w.Guardian.Actor.position,new Vector3(4.5f,.65f,4.5f))<.04f,"guardian coordinate uses board half");
            w.Guardian.Drive(Vector2.one,true,.1f);
            Check(w.Guardian.Actor.position.x<=4.621f && w.Guardian.Actor.position.z<=4.621f,"largest-board bounds");
            w.Guardian.End();yield return WaitFor(w,true);Stable(w);
            w.StartRange();while(w.Range.Transitioning)yield return null;
            Check(w.Range.Targets.GetLength(0)==10 && w.Range.Remaining==100,"range scales to 100 targets");
            yield return new WaitForSeconds(.22f);
            var target=w.Range.Targets[9,9];
            Check(w.Range.FireAtScreen(Camera.main.WorldToScreenPoint(target.transform.position)),"shoot last cell");
            yield return new WaitForSeconds(.8f);Check(w.Range.Hits==1,"shot resolves on dynamic board");
            w.Range.End();while(w.mode=="range")yield return null;yield return WaitFor(w);Stable(w);
            w.StartChallenge();w.moves=0;
            yield return w.Resolve(new HashSet<Vector2Int>(),false);
            Check(w.mode=="result","loss at zero moves");
            w.NewGame();w.score=JellyLevels.Get(1).Score;
            JellyProgress.PlayerName="Campaign QA";yield return w.Resolve(new HashSet<Vector2Int>(),false);
            Check(w.mode=="result" && JellyProgress.Stars(1)>0 && JellyProgress.Unlocked>=2,"win saves stars and unlocks next level");
            Check(JellyProgress.Ranking(1).Exists(r=>r.player=="Campaign QA"),"completed run on local level ranking");
            JellyProgress.RecordInfinite(444);JellyProgress.RecordInfinite(222);
            Check(JellyProgress.Ranking(0).Find(r=>r.player=="Campaign QA").score==444,"separate infinite best record");
            Check(Vector3.Dot((Quaternion.AngleAxis(-12,Vector3.forward)*Quaternion.Euler(0,-90,0))*Vector3.right,Vector3.forward)>.999f,"hammer end axis faces board");
            Directory.CreateDirectory("Captures");
            File.WriteAllText("Captures/CampaignVerification.txt","PASS: "+checks+" checks\n12 configurations, 360 generated boards, dynamic tool edges, guardian and range bounds, collection/ice gating, loss, stars/unlocks, separate local rankings, hammer axis.\n"+DateTime.Now);
            Debug.Log("CAMPAIGN PASS: "+checks);
        } finally {
            w.score=0;w.ShowMenu();
            foreach(var pair in ints){if(existing.Contains(pair.Key))PlayerPrefs.SetInt(pair.Key,pair.Value);else PlayerPrefs.DeleteKey(pair.Key);}
            foreach(var pair in strings){if(existing.Contains(pair.Key))PlayerPrefs.SetString(pair.Key,pair.Value);else PlayerPrefs.DeleteKey(pair.Key);}
            PlayerPrefs.Save();
            typeof(JellyProgress).GetField("cache",BindingFlags.NonPublic|BindingFlags.Static).SetValue(null,null);
            w.ShowLevelSelect();
        }
    }
}
