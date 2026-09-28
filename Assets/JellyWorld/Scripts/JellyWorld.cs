using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

namespace JellyWorldGame
{
    public partial class JellyWorld : MonoBehaviour
    {
        public Mesh jellyMesh;
        public Material[] candy;
        public Material cream, plum, white, blush, mint;
        public Font uiFont, titleFont;
        public Sprite roundSprite;
        public int score, moves, level=1;
        public bool busy;
        public string mode="menu";
        public JellyBoard board;
        public JellyTile[,] tiles=new JellyTile[8,8];
        Transform stage, hero, tileRoot, decor, effects;
        Canvas canvas;
        RectTransform menuUI, gameUI, modal;
        Text scoreText, moveText, goalText, toast, bestText, toolText, comboText;
        Text moveRule,chapterLabel;
        Image progress;
        readonly List<Text> toolLabels=new List<Text>();
        int[] charges={3,2,2};
        int activeTool=-1;
        Vector2Int selected=new Vector2Int(-1,-1);
        bool pointerDown;
        float idle;
        Camera cam;
        AudioSource audioSource;
        bool sound=true, paused;
        int Target { get { return LevelSpec.Score; } }
        Color Ink { get { return new Color(.22f,.16f,.36f); } }
        Color Purple { get { return new Color(.46f,.30f,.79f); } }

