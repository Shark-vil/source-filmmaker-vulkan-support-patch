# Builds build\SFM_Vulkan_Patcher.exe with the C# compiler that ships with .NET Framework 4.
# Downloads the official DXVK release, checks the 32-bit d3d9.dll against a pinned SHA-256 and embeds it.
#   .\build.ps1 [-Version 1.2.3]
param([string]$Version = "0.0.0-dev")

$ErrorActionPreference = "Stop"
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$DxvkVersion = "3.1.1"
$DxvkDllSha256 = "265888C31CA78DFFA290C39CB7E50BFB02762590E41927906E46FB32F01497FA"  # x32\d3d9.dll

$root = $PSScriptRoot
$out = Join-Path $root "build"
$cache = Join-Path $out "dxvk"
$dll = Join-Path $cache "d3d9.dll"
New-Item -ItemType Directory -Force $cache | Out-Null

if (-not (Test-Path $dll) -or (Get-FileHash $dll -Algorithm SHA256).Hash -ne $DxvkDllSha256) {
    $tgz = Join-Path $cache "dxvk-$DxvkVersion.tar.gz"
    $url = "https://github.com/doitsujin/dxvk/releases/download/v$DxvkVersion/dxvk-$DxvkVersion.tar.gz"
    Write-Host "Downloading $url"
    Invoke-WebRequest -Uri $url -OutFile $tgz -UseBasicParsing
    # Windows' own bsdtar; a GNU tar from Git would read "C:" as a remote host.
    & "$env:WINDIR\System32\tar.exe" -xzf $tgz -C $cache
    if ($LASTEXITCODE) { throw "tar failed" }
    Copy-Item (Join-Path $cache "dxvk-$DxvkVersion\x32\d3d9.dll") $dll -Force
}
$hash = (Get-FileHash $dll -Algorithm SHA256).Hash
if ($hash -ne $DxvkDllSha256) { throw "DXVK d3d9.dll hash mismatch: $hash" }

# AssemblyVersion must be numeric: "v1.2.3-beta" -> 1.2.3.0
$clean = $Version.TrimStart('v')
$numeric = ($clean -split '[-+]')[0]
if ($numeric -notmatch '^\d+(\.\d+){0,3}$') { $numeric = "0.0.0" }
$versionCs = Join-Path $out "Version.cs"
@"
using System.Reflection;
[assembly: AssemblyTitle("SFM Vulkan Patcher")]
[assembly: AssemblyProduct("SFM Vulkan Patcher")]
[assembly: AssemblyCompany("Shark_vil")]
[assembly: AssemblyCopyright("Copyright (c) Shark_vil")]
[assembly: AssemblyVersion("$numeric")]
[assembly: AssemblyFileVersion("$numeric")]
[assembly: AssemblyInformationalVersion("$clean")]
"@ | Set-Content -Encoding UTF8 $versionCs

$csc = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $csc)) { $csc = "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe" }
$exe = Join-Path $out "SFM_Vulkan_Patcher.exe"

& $csc /nologo /target:winexe /platform:anycpu /optimize+ /codepage:65001 "/out:$exe" "/win32icon:$(Join-Path $root 'icon.ico')" "/resource:$dll,dxvk_d3d9.dll" `
    /r:System.Windows.Forms.dll /r:System.Drawing.dll (Join-Path $root "src\Patcher.cs") $versionCs
if ($LASTEXITCODE) { throw "csc failed" }

$exeHash = (Get-FileHash $exe -Algorithm SHA256).Hash.ToLower()
"$exeHash  SFM_Vulkan_Patcher.exe" | Set-Content -Encoding ASCII (Join-Path $out "SHA256SUMS.txt")
Write-Host "Built $exe ($clean, DXVK $DxvkVersion)"
