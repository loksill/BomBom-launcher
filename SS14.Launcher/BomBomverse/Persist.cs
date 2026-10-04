using System.Collections.Generic;
using System.IO;
using BomBom.Config;
using Splat;
using SS14.Launcher.Models.Data;
using SS14.Launcher.Utility;

namespace SS14.Launcher.BomBomverse;

public static class Persist
{
    public static void SavePatchlistConfig(List<string> patches)
    {
        File.WriteAllLines(Path.Combine(LauncherPaths.DirUserData, BomBomVars.EnabledPatchListFileName), patches);
    }

    public static List<string> LoadPatchlistConfig()
    {
        string filePath = Path.Combine(LauncherPaths.DirUserData, BomBomVars.EnabledPatchListFileName);
        return File.Exists(filePath) ? [..File.ReadAllLines(filePath)] : [];
    }

    public static void UpdateLauncherConfig()
    {
        DataManager cfg = Locator.Current.GetRequiredService<DataManager>();

        BomBomConf.Logging = cfg.GetCVar(CVars.LogLauncherPatcher);
        BomBomConf.DebugAllowed = cfg.GetCVar(CVars.LogLoaderDebug);
        BomBomConf.TraceAllowed = cfg.GetCVar(CVars.LogLoaderTrace);
    }
}
