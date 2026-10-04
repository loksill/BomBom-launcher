using HarmonyLib;
using BomBom.Config;
using BomBom.Handbreak;
using BomBom.Misc;

namespace BomBom.Game.Patches;

/// <summary>
/// Whitelists command execution that is dependent on server
/// </summary>
/// <remarks>Breaks features that rely on this, for example White Dream's "notice" command. Mileage may vary.</remarks>
public static class Blackhole
{
    private static List<string> AllowedCommands = ["observe", "joingame", "ghostroles", "openahelp", "deadmin", "readmin", "say", "whisper"];

    public static void Patch()
    {
        if (!BomBomConf.DisableREC) return;

        BomBomLogger.Log(BomBomLogger.LogType.WARN, "Blackhole", "Blackholing RemoteExecutingCommand! This may break game functionality!");
        
        Helpers.PatchMethod(
            Helpers.TypeFromQualifiedName("Robust.Client.Console.ClientConsoleHost"),
            "RemoteExecuteCommand",
            typeof(Blackhole),
            "RECPref",
            HarmonyPatchType.Prefix
            );
    }

    private static bool RECPref(ref dynamic? session, ref string command)
    {
        if (session is null) return true;
        
        foreach (string comm in AllowedCommands)
        {
            if (command.StartsWith(comm + " "))
                return true;
        }
        
        BomBomLogger.Log(BomBomLogger.LogType.INFO, "Blackhole", $"Blocked \"{command}\" from executing.");
        return false;
    }
}