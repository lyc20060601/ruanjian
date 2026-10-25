using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace JellyWorldGame
{
    public partial class JellyWorld
    {
        readonly List<RenderTexture> mapPreviewTextures=new List<RenderTexture>();
        const int MapPreviewLayer=30;
        void ReleaseMapPreviews()
        {
            foreach(var texture in mapPreviewTextures)if(texture!=null){texture.Release();Destroy(texture);}
            mapPreviewTextures.Clear();
        }
        RenderTexture CreateMapPreview(JellyLevel spec)
        {
            var root=new GameObject("Temporary map diorama");root.layer=MapPreviewLayer;
            root.transform.position=new Vector3(10000,0,0);
            var cameraObject=new GameObject("Temporary map camera");
            var preview=cameraObject.AddComponent<Camera>();preview.enabled=false;
            preview.clearFlags=CameraClearFlags.SolidColor;preview.backgroundColor=Color.clear;
            preview.cullingMask=1<<MapPreviewLayer;preview.orthographic=true;preview.orthographicSize=spec.Size*.54f+.55f;
            preview.nearClipPlane=.1f;preview.farClipPlane=60;preview.allowHDR=false;preview.allowMSAA=true;
            preview.transform.position=root.transform.position+new Vector3(2.6f,10,-12);
            preview.transform.LookAt(root.transform.position);
            var target=new RenderTexture(384,256,24,RenderTextureFormat.ARGB32){name="Map diorama "+spec.Id,antiAliasing=4};
            target.Create();preview.targetTexture=target;
            cam.cullingMask &= ~(1<<MapPreviewLayer);
            try {
                var mask=spec.CreateMask();float half=(spec.Size-1)*.5f;
                for(int y=0;y<spec.Size;y++)for(int x=0;x<spec.Size;x++)if(mask[x,y]) {
                    var footing=Shape("Preview base",root.transform,new Vector3(x-half,-.22f,y-half),new Vector3(.99f,.28f,.99f),cream);
                    footing.layer=MapPreviewLayer;
                    var tile=Shape("Preview candy",root.transform,new Vector3(x-half,.08f,y-half),new Vector3(.88f,.55f,.88f),candy[(x+y+spec.Id)%5]);
                    tile.layer=MapPreviewLayer;tile.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
                }
                preview.Render();mapPreviewTextures.Add(target);return target;
            } catch {target.Release();Destroy(target);throw;}
            finally {preview.targetTexture=null;root.SetActive(false);Destroy(root);Destroy(cameraObject);}
        }
    }
}
