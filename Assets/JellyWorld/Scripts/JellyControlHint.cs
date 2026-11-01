using UnityEngine;
using UnityEngine.EventSystems;

namespace JellyWorldGame
{
    public sealed class JellyControlHint : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,IPointerClickHandler
    {
        public JellyWorld world;
        public string message;
        public void OnPointerEnter(PointerEventData data){if(world!=null)world.ShowControlHint(this);}
        public void OnPointerExit(PointerEventData data){if(world!=null)world.ClearControlHint(this);}
        public void OnPointerClick(PointerEventData data)
        {
            if(world==null)return;
            var selectable=GetComponent<UnityEngine.UI.Selectable>();
            if(selectable!=null && !selectable.IsInteractable())world.ShowControlHint(this);
            else world.ClearControlHint(this);
        }
        void OnDisable(){if(world!=null)world.ClearControlHint(this);}
    }
}
