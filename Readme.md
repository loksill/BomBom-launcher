# BomBom Launcher

[Русская версия](ReadmeRU.md)

discord: 

**BomBom** is a fork of [helix-launcher](https://github.com/banumbas/helix-launcher) with the Marsey functionality attached.

BomBom is currently in active development and testing.

Our goal is to create a launcher with the maximum amount of features and no limitations.

# License

The Space Station 14 launcher source code inherited from the upstream project remains licensed under the MIT License - see [LICENSE.txt](LICENSE.txt) and GNU Affero General Public License v3.0 (AGPL-3.0-only) - see [LICENSE-AGPL-3.0.txt](LICENSE-AGPL-3.0.txt).

All changes created for this fork are licensed under the GNU Affero General Public License v3.0 (AGPL-3.0-only) - see [LICENSE-AGPL-3.0.txt](LICENSE-AGPL-3.0.txt).

# Features

* Resource packs
* Plugins (Harmony patches and Subversion)
* Displaying game mode, map and ping in the launcher
* Redesigned menu with customization options
* Custom Discord RPC
* Config system

# Resource Packs

Resource packs replace client-facing asset files by path at launch time.

Pack directory:
* `%AppData%/Space Station 14/launcher/resource_packs/<PackName>` on Windows by default
* `~/.local/share/Space Station 14/launcher/resource_packs/<PackName>` on Linux by default (`$XDG_DATA_HOME` is respected if set)

Minimal pack structure:

```text
resource_packs/
  MyPack/
    meta.json
    Resources/
      Textures/
      Locale/
```

Minimal `meta.json`:

```json
{
  "name": "My Pack",
  "description": "Custom textures and locale",
  "target": ""
}
```

Notes:
* Files are overridden by their relative path inside `Resources/`.
* `target` is optional. Leave it empty to apply the pack to any fork.
* If you override files inside an `.rsi` directory, keep the correct `.rsi/meta.json` next to the changed textures.
* Only `Audio/`, `Fonts/`, `Locale/`, `Shaders/`, and `Textures/` roots are mounted from a pack.

# Building

Requirements:
* [.NET SDK 10.0](https://dotnet.microsoft.com/download)
* Git (submodules are used)
* Python 3 (used for batch packaging)
* Internet access (the .NET Runtime is downloaded on the first build)

Clone and build:

```bash
git clone --recursive https://github.com/loksill/BomBom-launcher.git
cd BomBom-launcher
dotnet restore
dotnet build --configuration Release
```

Run the launcher:

```bash
dotnet run --project SS14.Launcher/SS14.Launcher.csproj
```

Run tests:

```bash
dotnet test
```

## Batch build into executables

The `publish.py` script builds Release executables for the requested platforms, downloads the matching .NET Runtime for them, lays everything out in folders and packs the result into zip archives.

Build all platforms at once:

```bash
./publish.py windows linux osx
```

Only the platforms you need (`windows`, `linux`, `osx` in any combination):

```bash
./publish.py linux
./publish.py windows osx
```

x64 only, without arm64 (builds faster):

```bash
./publish.py windows linux --x64-only
```

Result:
* Archives are written to the repository root: `SS14.Launcher_Windows.zip`, `SS14.Launcher_Linux.zip`, `SS14.Launcher_macOS.zip`.
* Intermediate files go to `bin/publish/<Platform>/`.

Before each build the script deletes the `bin` directories of every project, so each run starts from a clean state. The first run also downloads the .NET Runtime (tens of megabytes) into `Dependencies/dotnet/`, which is reused afterwards.

### Package layout

Linux example:

```text
SS14.Launcher_Linux.zip
├── SS14.Launcher        # wrapper script: sets DOTNET_ROOT and runs the binary
├── SS14.desktop         # Linux desktop entry
├── bin_x64/
│   ├── SS14.Launcher    # launcher executable
│   ├── loader/          # SS14.Loader (game loader)
│   └── BomBom/Mods/     # empty folder for mods
└── dotnet_x64/          # downloaded .NET Runtime
```

Without `--x64-only`, `bin_arm64/` and `dotnet_arm64/` are added next to them. The Windows package also contains `Space Station 14 Launcher.exe` (bootstrap) and `console.bat` at the root, and the macOS package contains `Space Station 14 Launcher.app`.

Running a built package:
* Windows: unpack the archive and run `Space Station 14 Launcher.exe`.
* Linux: unpack the archive, run `chmod +x SS14.Launcher`, then `./SS14.Launcher`.

### Building the Windows package on Linux/macOS

The bootstrap (`Space Station 14 Launcher.exe`, NativeAOT, `net10.0-windows`) can only be built by the script on Windows. When building the Windows package on Linux/macOS, place a previously built `Space Station 14 Launcher.exe` in the repository root: the script picks it up instead of building one (this is exactly what GitHub Actions does in `.github/workflows/publish-release.yml`, building it on a separate Windows runner):

```bash
# on Windows
dotnet publish SS14.Launcher.Bootstrap/SS14.Launcher.Bootstrap.csproj -c Release -r win-x64
```

If the file is missing, the build stops with `Bootstrap executable not found`.

For Windows binaries the script also sets the PE subsystem to GUI (`exe_set_subsystem.py`) so that no console window opens on launch.

### Building a single executable without packaging

If you don't need a package, a plain `dotnet publish` is enough (`/p:FullRelease=True` disables development mode):

```bash
dotnet publish SS14.Launcher/SS14.Launcher.csproj -c Release -r win-x64 --self-contained false /p:FullRelease=True
```

The resulting `SS14.Launcher.exe` ends up in `SS14.Launcher/bin/Release/net10.0/win-x64/publish/`. `-r` accepts `win-x64`, `linux-x64`, `osx-x64` and their arm64 variants.

Such a binary runs on a machine with .NET Runtime 10.0 installed. For distribution without external dependencies use `publish.py` — it puts the runtime inside the archive.
