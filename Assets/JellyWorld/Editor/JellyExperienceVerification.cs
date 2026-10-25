using System;
using System.Reflection;
using UnityEngine;
using JellyWorldGame;

public static class JellyExperienceVerification
{
    public static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("EXPERIENCE CHECK FAILED: " + message);
    }

    [UnityEditor.MenuItem("Jelly World/Verify journey rules")]
    public static void VerifyRules()
    {
        JellyWorldBuilder.Verify();
        var type = typeof(JellyWorld).Assembly.GetType("JellyWorldGame.JellyJourney");
        Check(type != null, "journey progression rules exist");
        var canUse = type.GetMethod("CanUseTool");
        var unlock = type.GetMethod("ToolUnlockLevel");
        int[] levels = { 2, 4, 3, 6, 8 };
        for (int i = 0; i < levels.Length; i++) {
            Check((int)unlock.Invoke(null, new object[]{ i }) == levels[i], "documented unlock stage");
            Check(!(bool)canUse.Invoke(null, new object[]{ i, levels[i]-1, false }), "locked before milestone");
            Check((bool)canUse.Invoke(null, new object[]{ i, levels[i], false }), "available at milestone");
            Check((bool)canUse.Invoke(null, new object[]{ i, 12, false }), "returning player keeps tools");
            Check((bool)canUse.Invoke(null, new object[]{ i, 1, true }), "sandbox has all tools");
        }
        Check(!(bool)canUse.Invoke(null, new object[]{ -1, 12, true }), "invalid tool rejected");
        Check(!(bool)canUse.Invoke(null, new object[]{ 5, 12, true }), "out of range tool rejected");
        var rank = type.GetMethod("RankForStars");
        Check((int)rank.Invoke(null,new object[]{0}) == 0, "fresh player rank");
        Check((int)rank.Invoke(null,new object[]{8}) == 1, "rank below next threshold");
        Check((int)rank.Invoke(null,new object[]{9}) == 2, "rank reaches threshold");
        Check((int)rank.Invoke(null,new object[]{36}) == 5, "all stars rank");
        Debug.Log("JELLY JOURNEY RULES PASS: unlock boundaries, sandbox, existing players and growth thresholds");
    }
}
