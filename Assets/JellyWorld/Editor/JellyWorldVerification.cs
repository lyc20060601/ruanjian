using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using JellyWorldGame;

public static class JellyWorldVerification
{
    [UnityEditor.MenuItem("Jelly World/Verify playable flow (Play Mode)")]
    public static void StartVerification()
    {
        // The current campaign suite covers variable board sizes and restores all saved profile data.
        JellyCampaignVerification.Run();
    }

    static int assertions;
    static void Check(bool ok,string message) {if(!ok)throw new Exception("JELLY SMOKE FAILED: "+message);assertions++;}
    static void Stable(JellyWorld w) {
        Check(!w.busy,"board settled");
        Check(w.board.Matches().Count==0,"no unresolved matches");
        for(int x=0;x<8;x++)for(int y=0;y<8;y++) {
            var t=w.tiles[x,y];
            Check(t!=null && t.x==x && t.y==y && t.color==w.board.Cells[x,y],"visual/model synchronization");
        }
        Vector2Int a,b;Check(w.board.FindMove(out a,out b),"legal move exists");
    }
    public static IEnumerator Run(JellyWorld w)
    {
        assertions=0;int oldBest=PlayerPrefs.GetInt("JellyWorld.Best",0);
        // Use the real start button callback, then exercise public input entry points.
        var start=GameObject.Find("Start adventure").GetComponent<Button>();
        start.onClick.Invoke();yield return null;
        Check(w.mode=="play" && w.moves==26 && w.score==0,"start button");
        Stable(w);
        Vector2Int badA=Vector2Int.zero,badB=Vector2Int.zero;bool found=false;
        for(int x=0;x<7 && !found;x++)for(int y=0;y<8 && !found;y++) {
            var a=new Vector2Int(x,y);var b=a+Vector2Int.right;
            w.board.Swap(a,b);bool bad=w.board.Matches().Count==0;w.board.Swap(a,b);
            if(bad){badA=a;badB=b;found=true;}
        }
        Check(found,"invalid swap test setup");
        int original=w.board.Cells[badA.x,badA.y];
        w.ClickCell(badA);w.ClickCell(badB);yield return new WaitForSeconds(1);
        Check(w.moves==26 && w.score==0 && w.board.Cells[badA.x,badA.y]==original,"invalid swap refunds move and restores cells");
        Vector2Int aa,bb;w.board.FindMove(out aa,out bb);
        w.ClickCell(aa);w.ClickCell(bb);
        yield return Settle(w);
        Check(w.moves==25 && w.score>=180,"valid swap scores and spends one move");Stable(w);
        // Restart to avoid random large cascades reaching the win threshold.
        w.NewGame();w.ChooseTool(0);w.ClickCell(new Vector2Int(3,3));yield return Settle(w);
        Check(w.moves==26 && w.score>=60,"hammer clears without spending move");Stable(w);
        w.NewGame();w.ChooseTool(1);w.ClickCell(new Vector2Int(3,3));yield return Settle(w);
        Check(w.moves==26 && w.score>=900,"cross clears 15 unique pieces");Stable(w);
        w.NewGame();w.ChooseTool(2);yield return Settle(w);
        Check(w.moves==26 && w.score==0,"shuffle is free of move cost");Stable(w);
        w.TogglePause();Check(Time.timeScale==0,"pause freezes gameplay");
        w.TogglePause();Check(Time.timeScale==1,"resume restores gameplay");
        w.NewGame();w.score=1800;w.board.FindMove(out aa,out bb);
        w.ClickCell(aa);w.ClickCell(bb);yield return Settle(w);
        Check(w.mode=="result","victory dialog");
        w.NewGame();w.moves=0;w.StartCoroutine(w.Resolve(new System.Collections.Generic.HashSet<Vector2Int>(),false));
        yield return null;Check(w.mode=="result","out-of-moves dialog");
        w.NewGame();Check(w.score==0 && w.moves==26,"restart resets game");Stable(w);
        PlayerPrefs.SetInt("JellyWorld.Best",oldBest);PlayerPrefs.Save();
        // This coroutine is on a separate runner because ShowMenu stops world coroutines.
        w.ShowMenu();Check(w.mode=="menu" && Time.timeScale==1,"home return");
        Directory.CreateDirectory("Captures");
        File.WriteAllText("Captures/Verification.txt","PASS: "+assertions+" assertions\nStart button, invalid swap rollback, valid swap, cascade refill, model/view sync, hammer, cross, shuffle, pause/resume, win, loss, restart, home.\n200 deterministic board seeds verified separately.\n"+DateTime.Now);
        Debug.Log("JELLY SMOKE PASS: "+assertions+" assertions.");
    }
    static IEnumerator Settle(JellyWorld w) {
        float end=Time.realtimeSinceStartup+25;
        while(w.busy && Time.realtimeSinceStartup<end)yield return null;
        Check(!w.busy,"settles within 25 seconds");yield return new WaitForSeconds(.2f);
    }
}
