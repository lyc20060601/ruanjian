using UnityEngine;

namespace JellyWorldGame
{
    public static class JellyJourney
    {
        static readonly int[] UnlockLevels = { 2, 4, 3, 6, 8 };
        static readonly int[] RankStars = { 0, 3, 9, 18, 27, 36 };
        public static readonly string[] ToolNames = { "果冻锤", "十字星", "洗牌", "守护者", "糖弹靶场" };
        public static readonly string[] ChapterNames = { "云朵小径", "蜜桃群岛", "星糖花园" };
        public static readonly string[] ChapterNotes = { "从一次轻轻的交换开始", "巧用道具，寻找自己的节奏", "让每一步，都离目标更近" };
        public static readonly string[] RankNames = { "初来旅人", "糖果学徒", "软糖探索家", "群岛漫游者", "星糖收藏家", "果冻大师" };
        public static int ToolUnlockLevel(int index) { return index>=0 && index<UnlockLevels.Length ? UnlockLevels[index] : int.MaxValue; }
        public static bool CanUseTool(int index,int unlockedLevel,bool infinite)
        { return index>=0 && index<UnlockLevels.Length && (infinite || unlockedLevel>=UnlockLevels[index]); }
        public static int RankForStars(int stars)
        { int rank=0;for(int i=1;i<RankStars.Length;i++)if(stars>=RankStars[i])rank=i;return rank; }
        public static int TotalStars { get { int stars=0;for(int i=1;i<=JellyLevels.All.Length;i++)stars+=JellyProgress.Stars(i);return stars; } }
        public static string RankName { get { return RankNames[RankForStars(TotalStars)]; } }
        public static string GrowthNote {
            get {int stars=TotalStars,rank=RankForStars(stars);return rank==RankNames.Length-1?"所有星星，都已收藏":"再收集 "+(RankStars[rank+1]-stars)+" 颗星 · "+RankNames[rank+1];}
        }
        public static string NextUnlockNote {
            get {
                int next=-1;
                for(int i=0;i<UnlockLevels.Length;i++)if(UnlockLevels[i]>JellyProgress.Unlocked && (next<0 || UnlockLevels[i]<UnlockLevels[next]))next=i;
                return next<0?"全部道具已解锁 · 每次开局补满":"通过第 "+(UnlockLevels[next]-1)+" 关，获得"+ToolNames[next];
            }
        }
        public static string UnlockMessage(int before,int after)
        {
            string result="";
            for(int i=0;i<UnlockLevels.Length;i++)if(before<UnlockLevels[i] && after>=UnlockLevels[i])result+=(result.Length>0?" · ":"")+ToolNames[i];
            return result.Length==0?"":"新道具已解锁："+result;
        }
    }
}