        void Start()
        {
            BuildPresentation();
        }
        public void BuildPresentation()
        {
            for(int i=transform.childCount-1;i>=0;i--) {
                var old=transform.GetChild(i).gameObject;
                old.SetActive(false);
                if(Application.isPlaying) Destroy(old); else DestroyImmediate(old);
            }
            toolLabels.Clear();
            cam=Camera.main;
            if(cam==null) {
                var go=new GameObject("Main Camera"); go.tag="MainCamera";
                cam=go.AddComponent<Camera>(); go.AddComponent<AudioListener>();
            }
            cam.transform.position=new Vector3(0,0,-22);
            cam.transform.rotation=Quaternion.identity;
            cam.orthographic=true; cam.orthographicSize=6.15f;
            cam.clearFlags=CameraClearFlags.SolidColor;
            cam.backgroundColor=new Color(.72f,.86f,.97f);
            cam.allowHDR=true; cam.allowMSAA=true;
            audioSource=gameObject.GetComponent<AudioSource>();
            if(audioSource==null) audioSource=gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake=false;
            sound=PlayerPrefs.GetInt("JellyWorld.Sound",1)==1;
            InitializeAudio();
            BuildLandscape();
            effects=Group("Transient sugar sparkles",transform);
            BuildBoardStage();
            BuildHero();
            BuildUI();
            ShowMenu();
        }
        Transform Group(string name,Transform parent)
        {
            var g=new GameObject(name); g.transform.SetParent(parent,false); return g.transform;
        }
        public GameObject Shape(string name,Transform parent,Vector3 pos,Vector3 scale,Material mat,bool sphere=false)
        {
            GameObject g;
            if(sphere) {
                g=GameObject.CreatePrimitive(PrimitiveType.Sphere);
                var col=g.GetComponent<Collider>();
                if(Application.isPlaying) Destroy(col); else DestroyImmediate(col);
            } else {
                g=new GameObject(name); g.AddComponent<MeshFilter>().sharedMesh=jellyMesh;
                g.AddComponent<MeshRenderer>();
            }
            g.name=name; g.transform.SetParent(parent,false);
            g.transform.localPosition=pos; g.transform.localScale=scale;
            g.GetComponent<Renderer>().sharedMaterial=mat;
            if(name=="Cloud puff" || name.Contains("planet") || name.Contains("shore") || name.Contains("dune")) {
                g.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
                g.GetComponent<Renderer>().receiveShadows=false;
            }
            return g;
        }
        void BuildLandscape()
        {
            decor=Group("Marshmallow archipelago",transform);
            Shape("Pistachio planet",decor,new Vector3(0,-7,16),new Vector3(33,8,16),mint,true);
            Shape("Vanilla shore",decor,new Vector3(0,-7.2f,8),new Vector3(30,6,9),cream,true);
            Shape("Rose dune",decor,new Vector3(-10,-5.5f,14),new Vector3(12,7,10),blush,true);
            Shape("Lavender dune",decor,new Vector3(11,-5.2f,15),new Vector3(14,7,10),candy[4],true);
            for(int side=-1;side<=1;side+=2) {
                for(int i=0;i<4;i++) {
                    float x=side*(7.6f+i*.9f);
                    Shape("Cloud puff",decor,new Vector3(x,3.7f+(i%2)*.28f,6),new Vector3(2.2f,1.25f,1.1f),white,true);
                }
                for(int i=0;i<3;i++) {
                    float x=side*(10.1f+i*1.3f), y=-2.4f-i*.55f;
                    Shape("Candy tree stem",decor,new Vector3(x,y-.65f,3),new Vector3(.18f,2.2f,.18f),cream);
                    Shape("Candy tree crown",decor,new Vector3(x,y+.5f,3),new Vector3(1.5f,2.0f,1.25f),candy[(i+(side==1?2:0))%5],true);
                    Shape("Tree sugar glint",decor,new Vector3(x-.25f,y+.85f,2.4f),new Vector3(.22f,.45f,.07f),white,true);
                }
            }
            for(int i=0;i<22;i++) {
                float x=Mathf.Sin(i*12.32f)*11, y=-4.4f+Mathf.Cos(i*3.1f)*.35f;
                Shape("Sugar pebble",decor,new Vector3(x,y,1+i%3),new Vector3(.3f,.2f,.32f),candy[i%5],true);
            }
        }
        void BuildHero()
        {
            hero=Group("Welcome • jelly island",transform);
            hero.localPosition=new Vector3(4,-.1f,0);
            Shape("Floating island icing",hero,new Vector3(0,-2.9f,1),new Vector3(6.9f,1.0f,4.1f),cream,true);
            Shape("Floating island base",hero,new Vector3(0,-3.3f,1.4f),new Vector3(6.4f,1.5f,3.8f),candy[4],true);
            MakeCharacter(hero,new Vector3(-1.45f,-1.55f,0),1.95f,0,-9);
            MakeCharacter(hero,new Vector3(.8f,-1.35f,.3f),2.4f,2,8);
            MakeCharacter(hero,new Vector3(-.3f,.75f,.5f),2.0f,1,-12);
            MakeCharacter(hero,new Vector3(2.1f,.75f,1),1.0f,4,14);
            for(int band=0;band<3;band++) for(int i=0;i<34;i++) {
                float a=i/33f*Mathf.PI;
                float r=3.05f+band*.25f;
                Shape("Rainbow sugar arch",hero,new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r-.9f,2.8f),
                    Vector3.one*.39f,candy[new int[]{0,3,2}[band]],true);
            }
            for(int i=0;i<7;i++)
                Shape("Floating bonbon",hero,new Vector3(Mathf.Sin(i*4)*3.4f,2.0f+Mathf.Cos(i*5)*1.3f,.7f),
                    Vector3.one*(.18f+(i%2)*.12f),candy[i%5],true);
        }
        void MakeCharacter(Transform parent,Vector3 p,float size,int c,float angle)
        {
            var g=Group("Jelly friend",parent);g.localPosition=p;g.localScale=Vector3.one*size;
            g.localRotation=Quaternion.Euler(-9,-14,angle);
            Shape("Soft body",g,Vector3.zero,Vector3.one,candy[c]);
            Face(g,c);
        }
        void Face(Transform g,int c)
        {
            for(int s=-1;s<=1;s+=2) {
                Shape("Cocoa eye",g,new Vector3(s*.17f,.025f,-.50f),new Vector3(.082f,.12f,.045f),plum,true);
                Shape("Eye sparkle",g,new Vector3(s*.17f-.015f,.052f,-.525f),Vector3.one*.025f,white,true);
                Shape("Blushing cheek",g,new Vector3(s*.28f,-.105f,-.492f),new Vector3(.115f,.065f,.025f),blush,true);
            }
            Shape("Happy smile",g,new Vector3(0,-.12f,-.509f),new Vector3(.07f,.055f,.028f),plum,true);
            Shape("Glossy highlight",g,new Vector3(-.23f,.29f,-.457f),new Vector3(.18f,.09f,.036f),white,true);
            Shape("Glossy dot",g,new Vector3(-.08f,.31f,-.485f),Vector3.one*.045f,white,true);
        }
        Vector3 Pos(int x,int y) { return new Vector3(x-BoardHalf,y-BoardHalf,-.20f); }
        JellyTile Spawn(int x,int y,int color,bool fall)
        {
            var g=Group("Jelly "+x+","+y,tileRoot).gameObject;
            g.transform.localPosition=Pos(x,y)+(fall?Vector3.up*8:Vector3.zero);
            Shape("Jelly body",g.transform,Vector3.zero,Vector3.one,candy[color]);
            Face(g.transform,color);
            g.AddComponent<BoxCollider>().size=Vector3.one;
            var t=g.AddComponent<JellyTile>();t.x=x;t.y=y;t.color=color;t.target=Pos(x,y);t.born=Time.time;
            g.transform.localScale=new Vector3(.88f,.88f,.82f);
            tiles[x,y]=t;return t;
        }
        void ClearTiles()
        {
            for(int i=tileRoot.childCount-1;i>=0;i--) {
                var g=tileRoot.GetChild(i).gameObject;g.SetActive(false);
                if(Application.isPlaying) Destroy(g);else DestroyImmediate(g);
            }
            tiles=new JellyTile[BoardSize,BoardSize];
        }
        public void NewGame()
        {
            SaveInfiniteBest();
            CancelRange();rangeCharges=1;
            ResetGuardian();
            StopAllCoroutines();ClearEffects();Time.timeScale=1;paused=false;busy=false;pointerDown=false;mode="play";
            score=0;moves=InfiniteMode?26:LevelSpec.Moves;charges=new int[]{3,2,2};activeTool=-1;selected=new Vector2Int(-1,-1);
            board=new JellyBoard(Environment.TickCount,InfiniteMode?8:LevelSpec.Size,InfiniteMode?5:LevelSpec.Colors);
            BuildBoardStage();
            ClearTiles();
            ResetObjectives();
            for(int x=0;x<BoardSize;x++) for(int y=0;y<BoardSize;y++) Spawn(x,y,board.Cells[x,y],false);
            hero.gameObject.SetActive(false);stage.gameObject.SetActive(true);
            menuUI.gameObject.SetActive(false);gameUI.gameObject.SetActive(true);modal.gameObject.SetActive(false);
            if(infiniteUI!=null)infiniteUI.gameObject.SetActive(false);
            HideCampaignPages();
            Notice(InfiniteMode?"交换相邻果冻，三个同色消除":ObjectiveSummary());
            UpdateHUD();Tone(520,.13f);idle=0;
        }
        public void ShowMenu()
        {
            SaveInfiniteBest();
            CancelRange();
            CancelGuardian();
            StopAllCoroutines();ClearEffects();Time.timeScale=1;paused=false;busy=false;pointerDown=false;mode="menu";
            hero.gameObject.SetActive(true);stage.gameObject.SetActive(false);
            menuUI.gameObject.SetActive(true);gameUI.gameObject.SetActive(false);modal.gameObject.SetActive(false);
            if(infiniteUI!=null)infiniteUI.gameObject.SetActive(false);
            HideCampaignPages();
            bestText.text="已通关 "+JellyProgress.Completed+" / "+JellyLevels.All.Length;
        }
        void Update()
        {
            if(mode!="play" || paused)ResetBoardGesture();
            if(mode=="range") { if(Input.GetKeyDown(KeyCode.Escape)) TogglePause(); return; }
            if(mode=="golem") { if(Input.GetKeyDown(KeyCode.Escape)) TogglePause(); return; }
            if(cam!=null) {
                bool onBoard=mode=="play" || mode=="result";
                cam.orthographicSize=onBoard?Mathf.Max((BoardSize+2.7f)*.5f,(BoardSize+2.7f)*.89f/cam.aspect):Mathf.Max(6.15f,10.7f/cam.aspect);
                cam.transform.position=new Vector3(onBoard?(BoardSize+2.7f)*.19626f:0,0,-22);
            }
            if(hero!=null && hero.gameObject.activeSelf) hero.localPosition=new Vector3(4,-.1f+Mathf.Sin(Time.unscaledTime*.9f)*.13f,0);
            if(!Application.isPlaying || mode!="play") return;
            if(Input.GetKeyDown(KeyCode.Escape)) { if(ChoosingLanding)CancelLandingChoice();else TogglePause();return; }
            if(paused)return;
            ProcessBoardInput();
            if(busy)return;
            if(ChoosingLanding)UpdateLandingPreview();
            else if(activeTool>=0)UpdateToolPreview();
            idle+=Time.deltaTime;
            if(idle>7) { Hint(false);idle=-5; }
        }
        Vector2Int Hit()
        {
            RaycastHit hit;
            if(Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition),out hit,100)) {
                var t=hit.collider.GetComponent<JellyTile>();
                if(t!=null) return new Vector2Int(t.x,t.y);
            }
            return new Vector2Int(-1,-1);
        }
        public void ClickCell(Vector2Int p)
        {
            if(mode!="play" || busy || paused || !Inside(p)) return;
            if(ChoosingLanding) {ConfirmGuardianLanding(p);return;}
            idle=0;ClearHints();
            if(activeTool>=0) {
                int tool=activeTool;activeTool=-1;if(!InfiniteMode)charges[tool]--;
                Deselect();StartCoroutine(UseTargetTool(tool,p));return;
            }
            if(selected==p) { Deselect();return; }
            if(Inside(selected) && JellyBoard.Adjacent(selected,p)) {
                StartCoroutine(SwapRoutine(selected,p));return;
            }
            Deselect();selected=p;tiles[p.x,p.y].selected=true;Tone(650,.04f);
        }
        void Deselect()
        {
            if(Inside(selected) && tiles[selected.x,selected.y]!=null) tiles[selected.x,selected.y].selected=false;
            selected=new Vector2Int(-1,-1);
        }
        void ClearHints() { foreach(var t in tiles) if(t!=null) t.hinted=false; }
        public void Hint(bool announce=true)
        {
            if(busy || mode!="play" || paused || ChoosingLanding) return;
            ClearHints();Vector2Int a,b;
            if(board.FindMove(out a,out b)) {tiles[a.x,a.y].hinted=true;tiles[b.x,b.y].hinted=true;if(announce) Notice("交换跳动的两格");}
        }
        void SwapVisual(Vector2Int a,Vector2Int b)
        {
            var t=tiles[a.x,a.y];tiles[a.x,a.y]=tiles[b.x,b.y];tiles[b.x,b.y]=t;
            tiles[a.x,a.y].x=a.x;tiles[a.x,a.y].y=a.y;tiles[a.x,a.y].target=Pos(a.x,a.y);
            tiles[b.x,b.y].x=b.x;tiles[b.x,b.y].y=b.y;tiles[b.x,b.y].target=Pos(b.x,b.y);
        }
        public IEnumerator SwapRoutine(Vector2Int a,Vector2Int b)
        {
            if(busy || paused || !Inside(a) || !Inside(b) || !JellyBoard.Adjacent(a,b)) yield break;
            busy=true;RefreshToolPool();Deselect();ClearHints();board.Swap(a,b);SwapVisual(a,b);Tone(420,.06f);
            yield return new WaitForSeconds(.23f);
            var matches=board.Matches();
            if(matches.Count==0) {
                board.Swap(a,b);SwapVisual(a,b);Notice("需要三个同色相连");
                Tone(210,.08f);yield return new WaitForSeconds(.23f);busy=false;RefreshToolPool();yield break;
            }
            if(!InfiniteMode)moves--;yield return Resolve(matches,true);
        }
        public IEnumerator Resolve(HashSet<Vector2Int> matches,bool usedMove)
        {
            busy=true;int chain=0;UpdateHUD();
            while(matches.Count>0) {
                chain++;int gained=matches.Count*60*chain;score+=gained;
                comboText.text=chain>1?"连锁 ×"+chain:"+"+gained;
                PlaySound(JellySound.Pop,.6f,Mathf.Min(1.25f,1+(chain-1)*.06f));
                foreach(var p in matches) {
                    var t=tiles[p.x,p.y];
                    if(t!=null)RegisterClear(p.x,p.y,t.color);
                    if(t!=null) { Burst(t.transform.position,t.color);Destroy(t.gameObject);tiles[p.x,p.y]=null; }
                    board.Cells[p.x,p.y]=-1;
                }
                UpdateHUD();yield return new WaitForSeconds(.22f);
                CollapseBoard();
                yield return WaitForRefillFalls();
                yield return new WaitForSeconds(.06f);
                matches=board.Matches();
            }
            comboText.text="";busy=false;idle=0;UpdateHUD();
            if(!InfiniteMode && (ObjectivesMet || moves<=0)) { Finish(ObjectivesMet);yield break; }
            Vector2Int ma,mb;
            if(!board.FindMove(out ma,out mb)) {Notice("无可用组合，自动洗牌");Shuffle(false);}
            else Notice("");
        }
        void ClearEffects()
        {
            ActingTool=-1;ResetBoardGesture();
            if(effects==null)return;
            for(int i=effects.childCount-1;i>=0;i--) {
                var g=effects.GetChild(i).gameObject;g.SetActive(false);
                if(Application.isPlaying)Destroy(g);else DestroyImmediate(g);
            }
        }
        void Burst(Vector3 p,int c)
        {
            JellyVfx.Shatter(this,effects,p,c,mode=="golem"?Vector3.up:stage.TransformDirection(Vector3.back));
        }
        public void ChooseTool(int tool)
        {
            if(mode!="play" || busy || paused) return;
            ResetBoardGesture();CancelLandingChoice();
            if(!InfiniteMode && charges[tool]<=0) {Notice("这个道具已用完，下局会补满");return;}
            Deselect();ClearHints();
            if(tool==2) {Shuffle(true);return;}
            activeTool=activeTool==tool?-1:tool;
            Notice(activeTool<0?"已取消道具":tool==0?"果冻锤：点击要敲碎的一颗果冻":"十字星：点击中心，消除整行与整列");
            UpdateHUD();
        }
        void Shuffle(bool paid)
        {
            if(paid && !InfiniteMode) charges[2]--;
            activeTool=-1;Deselect();StartCoroutine(ShuffleAction(paid));
        }
        void Finish(bool win) {FinishLevel(win);}
        public void TogglePause()
        {
            if(mode!="play" && mode!="golem" && mode!="range")return;
            if(paused) {paused=false;Time.timeScale=1;modal.gameObject.SetActive(false);if(Guardian!=null)Guardian.SyncCursor();if(Range!=null)Range.SyncCursor();return;}
            paused=true;ResetBoardGesture();Time.timeScale=0;
            if(Guardian!=null)Guardian.SyncCursor();if(Range!=null)Range.SyncCursor();
            ShowDialog("暂停","","","继续",()=>TogglePause(),true);
            Button("Pause audio settings",modal,"音效设置",new Vector2(0,10),new Vector2(280,52),new Color(.85f,.93f,.91f),Ink,OpenAudioSettings,21);
        }
        void Notice(string s) { if(toast!=null)toast.text=s; }
        void UpdateHUD()
        {
            scoreText.text=score.ToString("N0");moveText.text=InfiniteMode?"∞":moves.ToString("00");
            goalText.text=InfiniteMode?"目标 —":"目标 "+Target.ToString("N0");
            progress.fillAmount=InfiniteMode?1:ObjectiveProgress();
            if(moveRule!=null)moveRule.text=InfiniteMode?"不限步数":"有效交换消耗 1 步";
            if(chapterLabel!=null)chapterLabel.text=InfiniteMode?"无限模式":"关卡 "+level;
            RefreshToolPool();
        }
        RectTransform Rect(string name,Transform parent,Vector2 pos,Vector2 size)
        {
            var g=new GameObject(name,typeof(RectTransform));
            var r=g.GetComponent<RectTransform>();r.SetParent(parent,false);
            r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.anchoredPosition=pos;r.sizeDelta=size;return r;
        }
        Image Panel(string name,Transform parent,Vector2 pos,Vector2 size,Color color)
        {
            var r=Rect(name,parent,pos,size);var image=r.gameObject.AddComponent<Image>();image.sprite=roundSprite;
            image.type=Image.Type.Sliced;image.color=color;return image;
        }
        Text Label(string name,Transform parent,string value,Vector2 pos,Vector2 size,int fontSize,Color color,TextAnchor align=TextAnchor.MiddleLeft,bool bold=false)
        {
            var r=Rect(name,parent,pos,size);var t=r.gameObject.AddComponent<Text>();
            t.font=bold?titleFont:uiFont;t.text=value;t.fontSize=fontSize;t.color=color;t.alignment=align;
            t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Overflow;t.raycastTarget=false;
            if(bold)t.fontStyle=FontStyle.Bold;return t;
        }
        Button Button(string name,Transform parent,string value,Vector2 pos,Vector2 size,Color bg,Color fg,Action action,int fontSize=23)
        {
            var image=Panel(name,parent,pos,size,bg);
            var b=image.gameObject.AddComponent<Button>();b.targetGraphic=image;
            b.navigation=new Navigation {mode=Navigation.Mode.None};
            var colors=b.colors;colors.highlightedColor=new Color(.96f,.94f,1);colors.pressedColor=new Color(.8f,.75f,.9f);b.colors=colors;
            b.onClick.AddListener(()=>{PlaySound(JellySound.Click,.45f);action();});
            b.gameObject.AddComponent<JellyButtonFeedback>().world=this;
            Label(name+" label",b.transform,value,Vector2.zero,size-new Vector2(16,4),fontSize,fg,TextAnchor.MiddleCenter);
            return b;
        }
        void BuildUI()
        {
            var r=Rect("Jelly World interface",transform,Vector2.zero,new Vector2(1600,900));
            canvas=r.gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=r.gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1600,900);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            r.gameObject.AddComponent<GraphicRaycaster>();
            if(FindObjectOfType<EventSystem>()==null) {
                var e=new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));e.transform.SetParent(transform);
            }
            menuUI=Rect("Home",r,Vector2.zero,new Vector2(1600,900));
            Label("Title",menuUI,"JELLY\nWORLD",new Vector2(-411,151),new Vector2(490,260),100,Ink,TextAnchor.MiddleLeft,true);
            Button("Start adventure",menuUI,"关卡模式",new Vector2(-441,-61),new Vector2(410,70),Purple,Color.white,ShowLevelSelect,27);
            Button("Infinite mode entry",menuUI,"无限模式",new Vector2(-441,-153),new Vector2(410,66),new Color(.22f,.57f,.55f),Color.white,ShowInfinitePage,25);
            Button("How to play",menuUI,"操作说明",new Vector2(-547,-241),new Vector2(198,48),new Color(1,1,1,.9f),Ink,ShowHelp,19);
            Button("Sound",menuUI,"音效设置",new Vector2(-333,-241),new Vector2(198,48),new Color(1,1,1,.9f),Ink,OpenAudioSettings,19);
            Button("Leaderboard",menuUI,"本地排行榜",new Vector2(-441,-309),new Vector2(410,49),new Color(1,1,1,.9f),Ink,ShowLeaderboard,21);
            bestText=Label("Best",menuUI,"",new Vector2(-441,-369),new Vector2(410,40),20,Ink);
            gameUI=Rect("Play HUD",r,Vector2.zero,new Vector2(1600,900));
            var stats=Panel("Compact score bar",gameUI,new Vector2(-178,412),new Vector2(766,63),new Color(1,.99f,.97f,.94f)).transform;
            chapterLabel=Label("World index",stats,"",new Vector2(-285,5),new Vector2(165,38),20,Ink);
            Label("Score caption",stats,"分数",new Vector2(-164,5),new Vector2(58,30),17,Purple);
            scoreText=Label("Score",stats,"0",new Vector2(-82,5),new Vector2(118,42),29,Ink,TextAnchor.MiddleLeft,true);
            goalText=Label("Goal",stats,"",new Vector2(80,5),new Vector2(180,38),20,Ink);
            Label("Move caption",stats,"步数",new Vector2(220,5),new Vector2(60,32),18,Purple);
            moveText=Label("Moves",stats,"26",new Vector2(291,5),new Vector2(80,40),30,Ink,TextAnchor.MiddleCenter,true);
            var track=Panel("Progress track",stats,new Vector2(0,-25),new Vector2(698,5),new Color(.90f,.87f,.94f));
            progress=Panel("Progress",track.transform,Vector2.zero,new Vector2(698,5),new Color(.95f,.57f,.69f));
            progress.type=Image.Type.Filled;progress.fillMethod=Image.FillMethod.Horizontal;
            Button("Back home",gameUI,"主页",new Vector2(560,412),new Vector2(108,51),new Color(1,1,1,.94f),Ink,()=>{paused=true;Time.timeScale=0;ShowDialog("返回主页","","本局进度将不会保留","返回",ShowMenu,true,()=>{paused=false;Time.timeScale=1;modal.gameObject.SetActive(false);});},20);
            Button("Pause",gameUI,"暂停",new Vector2(689,412),new Vector2(108,51),new Color(1,1,1,.94f),Ink,TogglePause,20);
            BuildToolPool(gameUI);
            Button("Hint",gameUI,"提示",new Vector2(-664,-408),new Vector2(118,48),new Color(1,1,1,.86f),Ink,()=>Hint(),19);
            toast=Label("Instruction",gameUI,"",new Vector2(-123,-408),new Vector2(880,40),20,Ink,TextAnchor.MiddleCenter);
            comboText=Label("Combo",gameUI,"",new Vector2(-176,354),new Vector2(610,44),28,Purple,TextAnchor.MiddleCenter,true);
            moveRule=Label("Goal foot",gameUI,"",new Vector2(521,-408),new Vector2(420,30),15,Ink,TextAnchor.MiddleCenter);
            BuildInfiniteUI(r);BuildGuardianUI(r);BuildRangeUI(r);BuildCampaignUI(r);
            modal=Rect("Dialog",r,Vector2.zero,new Vector2(1600,900));modal.gameObject.SetActive(false);
        }
        void ShowHelp()
        {
            ShowDialog("操作说明","",
                "在步数内完成积分、收集和清冰目标。\n交换相邻果冻，三个同色消除，连锁加分。\n消除冰壳覆盖格即可清冰，道具同样有效。\n守护者：WASD 移动，空格跳跃，F 重踏。\n靶场：左键射击；无限模式不限道具。",
                "开始游戏",StartChallenge,true);
        }
        void ShowDialog(string title,string subtitle,string detail,string primary,Action action,bool home,Action secondary=null,string secondaryLabel=null)
        {
            for(int i=modal.childCount-1;i>=0;i--) {var g=modal.GetChild(i).gameObject;g.SetActive(false);Destroy(g);}
            modal.gameObject.SetActive(true);
            Panel("Veil",modal,Vector2.zero,new Vector2(6000,4000),new Color(.16f,.12f,.28f,.46f));
            var card=Panel("Dialog card",modal,Vector2.zero,new Vector2(690,520),new Color(1,.98f,.96f)).transform;
            Label("Dialog heading",card,title,new Vector2(0,185),new Vector2(630,57),34,Purple,TextAnchor.MiddleCenter,true);
            Label("Dialog subtitle",card,subtitle,new Vector2(0,116),new Vector2(620,53),29,Ink,TextAnchor.MiddleCenter);
            Label("Dialog details",card,detail,new Vector2(0,15),new Vector2(610,138),21,new Color(.45f,.40f,.54f),TextAnchor.MiddleCenter);
            Button("Dialog primary",card,primary,new Vector2(0,-119),new Vector2(480,64),Purple,Color.white,action,25);
            if(home) Button("Dialog secondary",card,secondaryLabel??(secondary!=null?"继续游戏":"返回主页"),new Vector2(0,-200),new Vector2(300,45),new Color(.93f,.90f,.96f),Ink,secondary??ShowMenu,19);
        }
    }
}
