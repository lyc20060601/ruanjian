using System.Collections;
using UnityEngine;
using UnityEngine.UI;
namespace JellyWorldGame
{
    public partial class JellyWorld
    {
        public Material stone, stoneDark, rune, gold, glow;
        public Mesh fractureMesh;
        public JellyGolemMode Guardian { get; private set; }
        public bool IsPaused { get { return paused; } }
        public int GuardianCharges { get { return guardianCharges; } }
        public int guardianCharges=1;
        public bool ChoosingLanding {get;private set;}
        LineRenderer landingMarker;
        Image jumpCharge, jumpChargePanel;
        Text jumpChargeText;
        RectTransform guardianUI;
        Text summonText, guardianTimer, guardianEnergy, guardianScore, guardianView, guardianStatus;
        Image guardianBar;
        void BuildGuardianUI(Transform root)
        {
            guardianUI=Rect("Guardian HUD",root,Vector2.zero,new Vector2(1600,900));
            var card=Panel("Guardian identity",guardianUI,new Vector2(-530,350),new Vector2(430,128),new Color(.13f,.19f,.28f,.94f)).transform;
            Label("Guardian title",card,"守护者",new Vector2(0,31),new Vector2(382,35),22,new Color(.70f,1,.85f),TextAnchor.MiddleLeft,true);
            Label("Guardian subtitle",card,"带冰方块：先碎冰，再消除",new Vector2(0,-23),new Vector2(382,42),18,Color.white);
            var stat=Panel("Energy card",guardianUI,new Vector2(23,351),new Vector2(518,127),new Color(1,.98f,.94f,.95f)).transform;
            guardianTimer=Label("Guardian timer",stat,"20s",new Vector2(-173,20),new Vector2(105,52),33,Ink,TextAnchor.MiddleLeft,true);
            guardianEnergy=Label("Guardian energy",stat,"",new Vector2(74,23),new Vector2(322,42),21,Ink,TextAnchor.MiddleRight);
            var track=Panel("Guardian energy track",stat,new Vector2(0,-31),new Vector2(454,12),new Color(.82f,.85f,.88f));
            guardianBar=Panel("Guardian energy fill",track.transform,Vector2.zero,new Vector2(454,12),new Color(.29f,.74f,.65f));
            guardianBar.type=Image.Type.Filled;guardianBar.fillMethod=Image.FillMethod.Horizontal;
            Button("Guardian pause",guardianUI,"暂停  II",new Vector2(649,382),new Vector2(182,48),new Color(1,1,1,.92f),Ink,TogglePause,20);
            Button("Return to board",guardianUI,"结束召唤  ↗",new Vector2(649,315),new Vector2(182,48),new Color(.24f,.31f,.43f),Color.white,()=>{if(Guardian!=null)Guardian.End();},20);
            guardianScore=Label("Guardian collection",guardianUI,"",new Vector2(-530,242),new Vector2(430,54),23,Ink);
            var feedback=Panel("Guardian hit feedback",guardianUI,new Vector2(0,228),new Vector2(250,46),new Color(.16f,.22f,.31f,.94f));
            guardianStatus=Label("Guardian status",feedback.transform,"",Vector2.zero,new Vector2(235,42),21,Color.white,TextAnchor.MiddleCenter);
            var controls=Panel("Guardian controls",guardianUI,new Vector2(0,-371),new Vector2(1240,106),new Color(.13f,.19f,.28f,.93f)).transform;
            Label("Keys",controls,"W A S D 移动   SHIFT 跑动   长按 SPACE 蓄力跳跃   F 范围重踏",new Vector2(0,20),new Vector2(1190,38),22,Color.white,TextAnchor.MiddleCenter);
            Label("Camera keys",controls,"V 切换视角 · 第一人称移动鼠标自由看     TAB 释放 / 锁定鼠标     R 返回棋盘",new Vector2(0,-24),new Vector2(1190,32),17,new Color(.68f,.87f,.83f),TextAnchor.MiddleCenter);
            guardianView=Label("View status",guardianUI,"",new Vector2(-531,-278),new Vector2(440,38),19,Ink);
            Button("Switch guardian camera",guardianUI,"切换视角  V",new Vector2(611,-278),new Vector2(252,48),new Color(1,1,1,.88f),Ink,()=>{if(Guardian!=null)Guardian.ToggleView();},20);
            jumpChargePanel=Panel("Jump charge panel",guardianUI,new Vector2(0,-270),new Vector2(480,84),new Color(.12f,.17f,.25f,.97f));
            var chargeTrack=Panel("Jump charge track",jumpChargePanel.transform,new Vector2(0,-18),new Vector2(434,24),new Color(.34f,.39f,.47f));
            jumpCharge=Panel("Jump charge fill",chargeTrack.transform,Vector2.zero,new Vector2(430,20),new Color(1,.73f,.19f));
            jumpCharge.type=Image.Type.Filled;jumpCharge.fillMethod=Image.FillMethod.Horizontal;jumpCharge.fillAmount=0;
            jumpChargeText=Label("Jump charge label",jumpChargePanel.transform,"SPACE  长按蓄力 · 松开跳跃",new Vector2(0,18),new Vector2(444,30),21,Color.white,TextAnchor.MiddleCenter);
            jumpChargePanel.raycastTarget=false;chargeTrack.raycastTarget=false;jumpCharge.raycastTarget=false;
            guardianUI.gameObject.SetActive(false);
        }
        public void SummonGuardian()
        {
            if(mode!="play" || paused || busy) return;
            if(ChoosingLanding){CancelLandingChoice();return;}
            if(!InfiniteMode && guardianCharges<=0) {Notice("守护者正在休息，下一局可再次召唤");return;}
            Deselect();ClearHints();activeTool=-1;pointerDown=false;ChoosingLanding=true;
            Notice("选择守护者落点：点击一颗果冻 · ESC 或再点召唤可取消");UpdateHUD();
        }
        public bool ConfirmGuardianLanding(Vector2Int cell)
        {
            if(!ChoosingLanding || mode!="play" || paused || busy || !Inside(cell))return false;
            CancelLandingChoice();
            if(!InfiniteMode)guardianCharges--;
            mode="golem";busy=true;gameUI.gameObject.SetActive(false);guardianUI.gameObject.SetActive(true);
            if(Guardian==null) {
                Guardian=GetComponent<JellyGolemMode>();
                if(Guardian==null)Guardian=gameObject.AddComponent<JellyGolemMode>();
            }
            Guardian.Begin(this,stage,decor,cam,effects,cell);return true;
        }
        public void CancelLandingChoice()
        {
            if(!ChoosingLanding)return;
            ChoosingLanding=false;ClearHints();
            if(landingMarker!=null){landingMarker.gameObject.SetActive(false);Destroy(landingMarker.gameObject);}
            Notice("交换相邻果冻，或选择其他道具");UpdateHUD();
        }
        void UpdateLandingPreview()
        {
            Vector2Int p=Hit();ClearHints();
            if(!Inside(p)){if(landingMarker!=null)landingMarker.gameObject.SetActive(false);return;}
            tiles[p.x,p.y].hinted=true;
            if(landingMarker==null) {
                var g=new GameObject("Guardian landing preview");g.transform.SetParent(stage,false);
                landingMarker=g.AddComponent<LineRenderer>();landingMarker.useWorldSpace=false;landingMarker.loop=true;
                landingMarker.positionCount=4;landingMarker.sharedMaterial=glow;landingMarker.widthMultiplier=.055f;
                landingMarker.startColor=landingMarker.endColor=new Color(1,.84f,.27f,1);
            }
            landingMarker.gameObject.SetActive(true);Vector3 c=Pos(p.x,p.y)+Vector3.back*.5f;
            landingMarker.SetPositions(new[]{c+new Vector3(-.51f,-.51f,0),c+new Vector3(-.51f,.51f,0),c+new Vector3(.51f,.51f,0),c+new Vector3(.51f,-.51f,0)});
        }
        void CancelGuardian()
        {
            CancelLandingChoice();
            if(Guardian!=null)Guardian.Cancel();
            if(guardianUI!=null)guardianUI.gameObject.SetActive(false);
        }
        void ResetGuardian() {CancelGuardian();guardianCharges=1;}
        public void SetGuardianHUD(float seconds,int energy,int crushed,int stomps,bool first,string status)
        {
            guardianTimer.text=InfiniteMode?"∞":Mathf.CeilToInt(seconds).ToString("00")+"s";
            guardianEnergy.text=InfiniteMode?"无限能量    无限重踏":"破坏能量  "+energy+"/18    重踏  "+stomps;
            guardianBar.fillAmount=InfiniteMode?1:energy/18f;
            guardianScore.text="已踩碎  "+crushed+" 颗   /   +"+(crushed*60)+" 分";
            guardianView.text=first?"第一人称":"第三人称";
            guardianStatus.text=status;guardianStatus.transform.parent.gameObject.SetActive(!string.IsNullOrEmpty(status));
        }
        public void SetJumpHUD(float charge,bool charging,bool airborne)
        {
            charge=Mathf.Clamp01(charge);jumpCharge.fillAmount=charge;
            jumpCharge.color=charge>=.99f?new Color(.38f,1,.74f):new Color(1,.73f,.19f);
            jumpChargePanel.color=charging?new Color(.16f,.22f,.31f,.99f):new Color(.12f,.17f,.25f,.97f);
            jumpChargeText.text=airborne?"腾空中 · 即将落地":charging?(charge>=.99f?"蓄力已满  100%  ·  松开跳跃":"蓄力  "+Mathf.RoundToInt(charge*100)+"%  ·  松开跳跃"):"SPACE  长按蓄力 · 松开跳跃";
        }
        public bool CrushGuardianCell(int x,int y)
        {
            if(mode!="golem" || x<0 || x>=BoardSize || y<0 || y>=BoardSize || board.Cells[x,y]<0)return false;
            var tile=tiles[x,y];if(tile==null)return false;
            if(IsIced(x,y)) {
                BreakIce(x,y);iceRehitAt[x,y]=Time.time+.42f;tile.Pulse(.10f);return false;
            }
            if(iceRehitAt!=null && Time.time<iceRehitAt[x,y])return false;
            RegisterClear(x,y,tile.color);
            Burst(tile.transform.position,tile.color);
            tile.gameObject.SetActive(false);Destroy(tile.gameObject);
            tiles[x,y]=null;board.Cells[x,y]=-1;score+=60;
            return true;
        }
        public void GuardianImpact(bool heavy)
        {
            PlaySound(heavy?JellySound.Impact:JellySound.Step,heavy?.62f:.38f);
        }
        void CollapseBoard()
        {
            for(int x=0;x<BoardSize;x++) {
                int dest=0;
                for(int y=0;y<BoardSize;y++) if(board.Cells[x,y]>=0) {
                    if(dest!=y) {
                        board.Cells[x,dest]=board.Cells[x,y];board.Cells[x,y]=-1;
                        tiles[x,dest]=tiles[x,y];tiles[x,y]=null;
                        tiles[x,dest].y=dest;tiles[x,dest].FallTo(Pos(x,dest));
                    }
                    dest++;
                }
                for(int y=dest;y<BoardSize;y++) {
                    board.Cells[x,y]=board.Next();var tile=Spawn(x,y,board.Cells[x,y],false);
                    tile.transform.localPosition=new Vector3(x-BoardHalf,BoardHalf+1.3f+y-dest,-.20f);
                    tile.FallTo(Pos(x,y),0,true);
                }
            }
        }
        public void CompleteGuardian()
        {
            guardianUI.gameObject.SetActive(false);gameUI.gameObject.SetActive(true);
            mode="play";busy=true;UpdateHUD();StartCoroutine(AfterGuardian());
        }
        IEnumerator AfterGuardian()
        {
            yield return RefillGuardianBoard();
            yield return Resolve(board.Matches(),false);
        }
        IEnumerator RefillGuardianBoard()
        {
            int missing=0;
            for(int x=0;x<BoardSize;x++)for(int y=0;y<BoardSize;y++)if(board.Cells[x,y]<0)missing++;
            if(missing==0)yield break;
            Notice("补位 "+missing+" 格");
            // Leave the damage visible after the camera has returned before anything moves.
            yield return new WaitForSeconds(.5f);
            int[] firstEmpty=new int[BoardSize];
            for(int x=0;x<BoardSize;x++) {
                int dest=0;
                for(int y=0;y<BoardSize;y++)if(board.Cells[x,y]>=0) {
                    if(dest!=y) {
                        board.Cells[x,dest]=board.Cells[x,y];board.Cells[x,y]=-1;
                        var tile=tiles[x,y];tiles[x,dest]=tile;tiles[x,y]=null;tile.y=dest;
                        tile.FallTo(Pos(x,dest));
                    }
                    dest++;
                }
                firstEmpty[x]=dest;
            }
            Notice("补位中");
            for(int x=0;x<BoardSize;x++)for(int y=firstEmpty[x];y<BoardSize;y++) {
                int rank=y-firstEmpty[x];board.Cells[x,y]=board.Next();
                var tile=Spawn(x,y,board.Cells[x,y],false);
                tile.transform.localPosition=new Vector3(x-BoardHalf,BoardHalf+1.3f+rank*.98f,-.20f);
                tile.FallTo(Pos(x,y),0,true);
            }
            yield return WaitForRefillFalls();
            Notice("补位完成 · 检查连锁");
            yield return new WaitForSeconds(.08f);
        }
        IEnumerator WaitForRefillFalls()
        {
            while(true) {
                bool falling=false;
                foreach(var tile in tiles)if(tile!=null && tile.IsFalling){falling=true;break;}
                if(!falling)yield break;
                yield return null;
            }
        }
    }
}
