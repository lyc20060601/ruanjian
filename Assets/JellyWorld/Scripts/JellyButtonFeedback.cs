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
        JellyUISurface surface;
        RectTransform[] content;
        Vector2[] restingPositions;
        float press,hoverAmount;
        void Awake(){button=GetComponent<Button>();}
        void Start()
        {
            surface=GetComponent<JellyUISurface>();content=new RectTransform[transform.childCount];restingPositions=new Vector2[content.Length];
            for(int i=0;i<content.Length;i++){content[i]=transform.GetChild(i) as RectTransform;if(content[i]!=null)restingPositions[i]=content[i].anchoredPosition;}
        }
        void Update()
        {
            bool enabledButton=button!=null && button.interactable;
            float scale=enabledButton?(down?.98f:hover?1.009f:1):1;
            transform.localScale=Vector3.Lerp(transform.localScale,Vector3.one*scale,1-Mathf.Exp(-24*Time.unscaledDeltaTime));
            if(icon!=null)icon.localScale=Vector3.Lerp(icon.localScale,Vector3.one*(hover && enabledButton?1.035f:1),1-Mathf.Exp(-18*Time.unscaledDeltaTime));
            press=Mathf.Lerp(press,down && enabledButton?1:0,1-Mathf.Exp(-30*Time.unscaledDeltaTime));
            hoverAmount=Mathf.Lerp(hoverAmount,hover && enabledButton?1:0,1-Mathf.Exp(-18*Time.unscaledDeltaTime));
            if(surface!=null){surface.SetHover(hoverAmount);surface.SetPress(press);for(int i=0;i<content.Length;i++)if(content[i]!=null)content[i].anchoredPosition=restingPositions[i]+Vector2.up*surface.FaceOffset;}
        }
        public void OnPointerEnter(PointerEventData e){hover=true;if(button!=null && button.interactable && world!=null)world.PlaySound(JellySound.Hover,.18f);}
        public void OnPointerExit(PointerEventData e){hover=false;down=false;}
        public void OnPointerDown(PointerEventData e){down=true;}
        public void OnPointerUp(PointerEventData e){down=false;}
        void OnDisable()
        {
            hover=false;down=false;press=0;hoverAmount=0;transform.localScale=Vector3.one;if(icon!=null)icon.localScale=Vector3.one;
            if(surface!=null){surface.SetPress(0);surface.SetHover(0);}
            if(content!=null)for(int i=0;i<content.Length;i++)if(content[i]!=null)content[i].anchoredPosition=restingPositions[i];
        }
    }
}
