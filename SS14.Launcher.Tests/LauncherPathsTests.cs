using System.IO;
using NUnit.Framework;

namespace SS14.Launcher.Tests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
[TestOf(typeof(LauncherPaths))]
public sealed class LauncherPathsTests
{
    [Test]
    public void ModsLiveInTheUserDataDirectory()
    {
        Assert.That(LauncherPaths.DirPatch, Does.StartWith(LauncherPaths.DirDataRoot));
        Assert.That(Path.GetFileName(LauncherPaths.DirPatch), Is.EqualTo("Mods"));
        Assert.That(Path.GetFileName(Path.GetDirectoryName(LauncherPaths.DirPatch)), Is.EqualTo("BomBom"));
    }
}
