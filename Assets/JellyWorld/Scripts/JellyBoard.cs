using System;
using System.Collections.Generic;
using UnityEngine;

namespace JellyWorldGame
{
    // Pure board rules, kept independent from animation and input.
    public sealed class JellyBoard
    {
        public const int Size = 8, Colors = 5;
        public readonly int Width, ColorCount;
        public readonly int[,] Cells;
        readonly System.Random random;
        public JellyBoard(int seed,int width=Size,int colors=Colors) {
            if(width<3 || width>12 || colors<3 || colors>7)throw new ArgumentOutOfRangeException();
            Width=width;ColorCount=colors;Cells=new int[Width,Width];random=new System.Random(seed);Generate();
        }
        public int Next() { return random.Next(ColorCount); }
        public void Generate()
        {
            for (int attempt = 0; attempt < 1000; attempt++)
            {
                for (int y = 0; y < Width; y++)
                    for (int x = 0; x < Width; x++)
                    {
                        int c;
                        do { c = Next(); }
                        while ((x > 1 && Cells[x-1,y] == c && Cells[x-2,y] == c) ||
                               (y > 1 && Cells[x,y-1] == c && Cells[x,y-2] == c));
                        Cells[x,y] = c;
                    }
                Vector2Int a, b;
                if (FindMove(out a, out b)) return;
            }
            throw new InvalidOperationException("Could not generate a playable board.");
        }
        public static bool Inside(Vector2Int p) { return p.x >= 0 && p.x < Size && p.y >= 0 && p.y < Size; }
        public bool Contains(Vector2Int p) {return p.x>=0 && p.y>=0 && p.x<Width && p.y<Width;}
        public static bool Adjacent(Vector2Int a, Vector2Int b) { return Math.Abs(a.x-b.x)+Math.Abs(a.y-b.y)==1; }
        public void Swap(Vector2Int a, Vector2Int b)
        {
            int t=Cells[a.x,a.y]; Cells[a.x,a.y]=Cells[b.x,b.y]; Cells[b.x,b.y]=t;
        }
        public HashSet<Vector2Int> Matches()
        {
            var result = new HashSet<Vector2Int>();
            for(int y=0;y<Width;y++) for(int x=0;x<Width;x++)
            {
                int c=Cells[x,y]; if(c<0) continue;
                if(x+2<Width && Cells[x+1,y]==c && Cells[x+2,y]==c)
                    for(int i=x;i<Width && Cells[i,y]==c;i++) result.Add(new Vector2Int(i,y));
                if(y+2<Width && Cells[x,y+1]==c && Cells[x,y+2]==c)
                    for(int i=y;i<Width && Cells[x,i]==c;i++) result.Add(new Vector2Int(x,i));
            }
            return result;
        }
        public bool FindMove(out Vector2Int a, out Vector2Int b)
        {
            for(int y=0;y<Width;y++) for(int x=0;x<Width;x++)
            {
                a=new Vector2Int(x,y);
                for(int d=0;d<2;d++)
                {
                    b=a+(d==0?Vector2Int.right:Vector2Int.up);
                    if(!Contains(b) || Cells[a.x,a.y]==Cells[b.x,b.y]) continue;
                    Swap(a,b); bool valid=Matches().Count>0; Swap(a,b);
                    if(valid) return true;
                }
            }
            a=b=new Vector2Int(-1,-1); return false;
        }
    }
}
