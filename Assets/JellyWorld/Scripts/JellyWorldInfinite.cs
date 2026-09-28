using UnityEngine;
using UnityEngine.UI;
namespace JellyWorldGame
{
    public partial class JellyWorld
    {
        public bool InfiniteMode {get;private set;}
        RectTransform infiniteUI;
        Text infiniteBest;
        public void StartChallenge() {SaveInfiniteBest();InfiniteMode=false;level=1;NewGame();}
        public void StartInfinite() {SaveInfiniteBest();InfiniteMode=true;score=0;level=1;NewGame();}
        void SaveInfiniteBest()
        {
            if(!InfiniteMode || board==null)return;
            JellyProgress.RecordInfinite(score);
            int best=PlayerPrefs.GetInt("JellyWorld.InfiniteBest",0);
            if(score>best){PlayerPrefs.SetInt("JellyWorld.InfiniteBest",score);PlayerPrefs.Save();}
        }
        void OnApplicationQuit(){SaveInfiniteBest();}
        void BuildInfiniteUI(Transform root)
        {
            infiniteUI=Rect("Infinite playground page",root,Vector2.zero,new Vector2(1600,900));
            var card=Panel("Infinite options",infiniteUI,new Vector2(-422,0),new Vector2(560,430),new Color(1,.99f,.97f,.96f)).transform;
            Label("Infinite title",card,"无限模式",new Vector2(0,125),new Vector2(476,75),49,Ink);
            Label("Infinite description",card,"不限步数\n道具不限次数",new Vector2(0,21),new Vector2(476,93),25,Ink);
            Button("Enter infinite",card,"开始",new Vector2(0,-96),new Vector2(476,65),new Color(.22f,.57f,.55f),Color.white,StartInfinite,26);
            infiniteBest=Label("Infinite best",card,"",new Vector2(0,-163),new Vector2(476,34),18,Ink);
            Button("Infinite page back",infiniteUI,"返回",new Vector2(-648,357),new Vector2(160,48),new Color(1,1,1,.9f),Ink,ShowMenu,20);
            infiniteUI.gameObject.SetActive(false);
        }
        public void ShowInfinitePage()
        {
            ShowMenu();menuUI.gameObject.SetActive(false);infiniteUI.gameObject.SetActive(true);mode="infinite_lobby";
            infiniteBest.text="无限模式最高分   "+PlayerPrefs.GetInt("JellyWorld.InfiniteBest",0).ToString("N0");
        }
        public void ClearGuardianEffects(){ClearEffects();}
        public int ToolCharges(int index){return charges[index];}
    }
}
