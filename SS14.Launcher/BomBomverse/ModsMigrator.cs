using System;
using System.IO;
using System.Linq;
using BomBom.Config;
using Serilog;

namespace SS14.Launcher.BomBomverse;

/// <summary>
/// Moves mods that older builds kept next to the launcher executable (which is where they
/// were read from, and what the packaged builds shipped) into the user data directory,
/// which is where the launcher reads them from now.
/// </summary>
public static class ModsMigrator
{
    private static readonly StringComparison PathComparison = OperatingSystem.IsWindows()
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;

    /// <summary>
    /// Migrates the legacy &lt;install dir&gt;/BomBom folder into <see cref="LauncherPaths.DirBomBom"/>.
    /// </summary>
    public static void MigrateFromInstallDirectory()
    {
        MoveLegacyMods(
            Path.Combine(LauncherPaths.DirLauncherInstall, BomBomVars.BomBomFolder),
            LauncherPaths.DirBomBom);
    }

    /// <summary>
    /// Moves every file under <paramref name="source"/> into <paramref name="target"/>, keeping the
    /// relative layout. Files already present in the target are never overwritten, so running this
    /// more than once is harmless. The source folder is removed only once it holds no files left,
    /// so a partially failed move can never lose mods.
    /// </summary>
    public static void MoveLegacyMods(string source, string target)
    {
        source = Path.TrimEndingDirectorySeparator(Path.GetFullPath(source));
        target = Path.TrimEndingDirectorySeparator(Path.GetFullPath(target));

        if (!Directory.Exists(source) || string.Equals(source, target, PathComparison))
            return;

        int moved = 0;

        try
        {
            foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                string destination = Path.Combine(target, Path.GetRelativePath(source, file));
                if (File.Exists(destination))
                    continue;

                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.Move(file, destination);
                moved++;
            }

            if (!Directory.GetFiles(source, "*", SearchOption.AllDirectories).Any())
                Directory.Delete(source, true);
        }
        catch (Exception e)
        {
            Log.Warning(e, "Failed to migrate legacy mod folder {Source} to {Target}", source, target);
            return;
        }

        if (moved > 0)
            Log.Information("Migrated {Count} mod file(s) from {Source} to {Target}", moved, source, target);
    }
}
