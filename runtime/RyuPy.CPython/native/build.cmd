@echo off
rem 使用同目录 PowerShell 入口统一处理选中版本、工具链及仓库外构建目录。
rem 所有参数保持原始引号，避免中文和空格路径被拆分。
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0build.ps1" %*
exit /b %ERRORLEVEL%
