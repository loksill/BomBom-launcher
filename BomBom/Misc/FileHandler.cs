using System.Reflection;
using BomBom.Config;
using BomBom.PatchAssembly;
using BomBom.Patches;
using BomBom.Stealthsey;
using BomBom.Subversion;

namespace BomBom.Misc;

/// <summary>
/// Handles file operations in the patch folder
/// </summary>
public abstract class FileHandler
{
    /// <summary>
    /// Prepare data about enabled mods to send to the loader
    /// </summary>
    public static async Task PrepareMods(string[]? path = null)
    {
        path ??= new[] { BomBomVars.BomBomFolder };
        string[] patchPath = new[] { BomBomVars.BomBomPatchFolder };

        List<BomBomPatch> bomBomPatches = BomBomfier.GetBomBomPatches();
        List<SubverterPatch> subverterPatches = Subverter.GetSubverterPatches();

        IPC.Server server = new();

        // Prepare preloading BomBomPatches
        List<string> preloadpaths = bomBomPatches
            .Where(p => p is { Enabled: true, Preload: true })
            .Select(p => p.Asmpath)
            .ToList();

        // Send preloading BomBomPatches through named pipe
        string preloadData = string.Join(",", preloadpaths);
        Task preloadTask = server.ReadySend("PreloadBomBomPatchesPipe", preloadData);

        // If we actually do have any - remove them from the patch list
        if (preloadpaths.Count != 0)
        {
            bomBomPatches.RemoveAll(p => preloadpaths.Contains(p.Asmpath));
        }

        // Prepare remaining BomBomPatches
        List<string> bomBomAsmpaths = bomBomPatches.Where(p => p.Enabled).Select(p => p.Asmpath).ToList();
        string bomBomData = string.Join(",", bomBomAsmpaths);
        Task bomBomTask = server.ReadySend("BomBomPatchesPipe", bomBomData);

        // Prepare SubverterPatches
        List<string> subverterAsmpaths = subverterPatches.Where(p => p.Enabled).Select(p => p.Asmpath).ToList();
        string subverterData = string.Join(",", subverterAsmpaths);
        Task subverterTask = server.ReadySend("SubverterPatchesPipe", subverterData);

        // Wait for all tasks to complete
        await Task.WhenAll(preloadTask, bomBomTask, subverterTask);
    }

    /// <summary>
    /// Loads assemblies from a specified folder.
    /// </summary>
    /// <param name="path">folder with patch dll's, set to "BomBom/Mods" by default</param>
    /// <param name="pipe">Are we loading from an IPC pipe</param>
    /// <param name="pipename">Name of an IPC pipe to load the patches from</param>
    public static void LoadAssemblies(string[]? path = null, bool pipe = false, string pipename = "BomBomPatchesPipe")
    {
        path ??= new[] { BomBomVars.BomBomPatchFolder };

        if (!pipe)
        {
            PatchListManager.RecheckPatches();
        }

        List<string> files = pipe ? GetFilesFromPipe(pipename) : GetPatches(path);

        foreach (string file in files)
        {
            BomBomLogger.Log(BomBomLogger.LogType.DEBG, $"Loading assembly from {file}");
            LoadExactAssembly(file, pipe);
        }
    }

    /// <summary>
    /// Retrieve a list of patch filepaths from pipe
    /// </summary>
    /// <param name="name">Name of the pipe</param>
    public static List<string> GetFilesFromPipe(string name)
    {
        IPC.Client client = new();
        string data = client.ConnRecv(name);

        return string.IsNullOrEmpty(data) ? new List<string>() : data.Split(',').ToList();
    }

    /// <summary>
    /// Loads an assembly from the specified file path and initializes it.
    /// </summary>
    /// <param name="file">The file path of the assembly to load.</param>
    /// <param name="lockup">Load from the dll directly instead of reading from file</param>
    public static void LoadExactAssembly(string file, bool lockup = false)
    {
        Redial.Disable(); // Disable any AssemblyLoad callbacks found

        try
        {
            if (lockup)
            {
                Assembly assembly = Assembly.LoadFrom(file);
                AssemblyInitializer.Initialize(assembly, assembly.Location);
            }
            else
            {
                byte[] assemblyData = File.ReadAllBytes(file);
                Assembly assembly = Assembly.Load(assemblyData);
                AssemblyInitializer.Initialize(assembly, file);
            }
        }
        catch (FileNotFoundException)
        {
            BomBomLogger.Log(BomBomLogger.LogType.DEBG, $"{file} could not be found");
        }
        catch (PatchAssemblyException ex)
        {
            BomBomLogger.Log(BomBomLogger.LogType.FATL, ex.Message);
        }
        catch (Exception ex) // Catch any other exceptions that may occur
        {
            BomBomLogger.Log(BomBomLogger.LogType.FATL, $"An unexpected error occurred while loading {file}: {ex.Message}");
        }
        finally
        {
            Redial.Enable(); // Enable callbacks in case the game needs them
        }
    }

    /// <summary>
    /// Retrieves the file paths of all DLL files in a specified subdirectory
    /// </summary>
    /// <param name="subdir">An array of strings representing the path to the subdirectory</param>
    /// <returns>An array of strings containing the full paths to each DLL file in the specified subdirectory</returns>
    public static List<string> GetPatches(string[] subdir)
    {
        try
        {
            string[] updatedSubdir = subdir.Prepend(Directory.GetCurrentDirectory()).ToArray();
            string path = Path.Combine(updatedSubdir);

            if (Directory.Exists(path))
            {
                return Directory.GetFiles(path, "*.dll").ToList();
            }

            BomBomLogger.Log(BomBomLogger.LogType.DEBG, $"Directory {path} does not exist");
            return [];
        }
        catch (Exception ex)
        {
            BomBomLogger.Log(BomBomLogger.LogType.FATL, $"Failed to find patches: {ex.Message}");
            return [];
        }
    }
}
