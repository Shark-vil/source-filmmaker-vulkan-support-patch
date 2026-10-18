# SFM Vulkan Patcher

A small Windows tool that switches Source Filmmaker from DirectX 9 to Vulkan using [DXVK](https://github.com/doitsujin/dxvk), and launches SFM with quality presets.

## Why

SFM is a 32-bit program, so it can never use more than 4 GB of address space, no matter how much RAM you have. Big scenes with many models and high-resolution textures crash with "out of memory" long before your PC runs out of memory.

The stock DirectX 9 driver keeps a copy of every texture inside that 4 GB. DXVK can keep textures outside it (`d3d9.textureMemory`), which frees a lot of room for models, materials and maps. A 64-bit SFM does not exist, so this is the most effective way to fit heavier scenes.

## Usage

1. Download `SFM_Vulkan_Patcher.exe` from [Releases](../../releases).
2. Close SFM and run the patcher. It finds SFM in your Steam libraries; if not, click **SFM folder...** and pick `SourceFilmmaker\game`.
3. Click **Enable Vulkan**.
4. Pick a preset and resolution, then click **Launch SFM**, or **Save as .bat** to get a launcher you can reuse.

To go back to DirectX 9, click **Disable Vulkan**.

You need a GPU and driver with Vulkan 1.3 support.

## What it changes

- `game\bin\d3d9.dll`: the DXVK 3.1.1 (32-bit) DLL is put here. An existing `d3d9.dll` is renamed to `d3d9.dll.backup` and restored when you disable Vulkan.
- `game\dxvk.conf`: DXVK settings. An existing file with different contents is backed up first.
- `game\sfm_<preset>_<res>p.bat`: only when you click **Save as .bat**.

No other files are touched. **Disable Vulkan** only deletes `bin\d3d9.dll` if it is exactly the DXVK build embedded in the patcher.

## Presets

| Preset   | Shadows | Reflections | Anisotropy | Notes |
|----------|---------|-------------|------------|-------|
| Minimum  | 1024    | 256         | 1x         | Half-resolution textures; for very heavy scenes |
| Standard | 2048    | default     | 4x         | Close to SFM defaults |
| Medium   | 4096    | 512         | 8x         | Balance of quality and memory |
| Maximum  | 8192    | 1024        | 16x        | Each shadowed light uses about 256 MB |

The launch options are shown in a text box and can be edited before launching.

## Building

Run `.\build.ps1` in PowerShell on Windows. You don't need Visual Studio: the script uses the C# compiler that ships with .NET Framework 4. It downloads the official DXVK release, checks the DLL's SHA-256 and writes `build\SFM_Vulkan_Patcher.exe`.

GitHub Actions builds every push. Pushing a `v*` tag (for example `git tag v1.0.0 && git push --tags`) publishes a release with the exe.

## Language

The UI is in English. It switches to Russian when Windows is set to Russian.

## License

The patcher is under the MIT license (see `LICENSE`). The embedded DXVK is © Philip Rebohle and contributors, under the zlib/libpng license.
