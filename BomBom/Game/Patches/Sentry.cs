using System.Reflection;
using HarmonyLib;
using BomBom.Game.Misc;
using BomBom.Handbreak;
using BomBom.Misc;

namespace BomBom.Game.Patches;

/// <summary>
/// Hooks to EntryPoint
/// Signals to BomBom when the game proper is going to start
/// </summary>
public static class Sentry
{
    private static bool _starting;
    public static bool State => _starting;
    
    public static void Patch()
    {
        MethodInfo? PrefEP = typeof(Sentry).GetMethod("PrefBLR", BindingFlags.Static | BindingFlags.NonPublic);
        
        Type EP = AccessTools.TypeByName("Robust.Shared.ContentPack.BaseModLoader");
        MethodInfo? InitMi = AccessTools.Method(EP, "BroadcastRunLevel");

        Manual.Patch(InitMi, PrefEP, HarmonyPatchType.Prefix);
    }

    private static void PrefBLR(ref object level)
    {
        if (level is not Enum || Convert.ToInt32(level) != 1) return; // ModRunLevel.Init
        BomBomLogger.Log(BomBomLogger.LogType.DEBG, "Sentry set");
        _starting = true;
    }
}