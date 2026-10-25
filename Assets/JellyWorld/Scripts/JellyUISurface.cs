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
        bool selected,available=true,subdued;
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
        public void SetAvailable(bool value)
        {
            if(available==value)return;available=value;if(graphic!=null)graphic.SetVerticesDirty();
        }
        public void SetSubdued(bool value)
        {
            if(subdued==value)return;subdued=value;if(graphic!=null)graphic.SetVerticesDirty();
        }
        public override void ModifyMesh(VertexHelper helper)
        {
            if(!IsActive() || helper.currentVertCount==0)return;
            source.Clear();output.Clear();helper.GetUIVertexStream(source);
            var rect=graphic.rectTransform.rect;Color face=graphic.color;
            bool panel=style==SurfaceStyle.Panel,primary=style==SurfaceStyle.Primary;
            float focus=panel || !available?0:Mathf.Max(hover,selected?.65f:0);
            float alpha=face.a;
            // Temporary input blocking during cascades must not flatten every tool at once.
            bool curved=!panel && rect.height<=90 && rect.width>=rect.height && !subdued;
            float edgeDepth=curved?(primary?3.6f:2.3f):0;
            float shadow=(curved?(primary?.12f:.065f):0)+focus*.045f;
            if(shadow>.004f) {
                AddLayer(rect,new Vector2(0,-edgeDepth-2-focus),1.0f,new Color(.32f,.36f,.57f,shadow*.40f*alpha));
                AddLayer(rect,new Vector2(0,-edgeDepth-1-focus*.6f),.25f,new Color(.32f,.36f,.57f,shadow*.60f*alpha));
            }
            if(edgeDepth>0) {
                Color edge=Color.Lerp(face,new Color(face.r*.65f,face.g*.72f,face.b*.85f,alpha),.38f);
                AddLayer(rect,new Vector2(0,-edgeDepth),0,edge);
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
                vertex.uv1=curved?new Vector4(Mathf.InverseLerp(rect.xMin,rect.xMax,vertex.position.x),t,rect.width,rect.height):Vector4.zero;
                vertex.position+=Vector3.up*FaceOffset;output.Add(vertex);
            }
            helper.Clear();helper.AddUIVertexTriangleStream(output);
        }
        void AddLayer(Rect rect,Vector2 offset,float spread,Color color)
        {
            foreach(var original in source) {
                var vertex=original;
                vertex.uv1=Vector4.zero;
                var relative=(Vector2)vertex.position-rect.center;
                vertex.position+=new Vector3(offset.x+relative.x*spread*2/Mathf.Max(1,rect.width),offset.y+relative.y*spread*2/Mathf.Max(1,rect.height),0);
                var tint=color;tint.a*=(original.color.a/255f)/Mathf.Max(.001f,graphic.color.a);vertex.color=tint;
                output.Add(vertex);
            }
        }
    }
}
