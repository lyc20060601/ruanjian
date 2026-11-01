using UnityEngine;
using UnityEngine.UI;

namespace JellyWorldGame
{
    public partial class JellyWorld
    {
        RectTransform contextHelpUI;
        Text helpCaption;
        Button hintButton;
        JellyControlHint hoveredControl;
        string noticeText="";
        float noticeUntil;
        bool noticeAttention;

        void BuildContextHelp(Transform parent)
        {
            var panel=Card("Context help bar",parent,new Vector2(0,-410),new Vector2(1504,64),Ink);
            panel.raycastTarget=false;contextHelpUI=panel.rectTransform;
            helpCaption=Label("Help caption",contextHelpUI,"操作提示",new Vector2(-650,0),new Vector2(144,40),18,Color.white,TextAnchor.MiddleCenter,true);
            hintButton=Button("Hint",contextHelpUI,"显示提示",new Vector2(-650,0),new Vector2(144,44),Color.white,Ink,()=>Hint(),19);
            toast=Label("Instruction",contextHelpUI,"",new Vector2(-7,0),new Vector2(1090,50),19,Color.white,TextAnchor.MiddleLeft);
            moveRule=Label("Goal foot",contextHelpUI,"",new Vector2(651,0),new Vector2(150,40),17,new Color(.80f,.86f,.95f),TextAnchor.MiddleRight);
            RefreshContextHelp();
        }
        void ResetContextHints()
        {
            hoveredControl=null;noticeText="";noticeUntil=0;noticeAttention=false;
            RefreshContextHelp();
        }
        void Notice(string text,bool attention=false)
        {
            noticeText=text??"";noticeAttention=attention;noticeUntil=Time.unscaledTime+4.5f;
            RefreshContextHelp();
        }
        public void ShowControlHint(JellyControlHint source)
        {
            if(source==null || source.world!=this || !source.isActiveAndEnabled)return;
            hoveredControl=source;RefreshContextHelp();
        }
        public void ClearControlHint(JellyControlHint source)
        {
            if(hoveredControl!=source)return;
            hoveredControl=null;RefreshContextHelp();
        }
        void AttachControlHint(Button button,string message)
        {
            if(string.IsNullOrEmpty(message))return;
            var hint=button.GetComponent<JellyControlHint>();if(hint==null)hint=button.gameObject.AddComponent<JellyControlHint>();
            hint.world=this;hint.message=message;
        }
        void RefreshContextHelp()
        {
            if(contextHelpUI==null || toast==null)return;
            contextHelpUI.gameObject.SetActive(mode!="golem" && mode!="range");
            bool playing=mode=="play";
            hintButton.gameObject.SetActive(playing);helpCaption.gameObject.SetActive(!playing);
            hintButton.interactable=playing && !paused && !busy && !ChoosingLanding;
            moveRule.text=playing?(ChoosingLanding?"Esc 取消":paused?"Esc 继续":"Esc 暂停"):"";
            if(hoveredControl!=null && !hoveredControl.isActiveAndEnabled)hoveredControl=null;
            if(noticeText.Length>0 && Time.unscaledTime>=noticeUntil)noticeText="";
            string message;Color color=Color.white;
            if(noticeText.Length>0){message=(noticeAttention?"注意：":"")+noticeText;if(noticeAttention)color=new Color(1,.86f,.50f);}
            else if(hoveredControl!=null && !string.IsNullOrEmpty(hoveredControl.message)){message=hoveredControl.message;color=new Color(.79f,.94f,.94f);}
            else message=DefaultContextHelp();
            if(toast.text!=message)toast.text=message;
            toast.color=color;
        }
        string DefaultContextHelp()
        {
            if(mode=="menu")return "关卡模式有限步数；自由乐园不限步数；竞速模式比较达标用时。";
            if(mode=="race_lobby")return "选择目标后立即开始计时；达标前的消除与补位时间也计入成绩。";
            if(mode=="infinite_lobby")return "所有道具开放，不限步数；离开自由乐园时保存本局得分。";
            if(mode=="levels")return "点击已解锁关卡开始；锁定关卡需先完成上一关。";
            if(mode=="ranks")return "仅显示本机成绩；可在上方修改昵称，用左右按钮切换关卡。";
            if(mode=="result")return "本局已结束，请在成绩窗口选择下一步。";
            if(mode!="play")return "";
            if(paused)return RaceMode?"竞速计时已停止；继续游戏后恢复计时。":"棋盘已暂停；继续游戏后恢复操作。";
            if(ChoosingLanding)return "点击一颗果冻选择守护者落点；按 Esc 或再次点击守护者可取消。";
            if(activeTool==0)return "已选择果冻锤：点击要消除的果冻；再次点击果冻锤可取消。";
            if(activeTool==1)return "已选择十字星：点击中心果冻，消除整行整列；再次点击可取消。";
            if(ActingTool>=0)return poolNames[ActingTool]+"正在使用，请等待效果完成。";
            if(busy)return "正在消除和补位；可以提前拖动下一组相邻果冻。";
            if(RaceMode)return "拖动相邻果冻，凑成 3 颗同色；达到 "+RaceTarget.ToString("N0")+" 分即停表。";
            if(InfiniteMode)return "拖动或点击相邻果冻，凑成 3 颗同色；本模式不限步数。";
            return "拖动或点击相邻果冻，凑成 3 颗同色；在步数用完前完成右侧全部目标。";
        }
        string ButtonHelp(string name)
        {
            switch(name) {
                case "Continue journey":return "继续最近解锁的关卡，在限定步数内完成全部目标。";
                case "Start adventure":return "查看地图、目标和星星，选择已解锁关卡开始。";
                case "Infinite mode entry":return "不限步数，所有道具开放；离开时保存本局得分。";
                case "Race mode entry":return "选择 3000、6000 或 10000 分目标，竞速成绩按用时记录。";
                case "How to play":return "查看交换、连锁特效、通关与道具的操作说明。";
                case "Sound":return "调整音量、开关音效或减少特效。";
                case "Leaderboard":case "Level ranking":return "查看各关卡和自由乐园的本机历史前 6 次成绩。";
                case "Back home":return "返回主页前会提示确认；未完成的关卡或竞速不会保存。";
                case "Pause":return "暂停棋盘操作和竞速计时；也可以按 Esc。";
                case "Hint":return "标出一组可交换的果冻，不消耗步数。";
                case "Race restart":return "确认后从零开始计时，未完成的比赛不保存。";
                case "Enter infinite":return "进入自由乐园，不限步数，所有道具开放。";
                case "Previous rank":case "Next rank":return "切换所查看的关卡；自由乐园有独立成绩。";
                case "Reduced effects":return "减少碎片和页面淡入，保留操作与消除反馈。";
                case "Mute audio":return "开关所有游戏音效。";
                case "Preview audio":return "播放碎冰音效，试听当前音量。";
            }
            if(name.StartsWith("Race start "))return "点击后立即开始计时；达到 "+name.Substring(11)+" 分后结束。";
            if(name.StartsWith("Chapter tab "))return "切换章节，查看对应的关卡和目标。";
            return "";
        }
    }
}
