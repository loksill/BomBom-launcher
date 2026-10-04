using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Mvvm.Messaging;
using ReactiveUI;
using Splat;
using BomBom.Config;
using BomBom.Game.Patches;
using BomBom.Stealthsey;
using SS14.Launcher.BomBomverse;
using SS14.Launcher.Localization;
using SS14.Launcher.Models.ContentManagement;
using SS14.Launcher.Models.Data;
using SS14.Launcher.Models.EngineManager;
using SS14.Launcher.Models.Helix;
using SS14.Launcher.Models.Logins;
using SS14.Launcher.Models.KeybindConfigs;
using SS14.Launcher.Models.ResourcePacks;
using SS14.Launcher.Theme;
using SS14.Launcher.Utility;
using SS14.Launcher.Views;

namespace SS14.Launcher.ViewModels.MainWindowTabs;

public partial class OptionsTabViewModel : MainWindowTabViewModel
{
    public sealed record ThemeFontOption(string Name, string Descriptor);
    private const string DefaultBackground = "#25252A";
    private const string DefaultAccent = "#3E6C45";
    private const string DefaultForeground = "#EEEEEE";
    private const string DefaultPopup = "#202025";
    private const string DefaultGradientEnd = "#2E3746";
    public DataManager Cfg { get; }
    private readonly IEngineManager _engineManager;
    private readonly ContentManager _contentManager;
    private readonly KeybindConfigManager _keybindConfigManager;
    private readonly ResourcePackManager _resourcePackManager;
    private readonly LoginManager _loginManager;

    public ICommand SetHWIdCommand { get; }
    public ICommand GenHWIdCommand { get; }
    public ICommand SetRPCUsernameCommand { get; }
    public IEnumerable<HideLevel> HideLevels { get; } = Enum.GetValues<HideLevel>();

    public LanguageSelectorViewModel Language { get; } = new();
    public IEnumerable<LauncherTheme> Themes { get; } = Enum.GetValues(typeof(LauncherTheme)).Cast<LauncherTheme>();
    public IReadOnlyList<ThemeFontOption> ThemeFonts { get; } = new[]
    {
        new ThemeFontOption("Noto Sans", AppThemeManager.DefaultFontDescriptor),
        new ThemeFontOption("Trollfont", "avares://SS14.Launcher/Assets/Fonts/trollfont.otf#Trollfont"),
        new ThemeFontOption("Arial", "Arial"), new ThemeFontOption("Segoe UI", "Segoe UI"),
        new ThemeFontOption("Verdana", "Verdana"), new ThemeFontOption("Consolas", "Consolas")
    };
    public ObservableCollection<ResourcePackInfo> ResourcePacks { get; } = new();
    public ObservableCollection<KeybindConfigInfo> KeybindConfigs { get; } = new();
    public bool HasResourcePacks => ResourcePacks.Count > 0;
    public bool HasKeybindConfigs => KeybindConfigs.Count > 0;
    public string ResourcePacksDirectory => _resourcePackManager.PacksDirectory;
    public string KeybindConfigsDirectory => _keybindConfigManager.ConfigsDirectory;

    private Color _customThemeBackground;
    private Color _customThemeAccent;
    private Color _customThemeForeground;
    private Color _customThemePopup;
    private Color _customThemeGradientStart;
    private Color _customThemeGradientEnd;
    private ThemeFontOption? _selectedThemeFont;

    public OptionsTabViewModel()
    {
        Cfg = Locator.Current.GetRequiredService<DataManager>();
        _engineManager = Locator.Current.GetRequiredService<IEngineManager>();
        _contentManager = Locator.Current.GetRequiredService<ContentManager>();
        _resourcePackManager = Locator.Current.GetRequiredService<ResourcePackManager>();
        _keybindConfigManager = Locator.Current.GetRequiredService<KeybindConfigManager>();
        _loginManager = Locator.Current.GetRequiredService<LoginManager>();

        SetHWIdCommand = new RelayCommand(OnSetHWIdClick);
        GenHWIdCommand = new RelayCommand(OnGenHWIdClick);
        SetRPCUsernameCommand = new RelayCommand(OnSetRPCUsernameClick);

        Persist.UpdateLauncherConfig();
        SetTempHwid();

        DisableIncompatibleMacOS = OperatingSystem.IsMacOS();
        _customThemeBackground = ParseColor(Cfg.GetCVar(CVars.ThemeCustomBackground), DefaultBackground);
        _customThemeAccent = ParseColor(Cfg.GetCVar(CVars.ThemeCustomAccent), DefaultAccent);
        _customThemeForeground = ParseColor(Cfg.GetCVar(CVars.ThemeCustomForeground), DefaultForeground);
        _customThemePopup = ParseColor(Cfg.GetCVar(CVars.ThemeCustomPopup), DefaultPopup);
        _customThemeGradientStart = ParseColor(Cfg.GetCVar(CVars.ThemeCustomGradientStart), DefaultBackground);
        _customThemeGradientEnd = ParseColor(Cfg.GetCVar(CVars.ThemeCustomGradientEnd), DefaultGradientEnd);
        _selectedThemeFont = ThemeFonts.FirstOrDefault(font => font.Descriptor == Cfg.GetCVar(CVars.ThemeFont)) ?? ThemeFonts[0];
        ReloadResourcePacks();
        ReloadKeybindConfigs();
    }
    public bool DisableIncompatibleMacOS { get; }

