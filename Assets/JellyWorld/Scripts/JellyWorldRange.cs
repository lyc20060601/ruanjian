using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace JellyWorldGame
{
    public partial class JellyWorld
    {
        public JellySugarRange Range {get;private set;}
        public int rangeCharges=1;
        RectTransform rangeUI,rangeCrosshair;
        Text rangeAmmo,rangeHits,rangeHint;
        Image rangeFade;
        void BuildRangeUI(Transform root)
        {
            rangeUI=Rect("Sugar range HUD",root,Vector2.zero,new Vector2(1600,900));
            var heading=Panel("Range heading",rangeUI,new Vector2(-567,376),new Vector2(370,91),new Color(1,.98f,.95f,.96f)).transform;
            Label("Range title",heading,"糖弹靶场",new Vector2(0,15),new Vector2(325,42),29,Ink);
            Label("Range subtitle",heading,"命中方块即可消除",new Vector2(0,-23),new Vector2(325,26),17,Purple);
            var stats=Panel("Range counters",rangeUI,new Vector2(0,382),new Vector2(600,77),new Color(.19f,.22f,.34f,.96f)).transform;
            rangeAmmo=Label("Range ammo",stats,"",new Vector2(-144,0),new Vector2(263,52),28,new Color(.75f,1,.91f),TextAnchor.MiddleCenter);
            rangeHits=Label("Range hits",stats,"",new Vector2(143,0),new Vector2(263,52),24,Color.white,TextAnchor.MiddleCenter);
            Button("Exit shooting range",rangeUI,"返回棋盘",new Vector2(654,376),new Vector2(186,54),new Color(1,1,1,.95f),Ink,()=>{if(Range!=null)Range.End();},21);
            Button("Pause shooting range",rangeUI,"暂停",new Vector2(470,376),new Vector2(130,54),new Color(1,1,1,.95f),Ink,TogglePause,20);
            var bottom=Panel("Range controls",rangeUI,new Vector2(0,-392),new Vector2(920,62),new Color(.19f,.22f,.34f,.94f)).transform;
            rangeHint=Label("Range hint",bottom,"移动鼠标瞄准 · 按住左键发射 · Esc 暂停",Vector2.zero,new Vector2(880,50),22,Color.white,TextAnchor.MiddleCenter);
            rangeCrosshair=Rect("Range crosshair",rangeUI,Vector2.zero,new Vector2(30,30));
            for(int i=0;i<4;i++) {
                var p=i<2?new Vector2(i==0?-11:11,0):new Vector2(0,i==2?-11:11);
                var line=Panel("Crosshair segment",rangeCrosshair,p,i<2?new Vector2(8,3):new Vector2(3,8),Color.white);
                line.sprite=null;line.raycastTarget=false;
            }
            var dot=Panel("Crosshair center",rangeCrosshair,Vector2.zero,new Vector2(3,3),new Color(1,.8f,.35f));
            dot.raycastTarget=false;
            rangeUI.gameObject.SetActive(false);
            rangeFade=Panel("Scene transition",root,Vector2.zero,new Vector2(6000,4000),new Color(.18f,.16f,.26f,0));
            rangeFade.raycastTarget=false;rangeFade.gameObject.SetActive(false);
        }
        public void StartRange()
        {
            if(mode!="play" || busy || paused)return;
            if(!InfiniteMode && rangeCharges<=0){Notice("本局道具已用完");return;}
            CancelLandingChoice();Deselect();ClearHints();activeTool=-1;pointerDown=false;ClearEffects();
            if(!InfiniteMode)rangeCharges--;
            mode="range";busy=true;
            if(Range==null){Range=GetComponent<JellySugarRange>();if(Range==null)Range=gameObject.AddComponent<JellySugarRange>();}
            Range.Begin(this,cam,stage,decor);
        }
        public void SetRangeVisible(bool visible)
        {
            rangeUI.gameObject.SetActive(visible);gameUI.gameObject.SetActive(!visible);
        }
        public void SetRangeFade(float alpha)
        {
            rangeFade.gameObject.SetActive(alpha>0);
            rangeFade.color=new Color(.18f,.16f,.26f,alpha);rangeFade.raycastTarget=alpha>0;
        }
        public void SetRangeHUD(int ammo,int hits,int remaining,string hint=null)
        {
            rangeAmmo.text="糖弹  "+(InfiniteMode?"∞":ammo.ToString());
            rangeHits.text="命中 "+hits+" / "+(BoardSize*BoardSize);
            rangeHint.text=hint??(InfiniteMode?"移动鼠标瞄准 · 按住左键发射 · Esc 暂停":ObjectiveSummary()+" · 按住左键发射");
        }
        public void AimRangeUI(Vector2 screen,bool overUI)
        {
            Vector2 local;RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform,screen,null,out local);
            rangeCrosshair.anchoredPosition=local;rangeCrosshair.gameObject.SetActive(!overUI);
        }
        public GameObject CreateRangeTarget(Transform parent,int x,int y,Vector3 position)
        {
            var g=new GameObject("Target "+x+","+y);g.transform.SetParent(parent,false);
            g.transform.localPosition=position;g.transform.localScale=new Vector3(7.04f/BoardSize,5.92f/BoardSize,.68f);
            int color=board.Cells[x,y];
            Shape("Target jelly",g.transform,Vector3.zero,Vector3.one,candy[color]);Face(g.transform,color);
            bool frozen=frost!=null && frost[x,y];
            if(frozen)Shape("Target ice shell",g.transform,Vector3.zero,Vector3.one*1.10f,iceShell);
            g.AddComponent<BoxCollider>().size=Vector3.one;
            var target=g.AddComponent<JellyRangeTarget>();target.x=x;target.y=y;target.color=color;target.iced=frozen;return g;
        }
        public bool RemoveRangeCell(int x,int y)
        {
            if(mode!="range" || board.Cells[x,y]<0)return false;
            var tile=tiles[x,y];if(tile!=null){RegisterClear(x,y,tile.color);tile.gameObject.SetActive(false);Destroy(tile.gameObject);}
            tiles[x,y]=null;board.Cells[x,y]=-1;score+=60;return true;
        }
        public void RangePop(int color){PlaySound(JellySound.Shoot,.35f,1+color*.02f);}
        public void RangeHit(int color){PlaySound(JellySound.Pop,.50f,1+color*.02f);}
        public void CompleteRange()
        {
            rangeUI.gameObject.SetActive(false);gameUI.gameObject.SetActive(true);SetRangeFade(0);
            mode="play";busy=true;UpdateHUD();StartCoroutine(AfterGuardian());
        }
        void CancelRange()
        {
            if(Range!=null)Range.Cancel();
            if(rangeUI!=null)rangeUI.gameObject.SetActive(false);
            if(rangeFade!=null)SetRangeFade(0);
        }
    }
}
