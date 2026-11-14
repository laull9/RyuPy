<#
.SYNOPSIS
    为 Windows 平台构建选中 CPython 版本 的 ryupy_cpython.dll shim。
.DESCRIPTION
    支持 MSVC cl.exe、clang-cl 与 clang 工具链，输出目录始终由调用方指定到构建缓存或发布根，绝不污染源码。
#>
param (
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$PythonHome,

    [Parameter(Mandatory = $true, Position = 1)]
    [string]$Output,

    [Parameter(Mandatory = $false, Position = 2)]
    [string]$PythonVersion
)

$ErrorActionPreference = 'Stop'

$resolvedPython = (Resolve-Path $PythonHome).Path
if (-not (Test-Path $Output)) {
    New-Item -ItemType Directory -Path $Output -Force | Out-Null
}
$resolvedOutput = (Resolve-Path $Output).Path
$srcFile = Join-Path $PSScriptRoot "ryupy_cpython.c"
$targetDll = Join-Path $resolvedOutput "ryupy_cpython.dll"

# 从同一发行头文件精确派生 import library；python3.lib 是稳定 ABI 桩，不能用于嵌入。
if (-not $PythonVersion) {
    $header = Get-Content -LiteralPath (Join-Path $resolvedPython "include\patchlevel.h") -Raw
    $majorMatch = [regex]::Match($header, '(?m)^#define\s+PY_MAJOR_VERSION\s+(\d+)')
    $minorMatch = [regex]::Match($header, '(?m)^#define\s+PY_MINOR_VERSION\s+(\d+)')
    if (-not $majorMatch.Success -or -not $minorMatch.Success) {
        throw "无法从选中发行的 patchlevel.h 读取 Python 版本"
    }
    $PythonVersion = $majorMatch.Groups[1].Value + "." + $minorMatch.Groups[1].Value
}
$includes = @("/I$resolvedPython\include", "/I$resolvedPython\include\python$PythonVersion")
$cleanVer = $PythonVersion.Replace(".", "")
$libName = "python$cleanVer.lib"
if (-not (Test-Path -LiteralPath (Join-Path $resolvedPython "libs\$libName"))) {
    throw "选中发行缺少 import library：$libName"
}
$libs = @("/LIBPATH:$resolvedPython\libs", $libName)

if (Get-Command "cl.exe" -ErrorAction SilentlyContinue) {
    Write-Host "[ryupy-cpython] 使用 MSVC cl.exe 构建 Windows shim..."
    $clArgs = @("/nologo", "/O2", "/W3", "/WX", "/MD", "/LD") + $includes + @("/Fo$resolvedOutput\ryupy_cpython.obj", $srcFile, "/link") + $libs + @("/OUT:$targetDll", "/IMPLIB:$resolvedOutput\ryupy_cpython.lib")
    & cl.exe $clArgs
    if ($LASTEXITCODE -ne 0) {
        throw "MSVC 编译 ryupy_cpython.dll 失败，退出码: $LASTEXITCODE"
    }
    # 清理链接附带临时产物
    foreach ($name in @("ryupy_cpython.exp", "ryupy_cpython.lib", "ryupy_cpython.obj")) {
        $temporary = Join-Path $resolvedOutput $name
        if (Test-Path $temporary) { Remove-Item -LiteralPath $temporary -Force }
    }
    Write-Host "[ryupy-cpython] 编译成功: $targetDll"
    exit 0
}

if (Get-Command "clang.exe" -ErrorAction SilentlyContinue) {
    Write-Host "[ryupy-cpython] 使用 Clang 构建 Windows shim..."
    $clangArgs = @(
        "-shared", "-O2", "-Wall", "-Wextra", "-Werror",
        "-I$resolvedPython\include",
        "-I$resolvedPython\include\python$PythonVersion",
        $srcFile,
        "-L$resolvedPython\libs",
        ("-l" + $libName.Replace(".lib", "")),
        "-o", $targetDll
    )
    & clang.exe $clangArgs
    if ($LASTEXITCODE -ne 0) {
        throw "Clang 编译 ryupy_cpython.dll 失败，退出码: $LASTEXITCODE"
    }
    Write-Host "[ryupy-cpython] 编译成功: $targetDll"
    exit 0
}

throw "未找到 cl.exe 或 clang.exe，Windows 原生 shim 构建需要 MSVC 工具链或 Clang。"
