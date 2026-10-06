// Taken from Musya

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using SS14.Launcher.Models.Data;
using SS14.Launcher.Utility;

namespace SS14.Launcher.Models;

public sealed record LauncherSelfUpdateInfo(
    string VersionText,
    Version Version,
    string DownloadUrl,
    string ReleasePageUrl,
    string AssetName,
    bool InstallSupported,
    string ReleaseNotes,
    bool IsPreRelease,
    string ReleaseTag,
    DateTimeOffset PublishedAt);

public sealed class LauncherSelfUpdateService
{
    private readonly DataManager _cfg;

    public LauncherSelfUpdateService(DataManager cfg)
    {
        _cfg = cfg;
    }

    public async Task<LauncherSelfUpdateInfo?> CheckAsync(string repoInput, bool includePreRelease, CancellationToken cancel = default)
    {
        var releases = await GetAvailableAsync(repoInput, cancel);
        if (releases.Count == 0)
            return null;

        return SelectUpdate(releases, GetInstalledRelease(), includePreRelease);
    }

    /// <summary>
    ///     The release this build counts itself as being: the tags stamped into it at compile time,
    ///     falling back to the tag recorded when the built-in updater last installed a release.
    /// </summary>
    internal InstalledRelease GetInstalledRelease()
    {
        IReadOnlyList<string> releaseTags = LauncherVersion.ReleaseTags;

        if (releaseTags.Count == 0)
        {
            var recorded = _cfg.GetCVar(CVars.LauncherInstalledReleaseTag);
            if (!string.IsNullOrWhiteSpace(recorded))
                releaseTags = new[] { recorded };
        }

        return ResolveInstalledRelease(releaseTags, LauncherVersion.Version ?? new Version(0, 0, 0, 0));
    }

    internal static InstalledRelease ResolveInstalledRelease(IEnumerable<string> releaseTags, Version assemblyVersion)
    {
        var tags = releaseTags
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var version = assemblyVersion;
        foreach (var tag in tags)
        {
            if (TryParseReleaseVersion(tag, out _, out var parsed) && parsed > version)
                version = parsed;
        }

        return new InstalledRelease(version, tags);
    }

    internal static LauncherSelfUpdateInfo? SelectUpdate(
        IReadOnlyList<LauncherSelfUpdateInfo> releases,
        InstalledRelease installed,
        bool includePreRelease)
    {
        var filtered = releases
            .Where(r => includePreRelease || !r.IsPreRelease)
            .Where(r => !installed.Matches(r.ReleaseTag))
            .Where(r => r.Version > installed.Version)
            .OrderByDescending(r => r.Version)
            .ThenBy(r => r.IsPreRelease)
            .ThenByDescending(r => r.PublishedAt)
            .ToList();

        if (filtered.Count == 0)
        {
            Log.Debug(
                "Self-update: current version {CurrentVersion} (release {ReleaseTags}) is up-to-date",
                installed.Version,
                installed.Tags.Count > 0 ? string.Join(", ", installed.Tags) : "unknown");
            return null;
        }

        return filtered[0];
    }

