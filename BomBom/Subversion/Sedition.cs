using System.Reflection;
using BomBom.Misc;
using BomBom.Stealthsey;

namespace BomBom.Subversion;

/// <summary>
///     Manages Hidesey patch helper class
/// </summary>
public static class Sedition
{
    /// <summary>
    /// Hides a subversion module from the game
    /// </summary>
    /// <remarks>Assigned to a delegate in a subverter patch</remarks>
    private static void HideDelegate(Assembly asm)
    {
        BomBomLogger.Log(BomBomLogger.LogType.DEBG, "Hiding");
        Hidesey.HidePatch(asm);
    }

    public static void InitSedition(Assembly assembly, string? assemblyName)
    {
        Type? hideseyType = assembly.GetType("Sedition");
        if (hideseyType == null)
        {
            BomBomLogger.Log(BomBomLogger.LogType.DEBG, $"{assemblyName} has no Hidesey class");
            return;
        }

        SetupSedition(hideseyType);
    }

    private static void SetupSedition(Type hidesey)
    {
        MethodInfo? stealthseyHide =
            typeof(Sedition).GetMethod("HideDelegate", BindingFlags.Static | BindingFlags.NonPublic);
        FieldInfo? asmHideseyDelegate = hidesey.GetField("hideDelegate", BindingFlags.Public | BindingFlags.Static);

        if (asmHideseyDelegate == null || stealthseyHide == null)
        {
            BomBomLogger.Log(BomBomLogger.LogType.ERRO, $"Missing delegate fields on sedition");
            return;
        }

        try
        {
            Delegate logDelegate = Delegate.CreateDelegate(asmHideseyDelegate.FieldType, stealthseyHide);
            asmHideseyDelegate.SetValue(null, logDelegate);
        }
        catch (Exception e)
        {
            BomBomLogger.Log(BomBomLogger.LogType.FATL, $"Failed to to assign sedition delegate: {e.Message}");
        }
    }
}
