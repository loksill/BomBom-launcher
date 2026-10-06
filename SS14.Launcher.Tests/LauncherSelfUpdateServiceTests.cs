using System;
using System.Collections.Generic;
using NUnit.Framework;
using SS14.Launcher.Models;

namespace SS14.Launcher.Tests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
[TestOf(typeof(LauncherSelfUpdateService))]
public sealed class LauncherSelfUpdateServiceTests
{
    [TestCase("26.10.05.(5)-release", "26.10.05.(5)", 26, 10, 5, 5)]
    [TestCase("26.10.05.(1)-alpha-linuxonly", "26.10.05.(1)", 26, 10, 5, 1)]
    [TestCase("[UNSTABLE] 26.10.05.(4)-test-linuxonly-unstable", "26.10.05.(4)", 26, 10, 5, 4)]
    [TestCase("v0.0.0.7", "0.0.0.7", 0, 0, 0, 7)]
    [TestCase("v0.0.3", "0.0.3", 0, 0, 3, -1)]
    [TestCase("0.37.1", "0.37.1", 0, 37, 1, -1)]
    public void ParsesReleaseVersion(string input, string expectedText, int major, int minor, int build, int revision)
    {
        Assert.That(LauncherSelfUpdateService.TryParseReleaseVersion(input, out var text, out var version), Is.True);
        Assert.That(text, Is.EqualTo(expectedText));

        var expected = revision < 0 ? new Version(major, minor, build) : new Version(major, minor, build, revision);
        Assert.That(version, Is.EqualTo(expected));
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase("release-notes")]
    [TestCase("latest")]
    [TestCase("99999999999.1.2")]
    public void RejectsTextWithoutAVersion(string input)
    {
        Assert.That(LauncherSelfUpdateService.TryParseReleaseVersion(input, out _, out _), Is.False);
    }

    [Test]
    public void SameDayReleasesCompareByBuildNumber()
    {
        LauncherSelfUpdateService.TryParseReleaseVersion("26.10.05.(4)-release", out _, out var fourth);
        LauncherSelfUpdateService.TryParseReleaseVersion("26.10.05.(5)-release", out _, out var fifth);

        Assert.That(fifth, Is.GreaterThan(fourth));
    }

    [Test]
    public void DoesNotOfferTheInstalledReleaseAgain()
    {
        var installed = LauncherSelfUpdateService.ResolveInstalledRelease(
            new[] { "26.10.05.(5)-release" },
            new Version(0, 0, 0, 7));

        var update = LauncherSelfUpdateService.SelectUpdate(
            new[] { Release("26.10.05.(5)-release"), Release("26.10.05.(3)-release") },
            installed,
            includePreRelease: false);

        Assert.That(update, Is.Null);
    }

    [Test]
    public void OffersANewerReleaseThanTheInstalledOne()
    {
        var installed = LauncherSelfUpdateService.ResolveInstalledRelease(
            new[] { "26.10.05.(5)-release" },
            new Version(0, 0, 0, 7));

        var update = LauncherSelfUpdateService.SelectUpdate(
            new[]
            {
                Release("26.10.05.(6)-release"),
                Release("26.10.05.(5)-release"),
                Release("26.10.05.(3)-release")
            },
            installed,
            includePreRelease: false);

        Assert.That(update, Is.Not.Null);
        Assert.That(update!.ReleaseTag, Is.EqualTo("26.10.05.(6)-release"));
    }

    [Test]
    public void FallsBackToTheAssemblyVersionWithoutStampedTags()
    {
        var installed = LauncherSelfUpdateService.ResolveInstalledRelease(
            Array.Empty<string>(),
            new Version(0, 0, 0, 7));

        var update = LauncherSelfUpdateService.SelectUpdate(
            new[] { Release("26.10.05.(5)-release") },
            installed,
            includePreRelease: false);

        Assert.That(update, Is.Not.Null);
        Assert.That(update!.ReleaseTag, Is.EqualTo("26.10.05.(5)-release"));
    }

    [Test]
    public void UsesTheHighestStampedReleaseAsCurrentVersion()
    {
        var installed = LauncherSelfUpdateService.ResolveInstalledRelease(
            new[] { "26.10.05.(2)-release", "26.10.05.(5)-release", "v0.0.0.7" },
            new Version(0, 0, 0, 7));

        Assert.That(installed.Tags, Has.Count.EqualTo(3));
        Assert.That(installed.Version, Is.EqualTo(new Version(26, 10, 5, 5)));
        Assert.That(installed.Matches("26.10.05.(5)-release"), Is.True);
        Assert.That(installed.Matches("26.10.05.(6)-release"), Is.False);
    }

    [Test]
    public void KeepsPreReleasesOutOfTheOfferUnlessAllowed()
    {
        var installed = LauncherSelfUpdateService.ResolveInstalledRelease(
            new[] { "26.10.05.(5)-release" },
            new Version(0, 0, 0, 7));

        var preRelease = Release("26.10.05.(6)-alpha", preRelease: true);

        Assert.That(LauncherSelfUpdateService.SelectUpdate(new[] { preRelease }, installed, false), Is.Null);
        Assert.That(LauncherSelfUpdateService.SelectUpdate(new[] { preRelease }, installed, true), Is.Not.Null);
    }

    [Test]
    public void StampedTagsAreParseableVersions()
    {
        foreach (var tag in LauncherVersion.ReleaseTags)
        {
            Assert.That(
                LauncherSelfUpdateService.TryParseReleaseVersion(tag, out _, out _),
                Is.True,
                $"Stamped release tag '{tag}' is not a parseable version");
        }
    }

    private static LauncherSelfUpdateInfo Release(string tag, bool preRelease = false)
    {
        Assert.That(LauncherSelfUpdateService.TryParseReleaseVersion(tag, out var text, out var version), Is.True);

        return new LauncherSelfUpdateInfo(
            text,
            version,
            "https://example.com/SS14.Launcher_Windows.zip",
            "https://example.com/release",
            "SS14.Launcher_Windows.zip",
            true,
            "",
            preRelease,
            tag,
            DateTimeOffset.UnixEpoch);
    }
}