    public ThemeFontOption? SelectedThemeFont
    {
        get => _selectedThemeFont;
        set
        {
            if (value == null || Equals(_selectedThemeFont, value)) return;
            _selectedThemeFont = value;
            Cfg.SetCVar(CVars.ThemeFont, value.Descriptor);
            Cfg.CommitConfig();
            if (Application.Current != null) AppThemeManager.ApplyFont(Application.Current, value.Descriptor);
            this.RaisePropertyChanged(nameof(SelectedThemeFont));
        }
    }

    public void ApplyCustomFontFile(string path)
    {
        var descriptor = $"{new Uri(path).AbsoluteUri}#{System.IO.Path.GetFileNameWithoutExtension(path)}";
        Cfg.SetCVar(CVars.ThemeFont, descriptor);
        Cfg.CommitConfig();
        if (Application.Current != null) AppThemeManager.ApplyFont(Application.Current, descriptor);
        _selectedThemeFont = null;
        this.RaisePropertyChanged(nameof(SelectedThemeFont));
    }

    public LauncherTheme SelectedTheme
    {
        get => AppThemeManager.Normalize(Cfg.GetCVar(CVars.Theme));
        set
        {
            Cfg.SetCVar(CVars.Theme, (int)value);
            Cfg.CommitConfig();
            ApplyTheme();
            this.RaisePropertyChanged(nameof(SelectedTheme));
            this.RaisePropertyChanged(nameof(IsCustomThemeSelected));
        }
    }

    public bool IsCustomThemeSelected => SelectedTheme == LauncherTheme.Custom;

    public bool ThemeGradient { get => Cfg.GetCVar(CVars.ThemeGradient); set { Cfg.SetCVar(CVars.ThemeGradient, value); Cfg.CommitConfig(); ApplyTheme(); this.RaisePropertyChanged(nameof(ThemeGradient)); } }
    public bool ThemeDecor { get => Cfg.GetCVar(CVars.ThemeDecor); set { Cfg.SetCVar(CVars.ThemeDecor, value); Cfg.CommitConfig(); ApplyTheme(); this.RaisePropertyChanged(nameof(ThemeDecor)); } }

    public Color CustomThemeBackground { get => _customThemeBackground; set => SetColor(ref _customThemeBackground, value, CVars.ThemeCustomBackground, nameof(CustomThemeBackground)); }
    public Color CustomThemeAccent { get => _customThemeAccent; set => SetColor(ref _customThemeAccent, value, CVars.ThemeCustomAccent, nameof(CustomThemeAccent)); }
    public Color CustomThemeForeground { get => _customThemeForeground; set => SetColor(ref _customThemeForeground, value, CVars.ThemeCustomForeground, nameof(CustomThemeForeground)); }
    public Color CustomThemePopup { get => _customThemePopup; set => SetColor(ref _customThemePopup, value, CVars.ThemeCustomPopup, nameof(CustomThemePopup)); }
    public Color CustomThemeGradientStart { get => _customThemeGradientStart; set => SetColor(ref _customThemeGradientStart, value, CVars.ThemeCustomGradientStart, nameof(CustomThemeGradientStart)); }
    public Color CustomThemeGradientEnd { get => _customThemeGradientEnd; set => SetColor(ref _customThemeGradientEnd, value, CVars.ThemeCustomGradientEnd, nameof(CustomThemeGradientEnd)); }

