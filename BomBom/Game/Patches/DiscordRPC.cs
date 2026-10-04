using HarmonyLib;
using JetBrains.Annotations;
using BomBom.Config;
using BomBom.Handbreak;
using BomBom.Misc;

namespace BomBom.Game.Patches;

public static class DiscordRPC
{
    private static string FakeUsername = "BomBom";

    public static void Patch()
    {
        if (BomBomConf.KillRPC) Disable();
        else if (BomBomConf.FakeRPC) Fake();
    }

    /// <summary>
    /// Does not let DiscordRPC initialize independent of game config
    /// </summary>
    private static void Disable()
    {
        BomBomLogger.Log(BomBomLogger.LogType.DEBG, "DiscordRPC", "Disabling.");

        Helpers.PatchMethod(
            Helpers.TypeFromQualifiedName("Robust.Client.Utility.DiscordRichPresence"),
            "Initialize",
            typeof(DiscordRPC),
            "Skip",
            HarmonyPatchType.Prefix
            );
    }

    /// <summary>
    /// Changes the username displayed in DiscordRPC, if enabled
    /// </summary>
    private static void Fake()
    {
        BomBomLogger.Log(BomBomLogger.LogType.DEBG, "DiscordRPC", $"Faking RPC username to {FakeUsername}.");

        Helpers.PatchMethod(
            Helpers.TypeFromQualifiedName("Robust.Client.Utility.DiscordRichPresence"),
            "Update",
            typeof(DiscordRPC),
            "ChangeUsername",
            HarmonyPatchType.Prefix);
    }

    // ReSharper disable once RedundantAssignment
    [UsedImplicitly]
    private static void ChangeUsername(ref string username)
    {
        username = FakeUsername;
    }

    private static bool Skip() => false;

    public static void SetUsername(string name)
    {
        if (name != "")
            FakeUsername = name;
    }

}
