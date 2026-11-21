# ============================================================================
# Elsa Studio 可视化设计器资源安装脚本
# 从 npm 包 @elsa-workflows/elsa-studio-wasm 提取 Blazor WASM 产物，
# 复制到 src/WorkflowSystem.Web/wwwroot/elsa-studio/ 供设计器页面使用。
#
# 用法：在项目根目录执行  powershell -ExecutionPolicy Bypass -File scripts\setup-elsa-studio.ps1
# 前置：已安装 Node.js（含 npm）
# ============================================================================

$ErrorActionPreference = "Stop"

$version = "3.9.0"
$pkgName = "@elsa-workflows/elsa-studio-wasm"
$root = Split-Path -Parent $PSScriptRoot
$target = Join-Path $root "src\WorkflowSystem.Web\wwwroot\elsa-studio"
$tempDir = Join-Path $env:TEMP ("elsa-studio-pkg-" + [Guid]::NewGuid().ToString("N"))

Write-Host "==> 下载 $pkgName@$version ..."
New-Item -ItemType Directory -Path $tempDir | Out-Null
Push-Location $tempDir
try {
    npm pack "$pkgName@$version" | Out-Null
    $tgz = Get-ChildItem -Filter "*.tgz" | Select-Object -First 1
    tar -xzf $tgz.FullName
    Pop-Location

    $pkgContent = Join-Path $tempDir "package"

    Write-Host "==> 清理旧资源 ..."
    if (Test-Path $target) { Remove-Item -Recurse -Force $target }
    New-Item -ItemType Directory -Path $target | Out-Null

    Write-Host "==> 复制 Blazor WASM 产物（_framework / _content / 样式）..."
    Copy-Item -Recurse (Join-Path $pkgContent "_framework") (Join-Path $target "_framework")
    Copy-Item -Recurse (Join-Path $pkgContent "_content") (Join-Path $target "_content")
    Copy-Item (Join-Path $pkgContent "Elsa.Studio.Host.CustomElements.styles.css") $target

    Write-Host "==> 删除压缩副本（.br / .gz，本地 Kestrel 不压缩）以减小体积 ..."
    Get-ChildItem -Recurse -Path $target -Include "*.br", "*.gz" | Remove-Item -Force

    Write-Host ""
    Write-Host "完成。设计器资源已安装到：$target"
    Write-Host "注意：index.html 与 appsettings.json 为项目自有文件（已提交 git），不会覆盖。"
}
finally {
    Pop-Location -ErrorAction SilentlyContinue
    Remove-Item -Recurse -Force $tempDir -ErrorAction SilentlyContinue
}
