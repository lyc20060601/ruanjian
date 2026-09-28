using UnityEngine;
namespace JellyWorldGame
{
    public class JellyShockwave : MonoBehaviour
    {
        public float lifetime=.65f, radius=1.2f;
        public Color color;
        float age;
        LineRenderer line;
        void Start() {line=GetComponent<LineRenderer>();}
        void Update() {
            age+=Time.deltaTime;float t=age/lifetime;
            if(t>=1){Destroy(gameObject);return;}
            float r=Mathf.Lerp(.15f,radius,1-Mathf.Pow(1-t,3));transform.localScale=Vector3.one*r;
            var c=color;c.a*=1-t;line.startColor=line.endColor=c;
            line.widthMultiplier=.065f*(1-t)+.01f;
        }
    }
}