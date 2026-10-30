using UnityEngine;
using UnityEngine.UI;
namespace JellyWorldGame
{
    public partial class JellyWorld
    {
        public int BoardSize {get{return board!=null?board.Width:8;}}
        public int PlayableCellCount {get{return board!=null?board.PlayableCount:BoardSize*BoardSize;}}
        public float BoardHalf {get{return (BoardSize-1)*.5f;}}
        public float BoardLimit {get{return BoardHalf+.12f;}}
        public JellyLevel LevelSpec {get{return JellyLevels.Get(level);}}
        public readonly int[] collected=new int[7];
        public int FrostRemaining {get;private set;}
        public Material iceShell;
        bool[,] frost;
        float[,] iceRehitAt;
        GameObject[,] frostVisuals;
        RectTransform levelsUI,ranksUI;
        Transform levelContent,rankContent;
        InputField playerInput;
        int rankLevel=1;
        bool Inside(Vector2Int p){return board!=null && board.Contains(p);}
        public bool IsPlayableCell(int x,int y){return board!=null && board.Contains(new Vector2Int(x,y));}
        bool IsPlayableWorldPoint(Vector3 point)
        {
            int x=Mathf.RoundToInt(point.x+BoardHalf),y=Mathf.RoundToInt(point.z+BoardHalf);
            return IsPlayableCell(x,y);
        }
        public Vector3 ConstrainGuardianMove(Vector3 before,Vector3 desired)
        {
            if(IsPlayableWorldPoint(desired))return desired;
            var xOnly=new Vector3(desired.x,before.y,before.z);if(IsPlayableWorldPoint(xOnly))return xOnly;
            var zOnly=new Vector3(before.x,before.y,desired.z);if(IsPlayableWorldPoint(zOnly))return zOnly;
            return before;
        }
        public Vector3 NearestPlayableWorldPoint(Vector3 desired)
        {
            if(IsPlayableWorldPoint(desired))return desired;
            float best=float.MaxValue;Vector3 result=desired;
            for(int x=0;x<BoardSize;x++)for(int y=0;y<BoardSize;y++)if(IsPlayableCell(x,y)) {
                var candidate=new Vector3(x-BoardHalf,desired.y,y-BoardHalf);
                float distance=(new Vector2(candidate.x-desired.x,candidate.z-desired.z)).sqrMagnitude;
                if(distance<best){best=distance;result=candidate;}
            }
            return result;
        }
        void BuildBoardStage()
        {
            if(stage!=null){stage.gameObject.SetActive(false);if(Application.isPlaying)Destroy(stage.gameObject);else DestroyImmediate(stage.gameObject);}
            stage=Group("Jelly board "+BoardSize+"x"+BoardSize,transform);stage.localRotation=Quaternion.Euler(7,-7,0);
            for(int x=0;x<BoardSize;x++)for(int y=0;y<BoardSize;y++) {
                if(board!=null && !board.Contains(new Vector2Int(x,y)))continue;
                Vector3 p=Pos(x,y);
                Shape("Map base "+x+","+y,stage,p+new Vector3(0,0,.72f),new Vector3(1.10f,1.10f,.54f),plum);
                Shape("Map rim "+x+","+y,stage,p+new Vector3(0,0,.43f),new Vector3(1.02f,1.02f,.30f),cream);
                Shape("Sugar pocket "+x+","+y,stage,p+new Vector3(0,0,.22f),new Vector3(.94f,.94f,.14f),white);
            }
            tileRoot=Group("Living jelly pieces",stage);
        }
        void ResetObjectives()
        {
            System.Array.Clear(collected,0,collected.Length);
            iceRehitAt=new float[BoardSize,BoardSize];
            frost=new bool[BoardSize,BoardSize];frostVisuals=new GameObject[BoardSize,BoardSize];FrostRemaining=UnlimitedMoves?0:LevelSpec.Frost;
            var random=new System.Random(level*241+37);int count=0;
            while(count<FrostRemaining) {
                int x=random.Next(BoardSize),y=random.Next(BoardSize);if(!IsPlayableCell(x,y) || frost[x,y])continue;
                frost[x,y]=true;count++;
                var shell=Shape("Ice shell "+x+","+y,stage,Pos(x,y),new Vector3(.98f,.98f,.98f),iceShell);
                shell.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                frostVisuals[x,y]=shell;
            }
        }
        public bool IsIced(int x,int y){return frost!=null && x>=0 && y>=0 && x<BoardSize && y<BoardSize && frost[x,y];}
        bool BreakIce(int x,int y)
        {
            if(!IsIced(x,y))return false;
            frost[x,y]=false;FrostRemaining--;
            PlaySound(JellySound.IceBreak,.85f);
            if(mode!="range")JellyVfx.IceBreak(this,effects,stage.TransformPoint(Pos(x,y)),mode=="golem"?Vector3.up:-stage.forward);
            if(frostVisuals[x,y]!=null){frostVisuals[x,y].SetActive(false);Destroy(frostVisuals[x,y]);}
            return true;
        }
        void RegisterClear(int x,int y,int color)
        {
            if(color>=0 && color<collected.Length)collected[color]++;
            BreakIce(x,y);
        }
        public bool ObjectivesMet {
            get {
                if(RaceMode)return score>=RaceTarget;
                if(InfiniteMode)return false;var c=LevelSpec;
                return score>=c.Score && FrostRemaining==0 &&
                    (c.ColorA<0 || collected[c.ColorA]>=c.AmountA) &&
                    (c.ColorB<0 || collected[c.ColorB]>=c.AmountB);
            }
        }
        public string ObjectiveSummary()
        {
            if(RaceMode)return "竞速目标 "+RaceTarget.ToString("N0")+" 分";
            if(InfiniteMode)return "选择道具";var c=LevelSpec;var parts=new System.Collections.Generic.List<string>();
            if(c.ColorA>=0)parts.Add(JellyLevels.ColorNames[c.ColorA]+" "+Mathf.Min(collected[c.ColorA],c.AmountA)+"/"+c.AmountA);
            if(c.ColorB>=0)parts.Add(JellyLevels.ColorNames[c.ColorB]+" "+Mathf.Min(collected[c.ColorB],c.AmountB)+"/"+c.AmountB);
            if(c.Frost>0)parts.Add("冰层 "+(c.Frost-FrostRemaining)+"/"+c.Frost);
            if(parts.Count==0)parts.Add("积分 "+score+"/"+c.Score);
            return string.Join(" · ",parts.ToArray());
        }
        float ObjectiveProgress()
        {
            if(RaceMode)return Mathf.Clamp01((float)score/RaceTarget);
            var c=LevelSpec;float sum=Mathf.Clamp01((float)score/c.Score);int count=1;
            if(c.ColorA>=0){sum+=Mathf.Clamp01((float)collected[c.ColorA]/c.AmountA);count++;}
            if(c.ColorB>=0){sum+=Mathf.Clamp01((float)collected[c.ColorB]/c.AmountB);count++;}
            if(c.Frost>0){sum+=1-(float)FrostRemaining/c.Frost;count++;}
            return sum/count;
        }
        public bool StartLevel(int number)
        {
            if(number<1 || number>JellyLevels.All.Length || number>JellyProgress.Unlocked)return false;
            SaveInfiniteBest();LeaveRace();InfiniteMode=false;level=number;NewGame();return true;
        }
        void FinishLevel(bool win)
        {
            int previousUnlocked=JellyProgress.Unlocked;
            mode="result";PlaySound(win?JellySound.Win:JellySound.Lose,.6f);int stars=0,bonus=0;
            if(win) {
                stars=1+(moves>=Mathf.CeilToInt(LevelSpec.Moves*.2f)?1:0)+(moves>=Mathf.CeilToInt(LevelSpec.Moves*.45f)?1:0);
                bonus=moves*300;score+=bonus;JellyProgress.Win(level,score,stars);
            }
            PlayerPrefs.SetInt("JellyWorld.Best",Mathf.Max(score,PlayerPrefs.GetInt("JellyWorld.Best",0)));PlayerPrefs.Save();UpdateHUD();
            string detail=win?new string('★',stars)+new string('☆',3-stars)+"\n得分 "+score+"（剩余步数奖励 +"+bonus+"）":ObjectiveSummary()+"\n本局得分 "+score;
            if(win) {var unlocked=JellyJourney.UnlockMessage(previousUnlocked,JellyProgress.Unlocked);detail+="\n"+(unlocked.Length>0?unlocked:JellyJourney.RankName+" · ★ "+JellyJourney.TotalStars+" / 36");}
            bool final=level==JellyLevels.All.Length;
            if(win)ShowDialog("第 "+level+" 关完成",LevelSpec.Name,detail,final?"返回关卡":"下一关",
                ()=>{if(final)ShowLevelSelect();else StartLevel(level+1);},true,()=>StartLevel(level),"重玩本关",ShowLevelSelect,"关卡列表");
            else ShowDialog("步数已用完",LevelSpec.Name,detail,"重试",NewGame,true,ShowLevelSelect,"关卡列表");
        }
        void BuildCampaignUI(Transform parent)
        {
            BuildJourneyUI(parent);
            ranksUI=Rect("Local leaderboard",parent,Vector2.zero,new Vector2(1600,900));
            Panel("Rank backdrop",ranksUI,Vector2.zero,new Vector2(2400,1500),new Color(.94f,.96f,.98f,.98f));
            Label("Rank title",ranksUI,"本地排行榜",new Vector2(-534,376),new Vector2(430,65),40,Ink);
            Label("Rank scope",ranksUI,"仅保存本机 · 每关历史前 6 次成绩",new Vector2(-487,317),new Vector2(525,37),18,Purple);
            Button("Rank back",ranksUI,"返回关卡",new Vector2(661,377),new Vector2(178,52),Color.white,Ink,ShowLevelSelect,21);
            var input=Panel("Player name",ranksUI,new Vector2(394,313),new Vector2(305,46),Color.white);
            playerInput=input.gameObject.AddComponent<InputField>();playerInput.characterLimit=12;
            var inputText=Label("Player input text",input.transform,"",Vector2.zero,new Vector2(269,40),21,Ink);
            playerInput.textComponent=inputText;playerInput.text=JellyProgress.PlayerName;
            playerInput.onEndEdit.AddListener(value=>{JellyProgress.PlayerName=value;playerInput.text=JellyProgress.PlayerName;RenderRanks();});
            Label("Name label",ranksUI,"昵称",new Vector2(189,313),new Vector2(75,42),20,Ink);
            rankContent=Rect("Rank rows",ranksUI,Vector2.zero,new Vector2(1500,620));ranksUI.gameObject.SetActive(false);
        }
        void ClearUIChildren(Transform root)
        {
            for(int i=root.childCount-1;i>=0;i--){var g=root.GetChild(i).gameObject;g.SetActive(false);Destroy(g);}
        }
        void HideCampaignPages(){if(levelsUI!=null)levelsUI.gameObject.SetActive(false);if(ranksUI!=null)ranksUI.gameObject.SetActive(false);}
        public void ShowLevelSelect()
        {
            ShowMenu();mode="levels";menuUI.gameObject.SetActive(false);hero.gameObject.SetActive(false);levelsUI.gameObject.SetActive(true);
            RenderJourneyChapter((JellyProgress.Unlocked-1)/4);
        }
        public void ShowLeaderboard()
        {
            ShowMenu();mode="ranks";menuUI.gameObject.SetActive(false);hero.gameObject.SetActive(false);ranksUI.gameObject.SetActive(true);
            rankLevel=Mathf.Clamp(level,1,JellyLevels.All.Length);playerInput.text=JellyProgress.PlayerName;RenderRanks();
        }
        void RenderRanks()
        {
            ClearUIChildren(rankContent);
            Button("Previous rank",rankContent,"‹",new Vector2(-676,238),new Vector2(70,44),Color.white,Ink,()=>{rankLevel=(rankLevel+JellyLevels.All.Length)%(JellyLevels.All.Length+1);RenderRanks();},30);
            Button("Next rank",rankContent,"›",new Vector2(676,238),new Vector2(70,44),Color.white,Ink,()=>{rankLevel=(rankLevel+1)%(JellyLevels.All.Length+1);RenderRanks();},30);
            Label("Rank category",rankContent,rankLevel==0?"无限模式":"第 "+rankLevel+" 关 · "+JellyLevels.Get(rankLevel).Name,new Vector2(0,238),new Vector2(1080,48),26,Ink,TextAnchor.MiddleCenter);
            var rows=JellyProgress.Ranking(rankLevel);
            if(rows.Count==0)Label("Rank empty",rankContent,"暂无记录",new Vector2(0,55),new Vector2(650,55),29,Purple,TextAnchor.MiddleCenter);
            for(int i=0;i<rows.Count;i++) {
                var r=rows[i];var line=Panel("Rank "+i,rankContent,new Vector2(0,168-i*52),new Vector2(1350,45),i%2==0?Color.white:new Color(.88f,.93f,.95f)).transform;
                Label("Rank number",line,(i+1).ToString("00"),new Vector2(-591,0),new Vector2(85,40),23,Purple);
                Label("Rank player",line,r.player,new Vector2(-280,0),new Vector2(470,40),21,Ink);
                Label("Rank score",line,r.score.ToString("N0"),new Vector2(140,0),new Vector2(230,40),23,Ink,TextAnchor.MiddleRight,true);
                Label("Rank stars",line,rankLevel==0?"—":new string('★',r.stars),new Vector2(350,0),new Vector2(160,40),21,Purple,TextAnchor.MiddleCenter);
                Label("Rank date",line,r.date,new Vector2(544,0),new Vector2(213,40),18,Ink,TextAnchor.MiddleRight);
            }
        }
    }
}
