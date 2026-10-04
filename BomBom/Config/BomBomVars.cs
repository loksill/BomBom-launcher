using System;

namespace BomBom.Config;

/// <summary>
/// Variables only set at compile
/// </summary>
public static class BomBomVars
{
    /// <summary>
    /// Version of the plugin system
    /// </summary>
    public static readonly Version BomBomVersion = new Version("1.0.0");

    public static readonly string EnabledPatchListFileName = "patches.bombom";

    /// <summary>
    /// Namespace identifier for Harmony
    /// </summary>
    public static readonly string Identifier = "com.bombom.launcher";

    /// <summary>
    /// Max amount of loops allowed to catch game assemblies
    /// </summary>
    public static readonly int MaxLoops = 50;

    /// <summary>
    /// Cooldown to try the loop again, in ms
    /// </summary>
    public static readonly int LoopCooldown = 200;

    /// <summary>
    /// Name of folder containing files used by BomBom
    /// </summary>
    public static readonly string BomBomFolder = "BomBom";

    /// <summary>
    /// Folder containing mods
    /// </summary>
    public static readonly string BomBomPatchFolder = Path.Combine(BomBomFolder, "Mods");

    /// <summary>
    /// Log identified for bombom-launcher
    /// </summary>
    public static readonly string BomBomLoggerPrefix = "BOMBOM";

    public static readonly string BomBomLoggerFileName = "client.bombom.log";

    /// <summary>
    /// Refuse to play on servers over or equal to this engine version if hidesey is disabled
    /// <see cref="Abjure"/>
    /// </summary>
    public static readonly Version Detection = new Version("183.0.0");
}
