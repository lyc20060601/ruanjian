using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace JellyWorldGame
{
    public class JellyButtonFeedback : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,IPointerDownHandler,IPointerUpHandler
    {
        public JellyWorld world;
        public RectTransform icon;
        Button button;
        bool hover,down;
        void Awake(){button=GetComponent<Button>();}
        void Update()
        {
            bool enabledButton=button!=null && button.interactable;
            float scale=enabledButton?(down?.965f:hover?1.018f:1):1;
            transform.localScale=Vector3.Lerp(transform.localScale,Vector3.one*scale,1-Mathf.Exp(-24*Time.unscaledDeltaTime));
            if(icon!=null)icon.localScale=Vector3.Lerp(icon.localScale,Vector3.one*(hover && enabledButton?1.07f:1),1-Mathf.Exp(-18*Time.unscaledDeltaTime));
        }
        public void OnPointerEnter(PointerEventData e){hover=true;if(button!=null && button.interactable && world!=null)world.PlaySound(JellySound.Hover,.18f);}
        public void OnPointerExit(PointerEventData e){hover=false;down=false;}
        public void OnPointerDown(PointerEventData e){down=true;}
        public void OnPointerUp(PointerEventData e){down=false;}
        void OnDisable(){hover=false;down=false;transform.localScale=Vector3.one;if(icon!=null)icon.localScale=Vector3.one;}
    }
}