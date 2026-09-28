using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JellyWorldGame
{
    public partial class JellyWorld
    {
        public Mesh toolStarMesh,toolArrowMesh;
        public Material toolGlass,toolBeam;
        public int ActingTool {get;private set;}=-1;
        Transform ToolAnchor(string name)
        {
            var g=new GameObject(name).transform;g.SetParent(effects,false);g.position=stage.position;g.rotation=stage.rotation;return g;
        }
        void BeginToolAction(int tool)
        {
            ActingTool=tool;busy=true;Deselect();ClearHints();pointerDown=false;UpdateHUD();
        }
        void UpdateToolPreview()
        {
            ClearHints();Vector2Int p=Hit();if(!Inside(p))return;
            if(activeTool==0)tiles[p.x,p.y].hinted=true;
            else if(activeTool==1)for(int i=0;i<BoardSize;i++){tiles[i,p.y].hinted=true;tiles[p.x,i].hinted=true;}
        }
        IEnumerator RetireTool(Transform anchor,Transform model,float seconds)
        {
            Vector3 start=model.localPosition,size=model.localScale;
            for(float t=0;t<seconds;t+=Time.deltaTime) {
                float u=t/seconds;model.localPosition=start+new Vector3(0,u*.55f,-u*.9f);
                model.localScale=size*(1-Mathf.SmoothStep(0,1,u));yield return null;
            }
            if(anchor!=null)Destroy(anchor.gameObject);
        }
        IEnumerator UseTargetTool(int tool,Vector2Int cell)
        {
            BeginToolAction(tool);
            var anchor=ToolAnchor("Tool action "+tool);var model=JellyToolModels.Create(this,tool,anchor);
            Vector3 point=Pos(cell.x,cell.y);
            if(tool==0) {
                Notice("果冻锤");
                Vector3 raised=point+new Vector3(.25f,2.0f,-3.0f);
                raised.y=Mathf.Min(raised.y,BoardHalf+.5f);
                Vector3 contact=point+Vector3.back*(IsIced(cell.x,cell.y)?1.07f:.99f);
                Quaternion endRotation=Quaternion.AngleAxis(-12,Vector3.forward)*Quaternion.Euler(0,-90,0);
                model.localPosition=raised;model.localRotation=Quaternion.Euler(-25,-35,-32);
                for(float t=0;t<.08f;t+=Time.deltaTime) {
                    model.localScale=Vector3.one*.74f*Mathf.SmoothStep(0,1,t/.08f);yield return null;
                }
                model.localScale=Vector3.one*.74f;
                for(float t=0;t<.07f;t+=Time.deltaTime) {
                    float u=t/.07f;model.localPosition=raised+new Vector3(0,.25f*u,-.15f*u);
                    model.localRotation=Quaternion.Euler(-25-10*u,-35,-32-8*u);yield return null;
                }
                Vector3 from=model.localPosition;Quaternion rotation=model.localRotation;
                for(float t=0;t<.14f;t+=Time.deltaTime) {
                    float u=t/.14f;u*=u;
                    model.localPosition=Vector3.Lerp(from,contact,u);model.localRotation=Quaternion.Slerp(rotation,endRotation,u);yield return null;
                }
                model.localPosition=contact;model.localRotation=endRotation;
                GuardianImpact(true);
                JellyVfx.Ring(this,effects,stage.TransformPoint(point+Vector3.back*.45f),-stage.forward,.53f,.28f,new Color(1,.82f,.93f,.65f));
                StartCoroutine(RetireTool(anchor,model,.18f));
            } else {
                Notice("十字星");
                model.localPosition=point+Vector3.back*1.4f;
                for(float t=0;t<.18f;t+=Time.deltaTime) {
                    float u=t/.18f;model.localScale=Vector3.one*Mathf.Lerp(.12f,1.1f,Mathf.SmoothStep(0,1,u));
                    model.localRotation=Quaternion.Euler(0,0,180*(1-u));yield return null;
                }
                model.localRotation=Quaternion.identity;
                var beams=new Renderer[4];
                Vector3[] dirs={Vector3.right,Vector3.left,Vector3.up,Vector3.down};
                float[] distances={BoardHalf+.45f-point.x,point.x+BoardHalf+.45f,BoardHalf+.45f-point.y,point.y+BoardHalf+.45f};
                for(int i=0;i<4;i++) {
                    var g=GameObject.CreatePrimitive(PrimitiveType.Quad);g.name="Cross light beam";g.transform.SetParent(anchor,false);
                    Destroy(g.GetComponent<Collider>());beams[i]=g.GetComponent<Renderer>();beams[i].sharedMaterial=toolBeam;
                    beams[i].shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                    g.transform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(dirs[i].y,dirs[i].x)*Mathf.Rad2Deg);
                }
                for(float t=0;t<.14f;t+=Time.deltaTime) {
                    float u=Mathf.Clamp01(t/.10f);
                    for(int i=0;i<4;i++) {
                        float length=distances[i]*u;
                        beams[i].transform.localPosition=point+Vector3.back*.73f+dirs[i]*length*.5f;
                        beams[i].transform.localScale=new Vector3(length,.68f,1);
                    }
                    model.localScale=Vector3.one*(1.1f+.12f*Mathf.Sin(u*Mathf.PI));yield return null;
                }
                PlaySound(JellySound.Beam,.40f);StartCoroutine(FadeCross(anchor,model,beams));
            }
            var cells=new HashSet<Vector2Int>();
            if(tool==0)cells.Add(cell);else for(int i=0;i<BoardSize;i++){cells.Add(new Vector2Int(i,cell.y));cells.Add(new Vector2Int(cell.x,i));}
            ActingTool=-1;
            yield return Resolve(cells,false);
        }
        IEnumerator FadeCross(Transform anchor,Transform model,Renderer[] beams)
        {
            var block=new MaterialPropertyBlock();
            for(float t=0;t<.18f;t+=Time.deltaTime) {
                float u=t/.18f;block.SetFloat("_Opacity",1-u);
                foreach(var beam in beams)if(beam!=null)beam.SetPropertyBlock(block);
                model.localScale=Vector3.one*1.1f*(1-u);yield return null;
            }
            Destroy(anchor.gameObject);
        }
        IEnumerator ShuffleAction(bool paid)
        {
            BeginToolAction(2);activeTool=-1;Notice("洗牌");
            var anchor=ToolAnchor("Shuffle action");var model=JellyToolModels.Create(this,2,anchor);model.localPosition=new Vector3(0,0,-2.1f);
            var pieces=new JellyTile[BoardSize*BoardSize];var from=new Vector3[BoardSize*BoardSize];var gathered=new Vector3[BoardSize*BoardSize];
            for(int y=0;y<BoardSize;y++)for(int x=0;x<BoardSize;x++) {
                int i=x+y*BoardSize;var tile=tiles[x,y];pieces[i]=tile;from[i]=tile.transform.localPosition;
                tile.enabled=false;tile.selected=false;tile.hinted=false;
                float angle=i*2.39996f;float radius=.32f+Mathf.Sqrt(i/(float)(BoardSize*BoardSize))*1.20f;
                gathered[i]=new Vector3(Mathf.Cos(angle)*radius,Mathf.Sin(angle)*radius,-.65f-i*.007f);
            }
            for(float t=0;t<.25f;t+=Time.deltaTime) {
                float u=Mathf.SmoothStep(0,1,t/.25f);model.localScale=Vector3.one*u;
                for(int i=0;i<BoardSize*BoardSize;i++){pieces[i].transform.localPosition=Vector3.Lerp(from[i],gathered[i],u);pieces[i].transform.localScale=Vector3.one*Mathf.Lerp(.88f,.21f,u);}
                yield return null;
            }
            for(float t=0;t<.18f;t+=Time.deltaTime) {
                float u=t/.18f;model.localRotation=Quaternion.Euler(0,0,-u*230);
                for(int i=0;i<BoardSize*BoardSize;i++)pieces[i].transform.localPosition=Quaternion.Euler(0,0,u*240)*gathered[i];
                yield return null;
            }
            int[] order=new int[BoardSize*BoardSize];for(int i=0;i<BoardSize*BoardSize;i++)order[i]=i;
            var rng=new System.Random(System.Environment.TickCount);
            bool valid=false;
            for(int attempt=0;attempt<1024;attempt++) {
                for(int i=BoardSize*BoardSize-1;i>0;i--){int j=rng.Next(i+1);int temp=order[i];order[i]=order[j];order[j]=temp;}
                for(int i=0;i<BoardSize*BoardSize;i++)board.Cells[i%BoardSize,i/BoardSize]=pieces[order[i]].color;
                Vector2Int a,b;
                if(board.Matches().Count==0 && board.FindMove(out a,out b)){valid=true;break;}
            }
            if(!valid) {
                // Extremely skewed boards retain the existing safe-generation fallback.
                board.Generate();
                for(int i=0;i<BoardSize*BoardSize;i++) {
                    var old=pieces[order[i]];old.gameObject.SetActive(false);Destroy(old.gameObject);
                    pieces[order[i]]=Spawn(i%BoardSize,i/BoardSize,board.Cells[i%BoardSize,i/BoardSize],false);pieces[order[i]].enabled=false;
                    pieces[order[i]].transform.localPosition=gathered[i];
                }
            }
            var starts=new Vector3[BoardSize*BoardSize];
            for(int i=0;i<BoardSize*BoardSize;i++) {
                int x=i%BoardSize,y=i/BoardSize;var tile=pieces[order[i]];tiles[x,y]=tile;tile.x=x;tile.y=y;
                tile.target=Pos(x,y);starts[i]=tile.transform.localPosition;
            }
            PlaySound(JellySound.Shuffle,.48f);
            for(float t=0;t<.30f;t+=Time.deltaTime) {
                float u=Mathf.SmoothStep(0,1,t/.30f);model.localScale=Vector3.one*(1-u);
                for(int i=0;i<BoardSize*BoardSize;i++){var tile=tiles[i%BoardSize,i/BoardSize];tile.transform.localPosition=Vector3.Lerp(starts[i],tile.target,u);tile.transform.localScale=Vector3.one*Mathf.Lerp(.21f,.88f,u);}
                yield return null;
            }
            for(int i=0;i<BoardSize*BoardSize;i++) {
                var tile=tiles[i%BoardSize,i/BoardSize];tile.transform.localPosition=tile.target;tile.transform.localRotation=Quaternion.identity;
                tile.born=Time.time-2;tile.enabled=true;
            }
            Destroy(anchor.gameObject);ActingTool=-1;busy=false;idle=0;Notice("");UpdateHUD();
        }
        public IEnumerator PlayToolManifest(int tool,Vector2Int cell)
        {
            ActingTool=tool;RefreshToolPool();
            var anchor=ToolAnchor("Tool manifestation");var model=JellyToolModels.Create(this,tool,anchor);
            Vector3 at=tool==3?Pos(cell.x,cell.y)+Vector3.back*1.5f:new Vector3(0,0,-2.2f);
            Quaternion turn=tool==4?Quaternion.Euler(-12,-130,-12):Quaternion.identity;
            for(float t=0;t<.20f;t+=Time.deltaTime) {
                float u=t/.20f;
                model.localPosition=at+new Vector3(0,Mathf.Sin(u*Mathf.PI)*.28f,0);
                model.localScale=Vector3.one*Mathf.SmoothStep(0,1,Mathf.Clamp01(u*4));
                model.localRotation=turn*Quaternion.Euler(0,Mathf.Sin(u*Mathf.PI)*18,0);yield return null;
            }
            for(float t=0;t<.09f;t+=Time.deltaTime){model.localScale=Vector3.one*(1-t/.09f);yield return null;}
            Destroy(anchor.gameObject);ActingTool=-1;RefreshToolPool();
        }
        public void GuardianWarmupUI(bool on){gameUI.gameObject.SetActive(on);guardianUI.gameObject.SetActive(!on);}
    }
}
