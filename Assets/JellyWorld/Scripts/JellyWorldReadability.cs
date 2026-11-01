using UnityEngine;
using UnityEngine.UI;

namespace JellyWorldGame
{
    public partial class JellyWorld
    {
        Color UiBackdrop { get { return new Color(.925f,.94f,.965f); } }
        Color MutedText { get { return new Color(.33f,.37f,.44f); } }

        Image ReadingPanel(string name,Transform parent,Vector2 position,Vector2 size)
        {
            var panel=Card(name,parent,position,size,new Color(.99f,.995f,1));
            panel.raycastTarget=false;
            return panel;
        }
    }
}
