using HarmonyLib;
using BomBom.Config;
using BomBom.Handbreak;
using BomBom.Misc;

namespace BomBom.Game.Patches;

/// <summary>
/// Disable game's redialing feature, making sure you won't get sent to vore station if admins don't like you.
/// </summary>
/// <remarks>While redialing "for the funny" is not allowed by wizard's den you are not able to prove anything because its not even logged in a verifiable manner.</remarks>
/// <remarks><para>Not to be confused with <see cref="Stealthsey.Redial"/>.</para></remarks>
public static class Jammer
{
    /// "Disabling engine redial instead of putting a check in Loader's RedialAPI actually doesn't let the client close"
    /// Is, unfortunately, still just an excuse.
    public static void Patch()
    {
        if (!BomBomConf.JamDials) return;

        BomBomLogger.Log(BomBomLogger.LogType.INFO, "Jammer", "Disabling redialer");

        Helpers.PatchMethod(
            Helpers.TypeFromQualifiedName("Robust.Client.GameController"),
            "Redial",
            typeof(Jammer),
            "Disable",
            HarmonyPatchType.Prefix
            );
    }

    private static bool Disable()
    {
        BomBomLogger.Log(BomBomLogger.LogType.INFO, "Jammer", "Blocked a redial!");
        return false;
    }
}
