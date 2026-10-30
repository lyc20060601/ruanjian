using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;

namespace JellyWorldGame
{
    public partial class JellyWorld
    {
        Text continueLabel,homeGrowth,homeUnlock,journeyStars,chapterTitle,chapterNote;
        Text[] objectiveValues=new Text[4];
        Image[] objectiveBars=new Image[4];
        GameObject[] objectiveRows=new GameObject[4];
        readonly List<Material> comfortMaterials=new List<Material>();
        readonly List<Image> chapterTabs=new List<Image>();
        int journeyChapter;
        Material uiButtonMaterial;
        RectTransform toolPoolLayout;
        public bool ReducedEffects {get;private set;}
        public bool IsToolUnlocked(int index){return !RaceMode && JellyJourney.CanUseTool(index,JellyProgress.Unlocked,InfiniteMode);}
        public void SetReducedEffects(bool value)
        {ReducedEffects=value;PlayerPrefs.SetInt("JellyWorld.Comfort.ReducedEffects",value?1:0);PlayerPrefs.Save();}
        Color Accent {get{return new Color(.40f,.68f,.73f);}}
        Color Porcelain {get{return new Color(1,.981f,.959f);}}
        Image Card(string name,Transform parent,Vector2 position,Vector2 size,Color color)
        {
            var image=Panel(name,parent,position,size,color);
            image.gameObject.AddComponent<JellyUISurface>().style=JellyUISurface.SurfaceStyle.Panel;
            return image;
        }
        void ApplyComfortPresentation()
        {
            ReducedEffects=PlayerPrefs.GetInt("JellyWorld.Comfort.ReducedEffects",0)==1;
            cam.backgroundColor=new Color(1,.967f,.925f);
            if(!Application.isPlaying)return;
            QualitySettings.antiAliasing=4;
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.90f,.93f,1);
            RenderSettings.ambientEquatorColor=new Color(.78f,.77f,.84f);
            RenderSettings.ambientGroundColor=new Color(.65f,.59f,.69f);
            foreach(var light in FindObjectsOfType<Light>())if(light.type==LightType.Directional) {
                light.shadowStrength=.30f;
                if(light.shadows!=LightShadows.None){light.intensity=.92f;light.color=new Color(1,.97f,.94f);}
                else {light.intensity=.43f;light.color=new Color(.88f,.94f,1);}
            }
            var reflection=Resources.Load<Cubemap>("StudioLighting/CandyStudio");
            if(reflection!=null){RenderSettings.defaultReflectionMode=DefaultReflectionMode.Custom;RenderSettings.customReflection=reflection;RenderSettings.reflectionIntensity=.55f;}
            if(comfortMaterials.Count>0)return;
            string[] colors={"F578B6","52C9F4","F67565","FFD35A","AD8BEF","FAA24D","627EE8"};
            var palette=new Material[candy.Length];
            for(int i=0;i<candy.Length;i++)palette[i]=ComfortMaterial(candy[i],colors[i%colors.Length],.72f);
            rune=ComfortMaterial(rune,"65CAFF",.82f);
            candy=palette;cream=ComfortMaterial(cream,"FFF0D7",.44f);white=ComfortMaterial(white,"FFFEFC",.5f);
            plum=ComfortMaterial(plum,"3F4979",.44f);blush=ComfortMaterial(blush,"FFC0D8",.48f);mint=ComfortMaterial(mint,"FFDAB0",.4f);
        }
        Material ComfortMaterial(Material source,string hex,float gloss)
        {
            var result=new Material(source);result.name=source.name+" (comfort)";Color color;ColorUtility.TryParseHtmlString("#"+hex,out color);
            result.color=color;result.SetFloat("_Glossiness",gloss);result.SetFloat("_Metallic",0);result.enableInstancing=true;
            if(result.HasProperty("_EmissionColor"))result.SetColor("_EmissionColor",color*.018f);
            comfortMaterials.Add(result);return result;
        }
        void OnDestroy(){ReleaseMapPreviews();if(uiButtonMaterial!=null)Destroy(uiButtonMaterial);foreach(var material in comfortMaterials)if(material!=null)Destroy(material);}
        void BuildExperienceHome()
        {
            Label("Home wordmark",menuUI,"JELLY WORLD",new Vector2(-450,367),new Vector2(600,40),24,Ink,TextAnchor.MiddleLeft,true);
            Label("Home brand note",menuUI,"果冻漫游  /  好心情制造所",new Vector2(-450,332),new Vector2(600,28),15,new Color(.49f,.45f,.59f));
            var badge=Panel("Home sweet badge",menuUI,new Vector2(-626,268),new Vector2(248,34),new Color(.91f,.86f,.98f));badge.raycastTarget=false;
            Label("Home sweet badge text",badge.transform,"一口甜 · 一场小冒险",Vector2.zero,new Vector2(228,30),15,Purple,TextAnchor.MiddleCenter);
            Label("Home title",menuUI,"把好心情，\n消除成满分。",new Vector2(-438,159),new Vector2(624,170),62,Ink,TextAnchor.MiddleLeft,true);
            Label("Home introduction",menuUI,"交换果冻，收集星星。或者，和时间赛一场。",new Vector2(-438,43),new Vector2(624,36),20,new Color(.47f,.43f,.57f));
            var card=Rect("Journey home card",menuUI,new Vector2(-438,-117),new Vector2(624,260));
            bestText=Label("Best",card,"",new Vector2(0,97),new Vector2(624,32),18,Purple);
            var play=Button("Continue journey",card,"开始旅程",new Vector2(0,34),new Vector2(624,68),Purple,Color.white,ContinueJourney,25,emphasized:true);
            continueLabel=play.GetComponentInChildren<Text>();
            Button("Start adventure",card,"关卡地图  ↗",new Vector2(-161,-49),new Vector2(302,60),new Color(1,.87f,.78f),Ink,ShowLevelSelect,21);
            Button("Infinite mode entry",card,"自由乐园  ↗",new Vector2(161,-49),new Vector2(302,60),new Color(.82f,.93f,.90f),Ink,ShowInfinitePage,21);
            homeGrowth=Label("Home growth",card,"",new Vector2(0,-108),new Vector2(624,31),17,new Color(.43f,.40f,.55f));
            homeUnlock=Label("Home next unlock",card,"",new Vector2(0,-146),new Vector2(624,28),16,new Color(.53f,.48f,.60f));
            Button("How to play",menuUI,"玩法说明",new Vector2(-649,-340),new Vector2(202,46),Color.white,Ink,ShowHelp,18);
            Button("Sound",menuUI,"舒适设置",new Vector2(-438,-340),new Vector2(202,46),Color.white,Ink,OpenAudioSettings,18);
            Button("Leaderboard",menuUI,"排行榜",new Vector2(-227,-340),new Vector2(202,46),Color.white,Ink,ShowLeaderboard,18);
            var sprint=Card("Home sprint invitation",menuUI,new Vector2(370,-316),new Vector2(616,134),new Color(.96f,.92f,.99f));sprint.raycastTarget=false;
            Label("Home sprint eyebrow",sprint.transform,"THE SPRINT CLUB",new Vector2(-131,37),new Vector2(292,22),12,Purple,TextAnchor.MiddleLeft,true);
            Label("Home sprint title",sprint.transform,"计时竞速",new Vector2(-131,2),new Vector2(292,40),27,Ink,TextAnchor.MiddleLeft,true);
            Label("Home sprint description",sprint.transform,"更少时间，抵达新的终点。",new Vector2(-131,-34),new Vector2(292,28),16,new Color(.44f,.39f,.56f));
            Button("Race mode entry",sprint.transform,"挑战自己  →",new Vector2(166,0),new Vector2(224,60),RaceAccent,Color.white,ShowRaceLobby,22,emphasized:true);
            Label("Home hero caption",menuUI,"MEET YOUR SWEET LITTLE FRIENDS",new Vector2(373,310),new Vector2(610,30),13,Purple,TextAnchor.MiddleCenter,true);
            Label("Home sandbox note",menuUI,"12 场关卡冒险  /  无尽探索  /  3 档计时挑战",new Vector2(370,-410),new Vector2(650,27),14,Ink,TextAnchor.MiddleCenter);
            Button("Quit game",menuUI,"退出",new Vector2(676,366),new Vector2(130,46),Porcelain,Ink,()=>Application.Quit(),19);
        }
        void ContinueJourney(){StartLevel(JellyProgress.Unlocked);}
        void RefreshJourneyHome()
        {
            if(continueLabel==null)return;
            continueLabel.text=JellyProgress.Completed==0?"开始旅程  →":"继续旅程 · 第 "+JellyProgress.Unlocked+" 关  →";
            if(JellyProgress.Completed==12)continueLabel.text="再游终章  →";
            bestText.text=JellyJourney.RankName+"     ★ "+JellyJourney.TotalStars+" / 36";
            homeGrowth.text="已完成 "+JellyProgress.Completed+" / 12 关   ·   "+JellyJourney.GrowthNote;
            homeUnlock.text=JellyJourney.NextUnlockNote;
        }
        void BuildJourneyUI(Transform parent)
        {
            levelsUI=Rect("Level selection",parent,Vector2.zero,new Vector2(1600,900));
            levelsUI.gameObject.AddComponent<JellyUIReveal>();
            Panel("Level backdrop",levelsUI,Vector2.zero,new Vector2(6000,4000),new Color(1,.965f,.925f));
            Label("Journey brand",levelsUI,"JELLY WORLD  /  旅程",new Vector2(-530,393),new Vector2(440,34),20,new Color(.23f,.39f,.64f),TextAnchor.MiddleLeft,true);
            chapterTitle=Label("Level title",levelsUI,"",new Vector2(-470,318),new Vector2(560,76),46,Ink,TextAnchor.MiddleLeft,true);
            chapterNote=Label("Chapter subtitle",levelsUI,"",new Vector2(122,319),new Vector2(480,48),20,new Color(.23f,.39f,.64f));
            Button("Level back",levelsUI,"主页",new Vector2(675,384),new Vector2(145,47),Porcelain,Ink,ShowMenu,20);
            Button("Level ranking",levelsUI,"排行榜",new Vector2(515,384),new Vector2(145,47),Porcelain,Ink,ShowLeaderboard,20);
            chapterTabs.Clear();
            for(int i=0;i<3;i++) {
                int chapter=i;var button=Button("Chapter tab "+i,levelsUI,(i+1).ToString("00")+"   "+JellyJourney.ChapterNames[i],new Vector2(-493+i*493,226),new Vector2(470,60),Porcelain,Ink,()=>RenderJourneyChapter(chapter),22);
                chapterTabs.Add(button.GetComponent<Image>());
            }
            levelContent=Rect("Level cards",levelsUI,new Vector2(0,-33),new Vector2(1550,425));
            journeyStars=Label("Journey stars",levelsUI,"",new Vector2(0,-302),new Vector2(1440,44),22,Ink,TextAnchor.MiddleCenter);
            Label("Journey replay note",levelsUI,"每关保留最佳星星 · 道具解锁后，每局自动补满",new Vector2(0,-358),new Vector2(1440,34),18,new Color(.23f,.39f,.64f),TextAnchor.MiddleCenter);
            levelsUI.gameObject.SetActive(false);
        }
        void RenderJourneyChapter(int chapter)
        {
            journeyChapter=Mathf.Clamp(chapter,0,2);ReleaseMapPreviews();ClearUIChildren(levelContent);
            chapterTitle.text=JellyJourney.ChapterNames[journeyChapter];chapterNote.text=JellyJourney.ChapterNotes[journeyChapter];
            journeyStars.text=JellyJourney.RankName+"   ★ "+JellyJourney.TotalStars+" / 36     ·     "+JellyJourney.NextUnlockNote;
            for(int i=0;i<chapterTabs.Count;i++){
                chapterTabs[i].color=i==journeyChapter?ChapterAccent(i):Color.Lerp(ChapterAccent(i),Color.white,.78f);
                chapterTabs[i].GetComponentInChildren<Text>().color=Ink;
                chapterTabs[i].GetComponent<JellyUISurface>().SetSelected(i==journeyChapter);
            }
            for(int j=0;j<4;j++) {
                var spec=JellyLevels.All[journeyChapter*4+j];int number=spec.Id;bool open=number<=JellyProgress.Unlocked;
                var card=Card("Level "+number,levelContent,new Vector2(-552+j*368,0),new Vector2(344,405),Color.Lerp(ChapterAccent(journeyChapter),Color.white,open?.93f:.86f));
                Label("Level number",card.transform,number.ToString("00"),new Vector2(-118,161),new Vector2(55,40),27,new Color(.23f,.39f,.64f),TextAnchor.MiddleLeft,true);
                Label("Level state",card.transform,open?(JellyProgress.Stars(number)>0?"已完成":"待探索"):"未解锁",new Vector2(83,160),new Vector2(122,32),16,Ink,TextAnchor.MiddleRight);
                var map=Rect("Map preview",card.transform,new Vector2(0,65),new Vector2(284,176)).gameObject.AddComponent<RawImage>();
                map.texture=CreateMapPreview(spec);map.raycastTarget=false;
                Label("Level map name",card.transform,spec.MapName,new Vector2(0,-36),new Vector2(292,39),26,Ink,TextAnchor.MiddleCenter,true);
                Label("Level description",card.transform,spec.Name+" · "+spec.Moves+" 步",new Vector2(0,-72),new Vector2(292,30),18,new Color(.23f,.39f,.64f),TextAnchor.MiddleCenter);
                var goals=new List<string>{spec.Score+" 分"};
                if(spec.Frost>0)goals.Add("清冰 "+spec.Frost);
                if(spec.ColorA>=0)goals.Add(JellyLevels.ColorNames[spec.ColorA]+" "+spec.AmountA);
                if(spec.ColorB>=0)goals.Add(JellyLevels.ColorNames[spec.ColorB]+" "+spec.AmountB);
                Label("Level goal",card.transform,string.Join(" · ",goals.ToArray()),new Vector2(0,-115),new Vector2(292,52),16,Ink,TextAnchor.MiddleCenter);
                int stars=JellyProgress.Stars(number);
                Label("Level progress",card.transform,open?(stars>0?new string('★',stars)+new string('☆',3-stars):"开始探索  →"):"通过第 "+(number-1)+" 关开放",new Vector2(0,-163),new Vector2(292,37),20,open?Purple:Ink,TextAnchor.MiddleCenter);
                var surface=card.GetComponent<JellyUISurface>();surface.style=JellyUISurface.SurfaceStyle.Choice;surface.SetSelected(number==JellyProgress.Unlocked);
                var button=card.gameObject.AddComponent<Button>();button.targetGraphic=card;button.interactable=open;var colors=button.colors;colors.disabledColor=Color.white;colors.highlightedColor=Color.white;button.colors=colors;button.navigation=new Navigation{mode=Navigation.Mode.None};
                button.onClick.AddListener(()=>{PlaySound(JellySound.Click,.45f);StartLevel(number);});card.gameObject.AddComponent<JellyButtonFeedback>().world=this;
            }
        }
        Color ChapterAccent(int chapter)
        {
            return chapter==0?new Color(.38f,.78f,.98f):chapter==1?new Color(1,.69f,.49f):new Color(.76f,.65f,.98f);
        }
        void BuildObjectivePanel(Transform parent)
        {
            var card=Rect("Objectives card",parent,new Vector2(603,250),new Vector2(302,250));
            Label("Objectives heading",card,"JOURNEY / 本关目标",new Vector2(0,91),new Vector2(254,38),18,Purple,TextAnchor.MiddleLeft,true);
            for(int i=0;i<4;i++) {
                var row=Rect("Objective row "+i,card,new Vector2(0,44-i*44),new Vector2(254,42));objectiveRows[i]=row.gameObject;
                objectiveValues[i]=Label("Objective value "+i,row,"",new Vector2(0,6),new Vector2(254,28),18,Ink);
                var track=Panel("Objective track",row,new Vector2(0,-15),new Vector2(254,4),new Color(.91f,.91f,.96f));track.raycastTarget=false;
                objectiveBars[i]=Panel("Objective fill",track.transform,Vector2.zero,new Vector2(254,4),Accent);objectiveBars[i].type=Image.Type.Filled;objectiveBars[i].fillMethod=Image.FillMethod.Horizontal;objectiveBars[i].raycastTarget=false;
            }
        }
        void RefreshObjectives()
        {
            if(objectiveValues[0]==null)return;
            for(int i=0;i<4;i++)objectiveRows[i].SetActive(false);
            if(InfiniteMode){objectiveRows[0].SetActive(true);objectiveValues[0].text="慢慢玩，自由探索";objectiveBars[0].fillAmount=1;ArrangeToolPool(1);return;}
            var c=LevelSpec;int row=0;SetObjective(row++,"积分",score,c.Score);
            if(c.ColorA>=0)SetObjective(row++,JellyLevels.ColorNames[c.ColorA]+"果冻",collected[c.ColorA],c.AmountA);
            if(c.ColorB>=0)SetObjective(row++,JellyLevels.ColorNames[c.ColorB]+"果冻",collected[c.ColorB],c.AmountB);
            if(c.Frost>0)SetObjective(row++,"清除冰层",c.Frost-FrostRemaining,c.Frost);
            ArrangeToolPool(row);
        }
        void ArrangeToolPool(int rows)
        {if(toolPoolLayout!=null)toolPoolLayout.anchoredPosition=new Vector2(603,71-Mathf.Clamp(rows,1,4)*44);}
        void SetObjective(int row,string title,int value,int target)
        {
            objectiveRows[row].SetActive(true);bool complete=value>=target;
            objectiveValues[row].text=title+"    "+(complete?"完成 ✓":Mathf.Min(value,target)+" / "+target);
            objectiveValues[row].color=complete?new Color(.31f,.38f,.74f):Ink;objectiveBars[row].fillAmount=target>0?Mathf.Clamp01((float)value/target):1;
        }
        bool RequireTool(int index)
        {
            if(IsToolUnlocked(index))return true;
            Notice(RaceMode?"竞速模式仅靠消除与连锁得分，不使用道具":"通过第 "+(JellyJourney.ToolUnlockLevel(index)-1)+" 关解锁"+JellyJourney.ToolNames[index]);return false;
        }
    }
}