    public string ExportCustomThemeJson() => JsonSerializer.Serialize(new ThemePreset
    {
        Background = FormatColor(CustomThemeBackground), Accent = FormatColor(CustomThemeAccent), Foreground = FormatColor(CustomThemeForeground), Popup = FormatColor(CustomThemePopup),
        GradientStart = FormatColor(CustomThemeGradientStart), GradientEnd = FormatColor(CustomThemeGradientEnd), GradientEnabled = ThemeGradient, DecorEnabled = ThemeDecor
    }, new JsonSerializerOptions { WriteIndented = true });

    public bool TryImportCustomThemeJson(string json)
    {
        try
        {
            var preset = JsonSerializer.Deserialize<ThemePreset>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (preset == null) return false;
            CustomThemeBackground = ParseColor(preset.Background, DefaultBackground); CustomThemeAccent = ParseColor(preset.Accent, DefaultAccent);
            CustomThemeForeground = ParseColor(preset.Foreground, DefaultForeground); CustomThemePopup = ParseColor(preset.Popup, DefaultPopup);
            CustomThemeGradientStart = ParseColor(preset.GradientStart, DefaultBackground); CustomThemeGradientEnd = ParseColor(preset.GradientEnd, DefaultGradientEnd);
            ThemeGradient = preset.GradientEnabled ?? true; ThemeDecor = preset.DecorEnabled ?? true; SelectedTheme = LauncherTheme.Custom;
            return true;
        }
        catch { return false; }
    }

    public void ResetCustomTheme()
    {
        CustomThemeBackground = Color.Parse(DefaultBackground); CustomThemeAccent = Color.Parse(DefaultAccent); CustomThemeForeground = Color.Parse(DefaultForeground);
        CustomThemePopup = Color.Parse(DefaultPopup); CustomThemeGradientStart = Color.Parse(DefaultBackground); CustomThemeGradientEnd = Color.Parse(DefaultGradientEnd);
        ThemeGradient = true; ThemeDecor = true; SelectedTheme = LauncherTheme.Custom;
    }

    public void ReloadResourcePacks()
    {
        ResourcePacks.Clear();
        foreach (var pack in _resourcePackManager.LoadPacks()) ResourcePacks.Add(pack);
        this.RaisePropertyChanged(nameof(HasResourcePacks));
        this.RaisePropertyChanged(nameof(ResourcePacksDirectory));
    }

    public void SaveResourcePacks() => _resourcePackManager.SavePacks(ResourcePacks);
    public void OpenResourcePacksDirectory() => OpenDirectory(ResourcePacksDirectory);

    public void MoveResourcePack(ResourcePackInfo pack, int delta)
    {
        var index = ResourcePacks.IndexOf(pack);
        var next = index + delta;
        if (index < 0 || next < 0 || next >= ResourcePacks.Count) return;
        ResourcePacks.Move(index, next);
        SaveResourcePacks();
    }

    public void ReloadKeybindConfigs()
    {
        KeybindConfigs.Clear();
        foreach (var config in _keybindConfigManager.LoadConfigs()) KeybindConfigs.Add(config);
        this.RaisePropertyChanged(nameof(HasKeybindConfigs));
        this.RaisePropertyChanged(nameof(KeybindConfigsDirectory));
    }

    public void OpenKeybindConfigsDirectory() => OpenDirectory(KeybindConfigsDirectory);
    public void SelectKeybindConfig(KeybindConfigInfo config) { _keybindConfigManager.SelectConfig(config); ReloadKeybindConfigs(); }
    public void DeleteKeybindConfig(KeybindConfigInfo config) { _keybindConfigManager.DeleteConfig(config); ReloadKeybindConfigs(); }
    public void ClearKeybindConfigSelection() { _keybindConfigManager.ClearSelection(); ReloadKeybindConfigs(); }
    public void ImportCurrentKeybinds() { _keybindConfigManager.ImportCurrentKeybinds(); ReloadKeybindConfigs(); }

    private static void OpenDirectory(string path)
    {
        System.IO.Directory.CreateDirectory(path);
        Process.Start(new ProcessStartInfo { UseShellExecute = true, FileName = path });
    }

    private void SetColor(ref Color field, Color value, CVarDef<string> cvar, string property)
    {
        if (field == value) return;
        field = value; Cfg.SetCVar(cvar, FormatColor(value)); Cfg.CommitConfig();
        if (IsCustomThemeSelected) ApplyTheme();
        this.RaisePropertyChanged(property);
    }

