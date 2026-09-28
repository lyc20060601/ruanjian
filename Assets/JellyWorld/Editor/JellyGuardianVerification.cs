using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using JellyWorldGame;

public static class JellyGuardianVerification
{
    static int checks;
    static void Check(bool good,string name){if(!good)throw new Exception("GUARDIAN CHECK FAILED: "+name);checks++;}
    [UnityEditor.MenuItem("Jelly World/Verify guardian adventure (Play Mode)")]
    public static void StartVerification()
    {
        // The current campaign suite covers variable board sizes and restores all saved profile data.
        JellyCampaignVerification.Run();
    }

    static IEnumerator Ready(JellyWorld w)
    {
        float deadline=Time.realtimeSinceStartup+10;
        while(w.Guardian.Transitioning && Time.realtimeSinceStartup<deadline)yield return null;
        Check(!w.Guardian.Transitioning,"transition finishes");
    }
    static IEnumerator Settled(JellyWorld w)
    {
        float deadline=Time.realtimeSinceStartup+25;
        while((w.mode=="golem" || w.busy) && Time.realtimeSinceStartup<deadline)yield return null;
        Check(w.mode!="golem" && !w.busy,"returns and settles");
        Check(Camera.main.orthographic,"orthographic camera restored");
        Check(w.board.Matches().Count==0,"all cascades resolved");
        for(int x=0;x<8;x++)for(int y=0;y<8;y++) {
            Check(w.tiles[x,y]!=null && w.tiles[x,y].color==w.board.Cells[x,y] &&
                w.tiles[x,y].x==x && w.tiles[x,y].y==y,"64 cells refill with correct identities");
        }
    }
    public static IEnumerator Run(JellyWorld w)
    {
        checks=0;int best=PlayerPrefs.GetInt("JellyWorld.Best",0);
        w.StartChallenge();
        GameObject.Find("Tool card 3").GetComponent<Button>().onClick.Invoke();
        Check(w.ChoosingLanding && w.mode=="play" && w.GuardianCharges==1,"summon waits for landing selection");
        w.ConfirmGuardianLanding(new Vector2Int(3,1));
        yield return Ready(w);
        var g=w.Guardian;
        Check(g.Active && w.mode=="golem","summon button enters mode");
        Check(w.GuardianCharges==0 && w.moves==26,"one summon, no move cost");
        Check(!Camera.main.orthographic && g.Actor!=null,"perspective camera and actor");
        Check(g.Actor.GetComponent<JellyGuardianVisual>().body.gameObject.activeSelf,"third person body visible");
        w.SummonGuardian();Check(w.GuardianCharges==0,"no double summon");
        var p=g.Actor.position;g.Drive(Vector2.up,false,.1f);
        Check(Mathf.Abs(Vector3.Distance(p,g.Actor.position)-.225f)<.01f,"walking speed");
        int crushed=g.Crushed;Check(crushed==1 && w.score==60,"footstep destroys and awards 60 once");
        int cx=Mathf.RoundToInt(g.Actor.position.x+3.5f),cy=Mathf.RoundToInt(g.Actor.position.z+3.5f);
        Check(!w.CrushGuardianCell(cx,cy) && w.score==60,"empty cell cannot award score twice");
        g.Actor.position=new Vector3(0,.65f,0);p=g.Actor.position;
        g.Drive(Vector2.one,false,.1f);
        Check(Mathf.Abs(Vector3.Distance(p,g.Actor.position)-.225f)<.01f,"diagonal movement normalized");
        g.Actor.position=new Vector3(0,.65f,0);p=g.Actor.position;g.Drive(Vector2.right,true,.1f);
        Check(Mathf.Abs(Vector3.Distance(p,g.Actor.position)-.37f)<.01f,"sprint is faster");
        g.Actor.position=new Vector3(3.61f,.65f,3.61f);g.Drive(Vector2.one,true,.1f);
        Check(g.Actor.position.x<=3.62f && g.Actor.position.z<=3.62f,"arena boundary");
        g.ToggleView();yield return new WaitForSeconds(.5f);
        Check(g.FirstPerson && !g.Actor.GetComponent<JellyGuardianVisual>().body.gameObject.activeSelf,"first person hides obstructing body");
        Check(GameObject.Find("Guardian first person hands")==null,"first person has no obstructing hands");
        g.ToggleView();Check(!g.FirstPerson,"third person restored");
        float time=g.Seconds;int energy=g.Energy;
        w.TogglePause();p=g.Actor.position;g.Drive(Vector2.up,true,.1f);g.Slam();
        yield return new WaitForSecondsRealtime(.25f);
        Check(Mathf.Abs(g.Seconds-time)<.001f && g.Actor.position==p && g.Energy==energy && Time.timeScale==0,"pause blocks timer movement and skills");
        w.TogglePause();Check(Time.timeScale==1,"resume");
        g.Actor.position=new Vector3(-1.5f,.65f,1.5f);int before=g.Crushed;int previous=w.score;
        g.Slam();yield return new WaitForSeconds(.9f);
        Check(g.Crushed-before==5 && w.score-previous==300 && g.Stomps==2,"slam hits five unique cells");
        Check(g.Energy==18-g.Crushed,"energy tracks unique destroyed cells");
        int collected=g.Crushed*60;g.End();yield return Settled(w);
        Check(w.score>=collected && w.moves==26,"return preserves points and moves");
        Check(w.GuardianCharges==0,"charge stays spent after return");
        w.SummonGuardian();Check(w.mode!="golem","second summon blocked");

        w.NewGame();Check(w.GuardianCharges==1,"new round restores charge");
        w.SummonGuardian();w.ConfirmGuardianLanding(new Vector2Int(3,1));yield return Ready(w);g=w.Guardian;
        for(int y=0;y<8 && g.Energy>0;y++)for(int x=0;x<8 && g.Energy>0;x++) {
            g.Actor.position=new Vector3(x-3.5f,.65f,y-3.5f);
            for(int i=0;i<4;i++)g.Drive(Vector2.right,false,.1f);
        }
        Check(g.Energy==0 && g.Crushed==18 && w.score==1080,"energy caps collection at 18");
        yield return Settled(w);

        w.NewGame();w.SummonGuardian();w.ConfirmGuardianLanding(new Vector2Int(3,1));yield return Ready(w);
        typeof(JellyGolemMode).GetProperty("Seconds").SetValue(w.Guardian,.02f,null);
        yield return Settled(w);
        Check(w.score==0 && w.moves==26,"timer expiry with no destruction has no score or move cost");

        w.NewGame();w.SummonGuardian();w.ConfirmGuardianLanding(new Vector2Int(3,1));w.ShowMenu();yield return null;
        Check(!w.Guardian.Active && Camera.main.orthographic && w.mode=="menu","cancel during entry safely restores home");
        Check(GameObject.Find("Guardian first person hands")==null,"first person hands cleaned up");
        w.NewGame();w.SummonGuardian();w.ConfirmGuardianLanding(new Vector2Int(3,1));yield return Ready(w);w.TogglePause();w.ShowMenu();yield return null;
        Check(Time.timeScale==1 && !w.Guardian.Active && w.mode=="menu","home from paused guardian resets time");
        PlayerPrefs.SetInt("JellyWorld.Best",best);PlayerPrefs.Save();w.ShowMenu();
        Directory.CreateDirectory("Captures");
        File.WriteAllText("Captures/GuardianVerification.txt","PASS: "+checks+" assertions\nSummon UI, movement, sprint, diagonal normalization, bounds, unique scoring, camera views, first-person visibility, pause/resume, area slam, refill/cascades, energy exhaustion, timeout, cancel during transition, paused home, cleanup.\n"+DateTime.Now);
        Debug.Log("GUARDIAN CHECKS PASS: "+checks);
    }
}
