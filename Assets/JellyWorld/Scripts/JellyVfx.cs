using UnityEngine;
using UnityEngine.Rendering;
namespace JellyWorldGame
{
    public static class JellyVfx
    {
        public static void IceBreak(JellyWorld w,Transform parent,Vector3 p,Vector3 normal)
        {
            Quaternion basis=Quaternion.FromToRotation(Vector3.up,normal);
            for(int i=0;i<8;i++) {
                float angle=i*Mathf.PI*.25f;Vector3 radial=basis*new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                var shard=w.Shape("Broken ice shell",parent,Vector3.zero,new Vector3(.18f,.27f,.09f),w.iceShell);
                shard.GetComponent<MeshFilter>().sharedMesh=w.fractureMesh;shard.transform.position=p+radial*.3f;
                shard.transform.rotation=Random.rotation;
                var d=shard.AddComponent<JellyDebris>();d.velocity=radial*1.8f+normal*2.0f;
                d.gravity=Vector3.down*7;d.spin=Random.insideUnitSphere*300;d.life=.7f;d.planePoint=p-normal*.3f;d.planeNormal=normal;
            }
        }
        public static void Shatter(JellyWorld w,Transform parent,Vector3 p,int color,Vector3 normal)
        {
            var basis=Quaternion.FromToRotation(Vector3.up,normal);
            for(int i=0;i<15;i++) {
                float angle=i*2.39996f;
                Vector3 radial=basis*new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                var g=new GameObject(i<10?"Fractured jelly shard":"Sugar droplet");
                g.transform.SetParent(parent,false);g.transform.position=p+radial*.12f;
                g.AddComponent<MeshFilter>().sharedMesh=i<10?w.fractureMesh:w.jellyMesh;
                g.AddComponent<MeshRenderer>().sharedMaterial=i==14?w.white:w.candy[color];
                g.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
                float size=i<10?Random.Range(.16f,.30f):Random.Range(.05f,.10f);
                g.transform.localScale=new Vector3(size,size*Random.Range(.55f,1.3f),size);
                g.transform.rotation=Random.rotation;
                var d=g.AddComponent<JellyDebris>();
                d.velocity=radial*Random.Range(1.4f,3.4f)+normal*Random.Range(1.8f,3.9f);
                d.gravity=normal.y>.8f?Vector3.down*10:Vector3.down*7-normal*2;
                d.spin=Random.insideUnitSphere*540;d.life=Random.Range(.65f,1.1f);
                d.planePoint=p-normal*.25f;d.planeNormal=normal;
            }
            Ring(w,parent,p+normal*.1f,normal,.8f,.45f,new Color(1,.96f,.75f,.75f));
        }
        public static GameObject Ring(JellyWorld w,Transform parent,Vector3 p,Vector3 normal,float radius,float life,Color color)
        {
            var g=new GameObject("Expanding sugar shockwave");g.transform.SetParent(parent,false);
            g.transform.position=p;g.transform.rotation=Quaternion.FromToRotation(Vector3.forward,normal);
            var line=g.AddComponent<LineRenderer>();line.sharedMaterial=w.glow;
            line.useWorldSpace=false;line.loop=true;line.positionCount=64;line.widthMultiplier=.06f;
            line.numCornerVertices=2;line.startColor=line.endColor=color;
            line.shadowCastingMode=ShadowCastingMode.Off;line.receiveShadows=false;
            var points=new Vector3[64];for(int i=0;i<64;i++){float a=i*6.28318f/64;points[i]=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);}
            line.SetPositions(points);
            var fx=g.AddComponent<JellyShockwave>();fx.radius=radius;fx.lifetime=life;fx.color=color;
            return g;
        }
    }
}
