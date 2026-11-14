<#
.SYNOPSIS
    为 Windows 平台构建固定 CPython 3.12.13 的 ryupy_cpython.dll shim。
.DESCRIPTION
    支持 MSVC cl.exe、clang-cl 与 clang 工具链，输出目录始终由调用方指定到构建缓存或发布根，绝不污染源码。
#>
param (
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$PythonHome,

    [Parameter(Mandatory = $true, Position = 1)]
    [string]$Output
)

$ErrorActionPreference = 'Stop'

$resolvedPython = (Resolve-Path $PythonHome).Path
if (-not (Test-Path $Output)) {
    New-Item -ItemType Directory -Path $Output -Force | Out-Null
}
$resolvedOutput = (Resolve-Path $Output).Path
$srcFile = Join-Path $PSScriptRoot "ryupy_cpython.c"
$targetDll = Join-Path $resolvedOutput "ryupy_cpython.dll"

$includes = @(
    "/I$resolvedPython\include",
    "/I$resolvedPython\include\python3.12"
)
$libs = @("/LIBPATH:$resolvedPython\libs", "python312.lib")

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
        "-I$resolvedPython\include\python3.12",
        $srcFile,
        "-L$resolvedPython\libs",
        "-lpython312",
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
