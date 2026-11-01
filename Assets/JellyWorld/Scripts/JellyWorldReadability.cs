using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace JellyWorldGame
{
    public partial class JellyWorld
    {
        Color UiBackdrop { get { return new Color(.925f,.94f,.965f); } }
        Color MutedText { get { return new Color(.33f,.37f,.44f); } }
        Material landscapeGround,landscapeRose,landscapeLavender;
        Transform roseDune,lavenderDune;
        readonly List<GameObject> sceneryPebbles=new List<GameObject>();
        bool? homeBackdropApplied;

        void RefreshBackdropTheme()
        {
            bool home=mode=="menu";
            if(homeBackdropApplied==home)return;
            homeBackdropApplied=home;
            if(cam!=null)cam.backgroundColor=home?(Color)new Color32(255,244,230,255):UiBackdrop;
            if(!Application.isPlaying)return;
            // Only tint the existing scenery copies; jelly pieces keep their own palette.
            if(landscapeGround!=null)landscapeGround.color=home?new Color32(255,225,151,255):new Color32(229,236,235,255);
            if(landscapeRose!=null)landscapeRose.color=home?new Color32(255,175,225,255):new Color32(241,230,234,255);
            if(landscapeLavender!=null)landscapeLavender.color=home?new Color32(177,140,235,255):new Color32(230,232,242,255);
            // Keep the pink crest below the text column, and leave fewer edge candies on home.
            if(roseDune!=null)roseDune.localPosition=home?new Vector3(-11,-6.8f,14):new Vector3(-10,-5.5f,14);
            if(lavenderDune!=null)lavenderDune.localPosition=home?new Vector3(11,-6.1f,15):new Vector3(11,-5.2f,15);
            for(int i=0;i<sceneryPebbles.Count;i++)if(sceneryPebbles[i]!=null)sceneryPebbles[i].SetActive(!home || i%2==0);
        }

        Image ReadingPanel(string name,Transform parent,Vector2 position,Vector2 size)
        {
            var panel=Card(name,parent,position,size,new Color(.99f,.995f,1));
            panel.raycastTarget=false;
            return panel;
        }
    }
}
