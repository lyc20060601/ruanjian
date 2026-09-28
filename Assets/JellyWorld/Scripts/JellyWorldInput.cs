using UnityEngine;
using UnityEngine.EventSystems;
namespace JellyWorldGame
{
    public partial class JellyWorld
    {
        JellyTile dragSource,queuedSource,queuedTarget;
        Vector2 dragOrigin;
        bool gestureConsumed,dragAwaiting,toolGesture;
        public bool HasBufferedSwap {get{return queuedSource!=null && queuedTarget!=null;}}
        bool LiveTile(JellyTile tile)
        {
            return tile!=null && tile.gameObject.activeInHierarchy && Inside(new Vector2Int(tile.x,tile.y)) && tiles[tile.x,tile.y]==tile;
        }
        JellyTile PointerTile()
        {
            Physics.SyncTransforms();
            RaycastHit hit;
            if(Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition),out hit,100))return hit.collider.GetComponent<JellyTile>();
            return null;
        }
        bool PointerOnBoard(out Vector2 point)
        {
            float distance;
            var plane=new Plane(stage.forward,stage.TransformPoint(Vector3.back*.6f));
            var ray=cam.ScreenPointToRay(Input.mousePosition);
            if(plane.Raycast(ray,out distance)) {
                var local=stage.InverseTransformPoint(ray.GetPoint(distance));point=new Vector2(local.x,local.y);return true;
            }
            point=Vector2.zero;return false;
        }
        bool PointerOverUI(){return EventSystem.current!=null && EventSystem.current.IsPointerOverGameObject();}
        public void ResetBoardGesture()
        {
            pointerDown=false;dragSource=null;queuedSource=null;queuedTarget=null;
            gestureConsumed=false;dragAwaiting=false;toolGesture=false;
        }
        public bool RequestDragSwap(JellyTile source,JellyTile destination)
        {
            if(mode!="play" || paused || activeTool>=0 || ChoosingLanding || !LiveTile(source) || !LiveTile(destination))return false;
            var a=new Vector2Int(source.x,source.y);var b=new Vector2Int(destination.x,destination.y);
            if(!JellyBoard.Adjacent(a,b))return false;
            if(busy || source.IsFalling || destination.IsFalling) {
                queuedSource=source;queuedTarget=destination;
            } else {
                queuedSource=null;queuedTarget=null;StartCoroutine(SwapRoutine(a,b));
            }
            return true;
        }
        void ExecuteBufferedSwap()
        {
            if(busy || !HasBufferedSwap)return;
            var a=queuedSource;var b=queuedTarget;
            if(a.IsFalling || b.IsFalling)return;
            queuedSource=null;queuedTarget=null;
            // Track actual pieces rather than old coordinates: deleted or separated pieces cancel the gesture.
            if(LiveTile(a) && LiveTile(b) && JellyBoard.Adjacent(new Vector2Int(a.x,a.y),new Vector2Int(b.x,b.y)))
                RequestDragSwap(a,b);
        }
        void ProcessBoardInput()
        {
            if(activeTool>=0 || ChoosingLanding){queuedSource=null;queuedTarget=null;}
            ExecuteBufferedSwap();
            if(Input.GetMouseButtonDown(0) && !PointerOverUI()) {
                dragSource=PointerTile();pointerDown=LiveTile(dragSource);
                gestureConsumed=false;dragAwaiting=false;toolGesture=activeTool>=0 || ChoosingLanding;
                PointerOnBoard(out dragOrigin);idle=0;
            }
            if(pointerDown && Input.GetMouseButton(0) && !toolGesture && activeTool<0 && !ChoosingLanding) {
                Vector2 point;
                if(!PointerOnBoard(out point))return;
                if(dragAwaiting) {
                    if(!busy && !HasBufferedSwap) {
                        dragAwaiting=false;dragOrigin=point;
                        dragSource=PointerTile();
                    }
                } else if(LiveTile(dragSource) && !PointerOverUI()) {
                    Vector2 delta=point-dragOrigin;
                    // Trigger while the button is held, not only after mouse-up.
                    if(delta.magnitude>=.30f) {
                        Vector2Int direction=Mathf.Abs(delta.x)>=Mathf.Abs(delta.y)
                            ?new Vector2Int(delta.x>0?1:-1,0):new Vector2Int(0,delta.y>0?1:-1);
                        var next=new Vector2Int(dragSource.x,dragSource.y)+direction;
                        if(Inside(next) && RequestDragSwap(dragSource,tiles[next.x,next.y])) {
                            gestureConsumed=true;dragAwaiting=true;dragOrigin=point;
                        }
                    }
                }
            }
            if(Input.GetMouseButtonUp(0)) {
                if(pointerDown && !gestureConsumed && !busy && !PointerOverUI()) {
                    var end=PointerTile();
                    if(LiveTile(end))ClickCell(new Vector2Int(end.x,end.y));
                }
                pointerDown=false;dragSource=null;dragAwaiting=false;toolGesture=false;
            } else if(pointerDown && !Input.GetMouseButton(0)) {
                pointerDown=false;dragSource=null;
            }
        }
        void OnApplicationFocus(bool focused){if(!focused)ResetBoardGesture();}
    }
}
