// Taken from Musya

using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;

try
{
    var parsed = ParseArgs(args);
    if (!parsed.TryGetValue("--zip", out var zipPath) ||
        !parsed.TryGetValue("--target", out var targetDir) ||
        !parsed.TryGetValue("--launcher", out var launcherName))
    {
        Console.Error.WriteLine("Required args: --zip <path> --target <dir> --launcher <file>");
        return 2;
    }

    targetDir = Path.TrimEndingDirectorySeparator(Path.GetFullPath(targetDir));

    if (parsed.TryGetValue("--wait-pid", out var waitPidStr) &&
        int.TryParse(waitPidStr, out var waitPid))
    {
        try
        {
            using var proc = Process.GetProcessById(waitPid);
            if (!proc.WaitForExit(30000))
                throw new InvalidOperationException(
                    $"Launcher process {waitPid} did not exit within 30s, refusing to touch its files.");
        }
        catch (ArgumentException)
        {
            // Process already gone.
        }
    }

    if (!File.Exists(zipPath))
        throw new FileNotFoundException("Update archive does not exist.", zipPath);

    Directory.CreateDirectory(targetDir);

    var extractRoot = Path.Combine(Path.GetTempPath(), $"bombom_update_extract_{Guid.NewGuid():N}");
    Directory.CreateDirectory(extractRoot);

    try
    {
        ZipFile.ExtractToDirectory(zipPath, extractRoot);

        var layout = ResolveLayout(extractRoot, targetDir, launcherName);
        CopyTree(layout.SourceRoot, layout.DestinationRoot, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "BomBom" });

        var installedLauncher = Path.Combine(layout.DestinationRoot, layout.LauncherRelativePath);
        if (!File.Exists(installedLauncher))
            throw new FileNotFoundException("Updated launcher executable was not found.", installedLauncher);

        RelaunchLauncher(installedLauncher, layout.DestinationRoot, launcherName);
    }
    finally
    {
        TryDeleteDirectory(extractRoot);
    }

    return 0;
}
catch (Exception e)
{
    Console.Error.WriteLine(e);
    return 1;
}

static void TryDeleteDirectory(string path)
{
    try
    {
        if (Directory.Exists(path))
            Directory.Delete(path, true);
    }
    catch
    {
        // Best effort, this is just a temp directory.
    }
}

static Dictionary<string, string> ParseArgs(string[] args)
{
    var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    for (var i = 0; i < args.Length - 1; i += 2)
    {
        var key = args[i];
        var value = args[i + 1];
        if (!key.StartsWith("--", StringComparison.Ordinal))
            continue;
        result[key] = value;
    }

    return result;
}

/// <summary>
/// Works out which directory inside the extracted archive holds the launcher that is being
/// replaced, and which directory of the installed copy it corresponds to.
/// </summary>
static (string SourceRoot, string DestinationRoot, string LauncherRelativePath) ResolveLayout(
    string extractRoot,
    string targetDir,
    string launcherName)
{
    var candidates = Directory.GetFiles(extractRoot, launcherName, SearchOption.AllDirectories);
    if (candidates.Length == 0)
        throw new FileNotFoundException($"Update archive does not contain {launcherName}.");

    // The Linux package ships two files with this name: a shell wrapper at the package root
    // and the native apphost one level down (bin_x64/SS14.Launcher). The wrapper must never
    // drive the layout, otherwise the whole package gets unpacked into the launcher's own
    // directory and the wrapper is copied over the apphost, bricking the install.
    var native = candidates.Where(IsNativeExecutable).ToArray();
    var pool = native.Length > 0 ? native : candidates;

    var targetLeaf = LeafName(targetDir);
    var sourceFile = pool.FirstOrDefault(c => string.Equals(LeafName(Path.GetDirectoryName(c)), targetLeaf, StringComparison.Ordinal))
                     ?? pool.OrderByDescending(c => new FileInfo(c).Length).First();

    var sourceRoot = Path.GetDirectoryName(sourceFile)!;
    var launcherRelativePath = Path.GetRelativePath(extractRoot, sourceFile);
    var launcherRelativeDir = Path.GetDirectoryName(launcherRelativePath) ?? "";

    // Walk up from the launcher's own directory by as many levels as the launcher sits below
    // the package root (one on Linux, zero on Windows). That recovers the package root of the
    // installation, so the wrapper, the bundled runtime and the other architecture dirs are
    // refreshed too instead of only bin_*.
    var destinationRoot = targetDir;
    if (launcherRelativeDir.Length > 0)
    {
        foreach (var part in launcherRelativeDir.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Reverse())
        {
            if (!string.Equals(LeafName(destinationRoot), part, StringComparison.Ordinal))
                return (sourceRoot, targetDir, launcherName);

            var parent = Path.GetDirectoryName(destinationRoot);
            if (string.IsNullOrEmpty(parent))
                return (sourceRoot, targetDir, launcherName);

            destinationRoot = parent;
        }
    }

    // The file the running launcher occupies must end up being exactly the archived apphost.
    var expectedPath = Path.GetFullPath(Path.Combine(targetDir, launcherName));
    var resolvedPath = Path.GetFullPath(Path.Combine(destinationRoot, launcherRelativePath));
    if (!string.Equals(expectedPath, resolvedPath, StringComparison.Ordinal))
        return (sourceRoot, targetDir, launcherName);

    return (extractRoot, destinationRoot, launcherRelativePath);
}

static bool IsNativeExecutable(string path)
{
    try
    {
        using var fs = File.OpenRead(path);
        Span<byte> header = stackalloc byte[4];
        var read = fs.Read(header);
        if (read < 2)
            return false;

        if (read >= 4 && header[0] == 0x7F && header[1] == (byte)'E' && header[2] == (byte)'L' && header[3] == (byte)'F')
            return true;

        return header[0] == (byte)'M' && header[1] == (byte)'Z';
    }
    catch
    {
        return false;
    }
}

