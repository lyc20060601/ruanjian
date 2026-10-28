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
            if(w.board.Contains(new Vector2Int(x,y)))
                Check(t!=null && t.x==x && t.y==y && t.color==w.board.Cells[x,y] && t.color<w.board.ColorCount,"tile identity and color range");
            else Check(t==null && w.board.Cells[x,y]==JellyBoard.Blocked,"map hole stays empty");
        }
        Vector2Int a,b;Check(w.board.Matches().Count==0 && w.board.FindMove(out a,out b),"stable playable grid");
    }
    static Vector2Int LastPlayable(JellyBoard board)
    {
        for(int y=board.Width-1;y>=0;y--)for(int x=board.Width-1;x>=0;x--) {
            var p=new Vector2Int(x,y);if(board.Contains(p))return p;
        }
        return new Vector2Int(-1,-1);
    }
    static int CrossCount(JellyBoard board,Vector2Int center)
    {
        var cells=new HashSet<Vector2Int>();
        for(int i=0;i<board.Width;i++) {
            var horizontal=new Vector2Int(i,center.y);if(board.Contains(horizontal))cells.Add(horizontal);
            var vertical=new Vector2Int(center.x,i);if(board.Contains(vertical))cells.Add(vertical);
        }
        return cells.Count;
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
                    var b=new JellyBoard(seed,level.Size,level.Colors,level.CreateMask());Vector2Int a,c;
                    Check(b.Matches().Count==0 && b.FindMove(out a,out c),"level opening legal");
                    Check(b.PlayableCount==level.CellCount,"configured map cell count");
                    for(int x=0;x<level.Size;x++)for(int y=0;y<level.Size;y++)
                        Check(b.Contains(new Vector2Int(x,y))?b.Cells[x,y]>=0 && b.Cells[x,y]<level.Colors:b.Cells[x,y]==JellyBoard.Blocked,"configured map and color range");
                }
                w.level=level.Id;w.NewGame();yield return null;Stable(w);
                Check(w.moves==level.Moves && w.FrostRemaining==level.Frost,"level objectives and move budget");
                w.score=level.Score;
                if(level.Frost>0 || level.ColorA>=0)Check(!w.ObjectivesMet,"score alone cannot bypass collection or ice");
            }
            w.level=12;w.NewGame();yield return null;
            var edge=LastPlayable(w.board);int color=w.board.Cells[edge.x,edge.y];w.ChooseTool(0);w.ClickCell(edge);
            yield return WaitFor(w);Check(w.collected[color]>0,"hammer counts collection at last cell");Stable(w);
            int cross=CrossCount(w.board,edge);w.ChooseTool(1);w.ClickCell(edge);yield return WaitFor(w);Check(w.score>=cross*60,"cross clears every playable row and column cell");Stable(w);
            w.ChooseTool(2);yield return WaitFor(w);Stable(w);
            edge=LastPlayable(w.board);w.SummonGuardian();Check(w.ConfirmGuardianLanding(edge),"map-edge landing accepted");
            while(w.Guardian.Transitioning)yield return null;
            Check(Vector3.Distance(w.Guardian.Actor.position,new Vector3(edge.x-w.BoardHalf,.65f,edge.y-w.BoardHalf))<.04f,"guardian coordinate uses mapped cell");
            w.Guardian.Drive(Vector2.one,true,.1f);
            Check(w.Guardian.Actor.position.x<=4.621f && w.Guardian.Actor.position.z<=4.621f,"largest-board bounds");
            w.Guardian.End();yield return WaitFor(w,true);Stable(w);
            w.StartRange();while(w.Range.Transitioning)yield return null;
            Check(w.Range.Targets.GetLength(0)==10 && w.Range.Remaining==w.PlayableCellCount,"range uses mapped targets");
            yield return new WaitForSeconds(.22f);
            edge=LastPlayable(w.board);var target=w.Range.Targets[edge.x,edge.y];
            Check(w.Range.FireAtScreen(Camera.main.WorldToScreenPoint(target.transform.position)),"shoot last cell");
            yield return new WaitForSeconds(.8f);Check(w.Range.Hits==1,"shot resolves on dynamic board");
            w.Range.End();while(w.mode=="range")yield return null;yield return WaitFor(w);Stable(w);
            w.StartChallenge();w.moves=0;
            yield return w.Resolve(new HashSet<Vector2Int>(),false);
            Check(w.mode=="result","loss at zero moves");
            w.NewGame();w.score=JellyLevels.Get(1).Score;
            JellyProgress.PlayerName="Campaign QA";yield return w.Resolve(new HashSet<Vector2Int>(),false);
            Check(w.mode=="result" && JellyProgress.Stars(1)>0 && JellyProgress.Unlocked>=2,"win saves stars and unlocks next level");
            Check(w.score==JellyLevels.Get(1).Score+w.moves*300,"remaining moves award 300 points each");
            var replay=GameObject.Find("Dialog secondary");
            Check(replay!=null && replay.GetComponentInChildren<UnityEngine.UI.Text>().text=="重玩本关","victory shows replay button");
            replay.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Check(w.mode=="play" && w.level==1 && w.score==0 && w.moves==JellyLevels.Get(1).Moves,"replay restarts the same level");
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
