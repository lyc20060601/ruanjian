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
        Color RaceAccent { get { return new Color(.91f,.37f,.53f); } }

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
            bool record=JellyRace.Record(RaceTarget,RaceSeconds);
            PlaySound(JellySound.Win,.6f);UpdateHUD();
            ShowDialog(record?"新的竞速纪录！":"竞速完成",JellyRace.FormatTime(RaceSeconds,true),
                "目标 "+RaceTarget.ToString("N0")+" 分 · 本次 "+score.ToString("N0")+" 分\n"+
                "本机最快  "+JellyRace.BestLabel(RaceTarget)+"\n每一次连锁，都让终点更近一点。",
                "再跑一次  →",()=>StartRace(RaceTarget),true,ShowRaceLobby,"切换目标",ShowMenu,"返回主页");
            var timeLabel=modal.Find("Dialog card/Dialog subtitle").GetComponent<Text>();timeLabel.fontSize=48;timeLabel.fontStyle=FontStyle.Bold;
        }
        public void ShowRaceLobby()
        {
            ShowMenu();menuUI.gameObject.SetActive(false);hero.gameObject.SetActive(false);
            mode="race_lobby";raceUI.gameObject.SetActive(true);
            foreach(int target in JellyRace.Targets)
                raceUI.Find("Race choice "+target+"/Race choice best").GetComponent<Text>().text="本机最快  "+JellyRace.BestLabel(target);
        }
        void BuildRaceUI(Transform root)
        {
            raceUI=Rect("Race lobby",root,Vector2.zero,new Vector2(1600,900));
            raceUI.gameObject.AddComponent<JellyUIReveal>();
            var backdrop=Panel("Race backdrop",raceUI,Vector2.zero,new Vector2(6000,4000),Porcelain);backdrop.raycastTarget=false;
            Label("Race eyebrow",raceUI,"JELLY WORLD  /  SPRINT CLUB",new Vector2(-440,368),new Vector2(620,38),20,Purple,TextAnchor.MiddleLeft,true);
            Label("Race title",raceUI,"让每一次消除，都快一点。",new Vector2(-160,267),new Vector2(1180,90),49,Ink,TextAnchor.MiddleLeft,true);
            Label("Race intro",raceUI,"选一个终点。达到目标分数即停表，用更少时间刷新自己。",new Vector2(-160,190),new Vector2(1180,42),22,new Color(.39f,.42f,.55f));
            Button("Race back",raceUI,"返回主页",new Vector2(658,370),new Vector2(180,48),Color.white,Ink,ShowMenu,20);
            string[] names={"轻快热身","甜蜜冲刺","极限竞速"};
            string[] notes={"找到节奏，轻松出发","抓住连锁，向纪录进发","为热爱消除的你而设"};
            for(int i=0;i<JellyRace.Targets.Length;i++) {
                int target=JellyRace.Targets[i];
                var card=Card("Race choice "+target,raceUI,new Vector2(-490+i*490,-58),new Vector2(450,390),i==1?new Color(.96f,.92f,1):Color.white);
                Label("Race choice index",card.transform,"0"+(i+1)+"  /  "+names[i],new Vector2(0,139),new Vector2(374,37),22,Purple,TextAnchor.MiddleLeft,true);
                Label("Race choice target",card.transform,target.ToString("N0"),new Vector2(0,66),new Vector2(374,84),60,Ink,TextAnchor.MiddleLeft,true);
                Label("Race choice unit",card.transform,"目标分数",new Vector2(0,8),new Vector2(374,32),17,new Color(.44f,.44f,.55f));
                Label("Race choice note",card.transform,notes[i],new Vector2(0,-40),new Vector2(374,35),19,Ink);
                Label("Race choice best",card.transform,"",new Vector2(0,-82),new Vector2(374,32),17,Purple);
                Button("Race start "+target,card.transform,"开始竞速  →",new Vector2(0,-142),new Vector2(374,57),i==1?Purple:RaceAccent,Color.white,()=>StartRace(target),23,emphasized:true);
            }
            Label("Race fairness",raceUI,"8 × 8 标准棋盘  ·  五种果冻  ·  不限步数  ·  竞速不使用道具",new Vector2(0,-319),new Vector2(1440,40),20,Ink,TextAnchor.MiddleCenter);
            Label("Race pause rule",raceUI,"暂停或切出窗口时停表；消除动画与连锁时间计入用时。仅完成的比赛保存纪录。",new Vector2(0,-369),new Vector2(1440,35),17,new Color(.43f,.45f,.56f),TextAnchor.MiddleCenter);
            raceUI.gameObject.SetActive(false);

            raceHUD=Rect("Race dashboard",gameUI,new Vector2(603,0),new Vector2(302,700));
            Label("Race HUD tag",raceHUD,"SPRINT / 计时竞速",new Vector2(0,308),new Vector2(270,36),18,Purple,TextAnchor.MiddleLeft,true);
            var clockCard=Card("Race clock card",raceHUD,new Vector2(0,208),new Vector2(286,143),Purple).transform;
            Label("Race clock caption",clockCard,"当前用时",new Vector2(0,39),new Vector2(244,31),17,new Color(.91f,.87f,1));
            raceTimer=Label("Race timer",clockCard,"00:00.0",new Vector2(0,-8),new Vector2(250,65),42,Color.white,TextAnchor.MiddleLeft,true);
            raceGoal=Label("Race target",raceHUD,"",new Vector2(0,91),new Vector2(270,47),26,Ink,TextAnchor.MiddleLeft,true);
            var track=Panel("Race progress track",raceHUD,new Vector2(0,40),new Vector2(270,12),new Color(.89f,.86f,.94f));track.raycastTarget=false;
            raceProgress=Panel("Race progress fill",track.transform,Vector2.zero,new Vector2(270,12),RaceAccent);raceProgress.type=Image.Type.Filled;raceProgress.fillMethod=Image.FillMethod.Horizontal;raceProgress.raycastTarget=false;
            raceRemaining=Label("Race remaining",raceHUD,"",new Vector2(0,-3),new Vector2(270,40),19,Ink);
            raceBest=Label("Race PB",raceHUD,"",new Vector2(0,-78),new Vector2(270,72),21,Purple);
            raceRecordNote=Label("Race record note",raceHUD,"",new Vector2(0,-145),new Vector2(270,55),17,Ink);
            Label("Race rhythm note",raceHUD,"四连留下特效\n连续消除，冲向终点",new Vector2(0,-230),new Vector2(270,75),19,new Color(.40f,.43f,.56f));
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
            raceBest.text="个人最佳\n"+JellyRace.BestLabel(RaceTarget);
            int best=JellyRace.BestMilliseconds(RaceTarget);
            raceRecordNote.text=RaceCompleted?"本次达标，用时已锁定。":best==0?"完成首场，点亮你的纪录。":RaceSeconds*1000<best?"保持节奏，向个人最佳进发。":"超过最佳用时，完成也是进步。";
        }
    }
}
