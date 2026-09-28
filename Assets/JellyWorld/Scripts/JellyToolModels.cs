using UnityEngine;
using UnityEngine.Rendering;

namespace JellyWorldGame
{
    public static class JellyToolModels
    {
        static Transform Group(string name,Transform parent)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);return go.transform;
        }
        static GameObject Part(JellyWorld w,string name,Transform root,Vector3 at,Vector3 scale,Material mat,bool sphere=false)
        {
            var g=w.Shape(name,root,at,scale,mat,sphere);
            g.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
            return g;
        }
        static GameObject Cylinder(string name,Transform parent,Vector3 at,Vector3 scale,Material mat,Vector3 rotation)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name=name;g.transform.SetParent(parent,false);
            g.transform.localPosition=at;g.transform.localScale=scale;g.transform.localEulerAngles=rotation;
            g.GetComponent<Renderer>().sharedMaterial=mat;g.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
            if(Application.isPlaying)Object.Destroy(g.GetComponent<Collider>());else Object.DestroyImmediate(g.GetComponent<Collider>());
            return g;
        }
        public static Transform Create(JellyWorld w,int type,Transform parent)
        {
            var root=Group(new[]{"实体果冻锤","实体十字星","实体洗牌环","守护者徽章","实体糖弹发射器"}[type],parent);
            if(type==0)Hammer(w,root);
            else if(type==1)Cross(w,root);
            else if(type==2)Shuffle(w,root);
            else if(type==3)Guardian(w,root);
            else Launcher(w,root);
            return root;
        }
        static void Star(JellyWorld w,Transform parent,Vector3 at,Vector3 size,Material material)
        {
            var g=Part(w,"Candy star",parent,at,size,material);
            g.GetComponent<MeshFilter>().sharedMesh=w.toolStarMesh!=null?w.toolStarMesh:w.fractureMesh;
        }
        static void Hammer(JellyWorld w,Transform root)
        {
            Cylinder("Cream handle",root,new Vector3(0,-.80f,0),new Vector3(.25f,.55f,.25f),w.cream,Vector3.zero);
            Cylinder("Gold handle collar",root,new Vector3(0,-.40f,0),new Vector3(.38f,.10f,.38f),w.gold,Vector3.zero);
            Part(w,"Pink handle cap",root,new Vector3(0,-1.34f,0),new Vector3(.35f,.18f,.35f),w.candy[0],true);
            Cylinder("Pink jelly hammer head",root,Vector3.zero,new Vector3(.94f,.65f,.94f),w.candy[0],new Vector3(0,0,90));
            for(int s=-1;s<=1;s+=2) {
                Part(w,"Rounded mallet end",root,new Vector3(s*.66f,0,0),new Vector3(.25f,1.04f,1.04f),w.candy[0],true);
                Cylinder("Hammer end band",root,new Vector3(s*.55f,0,0),new Vector3(1.00f,.055f,1.00f),w.blush,new Vector3(0,0,90));
            }
            Star(w,root,new Vector3(0,0,-.505f),new Vector3(.42f,.42f,.08f),w.gold);
            Star(w,root,new Vector3(0,0,-.556f),new Vector3(.32f,.32f,.07f),w.mint);
            Part(w,"Hammer gloss",root,new Vector3(-.30f,.27f,-.37f),new Vector3(.34f,.08f,.045f),w.white,true);
        }
        static void Cross(JellyWorld w,Transform root)
        {
            for(int i=0;i<4;i++) {
                float a=i*Mathf.PI*.5f;Vector3 p=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0)*.75f;
                Part(w,"Pink candy ray",root,p,new Vector3(i%2==0?.75f:.29f,i%2==0?.29f:.75f,.30f),w.candy[0]);
            }
            Star(w,root,Vector3.zero,new Vector3(1.65f,1.65f,.27f),w.gold);
            Star(w,root,new Vector3(0,0,-.16f),new Vector3(1.45f,1.45f,.19f),w.candy[3]);
            Star(w,root,new Vector3(0,0,-.285f),new Vector3(.70f,.70f,.15f),w.mint);
            Part(w,"Star highlight",root,new Vector3(-.22f,.24f,-.38f),new Vector3(.15f,.07f,.03f),w.white,true);
        }
        static void Shuffle(JellyWorld w,Transform root)
        {
            for(int i=0;i<5;i++) {
                float a=(i*72+18)*Mathf.Deg2Rad;
                var p=new Vector3(Mathf.Cos(a)*.49f,Mathf.Sin(a)*.49f,-.08f);
                var g=Part(w,"Shuffle jelly",root,p,Vector3.one*.48f,w.candy[i]);
                g.transform.localRotation=Quaternion.Euler(15,-18,i*13-20);
            }
            for(int side=0;side<2;side++) {
                var g=Group("Curved shuffle arrow",root);
                var line=g.gameObject.AddComponent<LineRenderer>();line.sharedMaterial=side==0?w.mint:w.candy[4];
                line.useWorldSpace=false;line.positionCount=34;line.widthMultiplier=.12f;line.numCornerVertices=3;
                for(int i=0;i<34;i++){float a=(side*180+15+i*4)*Mathf.Deg2Rad;line.SetPosition(i,new Vector3(Mathf.Cos(a),Mathf.Sin(a),-.13f)*1.03f);}
                float end=(side*180+147)*Mathf.Deg2Rad;
                var arrow=Part(w,"Shuffle arrow tip",root,new Vector3(Mathf.Cos(end),Mathf.Sin(end),-.14f)*1.03f,new Vector3(.34f,.34f,.12f),side==0?w.mint:w.candy[4]);
                arrow.GetComponent<MeshFilter>().sharedMesh=w.toolArrowMesh;arrow.transform.localRotation=Quaternion.Euler(0,0,end*Mathf.Rad2Deg+90);
            }
        }
        static void Guardian(JellyWorld w,Transform root)
        {
            Part(w,"Gold emblem",root,Vector3.zero,new Vector3(1.15f,1.28f,.24f),w.gold);
            Part(w,"Lavender helmet",root,new Vector3(0,.05f,-.17f),new Vector3(.96f,.97f,.42f),w.candy[4]);
            Part(w,"Visor",root,new Vector3(0,-.02f,-.41f),new Vector3(.66f,.32f,.10f),w.plum);
            for(int s=-1;s<=1;s+=2) {
                Part(w,"Mint eyes",root,new Vector3(s*.19f,-.01f,-.48f),new Vector3(.13f,.11f,.04f),w.rune,true);
                Part(w,"Cyan temple",root,new Vector3(s*.48f,.03f,-.19f),new Vector3(.24f,.42f,.32f),w.candy[1]);
            }
            var crystal=Part(w,"Crest crystal",root,new Vector3(0,.62f,-.22f),new Vector3(.30f,.60f,.27f),w.rune);
            crystal.GetComponent<MeshFilter>().sharedMesh=w.fractureMesh;
        }
        static void Launcher(JellyWorld w,Transform root)
        {
            Cylinder("Mint candy barrel",root,Vector3.zero,new Vector3(.82f,.52f,.82f),w.mint,new Vector3(90,0,0));
            for(int s=-1;s<=1;s+=2)
                Cylinder("Pink barrel band",root,new Vector3(0,0,s*.39f),new Vector3(.91f,.065f,.91f),w.candy[0],new Vector3(90,0,0));
            Cylinder("Nozzle neck",root,new Vector3(0,0,.61f),new Vector3(.60f,.18f,.60f),w.candy[4],new Vector3(90,0,0));
            Cylinder("Nozzle interior",root,new Vector3(0,0,.83f),new Vector3(.69f,.015f,.69f),w.plum,new Vector3(90,0,0));
            var rim=Group("Round lavender nozzle",root);rim.localPosition=new Vector3(0,0,.87f);
            var line=rim.gameObject.AddComponent<LineRenderer>();line.sharedMaterial=w.candy[4];line.useWorldSpace=false;
            line.loop=true;line.positionCount=48;line.widthMultiplier=.16f;line.numCornerVertices=3;
            for(int i=0;i<48;i++){float a=i*Mathf.PI*2/48;line.SetPosition(i,new Vector3(Mathf.Cos(a),Mathf.Sin(a),0)*.36f);}
            var grip=Part(w,"Cream grip",root,new Vector3(0,-.65f,-.30f),new Vector3(.38f,.75f,.38f),w.cream);
            grip.transform.localRotation=Quaternion.Euler(-15,0,0);
            Part(w,"Pink grip inset",root,new Vector3(0,-.64f,-.51f),new Vector3(.29f,.57f,.10f),w.candy[0]);
            for(int i=0;i<3;i++)Part(w,"Grip ribs",root,new Vector3(0,-.49f-i*.16f,-.55f),new Vector3(.32f,.065f,.07f),w.blush);
            Part(w,"Glass candy capsule",root,new Vector3(0,.70f,-.29f),new Vector3(.72f,.76f,.72f),w.toolGlass!=null?w.toolGlass:w.white,true);
            for(int i=0;i<5;i++) {
                float a=i*Mathf.PI*2/5;
                Part(w,"Loaded sugar ball",root,new Vector3(Mathf.Cos(a)*.20f,.67f+Mathf.Sin(a)*.19f,-.33f+((i%2)*.18f)),Vector3.one*.27f,w.candy[i],true);
            }
            Cylinder("Capsule pink base",root,new Vector3(0,.39f,-.29f),new Vector3(.70f,.06f,.70f),w.candy[0],Vector3.zero);
            Part(w,"Capsule pink lid",root,new Vector3(0,1.02f,-.29f),new Vector3(.60f,.12f,.60f),w.candy[0],true);
            Part(w,"Gold capsule knob",root,new Vector3(0,1.13f,-.29f),Vector3.one*.15f,w.gold,true);
            var muzzle=Group("Candy muzzle",root);muzzle.localPosition=new Vector3(0,0,1.0f);
        }
        public static Mesh CreateStarMesh()
        {
            const int n=8;var verts=new System.Collections.Generic.List<Vector3>();var tris=new System.Collections.Generic.List<int>();
            Vector2[] points=new Vector2[n];for(int i=0;i<n;i++){float a=(i*45+90)*Mathf.Deg2Rad;float r=i%2==0?.5f:.20f;points[i]=new Vector2(Mathf.Cos(a)*r,Mathf.Sin(a)*r);}
            for(int i=0;i<n;i++) {
                Vector3 a=new Vector3(points[i].x,points[i].y,-.5f),b=new Vector3(points[(i+1)%n].x,points[(i+1)%n].y,-.5f);
                Add(verts,tris,Vector3.back*.5f,b,a);Add(verts,tris,Vector3.forward*.5f,a+Vector3.forward,b+Vector3.forward);
                Add(verts,tris,a,b,b+Vector3.forward);Add(verts,tris,a,b+Vector3.forward,a+Vector3.forward);
            }
            Mesh mesh=new Mesh();mesh.name="Four point candy star";mesh.SetVertices(verts);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        static void Add(System.Collections.Generic.List<Vector3> v,System.Collections.Generic.List<int> t,Vector3 a,Vector3 b,Vector3 c)
        {int i=v.Count;v.Add(a);v.Add(b);v.Add(c);t.Add(i);t.Add(i+1);t.Add(i+2);}
        public static Mesh CreateArrowMesh()
        {
            var v=new System.Collections.Generic.List<Vector3>();var t=new System.Collections.Generic.List<int>();
            Add(v,t,new Vector3(.5f,0,-.5f),new Vector3(-.5f,-.5f,-.5f),new Vector3(-.5f,.5f,-.5f));
            Add(v,t,new Vector3(.5f,0,.5f),new Vector3(-.5f,.5f,.5f),new Vector3(-.5f,-.5f,.5f));
            Vector3[] p={new Vector3(.5f,0,0),new Vector3(-.5f,.5f,0),new Vector3(-.5f,-.5f,0)};
            for(int i=0;i<3;i++){var a=p[i];var b=p[(i+1)%3];Add(v,t,a+Vector3.back*.5f,b+Vector3.back*.5f,b+Vector3.forward*.5f);Add(v,t,a+Vector3.back*.5f,b+Vector3.forward*.5f,a+Vector3.forward*.5f);}
            var m=new Mesh();m.name="Candy arrowhead";m.SetVertices(v);m.SetTriangles(t,0);m.RecalculateNormals();m.RecalculateBounds();return m;
        }
    }
}
