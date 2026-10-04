using System.Reflection;
using BomBom.Game.Misc;
using BomBom.Patches;
using BomBom.Misc;

namespace BomBom.PatchAssembly;

/// <summary>
/// Validates and manages patch assembly fields
/// </summary>
public static class AssemblyFieldHandler
{
    /// <summary>
    /// Initialize helper classes in patches
    /// </summary>
    public static void InitHelpers(IEnumerable<IPatch> patches)
    {
        foreach (IPatch patch in patches)
        {
            Assembly assembly = patch.Asm;
            string? assemblyName = assembly.GetName().Name;

            InitLogger(assembly, assemblyName);
            InitEntry(assembly, patch, assemblyName);
        }
    }

    /// <summary>
    /// Initializes BomBomLogger
    /// </summary>
    public static void InitLogger(Assembly assembly, string? assemblyName)
    {
        Type? loggerType = assembly.GetType("BomBomLogger");
        if (loggerType == null)
        {
            BomBomLogger.Log(BomBomLogger.LogType.DEBG, $"{assemblyName} has no BomBomLogger class");
            return;
        }

        SetupLogger(loggerType);
    }

    /// <summary>
    /// Initializes BomBomEntry
    /// </summary>
    private static void InitEntry(Assembly assembly, IPatch patch, string? assemblyName)
    {
        MethodInfo? entryMethod = GetEntry(assembly);
        if (entryMethod == null)
        {
            BomBomLogger.Log(BomBomLogger.LogType.DEBG, $"{assemblyName} has no BomBomEntry class");
            return;
        }

        patch.Entry = entryMethod;
    }



    /// <summary>
    /// Obtains fields for each of the game's assemblies.
    /// Returns null if any of the fields is null.
    /// </summary>
    /// <returns>List of fields of assemblies within a BomBomPatch</returns>
    /// <exception cref="Nullable">Returns null if any field in BomBomPatch is missing</exception>
    public static List<FieldInfo>? GetPatchAssemblyFields(Type bombomPatchType)
    {
        string[] fieldNames = new[] { "RobustClient", "RobustShared", "ContentClient", "ContentShared" };
        List<FieldInfo> targets = new List<FieldInfo>();

        foreach (string fieldName in fieldNames)
        {
            FieldInfo? field = bombomPatchType.GetField(fieldName);
            if (field == null) return null; // Not all fields could be caught, no point proceeding.
            targets.Add(field);
        }

        return targets;
    }

    /// <summary>
    /// Is the patch asking the loader to ignore setting fields
    /// </summary>
    /// <param name="DataType">Patch class type</param>
    public static bool DetermineIgnore(Type DataType)
    {
        FieldInfo? ignoreFieldInfo = DataType.GetField("ignoreFields");

        if (ignoreFieldInfo != null && ignoreFieldInfo.FieldType == typeof(bool))
        {
            return (bool)(ignoreFieldInfo.GetValue(null) ?? false);
        }

        return false;
    }


    /// <summary>
    /// Is the patch asking to be loaded before the game?
    /// If bombompatch has a bool called "preload" - check it.
    /// </summary>
    /// <param name="DataType">Patch class type</param>
    public static bool DeterminePreload(Type DataType)
    {
        FieldInfo? preloadFieldInfo = DataType.GetField("preload");

        if (preloadFieldInfo != null && preloadFieldInfo.FieldType == typeof(bool))
        {
            return (bool)(preloadFieldInfo.GetValue(null) ?? false);
        }

        return false;
    }

    /// <summary>
    /// Sets the assembly target in the patch assembly.
    /// In order: Robust.Client, Robust.Shared, Content.Client, Content.Shared
    /// </summary>
    /// <param name="targets">Array of assemblies from the BomBomPatch class</param>
    public static void SetAssemblyTargets(List<FieldInfo> targets)
    {
        targets[0].SetValue(null, GameAssemblies.RobustClient);
        targets[1].SetValue(null, GameAssemblies.RobustShared);
        targets[2].SetValue(null, GameAssemblies.ContentClient);
        targets[3].SetValue(null, GameAssemblies.ContentShared);
    }

    /// <summary>
    /// Set engine targets for preloading patches that can only target the engine.
    /// </summary>
    /// <param name="target">BomBomPatch.RobustClient from a patch assembly</param>
    /// <param name="client">BomBomPatch.RobustShared from a patch assembly</param>
    /// <see cref="DeterminePreload"/>
    public static void SetEngineTargets(FieldInfo client, FieldInfo shared)
    {
        client.SetValue(null, GameAssemblies.RobustClient);
        shared.SetValue(null, GameAssemblies.RobustShared);
    }

    /// <summary>
    /// Sets patch delegate to BomBomLogger::Log(AssemblyName, string)
    /// Executed only by the Loader.
    /// </summary>
    /// <param name="bombomLoggerType">BomBomLogger class from BomBomPatch</param>
    private static void SetupLogger(Type bombomLoggerType)
    {
        MethodInfo? logMethod = typeof(BomBomLogger).GetMethod("Log", new[] { typeof(AssemblyName), typeof(string) });
        FieldInfo? logDelegateField = bombomLoggerType?.GetField("logDelegate", BindingFlags.Public | BindingFlags.Static);

        if (logMethod == null || logDelegateField == null)
        {
            List<string> missingComps = [];
            if (logMethod == null) missingComps.Add("LogMethod");
            if (logDelegateField == null) missingComps.Add("LogDelegateField");

            string missingCompStr = string.Join(", ", missingComps);

            BomBomLogger.Log(BomBomLogger.LogType.FATL, $"Failed to connect patch to bombomlogger. Missing components: {missingCompStr}.");
            return;
        }

        try
        {
            Delegate logDelegate = Delegate.CreateDelegate(logDelegateField.FieldType, logMethod);
            logDelegateField.SetValue(null, logDelegate);
        }
        catch (Exception e)
        {
            BomBomLogger.Log(BomBomLogger.LogType.FATL, $"Failed to to assign logger delegate: {e.Message}");
        }
    }

    /// <summary>
    /// Get methodhandle for the entrypoint function in patch
    /// </summary>
    public static MethodInfo? GetEntry(Assembly assembly)
    {
        Type? entryType = assembly.GetType("BomBomEntry");
        MethodInfo? entry = entryType?.GetMethod("Entry", BindingFlags.Public | BindingFlags.Static);
        return entry; // This is going to be null if BomBomEntry is absent
    }


    /// <summary>
    /// Obtains patch metadata like name and description
    /// </summary>
    /// <param name="bombomPatchType">Patch data type</param>
    /// <param name="name">Returned name</param>
    /// <param name="description">Returned description</param>
    /// <remarks>If name or description cannot be found - "Unknown" is returned</remarks>
    public static void GetFields(Type? bombomPatchType, out string name, out string description)
    {
        name = bombomPatchType?.GetField("Name")?.GetValue(null) as string ?? "Unknown";
        description = bombomPatchType?.GetField("Description")?.GetValue(null) as string ?? "Unknown";
    }

}
