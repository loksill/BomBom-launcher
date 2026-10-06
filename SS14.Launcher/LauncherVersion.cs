using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SS14.Launcher;

public static class LauncherVersion
{
    public const string Name = "BomBom-launcher";

    private const string ReleaseTagMetadataKey = "ReleaseTag";

    public static Version? Version => typeof(LauncherVersion).Assembly.GetName().Version;

    /// <summary>
    ///     Release tags this build belongs to, stamped into the assembly at compile time by
    ///     MSBuild/ReleaseTag.targets. Empty for builds made outside of a tagged checkout, in which
    ///     case the self-updater falls back to the tag recorded by the last self-install.
    /// </summary>
    public static IReadOnlyList<string> ReleaseTags { get; } = ReadReleaseTags();

    private static string[] ReadReleaseTags()
    {
        List<string>? tags = null;

        foreach (var attribute in typeof(LauncherVersion).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>())
        {
            if (!string.Equals(attribute.Key, ReleaseTagMetadataKey, StringComparison.Ordinal))
                continue;

            var tag = attribute.Value?.Trim();
            if (string.IsNullOrEmpty(tag))
                continue;

            tags ??= new List<string>();
            if (!tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
                tags.Add(tag);
        }

        return tags?.ToArray() ?? Array.Empty<string>();
    }
}
