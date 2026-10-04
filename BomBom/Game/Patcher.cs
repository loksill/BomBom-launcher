using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using BomBom.Config;
using BomBom.Game.Managers;
using BomBom.Game.Misc;
using BomBom.Patches;
using BomBom.Misc;
using BomBom.Stealthsey.Reflection;

namespace BomBom.Game;

public static class Patcher
{
    /// <summary>
    /// Patches the game using assemblies in List.
    /// </summary>
    /// <param name="patchlist">A list of patches</param>
    public static void Patch<T>(List<T> patchlist) where T : IPatch
    {
        Harmony harmony = HarmonyManager.GetHarmony();

        foreach (T patch in patchlist)
        {
            PatchAssembly(harmony, patch);
        }
    }

    /// <inheritdoc cref="Patcher.Patch"/>
    [Patching]
    private static void PatchAssembly(Harmony harmony, IPatch patch)
    {
        AssemblyName assemblyName = patch.Asm.GetName();
        BomBomLogger.Log(BomBomLogger.LogType.INFO, $"Patching {assemblyName}");

        try
        {
            harmony.PatchAll(patch.Asm);
            Doorbreak.Enter(patch.Entry);
        }
        catch (Exception e)
        {
            HandlePatchException(assemblyName.Name!, e);
        }
    }

    /// <summary>
    /// Logic for managing patches that failed applying. Depending on config - throws exception or ignored.
    /// </summary>
    /// <exception cref="PatchAssemblyException">Thrown if ThrowOnFail is true.</exception>
    private static void HandlePatchException(string assemblyName, Exception e)
    {
        string errorMessage = $"Failed to patch {assemblyName}!\n{e}";

        if (BomBomConf.ThrowOnFail)
            throw new PatchAssemblyException(errorMessage);

        BomBomLogger.Log(BomBomLogger.LogType.FATL, errorMessage);
    }
}
