using UnityEngine;
namespace JellyWorldGame
{
    public class JellyDebris : MonoBehaviour
    {
        public Vector3 velocity, gravity, spin, planePoint, planeNormal;
        public float life=1;
        Vector3 size;
        float age;
        bool bounced;
        void Start() {size=transform.localScale;}
        void Update()
        {
            float dt=Time.deltaTime;age+=dt;
            if(age>=life){Destroy(gameObject);return;}
            velocity+=gravity*dt;transform.position+=velocity*dt;
            transform.Rotate(spin*dt,Space.Self);
            float distance=Vector3.Dot(transform.position-planePoint,planeNormal);
            if(distance<.03f && !bounced && Vector3.Dot(velocity,planeNormal)<0) {
                transform.position+=planeNormal*(.03f-distance);
                velocity=Vector3.Reflect(velocity,planeNormal)*.32f;bounced=true;
            }
            float shrink=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(life*.55f,life,age));
            transform.localScale=size*shrink;
        }
    }
}