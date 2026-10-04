using BomBom.Game.Patches;
using BomBom.Game.Patches.BomBomPorts;
using BomBom.Misc;
using BomBom.Stealthsey;

namespace BomBom.Config;

/// <summary>
/// Variables used and changed at runtime
/// </summary>
public static class BomBomConf
{
    /// <summary>
    /// Defines how strict is Hidesey
    /// </summary>
    public static HideLevel BomBomHide = HideLevel.Normal;

    /// <summary>
    /// Log patcher output to separate file
    /// </summary>
    public static bool SeparateLogger;

    /// <summary>
    /// Should we log anything from the loader
    /// </summary>
    public static bool Logging;

    /// <summary>
    /// Log DEBG messages
    /// </summary>
    public static bool DebugAllowed;

    /// <summary>
    /// Log TRCE messages
    /// </summary>
    public static bool TraceAllowed;

    /// <summary>
    /// Throws an exception on client if any patch had failed applying.
    /// </summary>
    public static bool ThrowOnFail;

    /// <see cref="HWID"/>
    public static bool ForceHWID;

    /// <see cref="DiscordRPC.Disable"/>
    public static bool KillRPC;

    /// <see cref="DiscordRPC.Fake"/>
    public static bool FakeRPC;

    /// <see cref="Jammer"/>
    public static bool JamDials;

    /// <see cref="Blackhole"/>
    public static bool DisableREC;

    /// <summary>
    /// Enables backports and fixes for the game
    /// </summary>
    /// <see cref="BomBom.Game.Patches.BomBomPorts.BomBomPortMan"/>
    public static bool Backports;

    /// <summary>
    /// Disable any backports
    /// </summary>
    public static bool DisableAnyBackports;

    /// <summary>
    /// Don't run any patches
    /// </summary>
    public static bool Patchless;

    /// <summary>
    /// Reflect changes made here to the Dictionary in the launcher's Connector.cs
    /// </summary>
    public static readonly Dictionary<string, Action<string>> EnvVarMap = new Dictionary<string, Action<string>>
    {
        { "BOMBOM_LOGGING", value => Logging = value == "true" },
        { "BOMBOM_LOADER_DEBUG", value => DebugAllowed = value == "true" },
        { "BOMBOM_LOADER_TRACE", value => TraceAllowed = value == "true" },
        { "BOMBOM_THROW_FAIL", value => ThrowOnFail = value == "true" },
        { "BOMBOM_SEPARATE_LOGGER", value => SeparateLogger = value == "true" },
        { "BOMBOM_FORCINGHWID", value => ForceHWID = value == "true" },
        { "BOMBOM_FORCEDHWID", value => HWID.SetHWID(value)},
        { "BOMBOM_DISABLE_PRESENCE", value => KillRPC = value == "true" },
        { "BOMBOM_FAKE_PRESENCE", value => FakeRPC = value == "true"},
        { "BOMBOM_PRESENCE_USERNAME", value => DiscordRPC.SetUsername(value)},
        { "BOMBOM_JAMMER", value => JamDials = value == "true" },
        { "BOMBOM_DISABLE_REC", value => DisableREC = value == "true" },
        { "BOMBOM_BACKPORTS", value => Backports = value == "true" },
        { "BOMBOM_NO_ANY_BACKPORTS", value => DisableAnyBackports = value == "true" },
        { "BOMBOM_FORKID", BomBomPortMan.SetForkID },
        { "BOMBOM_ENGINE", BomBomPortMan.SetEngineVer },
        { "BOMBOM_HIDE_LEVEL", value => BomBomHide = (HideLevel)Enum.Parse(typeof(HideLevel), value) },
        { "BOMBOM_PATCHLESS", value => Patchless = value == "true"}
    };

    // Conf variables that do not go into the EnvVarMap go here

    /// <summary>
    /// Wait for a debugger to attach in the loader process before executing anything
    /// </summary>
    public static bool JumpLoaderDebug;
}
