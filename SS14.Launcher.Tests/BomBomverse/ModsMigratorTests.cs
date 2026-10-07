using System;
using System.IO;
using NUnit.Framework;
using SS14.Launcher.BomBomverse;

namespace SS14.Launcher.Tests;

[TestFixture]
// Deliberately not parallelizable: SetUp/TearDown share the fixture instance.
[TestOf(typeof(ModsMigrator))]
public sealed class ModsMigratorTests
{
    private string _root = null!;

    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), $"bombom_migrate_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, true);
    }

    private string Source => Path.Combine(_root, "install", "BomBom");
    private string Target => Path.Combine(_root, "data", "BomBom");

    [Test]
    public void MovesModsIntoTheTargetFolder()
    {
        WriteFile(Path.Combine(Source, "Mods", "Cerberus.dll"), "mod");
        WriteFile(Path.Combine(Source, "Mods", "nested", "Other.dll"), "nested");
        WriteFile(Path.Combine(Source, "notes.txt"), "notes");
        Directory.CreateDirectory(Target);

        ModsMigrator.MoveLegacyMods(Source, Target);

        Assert.That(File.ReadAllText(Path.Combine(Target, "Mods", "Cerberus.dll")), Is.EqualTo("mod"));
        Assert.That(File.Exists(Path.Combine(Target, "Mods", "nested", "Other.dll")), Is.True);
        Assert.That(File.Exists(Path.Combine(Target, "notes.txt")), Is.True);
        Assert.That(Directory.Exists(Source), Is.False, "The emptied legacy folder should be cleaned up.");
    }

    [Test]
    public void KeepsFilesThatAlreadyExistInTheTarget()
    {
        WriteFile(Path.Combine(Source, "Mods", "Same.dll"), "legacy");
        WriteFile(Path.Combine(Target, "Mods", "Same.dll"), "current");

        ModsMigrator.MoveLegacyMods(Source, Target);

        Assert.That(File.ReadAllText(Path.Combine(Target, "Mods", "Same.dll")), Is.EqualTo("current"));
    }

    [Test]
    public void MissingLegacyFolderIsIgnored()
    {
        Directory.CreateDirectory(Target);

        ModsMigrator.MoveLegacyMods(Source, Target);

        Assert.That(Directory.GetFiles(Target, "*", SearchOption.AllDirectories), Is.Empty);
    }

    [Test]
    public void SameFolderIsLeftAlone()
    {
        WriteFile(Path.Combine(Source, "Mods", "Same.dll"), "legacy");

        ModsMigrator.MoveLegacyMods(Source, Source);

        Assert.That(File.Exists(Path.Combine(Source, "Mods", "Same.dll")), Is.True);
    }

    private static void WriteFile(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }
}
