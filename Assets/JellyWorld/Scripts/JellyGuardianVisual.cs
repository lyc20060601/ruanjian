using UnityEngine;
using UnityEngine.Rendering;
namespace JellyWorldGame
{
    public class JellyGuardianVisual : MonoBehaviour
    {
        public Transform body, head, leftLeg, rightLeg, leftArm, rightArm;
        Transform crystal;
        float gait;
        public void Build(JellyWorld w)
        {
            body=Group("Breathing stone body",transform,Vector3.zero);
            leftLeg=Leg(w,-1);rightLeg=Leg(w,1);
            Part(w,"Hip belt",body,new Vector3(0,.96f,0),new Vector3(1.05f,.38f,.65f),w.stoneDark);
            Part(w,"Soft stone torso",body,new Vector3(0,1.52f,0),new Vector3(1.40f,.91f,.76f),w.stone);
            Part(w,"Gold chest frame left",body,new Vector3(-.37f,1.58f,.40f),new Vector3(.63f,.60f,.19f),w.gold,new Vector3(0,-9,-8));
            Part(w,"Gold chest frame right",body,new Vector3(.37f,1.58f,.40f),new Vector3(.63f,.60f,.19f),w.gold,new Vector3(0,9,8));
            Part(w,"Azure enamel breastplate",body,new Vector3(-.37f,1.58f,.455f),new Vector3(.51f,.46f,.19f),w.candy[1],new Vector3(0,-9,-8));
            Part(w,"Amethyst enamel breastplate",body,new Vector3(.37f,1.58f,.455f),new Vector3(.51f,.46f,.19f),w.candy[4],new Vector3(0,9,8));
            for(int s=-1;s<=1;s+=2) {
                Part(w,"Golden waist guard",body,new Vector3(s*.43f,1.04f,.36f),new Vector3(.36f,.19f,.16f),w.gold,new Vector3(0,0,s*10));
                Part(w,"Rose side armor",body,new Vector3(s*.58f,1.12f,.05f),new Vector3(.26f,.39f,.56f),w.candy[0],new Vector3(0,0,s*-9));
                for(int r=0;r<2;r++)Part(w,"Breastplate rivet",body,new Vector3(s*.53f,1.46f+r*.25f,.59f),Vector3.one*.065f,w.gold);
                Part(w,"Back wing armor",body,new Vector3(s*.45f,1.51f,-.43f),new Vector3(.36f,.74f,.17f),s<0?w.candy[1]:w.candy[4],new Vector3(0,s*8,s*9));
            }
            crystal=Part(w,"Living sugar heart",body,new Vector3(0,1.52f,.49f),new Vector3(.32f,.49f,.22f),w.rune).transform;
            crystal.GetComponent<MeshFilter>().sharedMesh=w.fractureMesh;
            var back=Part(w,"Backpack crystal",body,new Vector3(0,1.55f,-.44f),new Vector3(.31f,.49f,.23f),w.rune);
            back.GetComponent<MeshFilter>().sharedMesh=w.fractureMesh;
            Part(w,"Back armor top",body,new Vector3(0,1.91f,-.28f),new Vector3(.81f,.15f,.25f),w.gold);
            leftArm=Arm(w,-1);rightArm=Arm(w,1);
            Part(w,"Neck",body,new Vector3(0,2.02f,0),new Vector3(.48f,.19f,.47f),w.stoneDark);
            head=Group("Guardian face",body,new Vector3(0,2.33f,.035f));
            head.localScale=Vector3.one*1.10f;
            Part(w,"Rounded stone head",head,Vector3.zero,new Vector3(.92f,.69f,.73f),w.stone);
            Part(w,"Enamel helmet",head,new Vector3(0,.22f,-.01f),new Vector3(1.01f,.23f,.78f),w.candy[1]);
            Part(w,"Golden helmet band",head,new Vector3(0,.30f,-.01f),new Vector3(1.04f,.065f,.79f),w.gold);
            Part(w,"Face inset",head,new Vector3(0,-.035f,.36f),new Vector3(.67f,.30f,.09f),w.stoneDark);
            for(int s=-1;s<=1;s+=2) {
                Part(w,"Mint eye",head,new Vector3(s*.20f,.025f,.425f),new Vector3(.14f,.085f,.035f),w.rune);
                Part(w,"Protective brow",head,new Vector3(s*.20f,.145f,.38f),new Vector3(.32f,.12f,.15f),w.cream,new Vector3(0,0,s*8));
                Part(w,"Cheek plate",head,new Vector3(s*.35f,-.17f,.29f),new Vector3(.16f,.19f,.19f),w.stone);
            }
            Part(w,"Kind smile",head,new Vector3(0,-.16f,.423f),new Vector3(.16f,.035f,.025f),w.gold);
            Part(w,"Moss cap",head,new Vector3(-.12f,.33f,0),new Vector3(.83f,.14f,.63f),w.mint);
            for(int i=0;i<5;i++) {
                var g=Part(w,"Rainbow crystal crest",head,new Vector3((i-2)*.19f,.51f,.015f),
                    new Vector3(.17f,.50f-Mathf.Abs(i-2)*.08f,.19f),i==2?w.rune:w.candy[i],new Vector3(0,0,(i-2)*-12));
                g.GetComponent<MeshFilter>().sharedMesh=w.fractureMesh;
            }
            // Thin luminous seams keep the character readable from behind.
            Seam(w,body,new[]{new Vector3(-.5f,1.74f,-.401f),new Vector3(-.35f,1.50f,-.421f),new Vector3(-.47f,1.28f,-.40f)});
            Seam(w,body,new[]{new Vector3(.48f,1.75f,-.401f),new Vector3(.36f,1.51f,-.421f),new Vector3(.48f,1.27f,-.40f)});
        }
        Transform Group(string name,Transform p,Vector3 at) {
            var g=new GameObject(name);g.transform.SetParent(p,false);g.transform.localPosition=at;return g.transform;
        }
        GameObject Part(JellyWorld w,string name,Transform p,Vector3 at,Vector3 scale,Material mat,Vector3 rotation=default(Vector3)) {
            var g=w.Shape(name,p,at,scale,mat);g.transform.localEulerAngles=rotation;return g;
        }
        Transform Leg(JellyWorld w,int side) {
            var joint=Group(side<0?"Left hip":"Right hip",body,new Vector3(side*.36f,.86f,0));
            Part(w,"Stone leg",joint,new Vector3(0,-.30f,0),new Vector3(.39f,.61f,.42f),w.stoneDark);
            Part(w,"Knee armor",joint,new Vector3(0,-.31f,.16f),new Vector3(.46f,.36f,.35f),w.stone);
            Part(w,"Enamel greave",joint,new Vector3(0,-.35f,.35f),new Vector3(.32f,.35f,.13f),side<0?w.candy[1]:w.candy[4]);
            Part(w,"Knee gold clasp",joint,new Vector3(0,-.18f,.36f),new Vector3(.37f,.085f,.15f),w.gold);
            Part(w,"Chunky foot",joint,new Vector3(0,-.72f,.13f),new Vector3(.59f,.28f,.78f),w.stone);
            Part(w,"Golden toe",joint,new Vector3(0,-.69f,.44f),new Vector3(.46f,.12f,.16f),w.gold);
            return joint;
        }
        Transform Arm(JellyWorld w,int side) {
            var joint=Group(side<0?"Left shoulder":"Right shoulder",body,new Vector3(side*.92f,1.75f,0));
            Part(w,"Gold pauldron frame",joint,Vector3.zero,new Vector3(.77f,.63f,.75f),w.gold,new Vector3(0,0,side*12));
            Part(w,"Candy enamel pauldron",joint,new Vector3(0,.045f,.08f),new Vector3(.68f,.57f,.70f),side<0?w.candy[4]:w.candy[1],new Vector3(0,0,side*12));
            var gem=Part(w,"Shoulder rose gem",joint,new Vector3(side*.22f,.36f,.10f),new Vector3(.20f,.30f,.20f),w.candy[0]);
            gem.GetComponent<MeshFilter>().sharedMesh=w.fractureMesh;
            Part(w,"Moss shoulder",joint,new Vector3(side*.06f,.29f,-.02f),new Vector3(.49f,.12f,.49f),w.mint);
            Part(w,"Elbow",joint,new Vector3(side*.04f,-.41f,0),new Vector3(.35f,.47f,.37f),w.stoneDark);
            Part(w,"Heavy fist",joint,new Vector3(side*.06f,-.73f,.06f),new Vector3(.59f,.55f,.58f),w.stone,new Vector3(0,0,-side*5));
            Part(w,"Gauntlet gold rim",joint,new Vector3(side*.06f,-.51f,.06f),new Vector3(.62f,.12f,.61f),w.gold);
            Part(w,"Enamel gauntlet",joint,new Vector3(side*.06f,-.71f,.33f),new Vector3(.48f,.36f,.14f),side<0?w.candy[1]:w.candy[4]);
            Part(w,"Mint knuckle",joint,new Vector3(side*.06f,-.65f,.35f),new Vector3(.28f,.13f,.035f),w.rune);
            return joint;
        }
        void Seam(JellyWorld w,Transform parent,Vector3[] points) {
            var g=Group("Glowing seam",parent,Vector3.zero);
            var l=g.gameObject.AddComponent<LineRenderer>();l.useWorldSpace=false;l.sharedMaterial=w.glow;
            l.positionCount=points.Length;l.SetPositions(points);l.widthMultiplier=.023f;
            l.startColor=l.endColor=new Color(.35f,1,.79f,.8f);l.shadowCastingMode=ShadowCastingMode.Off;
        }
        public void Animate(float speed,bool sprint,float slamPhase)
        {
            float dt=Time.deltaTime;gait+=dt*(sprint?15:10)*Mathf.Clamp01(speed);
            float swing=Mathf.Sin(gait)*(sprint?39:27)*speed;
            leftLeg.localRotation=Quaternion.Euler(swing,0,0);
            rightLeg.localRotation=Quaternion.Euler(-swing,0,0);
            leftArm.localRotation=Quaternion.Euler(-swing*.7f,0,-5);
            rightArm.localRotation=Quaternion.Euler(swing*.7f,0,5);
            float breath=Mathf.Sin(Time.time*2.2f)*.015f;
            body.localPosition=new Vector3(0,breath+Mathf.Abs(Mathf.Cos(gait))*.06f*speed,0);
            body.localRotation=Quaternion.Euler(speed*5,0,Mathf.Sin(gait)*2*speed);
            if(slamPhase>=0) {
                float jump=Mathf.Sin(Mathf.Clamp01(slamPhase/.64f)*Mathf.PI);
                body.localPosition=new Vector3(0,jump*1.15f,0);
                float squash=slamPhase>.64f?Mathf.Sin((slamPhase-.64f)/.36f*Mathf.PI)*.17f:0;
                body.localScale=new Vector3(1+squash,1-squash,1+squash);
                leftArm.localRotation=Quaternion.Euler(-jump*125,0,-15);
                rightArm.localRotation=Quaternion.Euler(-jump*125,0,15);
            } else body.localScale=Vector3.one;
            crystal.localScale=new Vector3(.32f,.49f,.22f)*(1+Mathf.Sin(Time.time*3)*.035f);
        }
        public void FirstPerson(bool first) {body.gameObject.SetActive(!first);}
        public void ChargePose(float amount)
        {
            body.localScale=new Vector3(1+amount*.07f,1-amount*.13f,1+amount*.07f);
            leftArm.localRotation=Quaternion.Euler(20+amount*25,0,-12);rightArm.localRotation=Quaternion.Euler(20+amount*25,0,12);
        }
        public void LeapPose(float phase)
        {
            float bend=Mathf.Sin(phase*Mathf.PI);
            leftArm.localRotation=Quaternion.Euler(-65*bend,0,-18);rightArm.localRotation=Quaternion.Euler(-65*bend,0,18);
            leftLeg.localRotation=Quaternion.Euler(-25*bend,0,0);rightLeg.localRotation=Quaternion.Euler(20*bend,0,0);
            body.localRotation=Quaternion.Euler(12*bend,0,0);
        }
        public void IceStompPose(float phase)
        {
            float lift=Mathf.Sin(Mathf.Clamp01(phase)*Mathf.PI);
            body.localPosition=Vector3.up*(lift*.08f);
            leftLeg.localRotation=Quaternion.identity;
            rightLeg.localRotation=Quaternion.Euler(-32*lift,0,0);
            leftArm.localRotation=Quaternion.Euler(12*lift,0,-5);
            rightArm.localRotation=Quaternion.Euler(-12*lift,0,5);
        }
    }
}
