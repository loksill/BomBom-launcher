using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using BomBom.Config;
using BomBom.Game;
using BomBom.Game.Managers;
using BomBom.Game.Misc;
using BomBom.Game.Patches;
using BomBom.Game.Patches.BomBomPorts;
using BomBom.PatchAssembly;
using BomBom.Patches;
using BomBom.Stealthsey;
using BomBom.Subversion;
using BomBom.Misc;
using BomBom.Stealthsey.Reflection;

namespace BomBom;

/// <summary>
/// Entrypoint for patching
/// </summary>
public class BomBomPatcher
{
    private static BomBomPatcher? _instance;
    private static ManualResetEvent? _flag;

    public static BomBomPatcher Instance
    {
        get
        {
            if (_instance == null)
            {
                throw new Exception("BomBomPatcher is not created. Call CreateInstance with the client assembly first.");
            }
            return _instance;
        }
    }

    public static void CreateInstance(Assembly? robClientAssembly, ManualResetEvent mre)
    {
        if (_instance != null)
        {
            throw new Exception("Instance already created.");
        }

        _instance = new BomBomPatcher(robClientAssembly, mre);
    }

    /// <exception cref="Exception">Excepts if Robust.Client assembly is null</exception>
    private BomBomPatcher(Assembly? robClientAssembly, ManualResetEvent mre)
    {
        if (robClientAssembly == null) throw new Exception("Robust.Client was null.");

        _flag = mre;

        // Initialize GameAssemblies
        GameAssemblies.Initialize(robClientAssembly);

        // Initialize loader
        //Utility.SetupFlags();
        Utility.ReadConf();
        HarmonyManager.Init(new Harmony(BomBomVars.Identifier));

        BomBomLogger.Log(BomBomLogger.LogType.INFO, $"BomBom-launcher started{(BomBomConf.Patchless ? " in patchless mode" : "")}, version {BomBomVars.BomBomVersion}");

        // Init backport manager
        BomBomPortMan.Initialize();

        // Hide the loader
        Hidesey.Initialize();

        Preload();

        // Tell the loader were done here, start the game
        _flag.Set();
    }

    // We might want to patch things before the loader has even a chance to execute anything
    [Patching]
    private void Preload()
    {
        Sentry.Patch();

        // Preload bombompatches, if available
        BomBomfier.Preload();

        // If set - Disable redialing and remote command execution
        Jammer.Patch();
        Blackhole.Patch();

        // Apply engine backports
        BomBomPortMan.PatchBackports();
    }

    /// <summary>
    /// Boots up the patcher
    /// Executed by the loader.
    /// </summary>
    public void Boot()
    {
        // Side-load custom code
        if (Subverse.CheckSubversions())
            Subverse.PatchSubverter();

        // Wait for the game itself to load
        GameAssemblyManager.TrySetContentAssemblies();

        // Post assembly-load hide methods
        Hidesey.PostLoad();

        ExecPatcher();

        Afterparty();
    }

    private void ExecPatcher()
    {
        // Prepare bombompatches
        FileHandler.LoadAssemblies(pipe: true);
        List<BomBomPatch> patches = BomBomfier.GetBomBomPatches();

        if (patches.Count != 0)
        {
            // Connect patches to internal logger
            AssemblyFieldHandler.InitHelpers(patches);
        }

        // Execute patches
        Patcher.Patch(patches);
    }

    private void Afterparty()
    {
        // TODO: Test if GameAssemblies.ClientInitialized works here
        while (!GameAssemblies.ClientInitialized()) // Wait until EntryPoint is just about to start
        {
            Thread.Sleep(125);
        }

        // If preclusion is triggered - close the game bruh
        if (Preclusion.State)
            Preclusion.Fire();

        // Apply content-related backports
        BomBomPortMan.PatchBackports(true);

        // Post-Load hidesey methods
        Hidesey.Cleanup();
    }
}
