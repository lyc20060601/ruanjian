using UnityEngine;

namespace JellyWorldGame
{
    [DisallowMultipleComponent,RequireComponent(typeof(CanvasGroup))]
    public sealed class JellyUIReveal : MonoBehaviour
    {
        CanvasGroup group;
        float elapsed;
        void OnEnable()
        {
            group=GetComponent<CanvasGroup>();elapsed=0;
            var world=GetComponentInParent<JellyWorld>();
            group.alpha=Application.isPlaying && (world==null || !world.ReducedEffects)?0:1;
        }
        void Update()
        {
            if(group==null || group.alpha>=1)return;
            elapsed+=Time.unscaledDeltaTime;
            group.alpha=Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/.18f));
        }
        void OnDisable(){if(group!=null)group.alpha=1;}
    }
}
