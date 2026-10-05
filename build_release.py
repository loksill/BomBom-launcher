#!/usr/bin/env python3

# One-shot release build:
# makes sure the bootstrap executable exists, then runs publish.py.

import argparse
import os
import shutil
import subprocess
import sys
import tempfile
import time

SCRIPT_DIR = os.path.dirname(os.path.realpath(__file__))

BOOTSTRAP_NAME = "Space Station 14 Launcher.exe"
BOOTSTRAP_PROJECT = "SS14.Launcher.Bootstrap/SS14.Launcher.Bootstrap.csproj"
BOOTSTRAP_CACHE_DIR = os.path.join(SCRIPT_DIR, "Dependencies", "bootstrap")
BOOTSTRAP_CACHE = os.path.join(BOOTSTRAP_CACHE_DIR, BOOTSTRAP_NAME)

ALL_PLATFORMS = ["windows", "linux", "osx"]


def main() -> None:
    parser = argparse.ArgumentParser(
        description="Builds the bootstrap executable if needed and packages the launcher."
    )
    parser.add_argument(
        "platform",
        nargs="*",
        default=ALL_PLATFORMS,
        help=f"platforms to build (default: {' '.join(ALL_PLATFORMS)})",
    )
    parser.add_argument("--x64-only", action="store_true", help="skip arm64 builds")
    parser.add_argument(
        "--rebuild-bootstrap",
        action="store_true",
        help="build the bootstrap even if a cached copy exists",
    )
    parser.add_argument(
        "--no-bootstrap",
        action="store_true",
        help="do not touch the bootstrap at all",
    )
    parser.add_argument(
        "--bootstrap-only",
        action="store_true",
        help="only make sure the bootstrap exists, then exit",
    )

    args = parser.parse_args()
    platforms: list[str] = args.platform

    sys.stdout.reconfigure(line_buffering=True)
    os.chdir(SCRIPT_DIR)
    start = time.time()

    for platform in platforms:
        if platform not in ALL_PLATFORMS:
            sys.exit(f"Unknown platform '{platform}'. Valid: {', '.join(ALL_PLATFORMS)}")

    needs_bootstrap = "windows" in platforms and os.name != "nt"

    if needs_bootstrap and not args.no_bootstrap:
        ensure_bootstrap(rebuild=args.rebuild_bootstrap)

    if args.bootstrap_only:
        return

    run_publish(platforms, x64_only=args.x64_only)

    produced = report_outputs(start)
    if "windows" in platforms and os.name != "nt" and not args.no_bootstrap:
        if not produced:
            print("Windows package was not produced, see errors above.")
            sys.exit(1)


def ensure_bootstrap(rebuild: bool) -> None:
    if not rebuild:
        existing = find_bootstrap()
        if existing:
            print(f"Bootstrap already present: {existing}")
            return

    print("Building bootstrap executable (IL fallback, cross-compilation for win-x64)...")
    print("Note: a smaller NativeAOT bootstrap can only be built on Windows.")

    tmp_dir = tempfile.mkdtemp(prefix="bomber_bootstrap_")
    try:
        try:
            subprocess.run(
                [
                    "dotnet",
                    "publish",
                    BOOTSTRAP_PROJECT,
                    "-c",
                    "Release",
                    "-r",
                    "win-x64",
                    "--self-contained",
                    "true",
                    "-p:EnableWindowsTargeting=true",
                    "-p:PublishAot=false",
                    "-p:PublishSingleFile=true",
                    "-p:EnableCompressionInSingleFile=true",
                    "-v",
                    "q",
                    "-o",
                    tmp_dir,
                ],
                check=True,
            )
        except (subprocess.CalledProcessError, FileNotFoundError) as e:
            sys.exit(
                "Failed to build the bootstrap executable.\n"
                "Build it on Windows instead:\n"
                f"    dotnet publish {BOOTSTRAP_PROJECT} -c Release -r win-x64\n"
                f"and put the resulting '{BOOTSTRAP_NAME}' into the repository root.\n"
                f"({e})"
            )

        built = None
        for name in os.listdir(tmp_dir):
            if name.lower().endswith(".exe"):
                built = os.path.join(tmp_dir, name)
                break

        if built is None:
            sys.exit(f"Bootstrap build produced no .exe in {tmp_dir}")

        with open(built, "rb") as f:
            if f.read(2) != b"MZ":
                sys.exit(f"Bootstrap build produced a non-Windows executable: {built}")

        os.makedirs(BOOTSTRAP_CACHE_DIR, exist_ok=True)
        shutil.move(built, BOOTSTRAP_CACHE)
        size_mb = os.path.getsize(BOOTSTRAP_CACHE) / (1024 * 1024)
        print(f"Bootstrap ready: {BOOTSTRAP_CACHE} ({size_mb:.1f} MiB)")
    finally:
        shutil.rmtree(tmp_dir, ignore_errors=True)


def find_bootstrap() -> str | None:
    for path in (os.path.join(SCRIPT_DIR, BOOTSTRAP_NAME), BOOTSTRAP_CACHE):
        if os.path.isfile(path):
            return path
    return None


def run_publish(platforms: list[str], x64_only: bool) -> None:
    cmd = [sys.executable, os.path.join(SCRIPT_DIR, "publish.py"), *platforms]
    if x64_only:
        cmd.append("--x64-only")

    print(f"Running: {' '.join(cmd[1:])}")
    result = subprocess.run(cmd, cwd=SCRIPT_DIR)
    if result.returncode != 0:
        sys.exit(result.returncode)


def report_outputs(start: float) -> list[str]:
    produced = []
    for name in ("Windows", "Linux", "macOS"):
        zip_path = os.path.join(SCRIPT_DIR, f"SS14.Launcher_{name}.zip")
        if os.path.isfile(zip_path) and os.path.getmtime(zip_path) >= start:
            size_mb = os.path.getsize(zip_path) / (1024 * 1024)
            print(f"Package: {os.path.basename(zip_path)} ({size_mb:.1f} MiB)")
            produced.append(zip_path)
    return produced


if __name__ == "__main__":
    main()
