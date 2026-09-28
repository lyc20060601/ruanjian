using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using JellyWorldGame;
public static class JellyInputVerification
{
    [UnityEditor.MenuItem("Jelly World/Verify falling and buffered drag (Play Mode)")]
    public static void Run()
    {
        if(!Application.isPlaying)return;
        new GameObject("Falling input verification").AddComponent<JellyTile>().StartCoroutine(Verify(UnityEngine.Object.FindObjectOfType<JellyWorld>()));
    }
    static void Check(bool ok,string message){if(!ok)throw new Exception("INPUT CHECK FAILED: "+message);}
    static IEnumerator Settle(JellyWorld w)
    {
        float end=Time.realtimeSinceStartup+15;
        while((w.busy || w.HasBufferedSwap) && Time.realtimeSinceStartup<end)yield return null;
        Check(!w.busy && !w.HasBufferedSwap,"settles and consumes queue");
    }
    static IEnumerator Verify(JellyWorld w)
    {
        w.StartChallenge();w.level=12;w.NewGame();yield return null;
        Vector2Int a,b;w.board.FindMove(out a,out b);
        var source=w.tiles[a.x,a.y];var destination=w.tiles[b.x,b.y];
        Vector3 target=source.target;
        source.transform.localPosition=target+Vector3.up*4;source.FallTo(target);
        destination.transform.localPosition=destination.target+Vector3.up*4;destination.FallTo(destination.target);
        w.busy=true;
        Check(w.RequestDragSwap(source,destination) && w.HasBufferedSwap,"accepts drag while falling");
        Check(w.moves==35,"buffering does not spend a move");
        Check(Mathf.Abs(JellyTile.FallDuration(4)-.5f)<.001f,"balanced acceleration timing");
        yield return new WaitForSeconds(.09f);
        float travelled=4-(source.transform.localPosition.y-target.y);
        Check(travelled>0 && travelled<3.9f,"fall has started without teleporting");
        w.TogglePause();Vector3 paused=source.transform.localPosition;
        yield return new WaitForSecondsRealtime(.12f);
        Check(source.transform.localPosition==paused && !w.HasBufferedSwap,"pause freezes fall and cancels buffered input");
        w.TogglePause();
        Check(w.RequestDragSwap(source,destination),"can submit after resume");
        w.busy=false;yield return null;
        if(source!=null && source.IsFalling)Check(w.HasBufferedSwap,"waits for actual landing");
        yield return Settle(w);
        Check(w.moves==34 && w.score>=180,"buffered legal swap executes once after landing");
        for(int x=0;x<w.BoardSize;x++)for(int y=0;y<w.BoardSize;y++)
            Check(w.tiles[x,y]!=null && w.tiles[x,y].color==w.board.Cells[x,y],"model and view stay synchronized");
        w.NewGame();w.board.FindMove(out a,out b);source=w.tiles[a.x,a.y];destination=w.tiles[b.x,b.y];
        w.busy=true;Check(w.RequestDragSwap(source,destination),"buffers before elimination");
        w.StartCoroutine(w.Resolve(new HashSet<Vector2Int>{a},false));
        yield return Settle(w);
        Check(w.moves==35 && !w.HasBufferedSwap,"deleted source cancels instead of swapping its replacement");
        w.NewGame();w.board.FindMove(out a,out b);w.busy=true;w.RequestDragSwap(w.tiles[a.x,a.y],w.tiles[b.x,b.y]);
        w.NewGame();Check(!w.HasBufferedSwap && !w.busy,"new round clears gestures");
        w.score=0;w.ShowLevelSelect();
        Directory.CreateDirectory("Captures");File.WriteAllText("Captures/FallingInputVerification.txt","PASS: acceleration, mid-fall buffering, pause cancellation, exactly-once swap, deleted source cancellation, board synchronization, round reset.\n"+DateTime.Now);
        Debug.Log("FALLING INPUT PASS");
    }
}
