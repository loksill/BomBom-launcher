using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using Serilog;

namespace SS14.Launcher.Models.Helix;

public sealed class RecentServerManager
{
    private const int MaxEntries = 8;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _recentServersPath;

    public ObservableCollection<RecentServerEntry> Entries { get; } = new();

    public RecentServerManager()
    {
        _recentServersPath = Path.Combine(LauncherPaths.DirUserData, "helix_recent_servers.json");
    }

    public void Initialize()
    {
        Load();
    }

    public void RememberServer(string address, string? displayName = null)
    {
        if (string.IsNullOrWhiteSpace(address))
            return;

        var existing = Entries.FirstOrDefault(e => string.Equals(e.Address, address, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            Entries.Remove(existing);
            displayName ??= existing.DisplayName;
        }

        Entries.Insert(0, new RecentServerEntry(address, string.IsNullOrWhiteSpace(displayName) ? address : displayName!));

        while (Entries.Count > MaxEntries)
        {
            Entries.RemoveAt(Entries.Count - 1);
        }

        Save();
    }

    public void Clear()
    {
        Entries.Clear();
        Save();
    }

    private void Load()
    {
        Entries.Clear();

        if (!File.Exists(_recentServersPath))
            return;

        try
        {
            var json = File.ReadAllText(_recentServersPath);
            var loaded = JsonSerializer.Deserialize<RecentServerEntry[]>(json, JsonOptions) ?? Array.Empty<RecentServerEntry>();

            foreach (var entry in loaded.Where(e => !string.IsNullOrWhiteSpace(e.Address)))
            {
                Entries.Add(entry);
            }
        }
        catch (Exception e)
        {
            Log.Warning(e, "Failed to load recent servers");
        }
    }

    private void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(Entries.ToArray(), JsonOptions);
            File.WriteAllText(_recentServersPath, json);
        }
        catch (Exception e)
        {
            Log.Warning(e, "Failed to save recent servers");
        }
    }
}

public sealed record RecentServerEntry(string Address, string DisplayName);
