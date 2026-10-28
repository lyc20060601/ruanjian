using System.Collections.Generic;
using UnityEngine;

namespace JellyWorldGame
{
    public enum JellySpecial { None, Row, Column, Blast, Rainbow }

    public struct JellySpecialCreation
    {
        public Vector2Int cell;
        public JellySpecial kind;
        public JellySpecialCreation(Vector2Int cell,JellySpecial kind){this.cell=cell;this.kind=kind;}
    }

    public static class JellySpecialRules
    {
        // One special may be created per natural match. A swap favors the moved tile.
        public static JellySpecialCreation FindCreation(JellyBoard board,HashSet<Vector2Int> matches,Vector2Int preferred)
        {
            JellySpecialCreation best=new JellySpecialCreation(new Vector2Int(-1,-1),JellySpecial.None);
            int bestPriority=0;
            foreach(var cell in matches) {
                int horizontal=Run(board,cell,Vector2Int.right);
                int vertical=Run(board,cell,Vector2Int.up);
                JellySpecial kind=JellySpecial.None;
                if(horizontal>=5 || vertical>=5)kind=JellySpecial.Rainbow;
                else if(horizontal>=3 && vertical>=3)kind=JellySpecial.Blast;
                else if(horizontal>=4)kind=JellySpecial.Row;
                else if(vertical>=4)kind=JellySpecial.Column;
                int priority=kind==JellySpecial.Rainbow?4:kind==JellySpecial.Blast?3:kind==JellySpecial.None?0:2;
                if(priority>bestPriority || priority==bestPriority && priority>0 &&
                   (cell==preferred || best.cell!=preferred && (cell.y<best.cell.y || cell.y==best.cell.y && cell.x<best.cell.x))) {
                    best=new JellySpecialCreation(cell,kind);bestPriority=priority;
                }
            }
            return best;
        }

        static int Run(JellyBoard board,Vector2Int center,Vector2Int direction)
        {
            int count=1,color=board.Cells[center.x,center.y];
            foreach(int sign in new[]{-1,1}) {
                var cell=center+direction*sign;
                while(board.Contains(cell) && board.Cells[cell.x,cell.y]==color) {
                    count++;cell+=direction*sign;
                }
            }
            return count;
        }

        public static HashSet<Vector2Int> Expand(JellyBoard board,JellyTile[,] tiles,HashSet<Vector2Int> initial)
        {
            var result=new HashSet<Vector2Int>(initial);
            var queue=new Queue<Vector2Int>(initial);
            while(queue.Count>0) {
                var cell=queue.Dequeue();
                if(!board.Contains(cell))continue;
                var tile=tiles[cell.x,cell.y];
                if(tile==null || tile.special==JellySpecial.None)continue;
                if(tile.special==JellySpecial.Row || tile.special==JellySpecial.Column) {
                    for(int i=0;i<board.Width;i++)Add(board,result,queue,tile.special==JellySpecial.Row?new Vector2Int(i,cell.y):new Vector2Int(cell.x,i));
                } else if(tile.special==JellySpecial.Blast) {
                    for(int x=cell.x-1;x<=cell.x+1;x++)for(int y=cell.y-1;y<=cell.y+1;y++)Add(board,result,queue,new Vector2Int(x,y));
                } else if(tile.special==JellySpecial.Rainbow) {
                    for(int x=0;x<board.Width;x++)for(int y=0;y<board.Width;y++)Add(board,result,queue,new Vector2Int(x,y));
                }
            }
            return result;
        }

        static void Add(JellyBoard board,HashSet<Vector2Int> result,Queue<Vector2Int> queue,Vector2Int cell)
        {
            if(board.Contains(cell) && result.Add(cell))queue.Enqueue(cell);
        }
    }
}
