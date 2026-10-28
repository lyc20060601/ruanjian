using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using JellyWorldGame;

public static class JellyExperienceVerification
{
    public static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("EXPERIENCE CHECK FAILED: " + message);
    }

    [UnityEditor.MenuItem("Jelly World/Verify journey rules")]
    public static void VerifyRules()
    {
        JellyWorldBuilder.Verify();
        var type = typeof(JellyWorld).Assembly.GetType("JellyWorldGame.JellyJourney");
        Check(type != null, "journey progression rules exist");
        var canUse = type.GetMethod("CanUseTool");
        var unlock = type.GetMethod("ToolUnlockLevel");
        int[] levels = { 2, 4, 3, 6, 8 };
        for (int i = 0; i < levels.Length; i++) {
            Check((int)unlock.Invoke(null, new object[]{ i }) == levels[i], "documented unlock stage");
            Check(!(bool)canUse.Invoke(null, new object[]{ i, levels[i]-1, false }), "locked before milestone");
            Check((bool)canUse.Invoke(null, new object[]{ i, levels[i], false }), "available at milestone");
            Check((bool)canUse.Invoke(null, new object[]{ i, 12, false }), "returning player keeps tools");
            Check((bool)canUse.Invoke(null, new object[]{ i, 1, true }), "sandbox has all tools");
        }
        Check(!(bool)canUse.Invoke(null, new object[]{ -1, 12, true }), "invalid tool rejected");
        Check(!(bool)canUse.Invoke(null, new object[]{ 5, 12, true }), "out of range tool rejected");
        var rank = type.GetMethod("RankForStars");
        Check((int)rank.Invoke(null,new object[]{0}) == 0, "fresh player rank");
        Check((int)rank.Invoke(null,new object[]{8}) == 1, "rank below next threshold");
        Check((int)rank.Invoke(null,new object[]{9}) == 2, "rank reaches threshold");
        Check((int)rank.Invoke(null,new object[]{36}) == 5, "all stars rank");
        Debug.Log("JELLY JOURNEY RULES PASS: unlock boundaries, sandbox, existing players and growth thresholds");
        VerifyLeaderboard();
        VerifySpecialRules();
    }

    static void VerifySpecialRules()
    {
        var board=new JellyBoard(42,6,4);
        Action reset=()=>{for(int x=0;x<6;x++)for(int y=0;y<6;y++)board.Cells[x,y]=(x+y)%4;};
        reset();
        var four=new HashSet<Vector2Int>();
        for(int x=1;x<=4;x++){board.Cells[x,2]=0;four.Add(new Vector2Int(x,2));}
        var created=JellySpecialRules.FindCreation(board,four,new Vector2Int(3,2));
        Check(created.kind==JellySpecial.Row && created.cell==new Vector2Int(3,2),"four horizontal pieces leave a row special at the swapped tile");
        reset();four.Clear();
        for(int y=1;y<=4;y++){board.Cells[2,y]=1;four.Add(new Vector2Int(2,y));}
        Check(JellySpecialRules.FindCreation(board,four,new Vector2Int(2,3)).kind==JellySpecial.Column,"four vertical pieces leave a column special");
        reset();var five=new HashSet<Vector2Int>();
        for(int x=0;x<5;x++){board.Cells[x,2]=0;five.Add(new Vector2Int(x,2));}
        Check(JellySpecialRules.FindCreation(board,five,new Vector2Int(2,2)).kind==JellySpecial.Rainbow,"five pieces leave a rainbow special");
        reset();var tee=new HashSet<Vector2Int>();
        for(int x=1;x<=3;x++){board.Cells[x,3]=1;tee.Add(new Vector2Int(x,3));}
        for(int y=1;y<=3;y++){board.Cells[2,y]=1;tee.Add(new Vector2Int(2,y));}
        Check(JellySpecialRules.FindCreation(board,tee,new Vector2Int(2,3)).kind==JellySpecial.Blast,"T match leaves a blast special");
        var tiles=new JellyTile[6,6];var roots=new List<GameObject>();
        try {
            for(int x=0;x<6;x++)for(int y=0;y<6;y++) {
                var go=new GameObject("Special QA tile");roots.Add(go);
                tiles[x,y]=go.AddComponent<JellyTile>();
            }
            tiles[2,3].special=JellySpecial.Blast;
            var expanded=JellySpecialRules.Expand(board,tiles,new HashSet<Vector2Int>{new Vector2Int(2,3)});
            Check(expanded.Count==9,"blast clears a 3 by 3 area once");
            tiles[3,3].special=JellySpecial.Row;
            expanded=JellySpecialRules.Expand(board,tiles,new HashSet<Vector2Int>{new Vector2Int(2,3)});
            Check(expanded.Count==12,"blast triggers another special without duplicate cells");
            tiles[2,3].special=JellySpecial.Rainbow;
            expanded=JellySpecialRules.Expand(board,tiles,new HashSet<Vector2Int>{new Vector2Int(2,3)});
            Check(expanded.Count==36,"rainbow clears every playable cell");
        } finally {foreach(var go in roots)UnityEngine.Object.DestroyImmediate(go);}
        Debug.Log("JELLY SPECIAL RULES PASS: four, T, five, chain explosions and rainbow");
    }

    static void VerifyLeaderboard()
    {
        const string prefix="JellyWorld.Campaign.";
        var ints=new Dictionary<string,int>();var strings=new Dictionary<string,string>();var present=new HashSet<string>();
        var cache=typeof(JellyProgress).GetField("cache",BindingFlags.Static|BindingFlags.NonPublic);
        var previousCache=cache.GetValue(null);
        foreach(var key in new[]{prefix+"Name",prefix+"Ranks"}) {
            if(PlayerPrefs.HasKey(key))present.Add(key);strings[key]=PlayerPrefs.GetString(key);
        }
        var keys=new List<string>{prefix+"Unlocked"};
        for(int i=1;i<=12;i++)foreach(var field in new[]{"Stars.","Best."})keys.Add(prefix+field+i);
        foreach(var key in keys){if(PlayerPrefs.HasKey(key))present.Add(key);ints[key]=PlayerPrefs.GetInt(key);}
        try {
            foreach(var key in keys)PlayerPrefs.DeleteKey(key);
            PlayerPrefs.SetString(prefix+"Ranks","{\"rows\":[{\"player\":\"Old player\",\"date\":\"2026-10-01\",\"level\":1,\"score\":2000,\"stars\":3}]}");
            cache.SetValue(null,null);
            Check(JellyProgress.Ranking(1).Single().player=="Old player","old leaderboard save remains readable");
            JellyProgress.PlayerName="Same player";
            JellyProgress.Win(1,1800,2);JellyProgress.Win(1,1800,1);
            var rows=JellyProgress.Ranking(1);
            Check(rows.Count==3 && rows[1].stars==2 && rows[2].stars==1,"same player and tied runs stay separate in insertion order");
            JellyProgress.Win(1,1700,1);JellyProgress.Win(1,2200,3);JellyProgress.Win(1,1900,2);
            JellyProgress.Win(1,1600,1);JellyProgress.Win(1,2300,3);
            Check(JellyProgress.Ranking(1).Select(r=>r.score).SequenceEqual(new[]{2300,2200,2000,1900,1800,1800}),"only the six highest runs survive, sorted descending");
            Check(JellyProgress.Best(1)==2300 && JellyProgress.Stars(1)==3 && JellyProgress.Unlocked==2,"personal best, growth and unlocks remain independent");
            cache.SetValue(null,null);
            Check(JellyProgress.Ranking(1).Count==6 && JellyProgress.Ranking(1).Last().stars==1,"six runs and ties survive a save reload");
            for(int level=2;level<=12;level++) {
                JellyProgress.PlayerName="Player "+level;
                for(int run=0;run<8;run++)JellyProgress.Win(level,level*1000+run,1);
            }
            for(int run=0;run<8;run++)JellyProgress.RecordInfinite(100+run);
            cache.SetValue(null,null);
            Check(JellyProgress.Ranking(1).First().player=="Same player","other levels and nicknames do not replace earlier runs");
            for(int level=0;level<=12;level++) {
                rows=JellyProgress.Ranking(level);
                Check(rows.Count==6 && rows.All(r=>r.level==level),"each level and infinite mode keep six independent runs");
            }
            Check(JellyProgress.Ranking(12).Select(r=>r.score).SequenceEqual(new[]{12007,12006,12005,12004,12003,12002}),"low scores evicted per level");
            Check(JellyProgress.Ranking(99).Count==0,"empty leaderboard stays empty");
            Debug.Log("JELLY LEADERBOARD PASS: top six runs, repeated names, stable ties, legacy save, reload and independent levels");
        } finally {
            foreach(var pair in ints){if(present.Contains(pair.Key))PlayerPrefs.SetInt(pair.Key,pair.Value);else PlayerPrefs.DeleteKey(pair.Key);}
            foreach(var pair in strings){if(present.Contains(pair.Key))PlayerPrefs.SetString(pair.Key,pair.Value);else PlayerPrefs.DeleteKey(pair.Key);}
            PlayerPrefs.Save();cache.SetValue(null,previousCache);
        }
    }
}
