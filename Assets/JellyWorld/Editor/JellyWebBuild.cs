using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
public static class JellyWebBuild
{
    [MenuItem("Jelly World/Build WebGL")]
    public static void Build()
    {
        if(EditorApplication.isPlaying){Debug.LogError("Stop Play Mode before building WebGL.");return;}
        if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL,BuildTarget.WebGL)) {
            Debug.LogError("WebGL Build Support is not installed. Add it to this Unity 2022.3 editor in Unity Hub, then run Jelly World / Build WebGL.");
            return;
        }
        Directory.CreateDirectory("Builds/WebGL");
        PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.dataCaching=true;
        PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=900;
        var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes=new[]{"Assets/Scenes/JellyWorld.unity"},
            locationPathName="Builds/WebGL",
            target=BuildTarget.WebGL,options=BuildOptions.None
        });
        Debug.Log("WebGL build: "+result.summary.result+" / "+result.summary.totalSize+" bytes");
    }
}
