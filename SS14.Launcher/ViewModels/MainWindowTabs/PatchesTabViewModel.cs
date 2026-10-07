using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Avalonia.Data.Converters;
using Microsoft.Toolkit.Mvvm.Input;
using Serilog;
using BomBom.Config;
using BomBom.Patches;
using BomBom.Subversion;
using BomBom.Misc;
using SS14.Launcher.BomBomverse;
using SS14.Launcher.Localization;

namespace SS14.Launcher.ViewModels.MainWindowTabs;

public class PatchesTabViewModel : MainWindowTabViewModel
{
    public override string Name => LocalizationManager.Instance.GetString("tab-plugins-title");
    public ObservableCollection<BomBomPatch> BomBomPatches { get; } = new();
    public ObservableCollection<SubverterPatch> SubverterPatches { get; } = new();
    public ICommand OpenPatchDirectoryCommand { get; }
    public ICommand ReloadModsCommand { get; }
    public ICommand EnableRefreshCommand { get; }

    public PatchesTabViewModel()
    {
        OpenPatchDirectoryCommand = new RelayCommand(OpenPatchDirectory);
        ReloadModsCommand = new RelayCommand(ReloadMods);
        EnableRefreshCommand = new RelayCommand(Refresh);
        ReloadMods();
    }

    private bool _first = true;

    private void ReloadMods()
    {
        FileHandler.LoadAssemblies();
        LoadPatches();

        if (!_first) return;

        EnableConfiguredPatches();
        _first = false;
    }

    private void LoadPatches()
    {
        LoadPatchList(BomBomfier.GetBomBomPatches(), BomBomPatches, "bombompatches");
        LoadPatchList(Subverter.GetSubverterPatches(), SubverterPatches, "subverterpatches");
    }

    private void EnableConfiguredPatches()
    {
        List<string> assemblies = Persist.LoadPatchlistConfig();
        LoadEnabledPatches(assemblies, BomBomPatches);
        LoadEnabledPatches(assemblies, SubverterPatches);
    }

    private static void OpenPatchDirectory()
    {
        string directory = BomBomPaths.BomBomDirectory;

        try
        {
            Directory.CreateDirectory(directory);
            Process.Start(new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = directory
            });
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to open mod directory {Directory}", directory);
        }
    }

    private static void LoadPatchList<T>(List<T> patches, ICollection<T> patchList, string patchName) where T : IPatch
    {
        foreach (T patch in patches.Where(patch => !patchList.Any(r => r.Equals(patch))))
        {
            patchList.Add(patch);
        }

        Log.Debug($"Refreshed {patchName}, got {patchList.Count}.");
    }

    private void Refresh()
    {
        List<string> assemblyFileNames = new();
        SaveEnabledPatches(BomBomPatches, assemblyFileNames);
        SaveEnabledPatches(SubverterPatches, assemblyFileNames);

        Log.Debug($"Saved {assemblyFileNames.Count} patches to config");
        Persist.SavePatchlistConfig(assemblyFileNames);
    }

    private static void SaveEnabledPatches(IEnumerable<IPatch> patches, List<string> fileNames)
    {
        foreach (IPatch patch in patches)
        {
            if (patch.Enabled)
            {
                fileNames.Add(Path.GetFileName(patch.Asmpath));
            }
        }
    }

    private static void LoadEnabledPatches(List<string> fileNames, IEnumerable<IPatch> patches)
    {
        foreach (IPatch patch in from filename in fileNames from patch in patches where Path.GetFileName(patch.Asmpath) == filename select patch)
        {
            patch.Enabled = true;
        }
    }
}

public class PathToFileNameConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string? path = value as string;
        return Path.GetFileName(path);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value;
    }
}

public class BooleanToPreloadConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? "(preload)" : "";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value;
    }
}
