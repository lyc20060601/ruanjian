using System;
using System.Collections;
using System.IO;
using UnityEngine;
using JellyWorldGame;
public static class JellyFeelVerification
{
    static void Check(bool ok,string name){if(!ok)throw new Exception("FEEL CHECK FAILED: "+name);}
    [UnityEditor.MenuItem("Jelly World/Verify ice hits and audio (Play Mode)")]
    public static void Run()
    {
        if(!Application.isPlaying)return;
        new GameObject("Ice and audio verification").AddComponent<JellyTile>().StartCoroutine(Verify(UnityEngine.Object.FindObjectOfType<JellyWorld>()));
    }
    static IEnumerator Verify(JellyWorld w)
    {
        w.StartChallenge();w.level=3;w.NewGame();yield return null;
        Check(w.soundClips.Length==14,"audio event map");
        foreach(var clip in w.soundClips)Check(clip!=null && clip.length>0,"downloaded clip assigned");
        Vector2Int iceCell=new Vector2Int(-1,-1);Vector2 step=Vector2.zero;
        Vector2Int[] neighbors={Vector2Int.right,Vector2Int.left,Vector2Int.up,Vector2Int.down};
        for(int x=0;x<w.BoardSize && iceCell.x<0;x++)for(int y=0;y<w.BoardSize;y++)if(w.IsIced(x,y)) {
            foreach(var direction in neighbors)if(w.IsPlayableCell(x+direction.x,y+direction.y)) {
                iceCell=new Vector2Int(x,y);step=new Vector2(direction.x,direction.y);break;
            }
            if(iceCell.x>=0)break;
        }
        Check(iceCell.x>=0,"frozen-cell fixture");
        w.SummonGuardian();w.ConfirmGuardianLanding(iceCell);
        while(w.Guardian.Transitioning)yield return null;
        var g=w.Guardian;int ice=w.FrostRemaining,score=w.score;var tile=w.tiles[iceCell.x,iceCell.y];int color=tile.color;
        g.Drive(step,false,.01f);
        Check(!w.IsIced(iceCell.x,iceCell.y) && w.FrostRemaining==ice-1,"first footfall removes ice");
        Check(w.tiles[iceCell.x,iceCell.y]==tile && w.board.Cells[iceCell.x,iceCell.y]==color && w.score==score && g.Crushed==0,"first footfall preserves fruit and score");
        Vector3 stopped=g.Actor.position;g.Drive(step,true,.1f);
        Check(g.Actor.position==stopped,"brief icy impact pause");
        Check(!w.CrushGuardianCell(iceCell.x,iceCell.y),"same-frame repeat cannot delete fruit");
        yield return new WaitForSeconds(.65f);
        Check(w.tiles[iceCell.x,iceCell.y]==null && w.board.Cells[iceCell.x,iceCell.y]==JellyBoard.Empty && w.score==score+60 && g.Crushed==1,"second footfall removes fruit once");
        Check(g.Energy==17,"only fruit consumes collection energy");
        Check(Mathf.Abs(g.Actor.localScale.x-.56f)<.001f,"smaller guardian");
        Check(Mathf.Abs(JellyTile.FallDuration(4)-.5f)<.001f,"balanced falling speed");
        w.score=0;w.ShowLevelSelect();
        Directory.CreateDirectory("Captures");
        File.WriteAllText("Captures/FeelVerification.txt","PASS: 14 downloaded audio clips, two-stage frozen stomp, ice delay, no duplicate score, correct energy, smaller guardian, balanced falling speed.\n"+DateTime.Now);
        Debug.Log("FEEL CHECKS PASS");
    }
}
