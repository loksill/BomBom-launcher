using BomBom.Game;
using BomBom.Misc;
using BomBom.PatchAssembly;

namespace BomBom.Patches;

/// <summary>
/// Manages BomBomPatch instances
/// </summary>
public static class BomBomfier
{
    public static List<BomBomPatch> GetBomBomPatches() => PatchListManager.GetPatchList<BomBomPatch>();

    /// <summary>
    /// Start preload of bombompatches that are flagged as such
    /// </summary>
    public static void Preload(string[]? path = null)
    {
        List<string> preloads = FileHandler.GetFilesFromPipe("PreloadBomBomPatchesPipe");

        if (preloads.Count == 0) return;

        BomBomLogger.Log(BomBomLogger.LogType.INFO, "Preloader", $"Preloading {preloads.Count} patches.");

        foreach (string patch in preloads)
        {
            BomBomLogger.Log(BomBomLogger.LogType.DEBG, "Preloader", $"Preloading {patch}");
            FileHandler.LoadExactAssembly(patch);
        }

        List<BomBomPatch> preloadedPatches = GetBomBomPatches();

        AssemblyFieldHandler.InitHelpers(preloadedPatches);

        if (preloadedPatches.Count != 0) Patcher.Patch(preloadedPatches);

        PatchListManager.ResetList();
    }
}