static string LeafName(string? path)
{
    if (string.IsNullOrEmpty(path))
        return "";

    return Path.GetFileName(Path.TrimEndingDirectorySeparator(Path.GetFullPath(path)));
}

/// <summary>
///     Files this process currently executes or has mapped into memory. Replacing them in
///     place either fails (running executable) or silently corrupts the running process
///     (mapped managed assembly), so they must go through the .pending staging path.
/// </summary>
static HashSet<string> GetPathsInUseBySelf()
{
    var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    var exePath = Process.GetCurrentProcess().MainModule?.FileName;
    if (!string.IsNullOrEmpty(exePath))
        result.Add(Path.GetFullPath(exePath));

    var entryAssembly = Assembly.GetEntryAssembly()?.Location;
    if (!string.IsNullOrEmpty(entryAssembly))
    {
        result.Add(Path.GetFullPath(entryAssembly));
        result.Add(Path.GetFullPath(Path.ChangeExtension(entryAssembly, ".pdb")));
    }

    return result;
}

static void CopyTree(string sourceRoot, string targetRoot, HashSet<string> excludedRootDirs)
{
    sourceRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(sourceRoot));
    targetRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(targetRoot));

    foreach (var dir in Directory.GetDirectories(sourceRoot, "*", SearchOption.AllDirectories))
    {
        var rel = Path.GetRelativePath(sourceRoot, dir);
        if (ShouldSkip(rel, excludedRootDirs))
            continue;

        Directory.CreateDirectory(Path.Combine(targetRoot, rel));
    }

    var inUse = GetPathsInUseBySelf();
    foreach (var file in Directory.GetFiles(sourceRoot, "*", SearchOption.AllDirectories))
    {
        var rel = Path.GetRelativePath(sourceRoot, file);
        if (ShouldSkip(rel, excludedRootDirs))
            continue;

        var dest = Path.Combine(targetRoot, rel);
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);

        if (inUse.Contains(Path.GetFullPath(dest)))
        {
            // Files this process is currently executing or has mapped cannot be replaced
            // in place: overwriting a running executable is refused outright, and overwriting
            // a mapped managed assembly corrupts the IL the JIT still has to read.
            // Stage the replacement and let the launcher finalize it on next startup.
            CopyPreservingMode(file, dest + ".pending");
            continue;
        }

        try
        {
            if (File.Exists(dest))
                File.SetAttributes(dest, FileAttributes.Normal);

            CopyPreservingMode(file, dest);
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            // Destination is still in use (ETXTBSY for a running executable on Linux).
            // Stage the replacement instead of aborting the whole update.
            CopyPreservingMode(file, dest + ".pending");
        }
    }
}

static void CopyPreservingMode(string source, string destination)
{
    File.Copy(source, destination, true);

    if (OperatingSystem.IsWindows())
        return;

    File.SetUnixFileMode(destination, File.GetUnixFileMode(source));
}

static bool ShouldSkip(string relativePath, HashSet<string> excludedRootDirs)
{
    var first = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
        .FirstOrDefault() ?? "";
    return excludedRootDirs.Contains(first);
}

static void RelaunchLauncher(string installedLauncher, string destinationRoot, string launcherName)
{
    var entryPoint = installedLauncher;

    if (!OperatingSystem.IsWindows())
    {
        // On Linux the package root carries a shell wrapper with the same file name that points
        // DOTNET_ROOT at the bundled runtime. Prefer it when it exists.
        var wrapper = Path.Combine(destinationRoot, launcherName);
        if (File.Exists(wrapper) &&
            !string.Equals(Path.GetFullPath(wrapper), Path.GetFullPath(installedLauncher), StringComparison.Ordinal))
        {
            entryPoint = wrapper;
        }
    }

    EnsureExecutable(installedLauncher);
    if (!string.Equals(Path.GetFullPath(entryPoint), Path.GetFullPath(installedLauncher), StringComparison.Ordinal))
        EnsureExecutable(entryPoint);

    var psi = new ProcessStartInfo
    {
        FileName = entryPoint,
        UseShellExecute = OperatingSystem.IsWindows(),
        WorkingDirectory = Path.GetDirectoryName(entryPoint) ?? destinationRoot
    };

    if (!OperatingSystem.IsWindows() &&
        string.Equals(Path.GetFullPath(entryPoint), Path.GetFullPath(installedLauncher), StringComparison.Ordinal))
    {
        // Executing the apphost directly skips the wrapper that exports DOTNET_ROOT.
        var runtimeDir = FindBundledRuntime(destinationRoot);
        if (runtimeDir != null)
            psi.Environment["DOTNET_ROOT"] = runtimeDir;
    }

    Process.Start(psi);
}

static void EnsureExecutable(string path)
{
    if (OperatingSystem.IsWindows() || !File.Exists(path))
        return;

    try
    {
        var mode = File.GetUnixFileMode(path);
        File.SetUnixFileMode(path, mode | UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute);
    }
    catch
    {
        // Best effort, Process.Start will report the real problem.
    }
}

static string? FindBundledRuntime(string startDir)
{
    string[] names = ["dotnet_x64", "dotnet_arm64", "dotnet"];

    var dir = Path.TrimEndingDirectorySeparator(Path.GetFullPath(startDir));
    for (var i = 0; i < 4 && !string.IsNullOrEmpty(dir); i++)
    {
        foreach (var name in names)
        {
            var candidate = Path.Combine(dir, name);
            if (Directory.Exists(candidate))
                return candidate;
        }

        dir = Path.GetDirectoryName(dir) ?? "";
    }

    return null;
}
