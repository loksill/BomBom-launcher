using System.Collections;
using System.Reflection;
using HarmonyLib;
using BomBom.Config;
using BomBom.Game.Patches.BomBomPorts.Attributes;
using BomBom.Misc;
using BomBom.Stealthsey;
using BomBom.Stealthsey.Reflection;

namespace BomBom.Game.Patches.BomBomPorts;

/// <summary>
/// Manages code backports, fixes and patches
/// </summary>
public static class BomBomPortMan
{
    public static string fork = "";
    public static Version engine = new Version();
    private static IEnumerable<Type>? _backports;

    public static void SetEngineVer(string eng) => engine = new Version(eng);
    public static void SetForkID(string forkid) => fork = forkid;

    public static void Initialize()
    {
        // https://www.youtube.com/watch?v=vmUGxXrlRmE
        if (!BomBomConf.Backports) return;

        BomBomLogger.Log(BomBomLogger.LogType.DEBG, "Backporter", $"Starting backporter against fork \"{fork}\", engine {engine}.");

        IEnumerable<Type> backports = GetBackports();
        BomBomLogger.Log(BomBomLogger.LogType.DEBG, "Backporter",$"Found {backports.Count()} available backports.");

        _backports = backports.Where(ValidateBackport);
        BomBomLogger.Log(BomBomLogger.LogType.DEBG, "Backporter",$"Found {_backports.Count()} valid backports.");
    }

    private static IEnumerable<Type> GetBackports()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        IEnumerable<Type> types = assembly.GetTypes();
        return types.Where(t => t.Namespace != null && t.Namespace.StartsWith("BomBom.Game.Patches.BomBomPorts.Fixes"));
    }

    /// <summary>
    /// Determines if this backport should be applied
    /// </summary>
    private static bool ValidateBackport(Type backport)
    {
        BackportTargetFork? BTF = backport.GetCustomAttribute<BackportTargetFork>();
        BackportTargetEngine? BTE = backport.GetCustomAttribute<BackportTargetEngine>();
        BackportTargetEngineAfter? BTEAf = backport.GetCustomAttribute<BackportTargetEngineAfter>();
        BackportTargetEngineBefore? BTEB = backport.GetCustomAttribute<BackportTargetEngineBefore>();
        BackportTargetEngineAny? BTEAny = backport.GetCustomAttribute<BackportTargetEngineAny>();

        // Discard if fork id is set and does not match
        if (BTF != null && BTF.ForkID != fork) return false;
        // Discard if target engine is set and does not match
        if (BTE != null && BTE.Ver.CompareTo(engine) != 0) return false;
        // Discard if target engine after is set and version is below
        if (BTEAf != null && BTEAf.Ver.CompareTo(engine) > 0) return false;
        // Discard if target engine before is set and version is above
        if (BTEB != null && BTEB.Ver.CompareTo(engine) < 0) return false;
        // Discard if any engine is targeted, but backports of this type are disabled
        if (BTEAny != null && BomBomConf.DisableAnyBackports) return false;

        return true;
    }

    [Patching]
    public static void PatchBackports(bool Content = false)
    {
        if (_backports == null) return;

        foreach (Type backport in _backports)
        {
            object instance = AccessTools.CreateInstance(backport);

            // We are backporting fixes to engine for now
            PropertyInfo contentProperty = AccessTools.Property(backport, "Content");
            bool content = contentProperty != null && (bool)(contentProperty.GetValue(instance) ?? false);

            switch (Content)
            {
                case false when content:
                case true when !content:
                    continue;
            }

            MethodInfo patchMethod = AccessTools.Method(backport, "Patch");
            patchMethod.Invoke(instance, null);

            BomBomLogger.Log(BomBomLogger.LogType.DEBG, "Backporter", $"Backported {backport.Name}.");
        }
    }
}
