using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace JellyWorldGame
{
    // Depth follows visual importance; information panels never cast a shadow.
    [RequireComponent(typeof(Graphic))]
    public sealed class JellyUISurface : BaseMeshEffect
    {
        public enum SurfaceStyle { Quiet, Primary, Choice, Panel }
        public SurfaceStyle style=SurfaceStyle.Quiet;
        public float pressTravel=3;
        float press,hover;
        bool selected;
        readonly List<UIVertex> source=new List<UIVertex>();
        readonly List<UIVertex> output=new List<UIVertex>();
        public float FaceOffset {get{return style==SurfaceStyle.Panel?0:hover*(style==SurfaceStyle.Choice?1.1f:.65f)*(1-press)-press*pressTravel;}}
        public void SetPress(float value){SetAmount(ref press,value);}
        public void SetHover(float value){SetAmount(ref hover,value);}
        void SetAmount(ref float current,float value)
        {
            value=Mathf.Clamp01(value);if(Mathf.Abs(current-value)<.002f)return;
            current=value;if(graphic!=null)graphic.SetVerticesDirty();
        }
        public void SetSelected(bool value)
        {
            if(selected==value)return;selected=value;if(graphic!=null)graphic.SetVerticesDirty();
        }
        public override void ModifyMesh(VertexHelper helper)
        {
            if(!IsActive() || helper.currentVertCount==0)return;
            source.Clear();output.Clear();helper.GetUIVertexStream(source);
            var rect=graphic.rectTransform.rect;Color face=graphic.color;
            bool panel=style==SurfaceStyle.Panel,primary=style==SurfaceStyle.Primary;
            float focus=panel?0:Mathf.Max(hover,selected?.65f:0);
            float alpha=face.a;
            // Only primary actions have resting elevation. Other shadows appear on interaction.
            float shadow=(primary?.075f:0)+focus*.045f;
            if(shadow>.004f) {
                AddLayer(rect,new Vector2(0,-3-focus),.8f,new Color(.32f,.36f,.57f,shadow*.40f*alpha));
                AddLayer(rect,new Vector2(0,-1.8f-focus*.6f),.1f,new Color(.32f,.36f,.57f,shadow*.60f*alpha));
            }
            if(primary) {
                Color edge=Color.Lerp(face,new Color(.30f,.47f,.72f,alpha),.22f);
                AddLayer(rect,new Vector2(0,-1.8f),0,edge);
            }
            float border=panel?.55f:style==SurfaceStyle.Choice?.65f+focus*.55f:focus*.8f;
            if(border>.1f) {
                Color outline=selected?new Color(.29f,.62f,.93f,.72f*alpha):new Color(.62f,.69f,.86f,(panel?.15f:.17f+focus*.15f)*alpha);
                AddLayer(rect,new Vector2(0,FaceOffset),border,outline);
            }
            foreach(var original in source) {
                var vertex=original;float t=Mathf.InverseLerp(rect.yMin,rect.yMax,vertex.position.y);
                Color color=vertex.color;
                var bottom=Color.Lerp(color,new Color(.76f,.78f,.91f,color.a),primary?.065f:.025f);
                var top=Color.Lerp(color,new Color(1,1,1,color.a),(primary?.18f:.075f)+focus*.035f);
                vertex.color=Color.Lerp(bottom,top,t);
                vertex.position+=Vector3.up*FaceOffset;output.Add(vertex);
            }
            helper.Clear();helper.AddUIVertexTriangleStream(output);
        }
        void AddLayer(Rect rect,Vector2 offset,float spread,Color color)
        {
            foreach(var original in source) {
                var vertex=original;
                var relative=(Vector2)vertex.position-rect.center;
                vertex.position+=new Vector3(offset.x+relative.x*spread*2/Mathf.Max(1,rect.width),offset.y+relative.y*spread*2/Mathf.Max(1,rect.height),0);
                var tint=color;tint.a*=(original.color.a/255f)/Mathf.Max(.001f,graphic.color.a);vertex.color=tint;
                output.Add(vertex);
            }
        }
    }
}
