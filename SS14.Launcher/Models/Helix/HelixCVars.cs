namespace SS14.Launcher.Models.Data;

public static partial class CVars
{
    /// <summary>
    /// Enable BomBom Discord Rich Presence.
    /// </summary>
    public static readonly CVarDef<bool> HelixDiscordRichPresenceEnabled =
        CVarDef.Create("HelixDiscordRichPresenceEnabled", true);

    /// <summary>
    /// Controls what should be shown in Discord Rich Presence while the SS14 client is running.
    /// </summary>
    public static readonly CVarDef<int> HelixDiscordInGamePresenceMode =
        CVarDef.Create("HelixDiscordInGamePresenceMode", 0);
}
