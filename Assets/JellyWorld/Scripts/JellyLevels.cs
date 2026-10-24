using System;
using System.Collections.Generic;
using UnityEngine;
namespace JellyWorldGame
{
    [Serializable]
    public sealed class JellyLevel
    {
        public int Id,Size,Colors,Moves,Score,Frost,ColorA=-1,AmountA,ColorB=-1,AmountB;
        public string Name,MapName;
        public string[] MapRows;
        public JellyLevel(int id,string name,int size,int colors,int moves,int score,string mapName,string[] mapRows,int frost=0,int colorA=-1,int amountA=0,int colorB=-1,int amountB=0)
        {
            Id=id;Name=name;Size=size;Colors=colors;Moves=moves;Score=score;MapName=mapName;MapRows=mapRows;
            Frost=frost;ColorA=colorA;AmountA=amountA;ColorB=colorB;AmountB=amountB;
            if(MapRows==null || MapRows.Length!=Size)throw new ArgumentException("Map height must equal level size.");
            for(int y=0;y<Size;y++)if(MapRows[y]==null || MapRows[y].Length!=Size)throw new ArgumentException("Map width must equal level size.");
        }
        public bool[,] CreateMask()
        {
            var mask=new bool[Size,Size];
            // Text rows are authored top-to-bottom; board coordinates grow bottom-to-top.
            for(int row=0;row<Size;row++)for(int x=0;x<Size;x++)mask[x,Size-1-row]=MapRows[row][x]=='#';
            return mask;
        }
        public int CellCount {
            get {int count=0;foreach(var row in MapRows)foreach(char c in row)if(c=='#')count++;return count;}
        }
    }
    public static class JellyLevels
    {
        public static readonly string[] ColorNames={"粉色","蓝色","薄荷","黄色","紫色","橙色","靛蓝"};
        public static readonly JellyLevel[] All={
            new JellyLevel(1,"三连入门",8,4,26,1800,"软糖方舟",new[]{
                ".######.","########","########","########","########","########","########",".######."}),
            new JellyLevel(2,"粉色收集",8,4,28,2400,"爱心果园",new[]{
                ".##..##.","########","########","########",".######.","..####..","...##...","...##..."},0,0,24),
            new JellyLevel(3,"初次破冰",9,4,30,2800,"冰晶钻石",new[]{
                "....#....","...###...","..#####..",".#######.","#########",".#######.","..#####..","...###...","....#...."},16),
            new JellyLevel(4,"薄荷清单",9,5,31,3200,"糖沙漏",new[]{
                "#########",".#######.","..#####..","...###...","....#....","...###...","..#####..",".#######.","#########"},0,2,30),
            new JellyLevel(5,"冰层路线",10,5,32,3600,"蝴蝶花园",new[]{
                "###....###","####..####","##########","##########",".########.","..######..",".########.","##########","####..####","###....###"},26),
            new JellyLevel(6,"双色收集",10,5,34,4200,"甜甜圈岛",new[]{
                ".########.","##########","###....###","##......##","##......##","##......##","##......##","###....###","##########",".########."},0,0,28,3,28),
            new JellyLevel(7,"六色棋盘",11,6,34,4800,"四叶糖花",new[]{
                "....###....","...#####...","..#######..",".####.####.","#####.#####","###.....###","#####.#####",".####.####.","..#######..","...#####...","....###...."}),
            new JellyLevel(8,"冰封边缘",11,6,36,5200,"薄荷王冠",new[]{
                "##..###..##","###.###.###","###########","###########",".#########.",".#########.","..#######..","..#######..","...#####...","...#####...","...#####..."},38),
            new JellyLevel(9,"橙色任务",11,6,36,5600,"双塔城堡",new[]{
                "###.....###","###.....###","#####.#####","###########","###########",".#########.",".#########.","###########","###########","###.....###","###.....###"},0,5,42),
            new JellyLevel(10,"七色破冰",12,7,38,6200,"糖果星芒",new[]{
                "....####....","....####....","..########..",".##########.","############","..########..","..########..","############",".##########.","..########..","....####....","....####...."},44),
            new JellyLevel(11,"双重目标",12,7,40,7000,"双翼峡谷",new[]{
                "#####..#####","#####..#####","############",".##########.","..########..","...######...","...######...","..########..",".##########.","############","#####..#####","#####..#####"},0,4,42,6,42),
            new JellyLevel(12,"冰晶终点",12,7,42,8000,"终焉水晶",new[]{
                "....####....","...######...","..########..",".##########.","############","####....####","####....####","############",".##########.","..########..","...######...","....####...."},48,1,50)
        };
        public static JellyLevel Get(int number){return All[Mathf.Clamp(number-1,0,All.Length-1)];}
    }
    [Serializable] public class JellyRun
    {
        public string player,date;
        public int level,score,stars;
    }
    [Serializable] class JellyRunStore {public List<JellyRun> rows=new List<JellyRun>();}
    public static class JellyProgress
    {
        const string Prefix="JellyWorld.Campaign.";
        static JellyRunStore cache;
        static JellyRunStore Store {
            get {
                if(cache!=null)return cache;
                try {cache=JsonUtility.FromJson<JellyRunStore>(PlayerPrefs.GetString(Prefix+"Ranks",""));}
                catch {cache=null;}
                if(cache==null || cache.rows==null)cache=new JellyRunStore();
                return cache;
            }
        }
        public static string PlayerName {
            get {return PlayerPrefs.GetString(Prefix+"Name","玩家");}
            set {
                string clean=(value??"").Trim().Replace("\n","").Replace("\r","");
                if(clean.Length==0)clean="玩家";if(clean.Length>12)clean=clean.Substring(0,12);
                PlayerPrefs.SetString(Prefix+"Name",clean);PlayerPrefs.Save();
            }
        }
        public static int Unlocked {get{return Mathf.Clamp(PlayerPrefs.GetInt(Prefix+"Unlocked",1),1,JellyLevels.All.Length);}}
        public static int Stars(int level){return Mathf.Clamp(PlayerPrefs.GetInt(Prefix+"Stars."+level,0),0,3);}
        public static int Best(int level){return PlayerPrefs.GetInt(Prefix+"Best."+level,0);}
        public static int Completed {get{int n=0;for(int i=1;i<=JellyLevels.All.Length;i++)if(Stars(i)>0)n++;return n;}}
        public static void Win(int level,int score,int stars)
        {
            PlayerPrefs.SetInt(Prefix+"Unlocked",Mathf.Max(Unlocked,Mathf.Min(level+1,JellyLevels.All.Length)));
            PlayerPrefs.SetInt(Prefix+"Stars."+level,Mathf.Max(Stars(level),stars));
            PlayerPrefs.SetInt(Prefix+"Best."+level,Mathf.Max(Best(level),score));
            Record(level,score,stars);PlayerPrefs.Save();
        }
        public static void RecordInfinite(int score){if(score>0)Record(0,score,0);}
        static void Record(int level,int score,int stars)
        {
            var row=Store.rows.Find(r=>r.level==level && r.player==PlayerName);
            if(row!=null && row.score>=score)return;
            if(row==null){row=new JellyRun{level=level,player=PlayerName};Store.rows.Add(row);}
            row.score=score;row.stars=stars;row.date=DateTime.Now.ToString("yyyy-MM-dd");
            Store.rows.Sort((a,b)=>b.score.CompareTo(a.score));
            if(Store.rows.Count>250)Store.rows.RemoveRange(250,Store.rows.Count-250);
            PlayerPrefs.SetString(Prefix+"Ranks",JsonUtility.ToJson(Store));PlayerPrefs.Save();
        }
        public static List<JellyRun> Ranking(int level)
        {
            var result=Store.rows.FindAll(r=>r.level==level);result.Sort((a,b)=>b.score.CompareTo(a.score));
            if(result.Count>10)result.RemoveRange(10,result.Count-10);return result;
        }
    }
}
