using System;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using SS14.Launcher.Utility;

namespace SS14.Launcher.Models.Data;

/// <summary>
/// Contains definitions for all launcher configuration values.
/// </summary>
/// <remarks>
/// The fields of this class are automatically searched for all CVar definitions.
/// </remarks>
/// <see cref="DataManager"/>
[UsedImplicitly]
public static partial class CVars
{
    /// <summary>
    /// Default to using compatibility options for rendering etc,
    /// that are less likely to immediately crash on buggy drivers.
    /// </summary>
    public static readonly CVarDef<bool> CompatMode = CVarDef.Create("CompatMode", false);

    /// <summary>
    /// On first launch, the launcher tells you that SS14 is EARLY ACCESS.
    /// This stores whether they dismissed that, though people will insist on pretending it defaults to true.
    /// </summary>
    public static readonly CVarDef<bool> HasDismissedEarlyAccessWarning
        = CVarDef.Create("HasDismissedEarlyAccessWarning", false);

    /// <summary>
    /// Used to warn users about the degradation of the Intel 13th and 14th generation CPUs
    /// This has proven multiple times to cause issues with game startup due to some memory access issue after enough degradation.
    /// <see href="https://www.reddit.com/r/intel/comments/1egthzw/megathread_for_intel_core_13th_14th_gen_cpu/"/>
    /// </summary>
    public static readonly CVarDef<bool> HasDismissedIntelDegradation
        = CVarDef.Create("HasDismissedIntelDegradation", false);

    /// <summary>
    /// Used to warn Apple Silicon users who are running the game under Rosetta 2 when they could be running the native build.
    /// </summary>
    public static readonly CVarDef<bool> HasDismissedRosettaWarning
        = CVarDef.Create("HasDismissedRosettaWarning", false);

    /// <summary>
    /// Disable checking engine build signatures when launching game.
    /// Only enable if you know what you're doing.
    /// </summary>
    /// <remarks>
    /// This is ignored on release builds, for security reasons.
    /// </remarks>
    public static readonly CVarDef<bool> DisableSigning = CVarDef.Create("DisableSigning", false);

    /// <summary>
    /// Enable local overriding of engine versions.
    /// </summary>
    /// <remarks>
    /// If enabled and on a development build,
    /// the launcher will pull all engine versions and modules from <see cref="EngineOverridePath"/>.
    /// This can be set to <c>RobustToolbox/release/</c> to instantly pull in packaged engine builds.
    /// </remarks>
    public static readonly CVarDef<bool> EngineOverrideEnabled = CVarDef.Create("EngineOverrideEnabled", false);

    /// <summary>
    /// Path to load engines from when using <see cref="EngineOverrideEnabled"/>.
    /// </summary>
    public static readonly CVarDef<string> EngineOverridePath = CVarDef.Create("EngineOverridePath", "");

    /// <summary>
    /// Verbose logging of launcher logs.
    /// </summary>
    public static readonly CVarDef<bool> LogLauncherVerbose = CVarDef.Create("LogLauncherVerbose", false);

    /// <summary>
    /// Enable multi-account support on release builds.
    /// </summary>
    public static readonly CVarDef<bool> MultiAccounts = CVarDef.Create("MultiAccounts", false);

    /// <summary>
    /// Currently selected login in the drop down.
    /// </summary>
    public static readonly CVarDef<string> SelectedLogin = CVarDef.Create("SelectedLogin", "");

    public static readonly CVarDef<string> Fingerprint = CVarDef.Create("Fingerprint", "");

    /// <summary>
    /// Maximum amount of TOTAL versions to keep in the content database.
    /// </summary>
    public static readonly CVarDef<int> MaxVersionsToKeep = CVarDef.Create("MaxVersionsToKeep", 15);

    /// <summary>
    /// Maximum amount of versions to keep of a specific fork ID.
    /// </summary>
    public static readonly CVarDef<int> MaxForkVersionsToKeep = CVarDef.Create("MaxForkVersionsToKeep", 3);

     /// <summary>
    /// If a download gets interrupted, keep the files for a week.
    /// </summary>
    public static readonly CVarDef<int> InterruptibleDownloadKeepHours = CVarDef.Create("InterruptibleDownloadKeepHours", 7 * 24);

    /// <summary>
    /// Whether to display override assets (trans rights).
    /// </summary>
    public static readonly CVarDef<bool> OverrideAssets = CVarDef.Create("OverrideAssets", true);

    /// <summary>
    /// Stores the minimum player count value used by the "minimum player count" filter.
    /// </summary>
    /// <seealso cref="ServerFilter.PlayerCountMin"/>
    public static readonly CVarDef<int> FilterPlayerCountMinValue = CVarDef.Create("FilterPlayerCountMinValue", 0);

