using UnityEngine;
using UnityEngine.UI;
namespace JellyWorldGame
{
    public partial class JellyWorld
    {
        public Sprite[] toolIcons=new Sprite[5];
        readonly Image[] poolCards=new Image[5];
        readonly Text[] poolCounts=new Text[5];
        static readonly string[] poolNames={"果冻锤","十字星","洗牌","守护者","糖弹靶场"};
        static readonly string[] poolDetails={"消除一格","消除整行整列","重新排列棋盘","选择落点，操控踩踏","进入靶场，射击消除"};
        void BuildToolPool(Transform parent)
        {
            toolPoolLayout=Rect("Tool pool",parent,new Vector2(603,-137),new Vector2(302,488));
            var tray=toolPoolLayout;
            Label("Pool title",tray,"旅途道具",new Vector2(0,209),new Vector2(254,38),23,Ink,TextAnchor.MiddleLeft,true);
            for(int i=0;i<5;i++) {
                int index=i;float y=144-i*76;
                var card=Panel("Tool card "+i,tray,new Vector2(0,y),new Vector2(270,68),Color.white);poolCards[i]=card;
                if(uiButtonMaterial!=null)card.material=uiButtonMaterial;
                var surface=card.gameObject.AddComponent<JellyUISurface>();surface.pressTravel=2;surface.style=JellyUISurface.SurfaceStyle.Choice;
                var button=card.gameObject.AddComponent<Button>();button.targetGraphic=card;var colors=button.colors;colors.disabledColor=Color.white;colors.highlightedColor=Color.white;button.colors=colors;button.navigation=new Navigation{mode=Navigation.Mode.None};
                button.onClick.AddListener(()=>{PlaySound(JellySound.Click,.45f);SelectPoolTool(index);});
                var icon=Rect("Tool icon "+i,card.transform,new Vector2(-97,0),new Vector2(59,59)).gameObject.AddComponent<Image>();
                icon.sprite=toolIcons!=null && i<toolIcons.Length?toolIcons[i]:null;icon.preserveAspect=true;icon.raycastTarget=false;
                var feedback=card.gameObject.AddComponent<JellyButtonFeedback>();feedback.world=this;feedback.icon=icon.rectTransform;
                Label("Tool name "+i,card.transform,poolNames[i],new Vector2(12,14),new Vector2(135,27),19,Ink);
                Label("Tool detail "+i,card.transform,"",new Vector2(32,-17),new Vector2(173,25),15,new Color(.38f,.42f,.54f));
                poolCounts[i]=Label("Quantity "+i,card.transform,"",new Vector2(107,14),new Vector2(39,28),17,new Color(.24f,.40f,.65f),TextAnchor.MiddleRight,true);
            }
            toolText=Label("Tool state",tray,"",new Vector2(0,-212),new Vector2(266,44),15,Ink,TextAnchor.MiddleCenter);
        }
        public void SelectPoolTool(int index)
        {
            if(index<0 || index>4 || mode!="play" || busy || paused || !RequireTool(index))return;
            if(index<3)ChooseTool(index);
            else if(index==3)SummonGuardian();
            else StartRange();
            RefreshToolPool();
        }
        void RefreshToolPool()
        {
            for(int i=0;i<5;i++) {
                if(poolCounts[i]==null)continue;
                int count=i<3?charges[i]:i==3?guardianCharges:rangeCharges;
                bool unlocked=IsToolUnlocked(i);
                poolCards[i].GetComponent<JellyUISurface>().SetSubdued(!unlocked || !InfiniteMode && count<=0);
                poolCounts[i].text=!unlocked?"—":InfiniteMode?"∞":count.ToString();
                var detail=poolCards[i].transform.Find("Tool detail "+i).GetComponent<Text>();
                detail.text=unlocked?poolDetails[i]:"通过第 "+(JellyJourney.ToolUnlockLevel(i)-1)+" 关解锁";
                poolCards[i].transform.Find("Tool icon "+i).GetComponent<Image>().color=unlocked?Color.white:new Color(1,1,1,.35f);
                bool chosen=i<2 && activeTool==i || i==3 && ChoosingLanding || ActingTool==i;
                poolCards[i].GetComponent<JellyUISurface>().SetSelected(chosen);
                poolCards[i].GetComponent<Button>().interactable=unlocked && !busy && !paused && (InfiniteMode || count>0);
                poolCards[i].color=chosen?new Color(.65f,.86f,1):!unlocked || !InfiniteMode && count<=0?new Color(.95f,.92f,.99f):Color.Lerp(candy[i].color,Color.white,.86f);
            }
            if(toolText!=null)toolText.text=ActingTool>=0?poolNames[ActingTool]+" · 使用中":ChoosingLanding?"点击棋盘选择落点":activeTool>=0?"点击棋盘使用 · 再点可取消":InfiniteMode?"全道具开放 · 自由探索":"每局补满 · 使用不消耗步数";
        }
    }
}
