using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BomBom.PatchAssembly;
using BomBom.Patches;
using BomBom.Misc;
using BomBom.Stealthsey;
using BomBom.Stealthsey.Reflection;

namespace BomBom.Subversion;

/// <summary>
/// Manages patches/addons based on the Subverter patch
/// </summary>
public static class Subverter
{
    public static List<SubverterPatch> GetSubverterPatches() => PatchListManager.GetPatchList<SubverterPatch>();
}

public class SubverterPatch : IPatch
{
    public string Asmpath { get; set; }
    public Assembly Asm { get; set; }
    public string Name { get; set; }
    public string Desc { get; set; }
    public MethodInfo? Entry { get; set; }
    public bool Enabled { get; set; }

    public SubverterPatch(string asmpath, Assembly asm, string name, string desc)
    {
        Asmpath = asmpath;
        Name = name;
        Desc = desc;
        Asm = asm;
    }

    public override bool Equals(object? obj)
    {
        if (obj is SubverterPatch other)
        {
            return this.Name == other.Name && this.Desc == other.Desc;
        }
        return false;
    }

    public override int GetHashCode() => HashCode.Combine(Name, Desc);
}