    /// <summary>
    /// Stores the maximum player count value used by the "maximum player count" filter.
    /// </summary>
    /// <seealso cref="ServerFilter.PlayerCountMax"/>
    public static readonly CVarDef<int> FilterPlayerCountMaxValue = CVarDef.Create("FilterPlayerCountMaxValue", 0);

    // Helix-Start
    /// <summary>
    /// Show map column in server lists.
    /// </summary>
    public static readonly CVarDef<bool> ServerListShowMap = CVarDef.Create("ServerListShowMap", true);

    /// <summary>
    /// Show mode column in server lists.
    /// </summary>
    public static readonly CVarDef<bool> ServerListShowMode = CVarDef.Create("ServerListShowMode", true);

    /// <summary>
    /// Show ping column in server lists.
    /// </summary>
    public static readonly CVarDef<bool> ServerListShowPing = CVarDef.Create("ServerListShowPing", true);
    // Helix-End

    /// <summary>
    /// Stores whether the user has seen the Wine warning.
    /// </summary>
    public static readonly CVarDef<bool> WineWarningShown = CVarDef.Create("WineWarningShown", false);

    /// <summary>
    /// Language the user selected. Null means it should be automatically selected based on system language.
    /// </summary>
    public static readonly CVarDef<string?> Language = CVarDef.Create<string?>("Language", null);

    public static readonly CVarDef<int> Theme = CVarDef.Create("Theme", 0);
    public static readonly CVarDef<bool> ThemeGradient = CVarDef.Create("ThemeGradient", true);
    public static readonly CVarDef<bool> ThemeDecor = CVarDef.Create("ThemeDecor", true);
    public static readonly CVarDef<string> ThemeFont = CVarDef.Create("ThemeFont", "avares://SS14.Launcher/Assets/Fonts/noto_sans/*.ttf#Noto Sans");
    public static readonly CVarDef<string> ThemeCustomBackground = CVarDef.Create("ThemeCustomBackground", "#25252A");
    public static readonly CVarDef<string> ThemeCustomAccent = CVarDef.Create("ThemeCustomAccent", "#3E6C45");
    public static readonly CVarDef<string> ThemeCustomForeground = CVarDef.Create("ThemeCustomForeground", "#EEEEEE");
    public static readonly CVarDef<string> ThemeCustomPopup = CVarDef.Create("ThemeCustomPopup", "#202025");
    public static readonly CVarDef<string> ThemeCustomGradientStart = CVarDef.Create("ThemeCustomGradientStart", "#25252A");
    public static readonly CVarDef<string> ThemeCustomGradientEnd = CVarDef.Create("ThemeCustomGradientEnd", "#2E3746");

    /// <summary>
    /// The CPU architecture this launcher was last run with.
    /// </summary>
    /// <remarks>
    /// Used to delete engine builds of other architectures on startup.
    /// Defaults to x64 so that people upgrading to a proper ARM64 launcher on e.g. Apple Silicon
    /// properly get their existing installations cleared.
    /// </remarks>
    public static readonly CVarDef<int> CurrentArchitecture = CVarDef.Create("CurrentArchitecture", (int) Architecture.X64);

    // BomBom plugin system start here

    // Stealthsey

    /// <summary>
    /// Define strict level
    /// </summary>
    public static readonly CVarDef<int> BomBomHide = CVarDef.Create("HideLevel", 2);

    // Logging

    /// <summary>
    /// Log messages coming from patches
    /// </summary>
    public static readonly CVarDef<bool> LogPatcher = CVarDef.Create("LogPatcher", true);

    /// <summary>
    /// Log debug messages coming from loader
    /// </summary>
    public static readonly CVarDef<bool> LogLoaderDebug = CVarDef.Create("LogLoaderDebug", false);

    /// <summary>
    /// Log patcher output to a separate file
    /// </summary>
    public static readonly CVarDef<bool> SeparateLogging = CVarDef.Create("SeparateLogging", false);

    /// <summary>
    /// Log patcher output in launcher
    /// </summary>
    public static readonly CVarDef<bool> LogLauncherPatcher = CVarDef.Create("LogLauncherPatcher", false);

    /// <summary>
    /// Log TRC messages
    /// </summary>
    public static readonly CVarDef<bool> LogLoaderTrace = CVarDef.Create("LogLoaderTrace", false);

    // Behavior

    /// <summary>
    /// Throw an exception if a patch fails to apply.
    /// </summary>
    public static readonly CVarDef<bool> ThrowPatchFail = CVarDef.Create("ThrowPatchFail", false);

