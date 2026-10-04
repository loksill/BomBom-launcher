using BomBom.Config;

namespace BomBom.Stealthsey;

public static class Abjure
{
    private static Version? engineVer { get; set; } 
    
    /// <summary>
    /// Checks against version with detection methods
    /// </summary>
    /// <returns>True if version is equal or over with detection and hidesey is disabled</returns>
    public static bool CheckMalbox(string engineversion, HideLevel BomBomHide)
    {
        engineVer = new Version(engineversion);

        return engineVer >= BomBomVars.Detection && BomBomHide == HideLevel.Disabled;
    }
}