using System.Reflection;

namespace BomBom.Game.Misc;

/// <summary>
/// Manages BomBomEntry
/// </summary>
public static class Doorbreak
{
    /// <summary>
    /// Invokes BomBomEntry
    /// </summary>
    /// <param name="entry">MethodInfo of BomBomEntry::Entry()</param>
    /// <param name="threading">Call in another thread</param>
    public static void Enter(MethodInfo? entry, bool threading = true)
    {
        if (entry == null) return;

        if (threading)
            new Thread(() => { entry.Invoke(null, []); }).Start();
        else
            entry.Invoke(null, []);
    }
}