    /// <summary>
    /// Do we disable the game's Discord RPC?
    /// </summary>
    public static readonly CVarDef<bool> DisableRPC = CVarDef.Create("DisableRPC", false);

    /// <summary>
    /// Do we fake the username on the game's Discord RPC?
    /// </summary>
    public static readonly CVarDef<bool> FakeRPC = CVarDef.Create("FakeRPC", false);

    /// <summary>
    /// Username to fake the game's Discord RPC with
    /// </summary>
    public static readonly CVarDef<string> RPCUsername = CVarDef.Create("RPCUsername", "");

    /// <summary>
    /// Do we disable redialing?
    /// </summary>
    public static readonly CVarDef<bool> JamDials = CVarDef.Create("JamDials", false);

    /// <summary>
    /// Do we disable remote command execution
    /// </summary>
    public static readonly CVarDef<bool> Blackhole = CVarDef.Create("Blackhole", false);

    /// <summary>
    /// Do we force a hwid value
    /// </summary>
    public static readonly CVarDef<bool> ForcingHWId = CVarDef.Create("ForcingHWId", false);

    /// <summary>
    /// Do we use the HWID value bound to LoginInfo
    /// </summary>
    public static readonly CVarDef<bool> LIHWIDBind = CVarDef.Create("LIHWIDBind", false);

    /// <summary>
    /// Apply backports to game when able
    /// </summary>
    public static readonly CVarDef<bool> Backports = CVarDef.Create("Backports", false);

    /// <summary>
    /// Disable all engine backports
    /// </summary>
    public static readonly CVarDef<bool> DisableAnyEngineBackports = CVarDef.Create("DisableAnyEngineBackports", false);

    // HWID

    /// <summary>
    /// Do we use a random HWID each time?
    /// </summary>
    public static readonly CVarDef<bool> RandHWID = CVarDef.Create("RandHWID", false);

    /// <summary>
    /// Do we automatically delete HWID from registry before connecting?
    /// </summary>
    public static readonly CVarDef<bool> AutoDeleteHWID = CVarDef.Create("AutoDeleteHWID", false);

    /// <summary>
    /// HWId to use on servers
    /// </summary>
    public static readonly CVarDef<string> ForcedHWId = CVarDef.Create("ForcedHWId", "");

    /// <summary>
    /// HWID2 - Disallow sending hwid to server.
    /// </summary>
    public static readonly CVarDef<bool> DisallowHwid = CVarDef.Create("DisallowHwid", false);

    /// <summary>
    /// Do not patch anything in the game modules
    /// </summary>
    public static readonly CVarDef<bool> Patchless = CVarDef.Create("Patchless", false);

    // Launcher self-updates

    /// <summary>
    /// Automatically install launcher updates when they become available.
    /// </summary>
    public static readonly CVarDef<bool> LauncherAutoUpdate = CVarDef.Create("LauncherAutoUpdate", false);

    /// <summary>
    /// Show a prompt when a launcher update is available.
    /// </summary>
    public static readonly CVarDef<bool> LauncherUpdateNotify = CVarDef.Create("LauncherUpdateNotify", true);

    /// <summary>
    /// Allow offering pre-release launcher builds.
    /// </summary>
    public static readonly CVarDef<bool> LauncherUpdateAllowPreRelease = CVarDef.Create("LauncherUpdateAllowPreRelease", false);

    /// <summary>
    /// GitHub repository used for launcher self-updates.
    /// </summary>
    public static readonly CVarDef<string> LauncherUpdateRepo = CVarDef.Create("LauncherUpdateRepo", "https://github.com/loksill/BomBom-launcher.git");

    // BomBom plugin system end here
}

/// <summary>
/// Base definition of a CVar.
/// </summary>
/// <seealso cref="DataManager"/>
/// <seealso cref="CVars"/>
public abstract class CVarDef
{
    public string Name { get; }
    public object? DefaultValue { get; }
    public Type ValueType { get; }

    private protected CVarDef(string name, object? defaultValue, Type type)
    {
        Name = name;
        DefaultValue = defaultValue;
        ValueType = type;
    }

    public static CVarDef<T> Create<T>(
        string name,
        T defaultValue)
    {
        return new CVarDef<T>(name, defaultValue);
    }
}

/// <summary>
/// Generic specialized definition of CVar definition.
/// </summary>
/// <typeparam name="T">The type of value stored in this CVar.</typeparam>
public sealed class CVarDef<T> : CVarDef
{
    public new T DefaultValue { get; }

    internal CVarDef(string name, T defaultValue) : base(name, defaultValue, typeof(T))
    {
        DefaultValue = defaultValue;
    }
}
