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
        static readonly string[] poolDetails={"消除一格","消除整行整列","重新排列棋盘","选择落点后操控踩踏","切换到靶场，射击消除"};
        void BuildToolPool(Transform parent)
        {
            var outer=Panel("Tool pool frame",parent,new Vector2(526,0),new Vector2(444,750),new Color(.45f,.70f,.69f));
            var tray=Panel("Tool pool",outer.transform,Vector2.zero,new Vector2(430,736),new Color(.96f,.97f,.94f)).transform;
            Label("Pool title",tray,"道具池",new Vector2(-94,322),new Vector2(184,44),29,Ink);
            Label("Pool count",tray,"5",new Vector2(176,322),new Vector2(45,40),22,Purple,TextAnchor.MiddleRight,true);
            for(int i=0;i<5;i++) {
                int index=i;float y=231-i*115;
                var card=Panel("Tool card "+i,tray,new Vector2(0,y),new Vector2(392,102),Color.white);
                poolCards[i]=card;
                var b=card.gameObject.AddComponent<Button>();b.targetGraphic=card;b.navigation=new Navigation{mode=Navigation.Mode.None};
                b.onClick.AddListener(()=>{Tone(700,.04f);SelectPoolTool(index);});
                var colors=b.colors;colors.highlightedColor=new Color(.90f,.98f,.96f);colors.pressedColor=new Color(.78f,.90f,.89f);b.colors=colors;
                var iconBg=Panel("Icon inset",card.transform,new Vector2(-143,0),new Vector2(86,86),new Color(.93f,.95f,.97f));
                var icon=Rect("Tool icon "+i,iconBg.transform,Vector2.zero,new Vector2(83,83)).gameObject.AddComponent<Image>();
                var feedback=card.gameObject.AddComponent<JellyButtonFeedback>();feedback.world=this;feedback.icon=icon.rectTransform;
                icon.sprite=toolIcons!=null && i<toolIcons.Length?toolIcons[i]:null;icon.preserveAspect=true;icon.raycastTarget=false;
                Label("Tool name "+i,card.transform,poolNames[i],new Vector2(6,22),new Vector2(188,36),23,Ink);
                Label("Tool detail "+i,card.transform,poolDetails[i],new Vector2(31,-22),new Vector2(238,30),16,new Color(.44f,.44f,.52f));
                var badge=Panel("Quantity badge",card.transform,new Vector2(155,23),new Vector2(53,32),new Color(.90f,.87f,.98f));
                poolCounts[i]=Label("Quantity "+i,badge.transform,"",Vector2.zero,new Vector2(51,31),20,Purple,TextAnchor.MiddleCenter,true);
            }
            toolText=Label("Tool state",tray,"选择道具",new Vector2(0,-329),new Vector2(386,59),16,Ink,TextAnchor.MiddleCenter);
        }
        public void SelectPoolTool(int index)
        {
            if(index<0 || index>4 || mode!="play" || busy || paused)return;
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
                poolCounts[i].text=InfiniteMode?"∞":count.ToString();
                bool chosen=i<2 && activeTool==i || i==3 && ChoosingLanding || ActingTool==i;
                poolCards[i].GetComponent<Button>().interactable=!busy && !paused && (InfiniteMode || count>0);
                poolCards[i].color=chosen?new Color(.79f,.96f,.88f):!InfiniteMode && count<=0?new Color(.91f,.91f,.92f):Color.white;
            }
            if(toolText!=null)toolText.text=ActingTool>=0?poolNames[ActingTool]+" · 使用中":ChoosingLanding?"点击棋盘选择落点":activeTool>=0?"点击棋盘使用 · 再点可取消":InfiniteMode?"选择道具":ObjectiveSummary();
        }
    }
}
