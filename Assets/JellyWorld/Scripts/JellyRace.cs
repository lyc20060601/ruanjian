using System;
using System.Globalization;
using UnityEngine;

namespace JellyWorldGame
{
    /// <summary>A monotonic race clock. Pauses and result screens never add time.</summary>
    public sealed class JellyRaceClock
    {
        double last;
        public double Seconds { get; private set; }
        public bool Running { get; private set; }
        public bool Paused { get; private set; }
        public bool Completed { get; private set; }

        public void Start(double now)
        {
            last=now;Seconds=0;Running=true;Paused=false;Completed=false;
        }
        public void Tick(double now)
        {
            if(double.IsNaN(now) || double.IsInfinity(now))return;
            double next=Math.Max(last,now);
            if(Running && !Paused)Seconds+=next-last;
            last=next;
        }
        public void SetPaused(bool value,double now) { Tick(now);Paused=value; }
        public bool Complete(double now)
        {
            if(!Running)return false;
            Tick(now);Running=false;Completed=true;return true;
        }
        public void Cancel() { Running=false; }
    }

    public static class JellyRace
    {
        public static readonly int[] Targets={3000,6000,10000};
        public static bool ValidTarget(int target) { return Array.IndexOf(Targets,target)>=0; }
        public static string BestKey(int target) { return "JellyWorld.Race.BestMilliseconds."+target; }
        public static int BestMilliseconds(int target) { return Math.Max(0,PlayerPrefs.GetInt(BestKey(target),0)); }
        public static bool Record(int target,double seconds)
        {
            if(!ValidTarget(target) || seconds<=0 || double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds>=int.MaxValue/1000d)
                return false;
            int milliseconds=Math.Max(1,(int)Math.Ceiling(seconds*1000));
            int best=BestMilliseconds(target);
            if(best>0 && milliseconds>=best)return false;
            PlayerPrefs.SetInt(BestKey(target),milliseconds);PlayerPrefs.Save();return true;
        }
        public static string FormatTime(double seconds,bool precise=false)
        {
            if(double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds<0)seconds=0;
            long milliseconds=(long)(precise?Math.Ceiling(Math.Min(seconds,2147483)*1000):Math.Floor(Math.Min(seconds,2147483)*1000));
            return FormatMilliseconds(milliseconds,precise);
        }
        static string FormatMilliseconds(long milliseconds,bool precise)
        {
            return (milliseconds/60000).ToString("00",CultureInfo.InvariantCulture)+":"+
                (milliseconds/1000%60).ToString("00",CultureInfo.InvariantCulture)+"."+
                (precise?(milliseconds%1000).ToString("000",CultureInfo.InvariantCulture):(milliseconds/100%10).ToString(CultureInfo.InvariantCulture));
        }
        public static string BestLabel(int target)
        {
            int best=BestMilliseconds(target);
            return best>0?FormatMilliseconds(best,true):"等待你的首个纪录";
        }
    }
}
