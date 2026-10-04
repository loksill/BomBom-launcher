using BomBom.Misc;

namespace BomBom.Game.Misc;

/// <summary>
/// Closes the game right before content pack is started.
/// EntryPoint of the content pack is assumed to be the start.
/// <seealso cref="Game.Patches.Sentry"/>
/// </summary>
public static class Preclusion
{
    private static bool _flag = false;

    /// <summary>
    /// Signal to bombom-launcher to close the game before the game starts
    /// </summary>
    public static void Trigger(string reason)
    {
        if (_flag)
        {
            BomBomLogger.Log(BomBomLogger.LogType.INFO, "Preclusion", $"Preclusion was triggered more than once, reason: {reason}");
            return;
        }
        
        BomBomLogger.Log(BomBomLogger.LogType.INFO, "Preclusion", $"Stopping content boot, reason: {reason}");
        _flag = true;
    }

    public static bool State => _flag;
    
    public static void Fire()
    {
        Environment.Exit(0);
    }
}