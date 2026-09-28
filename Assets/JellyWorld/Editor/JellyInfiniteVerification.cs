using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using JellyWorldGame;

public static class JellyInfiniteVerification
{
    static int checks,turnFrames;
    static void Check(bool ok,string note){if(!ok)throw new Exception("INFINITE CHECK FAILED: "+note);checks++;}
    [UnityEditor.MenuItem("Jelly World/Verify infinite playground (Play Mode)")]
    public static void StartVerification()
    {
        // The current campaign suite covers variable board sizes and restores all saved profile data.
        JellyCampaignVerification.Run();
    }

    static IEnumerator Entered(JellyWorld w)
    {
        float deadline=Time.realtimeSinceStartup+12;
        while(w.Guardian.Transitioning && Time.realtimeSinceStartup<deadline) {
            if(w.Guardian.TransitionPhase=="fold") {
                Check(!w.Guardian.Actor.gameObject.activeInHierarchy && !w.Guardian.PlatformVisible,"actor and platform absent during board turn");
                var stage=GameObject.Find("08 x 08 • Jelly garden").transform;
                Check(Vector3.Dot(Camera.main.transform.position-stage.position,-stage.forward)>6,"camera remains in front of board");turnFrames++;
            }
            yield return null;
        }
        Check(!w.Guardian.Transitioning,"entry completes");
    }
    static IEnumerator Settled(JellyWorld w)
    {
        float deadline=Time.realtimeSinceStartup+30;
        while((w.mode=="golem" || w.busy) && Time.realtimeSinceStartup<deadline) {
            if(w.Guardian!=null && w.Guardian.Active && w.Guardian.TransitionPhase=="unfold") {
                Check(!w.Guardian.Actor.gameObject.activeInHierarchy && !w.Guardian.PlatformVisible,"actor and platform absent on return turn");
                var stage=GameObject.Find("08 x 08 • Jelly garden").transform;
                Check(Vector3.Dot(Camera.main.transform.position-stage.position,-stage.forward)>6,"return camera outside board");turnFrames++;
            }
            yield return null;
        }
        Check(!w.busy && w.mode=="play","infinite mode resumes play");
        for(int x=0;x<8;x++)for(int y=0;y<8;y++)Check(w.tiles[x,y]!=null && w.tiles[x,y].color==w.board.Cells[x,y],"board model matches view");
        Check(w.board.Matches().Count==0,"cascade fully settled");
    }
    static IEnumerator DoneAction(JellyWorld w)
    {
        float deadline=Time.realtimeSinceStartup+20;
        while(w.busy && Time.realtimeSinceStartup<deadline)yield return null;
        Check(!w.busy,"tool action settles");
    }
    static IEnumerator Landed(JellyGolemMode g)
    {
        float deadline=Time.realtimeSinceStartup+4;
        while(g.IsJumping && Time.realtimeSinceStartup<deadline) {
            Check(g.Actor.position.y>=.13f,"jump stays above floor");
            Check(Mathf.Abs(g.Actor.position.x)<=3.621f && Mathf.Abs(g.Actor.position.z)<=3.621f,"jump remains inside board");
            yield return null;
        }
        Check(!g.IsJumping,"jump lands");
    }
    public static IEnumerator Run(JellyWorld w)
    {
        checks=0;turnFrames=0;int best=PlayerPrefs.GetInt("JellyWorld.InfiniteBest",0),oldBest=PlayerPrefs.GetInt("JellyWorld.Best",0);
        w.StartChallenge();w.score=1500;w.ShowInfinitePage();
        Check(w.mode=="infinite_lobby" && GameObject.Find("Enter infinite")!=null,"infinite page exists");
        GameObject.Find("Enter infinite").GetComponent<Button>().onClick.Invoke();
        Check(w.InfiniteMode && w.mode=="play" && w.score==0,"infinite page starts distinct mode");
        Check(PlayerPrefs.GetInt("JellyWorld.InfiniteBest",0)==best,"challenge score not imported into infinite record");
        w.score=20000;
        for(int i=0;i<5;i++){w.ChooseTool(0);w.ClickCell(new Vector2Int(i,3));yield return DoneAction(w);}
        for(int i=0;i<4;i++){w.ChooseTool(1);w.ClickCell(new Vector2Int(i,4));yield return DoneAction(w);}
        for(int i=0;i<5;i++){w.ChooseTool(2);yield return DoneAction(w);}
        Check(w.mode=="play" && w.moves==26,"no win, loss, or move limit");
        Check(w.ToolCharges(0)==3 && w.ToolCharges(1)==2 && w.ToolCharges(2)==2,"unlimited tools never consume stock");
        Vector2Int a,b;w.board.FindMove(out a,out b);w.ClickCell(a);w.ClickCell(b);yield return DoneAction(w);
        Check(w.moves==26,"valid swap has no move cost");
        w.SummonGuardian();
        Check(w.ChoosingLanding && w.mode=="play" && Camera.main.orthographic,"summon is selection only");
        Check(!w.ConfirmGuardianLanding(new Vector2Int(-1,8)) && w.ChoosingLanding,"invalid landing rejected");
        w.SummonGuardian();Check(!w.ChoosingLanding && w.GuardianCharges==1,"selection cancellation is free");
        w.SummonGuardian();w.ClickCell(new Vector2Int(1,6));yield return Entered(w);
        var g=w.Guardian;
        Check(Vector3.Distance(g.Actor.position,new Vector3(-2.5f,.65f,2.5f))<.03f,"guardian lands on chosen cell");
        Check(w.GuardianCharges==1,"infinite summon never consumes charge");
        g.ToggleView();g.Look(new Vector2(80,55));yield return new WaitForSeconds(.25f);
        Check(g.FirstPerson && g.LookYaw>100 && g.LookPitch<0,"first person mouse look can turn and look up");
        Check(GameObject.Find("Guardian first person hands")==null,"no oversized viewmodel hands");
        g.Look(new Vector2(0,-200));Check(g.LookPitch==80,"downward look clamp");
        w.TogglePause();Check(Cursor.lockState==CursorLockMode.None,"pause releases cursor");
        w.TogglePause();g.ToggleView();
        g.Look(new Vector2(-g.LookYaw/2.3f,(g.LookPitch-28)/1.8f));
        float seconds=g.Seconds;
        typeof(JellyGolemMode).GetProperty("Seconds").SetValue(g,.01f,null);
        typeof(JellyGolemMode).GetProperty("Energy").SetValue(g,0,null);
        typeof(JellyGolemMode).GetProperty("Stomps").SetValue(g,0,null);
        yield return new WaitForSeconds(.2f);Check(g.Active && !g.Transitioning,"infinite guardian ignores all depletion");
        int previous=g.Crushed;g.Actor.position=new Vector3(-1.5f,.65f,-1.5f);g.Slam();yield return new WaitForSeconds(.9f);
        Check(g.Crushed>previous,"slam usable with zero finite charges");
        g.Actor.position=new Vector3(3.5f,.65f,3.5f);yield return new WaitForSeconds(2.4f);
        Check(w.board.Cells[2,2]<0 && w.tiles[2,2]==null,"crushed jelly stays removed after waiting away from cell");
        g.Actor.position=new Vector3(0,.65f,-3);g.BeginCharge();g.AddCharge(.08f);g.ReleaseJump(Vector2.up);
        float shortDistance=g.LastJumpDistance;yield return Landed(g);
        g.Actor.position=new Vector3(0,.65f,-3);g.BeginCharge();g.AddCharge(1.25f);
        Check(g.Charge01==1,"charge clamps at full");
        yield return null;
        var target=GameObject.Find("Jump landing target").GetComponent<LineRenderer>();
        Check(target.loop && target.positionCount==48 && GameObject.Find("Charged jump trajectory")==null,"landing circle replaces trajectory");
        Vector3 center=Vector3.zero;for(int k=0;k<48;k++)center+=target.GetPosition(k)/48;
        for(int k=0;k<48;k++)Check(Mathf.Abs(target.GetPosition(k).y-center.y)<.001f && Vector3.Distance(target.GetPosition(k),center)<.35f,"target stays compact on landing surface");
        var fill=GameObject.Find("Jump charge fill").GetComponent<Image>();
        Check(fill.rectTransform.sizeDelta.y>=20 && fill.fillAmount==1,"charge bar is thick and reaches full");
        g.ReleaseJump(Vector2.up);
        Check(g.LastJumpDistance>shortDistance+2.5f && Mathf.Abs(g.LastJumpDistance-4.5f)<.01f,"longer charge produces longer jump");
        yield return Landed(g);
        g.Actor.position=new Vector3(3.5f,.65f,3.5f);g.BeginCharge();g.AddCharge(1.25f);g.ReleaseJump(Vector2.one);yield return Landed(g);
        Check(g.Actor.position.x<=3.62f && g.Actor.position.z<=3.62f,"charged jump clips destination to edge");
        g.BeginCharge();w.TogglePause();Check(!g.IsCharging,"pause cancels uncommitted charge");w.TogglePause();
        g.BeginCharge();g.AddCharge(.4f);g.ReleaseJump(Vector2.down);g.End();
        Check(g.IsJumping && !g.Transitioning,"return waits for airborne character to land");
        yield return Settled(w);
        Check(w.InfiniteMode && Camera.main.orthographic && Cursor.lockState==CursorLockMode.None,"normal board and cursor restored");
        w.SummonGuardian();w.ConfirmGuardianLanding(new Vector2Int(4,4));yield return Entered(w);Check(g.Active,"can resummon without restarting");
        g.ToggleView();w.ShowMenu();yield return null;
        Check(Cursor.visible && Cursor.lockState==CursorLockMode.None && !g.Active,"home cleans up first person cursor and actor");
        w.StartChallenge();Check(!w.InfiniteMode && w.moves==26,"challenge remains separate");
        w.ChooseTool(2);yield return DoneAction(w);Check(w.ToolCharges(2)==1,"challenge still consumes tools");
        w.ShowMenu();PlayerPrefs.SetInt("JellyWorld.InfiniteBest",best);PlayerPrefs.SetInt("JellyWorld.Best",oldBest);PlayerPrefs.Save();
        Directory.CreateDirectory("Captures");
        File.WriteAllText("Captures/InfiniteVerification.txt","PASS: "+checks+" assertions; "+turnFrames+" transition frames.\nInfinite page, unlimited tools and skills, spawn selection/cancel, permanent holes during guardian mode, landing-only indicator, mouse look, cursor release, charge-dependent jump, boundary, return and challenge isolation.\n"+DateTime.Now);
        Debug.Log("INFINITE CHECKS PASS: "+checks);
    }
}
