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
* Python 3 (only for packaging release builds)

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

Create release packages:

```bash
./publish.py windows linux osx
```

Archives are written to `bin/publish/` (`SS14.Launcher_Windows.zip`, etc.). Use `--x64-only` to skip arm64 builds.