    /// <summary>
    ///     What a build counts itself as when checking for updates.
    /// </summary>
    /// <param name="Version">Version compared against the versions of the releases on GitHub.</param>
    /// <param name="Tags">Release tags the build belongs to, used to recognise itself in the release list.</param>
    internal readonly record struct InstalledRelease(Version Version, IReadOnlyList<string> Tags)
    {
        public bool Matches(string releaseTag)
        {
            foreach (var tag in Tags)
            {
                if (string.Equals(tag, releaseTag, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }

    public async Task<IReadOnlyList<LauncherSelfUpdateInfo>> GetAvailableAsync(string repoInput, CancellationToken cancel = default)
    {
        if (!TryParseRepo(repoInput, out var owner, out var repo))
        {
            Log.Debug("Self-update: invalid repo input: {RepoInput}", repoInput);
            return Array.Empty<LauncherSelfUpdateInfo>();
        }

        var endpoint = $"https://api.github.com/repos/{owner}/{repo}/releases?per_page=100";
        using var req = new HttpRequestMessage(HttpMethod.Get, endpoint);
        req.Headers.UserAgent.ParseAdd($"{LauncherVersion.Name}/{LauncherVersion.Version}");
        req.Headers.Accept.ParseAdd("application/vnd.github+json");

        using var client = CreateHttpClientForUpdateChecks();
        using var resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancel);
        if (resp.StatusCode == HttpStatusCode.NotFound)
        {
            Log.Debug("Self-update: no releases for {Owner}/{Repo}", owner, repo);
            return Array.Empty<LauncherSelfUpdateInfo>();
        }

        resp.EnsureSuccessStatusCode();

        await using var stream = await resp.Content.ReadAsStreamAsync(cancel);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancel);
        if (doc.RootElement.ValueKind != JsonValueKind.Array)
            return Array.Empty<LauncherSelfUpdateInfo>();

        var list = new List<LauncherSelfUpdateInfo>();
        foreach (var root in doc.RootElement.EnumerateArray())
        {
            if (!TryBuildReleaseInfo(root, owner, repo, out var info))
                continue;

            list.Add(info);
        }

        return list
            .OrderByDescending(r => r.Version)
            .ThenBy(r => r.IsPreRelease)
            .ThenByDescending(r => r.PublishedAt)
            .ToList();
    }

    private static bool TryBuildReleaseInfo(JsonElement root, string owner, string repo, out LauncherSelfUpdateInfo info)
    {
        info = default!;

        if (!root.TryGetProperty("tag_name", out var tagProp))
            return false;

        var tagName = (tagProp.GetString() ?? "").Trim();
        var isPre = root.TryGetProperty("prerelease", out var preProp) && preProp.GetBoolean();

        string? releaseName = root.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;
        if (!TryParseVersionFromTagOrTitle(tagName, releaseName, out var versionText, out var parsedVersion))
        {
            Log.Debug("Self-update: could not parse release version from tag '{Tag}' or title '{Name}'", tagName, releaseName);
            return false;
        }

        var rawNotes = root.TryGetProperty("body", out var bodyProp) ? bodyProp.GetString() ?? "" : "";
        var formattedNotes = FormatMarkdownToText(rawNotes);

        var releasePage = root.TryGetProperty("html_url", out var htmlUrlProp)
            ? htmlUrlProp.GetString() ?? $"https://github.com/{owner}/{repo}/releases"
            : $"https://github.com/{owner}/{repo}/releases";

        var publishedAt = DateTimeOffset.MinValue;
        if (root.TryGetProperty("published_at", out var pubProp))
        {
            _ = DateTimeOffset.TryParse(pubProp.GetString(), out publishedAt);
        }

        if (OperatingSystem.IsMacOS())
        {
            info = new LauncherSelfUpdateInfo(
                versionText,
                parsedVersion,
                releasePage,
                releasePage,
                "",
                false,
                formattedNotes,
                isPre,
                tagName,
                publishedAt);
            return true;
        }

        string assetUrl = releasePage;
        string assetName = "";
        var installSupported = false;

        if (root.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
        {
            var wantedOs = OperatingSystem.IsWindows() ? "windows" : "linux";
            foreach (var asset in assets.EnumerateArray())
            {
                var name = asset.TryGetProperty("name", out var assetNameProp) ? assetNameProp.GetString() ?? "" : "";
                var url = asset.TryGetProperty("browser_download_url", out var urlProp) ? urlProp.GetString() ?? "" : "";
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(url))
                    continue;

                var lower = name.ToLowerInvariant();
                if (!lower.EndsWith(".zip", StringComparison.Ordinal))
                    continue;

                if (!AssetMatchesCurrentOs(lower, wantedOs))
                    continue;

                assetUrl = url;
                assetName = name;
                installSupported = true;
                break;
            }
        }

        info = new LauncherSelfUpdateInfo(
            versionText,
            parsedVersion,
            assetUrl,
            releasePage,
            assetName,
            installSupported,
            formattedNotes,
            isPre,
            tagName,
            publishedAt);
        return true;
    }

    private static bool TryParseVersionFromTagOrTitle(string? tagName, string? releaseName, out string normalizedText, out Version version)
    {
        if (TryParseReleaseVersion(tagName, out normalizedText, out version))
            return true;

        return TryParseReleaseVersion(releaseName, out normalizedText, out version);
    }

    /// <summary>
    ///     Pulls a <see cref="Version"/> out of a release tag or release title.
    ///     BomBom releases are tagged like <c>26.10.05.(5)-release</c>: the parenthesised build number
    ///     is part of the release's identity, so it has to survive into the parsed version. Otherwise
    ///     every release published on the same day compares as equal to the installed one.
    /// </summary>
    internal static bool TryParseReleaseVersion(string? input, out string versionText, out Version version)
    {
        versionText = "";
        version = new Version(0, 0, 0, 0);

        if (string.IsNullOrWhiteSpace(input))
            return false;

        var trimmed = input.Trim().TrimStart('v', 'V');
        if (Version.TryParse(trimmed, out var direct))
        {
            versionText = trimmed;
            version = direct;
            return true;
        }

        var match = Regex.Match(trimmed, @"\d+(?:\.\d+|\.\(\d+\)){2,}");
        if (!match.Success)
            return false;

        // Remote tags are untrusted input, so a component that does not fit a Version is rejected
        // instead of throwing.
        var parts = new List<int>(4);
        foreach (Match digit in Regex.Matches(match.Value, @"\d+"))
        {
            if (parts.Count == 4)
                break;

            if (!int.TryParse(digit.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var part))
                return false;

            parts.Add(part);
        }

        // The pattern above always yields at least three components, anything else is not a version.
        if (parts.Count < 3)
            return false;

        versionText = match.Value;
        version = parts.Count >= 4
            ? new Version(parts[0], parts[1], parts[2], parts[3])
            : new Version(parts[0], parts[1], parts[2]);

        return true;
    }

    private static string FormatMarkdownToText(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return "";

        var lines = markdown.Replace("\r\n", "\n").Split('\n');
        var result = new List<string>(lines.Length);
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd();

            if (IsMarkdownTableHeader(line) && i + 1 < lines.Length && IsMarkdownTableSeparator(lines[i + 1]))
            {
                var headers = SplitMarkdownTableRow(line);
                i += 2; // skip header and separator

                for (; i < lines.Length; i++)
                {
                    var rowLine = lines[i].Trim();
                    if (!IsMarkdownTableRow(rowLine))
                    {
                        i--;
                        break;
                    }

                    var cells = SplitMarkdownTableRow(rowLine);
                    if (cells.Count == 0)
                        continue;

                    var parts = new List<string>();
                    for (var c = 0; c < Math.Min(headers.Count, cells.Count); c++)
                    {
                        if (string.IsNullOrWhiteSpace(cells[c]))
                            continue;

                        parts.Add($"{headers[c]}: {cells[c]}");
                    }

                    if (parts.Count > 0)
                        result.Add("• " + string.Join(" | ", parts));
                }

                continue;
            }

            if (line.StartsWith("### ", StringComparison.Ordinal))
                line = line[4..].Trim() + ":";
            else if (line.StartsWith("## ", StringComparison.Ordinal))
                line = line[3..].Trim() + ":";
            else if (line.StartsWith("# ", StringComparison.Ordinal))
                line = line[2..].Trim() + ":";
            else if (line.StartsWith("- ", StringComparison.Ordinal) || line.StartsWith("* ", StringComparison.Ordinal))
                line = "• " + line[2..].Trim();

            line = line.Replace("**", "").Replace("__", "").Replace("`", "");
            result.Add(line);
        }

        return string.Join('\n', result).Trim();
    }

    private static bool IsMarkdownTableHeader(string line)
    {
        return IsMarkdownTableRow(line);
    }

    private static bool IsMarkdownTableSeparator(string line)
    {
        var trimmed = line.Trim();
        if (!trimmed.Contains('|', StringComparison.Ordinal))
            return false;

        var cells = SplitMarkdownTableRow(trimmed);
        if (cells.Count == 0)
            return false;

        return cells.All(c =>
        {
            var raw = c.Trim();
            return raw.Length > 0 && raw.All(ch => ch is '-' or ':' or ' ');
        });
    }

    private static bool IsMarkdownTableRow(string line)
    {
        var trimmed = line.Trim();
        if (trimmed.Length < 3)
            return false;

        return trimmed.Contains('|', StringComparison.Ordinal) &&
               (trimmed.StartsWith('|') || trimmed.EndsWith('|'));
    }

    private static List<string> SplitMarkdownTableRow(string line)
    {
        var trimmed = line.Trim().Trim('|');
        return trimmed
            .Split('|')
            .Select(v => v.Trim())
            .ToList();
    }

    private static bool AssetMatchesCurrentOs(string assetNameLower, string wantedOs)
    {
        if (wantedOs == "windows")
            return assetNameLower.Contains("windows", StringComparison.Ordinal) || assetNameLower.Contains("win", StringComparison.Ordinal);

        return assetNameLower.Contains("linux", StringComparison.Ordinal) || assetNameLower.Contains("lin", StringComparison.Ordinal);
    }

    private static bool TryParseRepo(string input, out string owner, out string repo)
    {
        owner = "";
        repo = "";
        if (string.IsNullOrWhiteSpace(input))
            return false;

        var normalized = input.Trim();
        if (!normalized.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !normalized.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            normalized = "https://" + normalized;
        }

        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri))
            return false;

        if (!uri.Host.Contains("github.com", StringComparison.OrdinalIgnoreCase))
            return false;

        var path = uri.AbsolutePath.Trim('/');
        var match = Regex.Match(path, @"^(?<owner>[^/]+)/(?<repo>[^/]+?)(?:\.git)?$");
        if (!match.Success)
            return false;

        owner = match.Groups["owner"].Value;
        repo = match.Groups["repo"].Value;
        return !string.IsNullOrWhiteSpace(owner) && !string.IsNullOrWhiteSpace(repo);
    }

    private HttpClient CreateHttpClientForUpdateChecks()
    {
        return HappyEyeballsHttp.CreateHttpClient();
    }
}
