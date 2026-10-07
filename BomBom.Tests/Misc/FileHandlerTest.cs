using BomBom.Config;
using BomBom.Misc;

namespace BomBom.MiscTests;

[TestFixture]
public class FileHandlerTest
{
    private string _root = null!;
    private string _previousRoot = null!;

    [SetUp]
    public void Setup()
    {
        _previousRoot = BomBomPaths.Root;
        _root = Path.Combine(Path.GetTempPath(), $"bombom_test_{Guid.NewGuid():N}");
        BomBomPaths.Root = _root;
        Directory.CreateDirectory(BomBomPaths.PatchDirectory);
    }

    [TearDown]
    public void TearDown()
    {
        BomBomPaths.Root = _previousRoot;

        if (Directory.Exists(_root))
            Directory.Delete(_root, true);
    }

    [Test]
    public void GetPatches_ResolvesFolderAgainstRoot_NotCurrentDirectory()
    {
        File.WriteAllBytes(Path.Combine(BomBomPaths.PatchDirectory, "one.dll"), [0x4D, 0x5A]);
        File.WriteAllBytes(Path.Combine(BomBomPaths.PatchDirectory, "two.dll"), [0x4D, 0x5A]);
        File.WriteAllText(Path.Combine(BomBomPaths.PatchDirectory, "readme.txt"), "not a patch");

        List<string> patches = FileHandler.GetPatches(new[] { BomBomVars.BomBomPatchFolder });

        Assert.That(patches, Has.Count.EqualTo(2), "Only DLLs inside the root folder should be picked up.");
        Assert.That(
            patches.All(path => path.StartsWith(_root, StringComparison.Ordinal)),
            Is.True,
            $"Expected every patch path to live under {_root}.");
    }

    [Test]
    public void GetPatches_MissingFolder_ReturnsEmpty()
    {
        Directory.Delete(BomBomPaths.PatchDirectory);

        List<string> patches = FileHandler.GetPatches(new[] { BomBomVars.BomBomPatchFolder });

        Assert.That(patches, Is.Empty);
    }
}
