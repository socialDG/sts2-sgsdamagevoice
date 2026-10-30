<#
  编译并部署 三国杀无双音效（STS2-sgsdamageVoice，按伤害阈值播放音效）。

  用法（二选一）：
    .\build\build.cmd              # 推荐：自动用 Bypass 执行策略
    .\build\build.cmd -NoDeploy    # 只编译到 dist\，不部署
#>
[CmdletBinding()]
param(
    [switch]$NoDeploy
)

$ErrorActionPreference = 'Stop'

$Root       = Split-Path -Parent $PSScriptRoot
$ModName    = 'STS2-sgsdamageVoice'
$LegacyMods = @('STS2-WswjqVoice')
$SrcDir     = Join-Path $Root 'src\SgsDamageVoice'
$AssetsDir  = Join-Path $Root 'assets'
$ObjDir     = Join-Path $Root 'build\obj'
$DistDir    = Join-Path $Root ('dist\' + $ModName)

$Csc        = Join-Path $Root '.tools\x\microsoft.net.compilers.toolset.5.9.0\tasks\net472\csc.exe'
$RefPackDir = Join-Path $Root '.tools\x\microsoft.netcore.app.ref.9.0.22\ref\net9.0'
$GameDir    = 'D:\SteamLibrary\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64'
$RitsuDir   = 'D:\SteamLibrary\steamapps\workshop\content\2868840\3747602295'

# 音效键 -> assets 下的文件名（想换音效只改这里，左边的键名不要改）
$AudioMap = [ordered]@{
    'player_low'  = 'playerattack2enemy_1.mp3'
    'player_high' = 'playerattack2enemy_2.mp3'
    'enemy_low'   = 'enemyattack2player_1.mp3'
    'enemy_high'  = 'enemyattack2player_2.mp3'
}

function Write-Step($text) { Write-Host "==> $text" -ForegroundColor Cyan }

# ---------------------------------------------------------------- 前置检查
Write-Step '检查工具链'
$missing = @()
foreach ($p in @($Csc, $RefPackDir, $GameDir, $RitsuDir)) {
    if (-not (Test-Path $p)) { $missing += $p }
}
if ($missing.Count -gt 0) { throw "缺少以下路径：`n  $($missing -join "`n  ")" }

# ---------------------------------------------------------------- 组装响应文件
# 所有 csc 参数都写进 .rsp：路径里有空格（Slay the Spire 2），直接当命令行参数会把引号弄乱。
Write-Step '组装 csc 响应文件'
New-Item -ItemType Directory -Force -Path $ObjDir | Out-Null
New-Item -ItemType Directory -Force -Path $DistDir | Out-Null

$outDll = Join-Path $DistDir ($ModName + '.dll')
$lines  = New-Object System.Collections.Generic.List[string]
$lines.Add('-target:library')
$lines.Add('-nostdlib+')
$lines.Add('-langversion:latest')
$lines.Add('-nullable:enable')
$lines.Add('-optimize+')
$lines.Add('-deterministic')
$lines.Add('-codepage:65001')
$lines.Add('-warnaserror-')
$lines.Add('-out:"' + $outDll + '"')

foreach ($dll in Get-ChildItem $RefPackDir -Filter *.dll) {
    $lines.Add('-r:"' + $dll.FullName + '"')
}

foreach ($name in @('sts2.dll', 'GodotSharp.dll', '0Harmony.dll')) {
    $p = Join-Path $GameDir $name
    if (-not (Test-Path $p)) { throw "缺少游戏程序集：$p" }
    $lines.Add('-r:"' + $p + '"')
}

$ritsuRefs = @(
    (Join-Path $RitsuDir 'compat\0.111.0\STS2-RitsuLib.dll'),
    (Join-Path $RitsuDir 'compat\0.111.0\STS2-RitsuLib.Runtime.dll'),
    (Join-Path $RitsuDir 'shared\STS2-RitsuLib.Shared.dll'),
    (Join-Path $RitsuDir 'shared\STS2-RitsuLib.Settings.dll'),
    (Join-Path $RitsuDir 'shared\STS2-RitsuLib.Ui.dll')
)
foreach ($p in $ritsuRefs) {
    if (-not (Test-Path $p)) { throw "缺少 RitsuLib 程序集：$p" }
    $lines.Add('-r:"' + $p + '"')
}

$sources = @(Get-ChildItem $SrcDir -Filter *.cs)
if ($sources.Count -eq 0) { throw "在 $SrcDir 下找不到 .cs 源文件" }
foreach ($s in $sources) { $lines.Add('"' + $s.FullName + '"') }

$rspPath = Join-Path $ObjDir 'build.rsp'
Set-Content -Path $rspPath -Value ($lines -join "`r`n") -Encoding UTF8

# ---------------------------------------------------------------- 编译
Write-Step "编译 $ModName.dll（$($sources.Count) 个源文件）"
$sw = [System.Diagnostics.Stopwatch]::StartNew()
& $Csc ('@' + $rspPath)
$code = $LASTEXITCODE
$sw.Stop()

if ($code -ne 0) { throw "编译失败（csc 退出码 $code）" }
if (-not (Test-Path $outDll)) { throw "编译未生成 $outDll" }
Write-Host ("    生成 {0} ({1:N0} 字节, {2:N1} 秒)" -f (Split-Path -Leaf $outDll), (Get-Item $outDll).Length, $sw.Elapsed.TotalSeconds) -ForegroundColor Green

# ---------------------------------------------------------------- 打包
Write-Step '复制清单与音效'
Copy-Item (Join-Path $SrcDir 'mod_manifest.json') (Join-Path $DistDir 'mod_manifest.json') -Force

$audioOut = Join-Path $DistDir 'audio'
if (Test-Path $audioOut) { Remove-Item $audioOut -Recurse -Force }
New-Item -ItemType Directory -Force -Path $audioOut | Out-Null

$mapLines = New-Object System.Collections.Generic.List[string]
$mapLines.Add('# 音效键 = 文件名（相对 audio 目录）。等号右边可以换成别的文件名。')
foreach ($key in $AudioMap.Keys) {
    $file = $AudioMap[$key]
    $src  = Join-Path $AssetsDir $file
    if (-not (Test-Path $src)) { throw "缺少音频文件：$src" }
    Copy-Item $src (Join-Path $audioOut $file) -Force
    $mapLines.Add(('{0} = {1}' -f $key, $file))
    $hash = (Get-FileHash $src -Algorithm SHA256).Hash.Substring(0, 12)
    Write-Host ("    {0,-12} <- {1}  ({2:N0} 字节, sha256:{3})" -f $key, $file, (Get-Item $src).Length, $hash)
}
Set-Content -Path (Join-Path $audioOut 'map.txt') -Value ($mapLines -join "`r`n") -Encoding UTF8

# ---------------------------------------------------------------- 部署
if ($NoDeploy) {
    Write-Step "已跳过部署（-NoDeploy）。产物在 $DistDir"
    return
}

# 游戏只从「可执行文件同级的 mods 目录」加载本地 mod（见 ModManager.Initialize 文档），
# 所以部署目标是游戏安装目录下的 mods\STS2-sgsdamageVoice，而不是 %APPDATA%。
$modsRoot = Join-Path (Split-Path -Parent $GameDir) 'mods'
$target   = Join-Path $modsRoot $ModName

if (Get-Process -Name SlayTheSpire2 -ErrorAction SilentlyContinue) {
    throw '游戏正在运行，DLL 被占用无法覆盖。请先退出游戏再编译部署。'
}

Write-Step "部署到 $target"
New-Item -ItemType Directory -Force -Path $modsRoot | Out-Null

# 旧名字的部署目录必须清掉，否则新旧两个 mod 会同时加载、同一段伤害响两次。
foreach ($legacy in $LegacyMods) {
    $legacyPath = Join-Path $modsRoot $legacy
    if (Test-Path $legacyPath) {
        Write-Host ("    清理旧版本目录：{0}" -f $legacyPath)
        Remove-Item $legacyPath -Recurse -Force
    }
}

if (Test-Path $target) { Remove-Item $target -Recurse -Force }
New-Item -ItemType Directory -Force -Path $target | Out-Null
Copy-Item (Join-Path $DistDir '*') $target -Recurse -Force

Write-Host ("完成。已部署：{0}" -f $target) -ForegroundColor Green
Get-ChildItem $target -Recurse -File | ForEach-Object {
    Write-Host ('    ' + $_.FullName.Substring($target.Length + 1) + '  ' + $_.Length)
}
