using UnityEngine;
namespace JellyWorldGame
{
    public class JellyRangeTarget : MonoBehaviour
    {
        public int x,y,color;
        public bool taken;
        public bool iced;
        Vector3 home;
        void Start(){home=transform.localPosition;}
        void Update()
        {
            transform.localPosition=home+Vector3.up*Mathf.Sin(Time.time*1.6f+x*.7f+y)*.025f;
        }
    }
}
