using UnityEngine;
namespace JellyWorldGame
{
    public class JellyTile : MonoBehaviour
    {
        public int x,y,color;
        public bool selected, hinted, hovered, pressed;
        float interactionScale;
        public Vector2 pointerLean;
        public Vector3 target;
        public float born;
        public bool IsFalling {get;private set;}
        Vector3 fallFrom;
        float fallStart,fallDuration;
        Renderer[] waitingRenderers;
        public const float FallAcceleration=32f;
        float squash, tapTime=-10, tapStrength;
        public void Pulse(float strength=.1f){tapTime=Time.time;tapStrength=strength;}
        public static float FallDuration(float distance){return Mathf.Max(.08f,Mathf.Sqrt(2*Mathf.Max(0,distance)/FallAcceleration));}
        public void FallTo(Vector3 destination,float delay=0,bool hideUntilStart=false)
        {
            target=destination;fallFrom=transform.localPosition;fallStart=Time.time+delay;
            fallDuration=FallDuration(Vector3.Distance(fallFrom,destination));IsFalling=true;
            if(hideUntilStart) {
                waitingRenderers=GetComponentsInChildren<Renderer>();
                foreach(var r in waitingRenderers)r.enabled=false;
            }
        }
        void Update()
        {
            if(IsFalling) {
                if(Time.time>=fallStart) {
                    if(waitingRenderers!=null){foreach(var r in waitingRenderers)if(r!=null)r.enabled=true;waitingRenderers=null;}
                    float elapsed=Time.time-fallStart;
                    float t=Mathf.Clamp01(elapsed/fallDuration);
                    transform.localPosition=Vector3.Lerp(fallFrom,target,t*t);
                    squash=.025f+.045f*t;
                    if(elapsed>=fallDuration) {
                        float settle=Mathf.Clamp01((elapsed-fallDuration)/.16f);
                        squash=-.17f*Mathf.Sin(settle*Mathf.PI)*Mathf.Exp(-settle*.7f);
                    }
                    if(elapsed>=fallDuration+.16f) {
                        IsFalling=false;born=Time.time-2f;
                    }
                }
            } else transform.localPosition=Vector3.Lerp(transform.localPosition,target,1-Mathf.Exp(-18*Time.deltaTime));
            float age=Time.time-born;
            float bounce=IsFalling?squash:Mathf.Sin(age*19)*Mathf.Exp(-age*4)*.16f;
            float tap=Time.time-tapTime;
            if(tap<.22f)bounce-=tapStrength*Mathf.Sin(tap/.22f*Mathf.PI);
            float desired=pressed?-.045f:selected?.065f:hovered?.035f:hinted?.03f+.018f*Mathf.Sin(Time.time*5):0;
            interactionScale=Mathf.Lerp(interactionScale,desired,1-Mathf.Exp(-24*Time.deltaTime));
            float pulse=interactionScale;
            transform.localScale=new Vector3(.88f-bounce*.5f+pulse,.88f+bounce+pulse,.82f-bounce*.25f);
            var tilt=Quaternion.Euler(pressed?-pointerLean.y*5:0,pressed?pointerLean.x*5:0,selected?Mathf.Sin(Time.time*4)*1.3f:0);
            transform.localRotation=Quaternion.Slerp(transform.localRotation,tilt,1-Mathf.Exp(-22*Time.deltaTime));
        }
    }
}
