using System;
using System.IO;

namespace BomBom.Config;

/// <summary>
/// Resolves the folders BomBom works with against a single root directory.
/// The current working directory must never be used for this: packaged builds are
/// started with a working directory that differs from the directory the launcher
/// itself lives in (the wrapper scripts cd into the package root), which made the
/// launcher look for mods in a folder that neither existed nor was created by it.
/// The launcher points this at the user data root instead.
/// </summary>
public static class BomBomPaths
{
    private static string? _root;

    /// <summary>
    /// Root directory BomBom's folders live in.
    /// Defaults to the directory the executable lives in, and is set to the launcher's
    /// user data root during launcher startup.
    /// </summary>
    public static string Root
    {
        get => _root ?? AppContext.BaseDirectory;
        set => _root = value;
    }

    /// <summary>
    /// Folder containing files used by BomBom.
    /// </summary>
    public static string BomBomDirectory => Path.GetFullPath(Path.Combine(Root, BomBomVars.BomBomFolder));

    /// <summary>
    /// Folder containing mods.
    /// </summary>
    public static string PatchDirectory => Path.GetFullPath(Path.Combine(Root, BomBomVars.BomBomPatchFolder));
}
