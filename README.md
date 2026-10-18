# SFM Vulkan Patcher

Switches Source Filmmaker from DirectX 9 to Vulkan in one click.

## Why

SFM is a 32-bit program and can't use more than 4 GB of memory, no matter how much RAM your PC has. Big scenes crash with "out of memory" because of that.

With Vulkan, textures no longer eat into those 4 GB, so you can load more models, materials and maps before SFM crashes.

## How to use

1. Download `SFM_Vulkan_Patcher.exe` from [Releases](../../releases).
2. Close SFM and run the patcher. It finds SFM on its own; if it doesn't, click **SFM folder...** and pick `SourceFilmmaker\game`.
3. Click **Enable Vulkan**.
4. Pick a quality preset and resolution, then click **Launch SFM**. You can also click **Save as .bat** to get a shortcut that launches SFM with these settings.

To go back, click **Disable Vulkan**.

Your graphics card needs Vulkan 1.3 support (most cards from the last few years have it).

## Presets

- **Minimum**: lowest memory use, for very heavy scenes. Textures are at half resolution.
- **Standard**: about the same as SFM's defaults.
- **Medium**: better shadows and reflections.
- **Maximum**: best quality, uses the most memory.

## Credits

[DXVK](https://github.com/doitsujin/DXVK) by Philip Rebohle and contributors does the actual DirectX 9 to Vulkan translation.
