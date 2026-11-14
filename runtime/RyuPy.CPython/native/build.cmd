@echo off
rem 使用 MSVC cl.exe 或 clang 编译 Windows 平台下固定 CPython 3.12.13 的 ryupy_cpython.dll shim。
rem 输出目录由调用方指定到构建缓存或发布目录，绝不污染源码。
setlocal enabledelayedexpansion

if "%~2"=="" (
    echo 用法: %~nx0 ^<python_home^> ^<output_dir^>
    exit /b 1
)

set "PYTHON_HOME=%~f1"
set "OUTPUT_DIR=%~f2"

if not exist "%OUTPUT_DIR%" mkdir "%OUTPUT_DIR%"

set "INCLUDE_FLAG=/I"%PYTHON_HOME%\include" /I"%PYTHON_HOME%\include\python3.12""
set "LIB_FLAG=/LIBPATH:"%PYTHON_HOME%\libs" python312.lib"
set "SRC=%~dp0ryupy_cpython.c"
set "OUT=%OUTPUT_DIR%\ryupy_cpython.dll"

where cl.exe >nul 2>&1
if %ERRORLEVEL% equ 0 (
    echo [ryupy-cpython] 使用 MSVC cl 编译 Windows shim...
    cl.exe /nologo /O2 /W3 /WX /MD /LD %INCLUDE_FLAG% "%SRC%" /link %LIB_FLAG% /OUT:"%OUT%"
    if %ERRORLEVEL% neq 0 (
        echo [错误] MSVC 编译 ryupy_cpython.dll 失败
        exit /b %ERRORLEVEL%
    )
    rem 清理临时生成的 obj/exp/lib 临时产物
    if exist "%OUTPUT_DIR%\ryupy_cpython.exp" del /q "%OUTPUT_DIR%\ryupy_cpython.exp"
    if exist "%OUTPUT_DIR%\ryupy_cpython.lib" del /q "%OUTPUT_DIR%\ryupy_cpython.lib"
    if exist "%OUTPUT_DIR%\ryupy_cpython.obj" del /q "%OUTPUT_DIR%\ryupy_cpython.obj"
    echo [ryupy-cpython] 编译成功: %OUT%
    exit /b 0
)

where clang.exe >nul 2>&1
if %ERRORLEVEL% equ 0 (
    echo [ryupy-cpython] 使用 clang 编译 Windows shim...
    clang.exe -shared -O2 -Wall -Wextra -Werror -I"%PYTHON_HOME%\include" -I"%PYTHON_HOME%\include\python3.12" "%SRC%" -L"%PYTHON_HOME%\libs" -lpython312 -o "%OUT%"
    if %ERRORLEVEL% neq 0 (
        echo [错误] clang 编译 ryupy_cpython.dll 失败
        exit /b %ERRORLEVEL%
    )
    echo [ryupy-cpython] 编译成功: %OUT%
    exit /b 0
)

echo [错误] 未找到 cl.exe 或 clang.exe，Windows 原生 shim 构建需要 MSVC 工具链或 Clang。
exit /b 1