    private void ApplyTheme()
    {
        if (Application.Current == null) return;
        AppThemeManager.ApplyTheme(Application.Current, SelectedTheme, ThemeGradient, ThemeDecor,
            new AppThemeManager.CustomThemeColors(CustomThemeBackground, CustomThemeAccent, CustomThemeForeground, CustomThemePopup, CustomThemeGradientStart, CustomThemeGradientEnd));
        if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: MainWindow window })
            window.RefreshTitleBarColors();
    }

    private static Color ParseColor(string? value, string fallback) { try { return Color.Parse(value ?? fallback); } catch { return Color.Parse(fallback); } }
    private static string FormatColor(Color color) => $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
    private sealed class ThemePreset { public string? Background { get; set; } public string? Accent { get; set; } public string? Foreground { get; set; } public string? Popup { get; set; } public string? GradientStart { get; set; } public string? GradientEnd { get; set; } public bool? GradientEnabled { get; set; } public bool? DecorEnabled { get; set; } }

    public override string Name => LocalizationManager.Instance.GetString("tab-options-title");

    public bool CompatMode
    {
        get => Cfg.GetCVar(CVars.CompatMode);
        set
        {
            Cfg.SetCVar(CVars.CompatMode, value);
            Cfg.CommitConfig();
        }
    }

    public bool LogLauncherVerbose
    {
        get => Cfg.GetCVar(CVars.LogLauncherVerbose);
        set
        {
            Cfg.SetCVar(CVars.LogLauncherVerbose, value);
            Cfg.CommitConfig();
        }
    }

    public bool OverrideAssets
    {
        get => Cfg.GetCVar(CVars.OverrideAssets);
        set
        {
            Cfg.SetCVar(CVars.OverrideAssets, value);
            Cfg.CommitConfig();
        }
    }

    // Helix-Start
    public bool ServerListShowMap
    {
        get => Cfg.GetCVar(CVars.ServerListShowMap);
        set
        {
            Cfg.SetCVar(CVars.ServerListShowMap, value);
            Cfg.CommitConfig();
            NotifyServerListDisplaySettingsChanged();
        }
    }

    public bool ServerListShowMode
    {
        get => Cfg.GetCVar(CVars.ServerListShowMode);
        set
        {
            Cfg.SetCVar(CVars.ServerListShowMode, value);
            Cfg.CommitConfig();
            NotifyServerListDisplaySettingsChanged();
        }
    }

    public bool ServerListShowPing
    {
        get => Cfg.GetCVar(CVars.ServerListShowPing);
        set
        {
            Cfg.SetCVar(CVars.ServerListShowPing, value);
            Cfg.CommitConfig();
            NotifyServerListDisplaySettingsChanged();
        }
    }

    public bool HelixDiscordRichPresenceEnabled
    {
        get => Cfg.GetCVar(CVars.HelixDiscordRichPresenceEnabled);
        set
        {
            Cfg.SetCVar(CVars.HelixDiscordRichPresenceEnabled, value);
            Cfg.CommitConfig();
            NotifyHelixDiscordRichPresenceSettingsChanged();
        }
    }

    public bool HelixDiscordInGameUseOriginal
    {
        get => GetHelixDiscordInGamePresenceMode() == HelixDiscordInGamePresenceMode.Original;
        set
        {
            if (value)
                SetHelixDiscordInGamePresenceMode(HelixDiscordInGamePresenceMode.Original);
        }
    }

    public bool HelixDiscordInGameUseHelix
    {
        get => GetHelixDiscordInGamePresenceMode() == HelixDiscordInGamePresenceMode.Helix;
        set
        {
            if (value)
                SetHelixDiscordInGamePresenceMode(HelixDiscordInGamePresenceMode.Helix);
        }
    }

    private HelixDiscordInGamePresenceMode GetHelixDiscordInGamePresenceMode()
    {
        var value = Cfg.GetCVar(CVars.HelixDiscordInGamePresenceMode);
        return HelixDiscordPresenceSettings.GetInGamePresenceMode(value);
    }

    private void SetHelixDiscordInGamePresenceMode(HelixDiscordInGamePresenceMode mode)
    {
        Cfg.SetCVar(CVars.HelixDiscordInGamePresenceMode, (int)mode);
        Cfg.CommitConfig();
        NotifyHelixDiscordRichPresenceSettingsChanged();
    }

    private static void NotifyHelixDiscordRichPresenceSettingsChanged()
    {
        WeakReferenceMessenger.Default.Send(new HelixDiscordRichPresenceSettingsChanged());
    }
    // Helix-End

    // BomBom plugin system start here

    public HideLevel HideLevel
    {
        get => (HideLevel)Cfg.GetCVar(CVars.BomBomHide);
        set
        {
            Cfg.SetCVar(CVars.BomBomHide, (int)value);
            this.RaisePropertyChanged(nameof(HideLevel));
            Cfg.CommitConfig();
        }
    }

    public bool LogPatches
    {
        get => Cfg.GetCVar(CVars.LogPatcher);
        set
        {
            Cfg.SetCVar(CVars.LogPatcher, value);
            Cfg.CommitConfig();
        }
    }

    public bool LogLauncherPatcher
    {
        get => Cfg.GetCVar(CVars.LogLauncherPatcher);
        set
        {
            Cfg.SetCVar(CVars.LogLauncherPatcher, value);
            Cfg.CommitConfig();
            Persist.UpdateLauncherConfig();
        }
    }

    public bool LogLoaderDebug
    {
        get => Cfg.GetCVar(CVars.LogLoaderDebug);
        set
        {
            Cfg.SetCVar(CVars.LogLoaderDebug, value);
            Cfg.CommitConfig();
            Persist.UpdateLauncherConfig();
        }
    }

    public bool LogLoaderTrace
    {
        get => Cfg.GetCVar(CVars.LogLoaderTrace);
        set
        {
            Cfg.SetCVar(CVars.LogLoaderTrace, value);
            Cfg.CommitConfig();
            Persist.UpdateLauncherConfig();
        }
    }

    public bool SeparateLogging
    {
        get => Cfg.GetCVar(CVars.SeparateLogging);
        set
        {
            Cfg.SetCVar(CVars.SeparateLogging, value);
            Cfg.CommitConfig();
        }
    }

    public bool ThrowPatchFail
    {
        get => Cfg.GetCVar(CVars.ThrowPatchFail);
        set
        {
            Cfg.SetCVar(CVars.ThrowPatchFail, value);
            Cfg.CommitConfig();
        }
    }

    public bool Patchless
    {
        get => Cfg.GetCVar(CVars.Patchless);
        set
        {
            Cfg.SetCVar(CVars.Patchless, value);
            Cfg.CommitConfig();
        }
    }

    public bool DisableRPC
    {
        get => Cfg.GetCVar(CVars.DisableRPC);
        set
        {
            Cfg.SetCVar(CVars.DisableRPC, value);
            Cfg.CommitConfig();
        }
    }

    public bool FakeRPC
    {
        get => Cfg.GetCVar(CVars.FakeRPC);
        set
        {
            Cfg.SetCVar(CVars.FakeRPC, value);
            this.RaisePropertyChanged(nameof(FakeRPC));
            Cfg.CommitConfig();
        }
    }

    public string RPCUsername
    {
        get => Cfg.GetCVar(CVars.RPCUsername);
        set
        {
            Cfg.SetCVar(CVars.RPCUsername, value);
            Cfg.CommitConfig();
        }
    }

    public bool DisableRedial
    {
        get => Cfg.GetCVar(CVars.JamDials);
        set
        {
            Cfg.SetCVar(CVars.JamDials, value);
            Cfg.CommitConfig();
        }
    }

    public bool Blackhole
    {
        get => Cfg.GetCVar(CVars.Blackhole);
        set
        {
            Cfg.SetCVar(CVars.Blackhole, value);
            Cfg.CommitConfig();
        }
    }

    public bool ForcingHWID
    {
        get => Cfg.GetCVar(CVars.ForcingHWId);
        set
        {
            Cfg.SetCVar(CVars.ForcingHWId, value);
            this.RaisePropertyChanged(nameof(ForcingHWID));
            Cfg.CommitConfig();
        }
    }

    public bool LIHWIDBind
    {
        get => Cfg.GetCVar(CVars.LIHWIDBind);
        set
        {
            Cfg.SetCVar(CVars.LIHWIDBind, value);
            Cfg.CommitConfig();
            SetTempHwid();
            this.RaisePropertyChanged(nameof(HWIdString));
        }
    }

    public bool RandHWID
    {
        get => Cfg.GetCVar(CVars.RandHWID);
        set
        {
            Cfg.SetCVar(CVars.RandHWID, value);
            Cfg.CommitConfig();
        }
    }

    public bool HWID2OptOut
    {
        get => Cfg.GetCVar(CVars.DisallowHwid);
        set
        {
            Cfg.SetCVar(CVars.DisallowHwid, value);
            Cfg.CommitConfig();
        }
    }

    private string _hwidString = "";
    public string HWIdString
    {
        get => _hwidString;
        set => _hwidString = value;
    }

    private Brush _hwidTextBoxBorderBrush = new SolidColorBrush(Color.Parse("#FF888888"));
    public Brush HWIDTextBoxBorderBrush
    {
        get => _hwidTextBoxBorderBrush;
        set
        {
            _hwidTextBoxBorderBrush = value;
            this.RaisePropertyChanged(nameof(HWIDTextBoxBorderBrush));
        }
    }

    public bool Backports
    {
        get => Cfg.GetCVar(CVars.Backports);
        set
        {
            Cfg.SetCVar(CVars.Backports, value);
            Cfg.CommitConfig();
        }
    }

    public bool DisableAnyEngineBackports
    {
        get => Cfg.GetCVar(CVars.DisableAnyEngineBackports);
        set
        {
            Cfg.SetCVar(CVars.DisableAnyEngineBackports, value);
            Cfg.CommitConfig();
        }
    }

    private void SetTempHwid()
    {
        if (!LIHWIDBind)
        {
            _hwidString = Cfg.GetCVar(CVars.ForcedHWId);
            return;
        }

        _hwidString = _loginManager.ActiveAccount != null ? _loginManager.ActiveAccount.LoginInfo.HWID : "";
    }

    private void OnSetHWIdClick()
    {
        Cfg.SetCVar(CVars.ForcedHWId, _hwidString);

        if (HWID.CheckHWID(_hwidString))
        {
            HWIDTextBoxBorderBrush = new SolidColorBrush(Color.Parse("#FF888888"));
            Cfg.CommitConfig();
        }
        else
        {
            HWIDTextBoxBorderBrush = new SolidColorBrush(Brushes.Red.Color);
        }

        this.RaisePropertyChanged(nameof(HWIdString));
    }

    private void OnGenHWIdClick()
    {
        HWIdString = HWID.GenerateRandom();
        OnSetHWIdClick();
    }

    private void OnSetRPCUsernameClick()
    {
        Cfg.SetCVar(CVars.RPCUsername, RPCUsername);
        Cfg.CommitConfig();
    }

    // BomBom plugin system end here

    public void ClearEngines()

    {
        _engineManager.ClearAllEngines();
    }

    public async Task<bool> ClearServerContent()
    {
        return await _contentManager.ClearAll();
    }

    public void OpenLogDirectory()
    {
        Process.Start(new ProcessStartInfo
        {
            UseShellExecute = true,
            FileName = LauncherPaths.DirLogs
        });
    }

    public void OpenAccountSettings()
    {
        Helpers.OpenUri(ConfigConstants.AccountManagementUrl);
    }

    // Helix-Start
    private static void NotifyServerListDisplaySettingsChanged()
    {
        WeakReferenceMessenger.Default.Send(new ServerListDisplaySettingsChanged());
    }
    // Helix-End
}

