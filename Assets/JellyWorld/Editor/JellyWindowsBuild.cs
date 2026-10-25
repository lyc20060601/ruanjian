using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class JellyWindowsBuild
{
    [MenuItem("Jelly World/Build Windows playtest")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before building.");
        JellyExperienceVerification.VerifyRules();
        Directory.CreateDirectory("Builds/CandyBalanced");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { "Assets/Scenes/JellyWorld.unity" },
            locationPathName = "Builds/CandyBalanced/JellyWorld.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception("Windows playtest build failed: " + report.summary.result);
        Debug.Log("JELLY WINDOWS BUILD PASS: " + report.summary.totalSize + " bytes");
    }
}
