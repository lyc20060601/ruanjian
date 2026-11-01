using UnityEngine;
using UnityEngine.UI;

namespace JellyWorldGame
{
    public partial class JellyWorld
    {
        public bool RaceMode { get; private set; }
        public int RaceTarget { get; private set; }=6000;
        public double RaceSeconds { get { return raceClock.Seconds; } }
        public bool RaceCompleted { get { return raceClock.Completed; } }
        bool UnlimitedMoves { get { return InfiniteMode || RaceMode; } }
        readonly JellyRaceClock raceClock=new JellyRaceClock();
        RectTransform raceUI,raceHUD;
        Text raceTimer,raceBest,raceRemaining,raceGoal,raceRecordNote;
        Image raceProgress;
        Text moveCaption;
        bool raceResultShown;
        Color RaceAccent { get { return new Color(.68f,.16f,.32f); } }

        public void StartRace(int target)
        {
            if(!JellyRace.ValidTarget(target))return;
            SaveInfiniteBest();InfiniteMode=false;RaceMode=true;RaceTarget=target;level=1;
            NewGame();
        }
        void BeginRaceRun()
        {
            raceResultShown=false;
            if(RaceMode)raceClock.Start(Time.realtimeSinceStartupAsDouble);
            else raceClock.Cancel();
        }
        void LeaveRace()
        {
            raceClock.Cancel();RaceMode=false;
            if(raceUI!=null)raceUI.gameObject.SetActive(false);
        }
        void SyncRacePause() { if(RaceMode)raceClock.SetPaused(paused,Time.realtimeSinceStartupAsDouble); }
        void TickRace()
        {
            if(!RaceMode || mode!="play")return;
            raceClock.SetPaused(paused,Time.realtimeSinceStartupAsDouble);
            CheckRaceScore();
            RefreshRaceHUD();
            if(raceClock.Completed && !busy && !raceResultShown)FinishRace();
        }
        void CheckRaceScore()
        {
            if(RaceMode && score>=RaceTarget)raceClock.Complete(Time.realtimeSinceStartupAsDouble);
        }
        void FinishRace()
        {
            if(!RaceMode || !raceClock.Completed || raceResultShown)return;
            raceResultShown=true;mode="result";busy=false;ResetBoardGesture();Deselect();ClearHints();
            int previousBest=JellyRace.BestMilliseconds(RaceTarget);
            bool record=JellyRace.Record(RaceTarget,RaceSeconds);
            double difference=(System.Math.Ceiling(RaceSeconds*1000)-previousBest)/1000d;
            string comparison=previousBest==0?"首次完成，已保存本机纪录。":difference==0?"与本机最快用时持平":
                record?"比上次纪录快 "+(-difference).ToString("0.000")+" 秒":"比本机最快慢 "+difference.ToString("0.000")+" 秒";
            PlaySound(JellySound.Win,.6f);UpdateHUD();
            ShowDialog(record?"新的竞速纪录！":"竞速完成",JellyRace.FormatTime(RaceSeconds,true),
                "目标 "+RaceTarget.ToString("N0")+" 分 · 本次 "+score.ToString("N0")+" 分\n"+
                "本机最快  "+JellyRace.BestLabel(RaceTarget)+"\n"+comparison,
                "再跑一次  →",()=>StartRace(RaceTarget),true,ShowRaceLobby,"切换目标",ShowMenu,"返回主页");
            var timeLabel=modal.Find("Dialog card/Dialog subtitle").GetComponent<Text>();timeLabel.fontSize=48;timeLabel.fontStyle=FontStyle.Bold;
        }
        public void ShowRaceLobby()
        {
            ShowMenu();menuUI.gameObject.SetActive(false);hero.gameObject.SetActive(false);
            mode="race_lobby";raceUI.gameObject.SetActive(true);
            foreach(int target in JellyRace.Targets)
                raceUI.Find("Race choice "+target+"/Race choice best").GetComponent<Text>().text="本机最快\n"+(JellyRace.BestMilliseconds(target)>0?JellyRace.BestLabel(target):"暂无纪录");
        }
        void BuildRaceUI(Transform root)
        {
            raceUI=Rect("Race lobby",root,Vector2.zero,new Vector2(1600,900));
            raceUI.gameObject.AddComponent<JellyUIReveal>();
            var backdrop=Panel("Race backdrop",raceUI,Vector2.zero,new Vector2(6000,4000),UiBackdrop);backdrop.raycastTarget=false;
            Label("Race title",raceUI,"计时竞速",new Vector2(-160,320),new Vector2(1180,85),48,Ink,TextAnchor.MiddleLeft,true);
            Label("Race intro",raceUI,"选择目标分数，达标时停止计时，用时越短成绩越好。",new Vector2(-160,245),new Vector2(1180,42),23,MutedText);
            Button("Race back",raceUI,"返回主页",new Vector2(658,370),new Vector2(180,48),Color.white,Ink,ShowMenu,20);
            string[] names={"短程竞速","标准竞速","长程竞速"};
            for(int i=0;i<JellyRace.Targets.Length;i++) {
                int target=JellyRace.Targets[i];
                var card=ReadingPanel("Race choice "+target,raceUI,new Vector2(-490+i*490,-25),new Vector2(450,360));
                Label("Race choice index",card.transform,names[i],new Vector2(0,139),new Vector2(374,37),24,Ink,TextAnchor.MiddleLeft,true);
                Label("Race choice target",card.transform,target.ToString("N0"),new Vector2(0,66),new Vector2(374,84),60,Ink,TextAnchor.MiddleLeft,true);
                Label("Race choice unit",card.transform,"目标分数",new Vector2(0,8),new Vector2(374,32),17,new Color(.44f,.44f,.55f));
                Label("Race choice best",card.transform,"",new Vector2(0,-52),new Vector2(374,63),19,MutedText);
                Button("Race start "+target,card.transform,"开始竞速",new Vector2(0,-130),new Vector2(374,57),Purple,Color.white,()=>StartRace(target),23,emphasized:true);
            }
            var rules=ReadingPanel("Race rules",raceUI,new Vector2(0,-296),new Vector2(1430,64));
            Label("Race fairness",rules.transform,"8 × 8 棋盘  ·  5 种果冻  ·  不限步数  ·  不使用道具",Vector2.zero,new Vector2(1370,44),21,Ink,TextAnchor.MiddleCenter);
            Label("Race pause rule",raceUI,"暂停或切出窗口时停表；仅完成的比赛保存纪录。",new Vector2(0,-351),new Vector2(1440,32),18,MutedText,TextAnchor.MiddleCenter);
            raceUI.gameObject.SetActive(false);

            raceHUD=Rect("Race dashboard",gameUI,new Vector2(603,0),new Vector2(302,700));
            ReadingPanel("Race dashboard backing",raceHUD,Vector2.zero,new Vector2(302,700));
            Label("Race HUD tag",raceHUD,"计时竞速",new Vector2(0,308),new Vector2(254,36),22,Ink,TextAnchor.MiddleLeft,true);
            var clockCard=Card("Race clock card",raceHUD,new Vector2(0,208),new Vector2(286,143),Purple).transform;
            Label("Race clock caption",clockCard,"当前用时",new Vector2(0,39),new Vector2(244,31),17,new Color(.91f,.87f,1));
            raceTimer=Label("Race timer",clockCard,"00:00.0",new Vector2(0,-8),new Vector2(250,65),42,Color.white,TextAnchor.MiddleLeft,true);
            raceGoal=Label("Race target",raceHUD,"",new Vector2(0,91),new Vector2(270,47),26,Ink,TextAnchor.MiddleLeft,true);
            var track=Panel("Race progress track",raceHUD,new Vector2(0,40),new Vector2(270,12),new Color(.89f,.86f,.94f));track.raycastTarget=false;
            raceProgress=Panel("Race progress fill",track.transform,Vector2.zero,new Vector2(270,12),RaceAccent);raceProgress.type=Image.Type.Filled;raceProgress.fillMethod=Image.FillMethod.Horizontal;raceProgress.raycastTarget=false;
            raceRemaining=Label("Race remaining",raceHUD,"",new Vector2(0,-3),new Vector2(270,40),19,Ink);
            raceBest=Label("Race PB",raceHUD,"",new Vector2(0,-78),new Vector2(270,72),21,Purple);
            raceRecordNote=Label("Race record note",raceHUD,"",new Vector2(0,-145),new Vector2(270,55),17,Ink);
            Label("Race rhythm note",raceHUD,"不限步数 · 不使用道具\n首次达标即停止计时",new Vector2(0,-230),new Vector2(254,75),18,MutedText);
            Button("Race restart",raceHUD,"重新挑战",new Vector2(0,-313),new Vector2(270,49),Color.white,Ink,()=>{
                paused=true;SyncRacePause();Time.timeScale=0;
                ShowDialog("重新开始？","计时会从零开始","本局尚未完成的成绩不会保存。","重新挑战",()=>StartRace(RaceTarget),true,
                    ()=>{paused=false;SyncRacePause();Time.timeScale=1;modal.gameObject.SetActive(false);},"继续这局");
            },20);
            raceHUD.gameObject.SetActive(false);
        }
        void RefreshRaceHUD()
        {
            if(raceHUD==null)return;
            raceHUD.gameObject.SetActive(RaceMode);
            var objectives=gameUI.Find("Objectives card");if(objectives!=null)objectives.gameObject.SetActive(!RaceMode);
            if(toolPoolLayout!=null)toolPoolLayout.gameObject.SetActive(!RaceMode);
            if(moveCaption!=null)moveCaption.text=RaceMode?"用时":"步数";
            if(!RaceMode)return;
            raceTimer.text=JellyRace.FormatTime(RaceSeconds);
            moveText.text=JellyRace.FormatTime(RaceSeconds);moveText.fontSize=26;
            raceGoal.text=RaceTarget.ToString("N0")+" 分的终点";
            raceProgress.fillAmount=Mathf.Clamp01((float)score/RaceTarget);
            raceRemaining.text=score>=RaceTarget?"目标达成！":"距离终点还差 "+Mathf.Max(0,RaceTarget-score).ToString("N0")+" 分";
            raceBest.text="本机最快\n"+(JellyRace.BestMilliseconds(RaceTarget)>0?JellyRace.BestLabel(RaceTarget):"暂无纪录");
            int best=JellyRace.BestMilliseconds(RaceTarget);
            raceRecordNote.text=RaceCompleted?"计时已停止":best>0 && RaceSeconds*1000>=best?"已超过本机最快用时":"未完成的比赛不保存成绩";
        }
    }
}