public sealed class ThemeDescriptionConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var key = (LauncherTheme)(value ?? LauncherTheme.Dark) switch
        {
            LauncherTheme.Light => "launcher-themes-light",
            LauncherTheme.DarkRed => "launcher-themes-dark-red",
            LauncherTheme.DarkPurple => "launcher-themes-dark-purple",
            LauncherTheme.MidnightBlue => "launcher-themes-midnight-blue",
            LauncherTheme.EmeraldDusk => "launcher-themes-emerald-dusk",
            LauncherTheme.CopperNight => "launcher-themes-copper-night",
            LauncherTheme.Custom => "launcher-themes-custom",
            _ => "launcher-themes-dark"
        };
        return LocalizationManager.Instance.GetString(key);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => value;
}

public sealed class HideLevelDescriptionConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return (HideLevel)(value ?? HideLevel.Normal) switch
        {
            HideLevel.Disabled => LocalizationManager.Instance.GetString("tab-options-plugins-hide-disabled"),
            HideLevel.Duplicit => LocalizationManager.Instance.GetString("tab-options-plugins-hide-duplicit"),
            HideLevel.Normal => LocalizationManager.Instance.GetString("tab-options-plugins-hide-normal"),
            HideLevel.Explicit => LocalizationManager.Instance.GetString("tab-options-plugins-hide-explicit"),
            HideLevel.Unconditional => LocalizationManager.Instance.GetString("tab-options-plugins-hide-unconditional"),
            _ => ""
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => value;
}
