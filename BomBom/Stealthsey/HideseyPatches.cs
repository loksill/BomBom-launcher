using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using BomBom.Config;
using BomBom.Misc;
using BomBom.Stealthsey.Reflection;

namespace BomBom.Stealthsey;

/// <summary>
/// Manual patches used with Hidesey
/// Not based off BomBomPatch or SubverterPatch
/// </summary>
public static class HideseyPatches
{
    public static void Lie<T>(ref T __result)
    {
        __result = __result switch
        {
            Assembly[] assemblies => (T)(object)Hidesey.LyingDomain(assemblies),
            IEnumerable<Assembly> assemblyEnumerable => (T)(object)Hidesey.LyingContext(assemblyEnumerable),
            IEnumerable<AssemblyLoadContext> assemblyLoadContextEnumerable => (T)(object)Hidesey.LyingManifest(
                assemblyLoadContextEnumerable),
            AssemblyName[] assemblyNames => (T)(object)Hidesey.LyingReference(assemblyNames),
            Type[] types => (T)(object)Hidesey.LyingTyper(types),
            _ => throw new InvalidOperationException("Unsupported type for LiePatch")
        };
    }

    /// <summary>
    /// This patch skips function execution
    /// </summary>
    public static bool Skip() => false;

    public static bool SkipPatchless() => !BomBomConf.Patchless;

    /// <summary>
    /// Prefix patch that checks if BomBomHide matches or above the attributed HideLevelRequirement
    /// </summary>
    public static bool LevelCheck(MethodBase __originalMethod)
    {

        string fullMethodName = $"{__originalMethod.DeclaringType?.FullName}::{__originalMethod.Name}";
        string parameters = string.Join(", ", __originalMethod.GetParameters().Select(p => p.ParameterType.Name));
        fullMethodName += $"({parameters})";

        object[] customAttributes = __originalMethod.GetCustomAttributes(false);

        // Check if the method has a HideLevelRequirement attribute and compare the required level with the current BomBomHide level.
        HideLevelRequirement? hideLevelRequirement = customAttributes.OfType<HideLevelRequirement>().FirstOrDefault();
        if (hideLevelRequirement != null && BomBomConf.BomBomHide < hideLevelRequirement.Level)
        {
            BomBomLogger.Log(BomBomLogger.LogType.DEBG,
                $"Not executing {fullMethodName} due to lower BomBomHide level. " +
                $"Required: {hideLevelRequirement.Level}, Current: {BomBomConf.BomBomHide}");
            return false;
        }

        // Check if the method has a HideLevelRestriction attribute and ensure the current BomBomHide level is below the threshold.
        HideLevelRestriction? hideLevelRestriction = customAttributes.OfType<HideLevelRestriction>().FirstOrDefault();
        if (hideLevelRestriction != null && BomBomConf.BomBomHide >= hideLevelRestriction.MaxLevel)
        {
            BomBomLogger.Log(BomBomLogger.LogType.DEBG,
                $"Not executing {fullMethodName} due to equal or above BomBomHide level. " +
                $"Threshold: {hideLevelRestriction.MaxLevel}, Current: {BomBomConf.BomBomHide}");
            return false;
        }

        return true;
    }
}
