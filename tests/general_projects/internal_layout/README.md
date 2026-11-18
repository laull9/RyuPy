# internal 布局的 Runtime-only SDK 引导门

`runtime_only_jit.py` 复制实际官方 dotnet muxer，只链接该安装的 `host/fxr` 和 `shared`。
私有根没有 `sdk` 或 `packs`；先要求 `dotnet --list-sdks` 成功且为空，并记录可用
Microsoft.NETCore.App。固定 DOTNET、DOTNET_ROOT、禁用多级查找，清除 SDK/MSBuild
寻址环境。PATH 只含 `/usr/bin:/bin`；这两个目录如提供另一套 dotnet，驱动立即拒绝。
DOTNET_CLI_HOME 使用私有目录，用户 HOME 保持原样。官方安装及用户文件保持原样。

```bash
python3 -B tests/general_projects/internal_layout/runtime_only_jit.py \
  --cli /absolute/path/to/frozen/ryupy \
  --python /absolute/path/to/cpython-3.12.13/bin/python3.12
```

通过 `--dotnet` 指定实际官方 muxer，默认使用当前 PATH 找到的 muxer。
`--evidence` 指定仓外报告父目录；默认写入 `~/.cache/ryupy/validation/runtime-only-jit/`。

两个不同手写输入分别测试命令行发票计算和标准输入收据处理，以固定 CPython 同次
运行确认输入有效，再通过 `--native` 尝试构建 JIT。当前产品用官方 SDK 生成 apphost；
无 SDK 时构建应在调用 msbuild 之前给出中文需求、空 SDK 清单、微软官方 SDK 下载页、
官方脚本的一键安装命令，以及设置 DOTNET 后重试的说明。驱动只检查诊断，完全不执行
安装命令。用户显式安装 SDK 后，正常 JIT 构建与公开入口搬迁由其它布局产品门验收。

`--expect-sdk-failure` 仅用于历史红证：两个输入必须实际失败，并暴露旧版 apphost/SDK
底层错误。最终引导门不加此参数，要求完整中文操作指引。临时 Runtime-only 根、输入和
失败构建输出均在退出时清理，原始命令、三流、Runtime 清单与结果保存在仓外。

此门目前针对 POSIX 本机；`host/fxr`、`shared` 链接到明确的已安装官方目录，其它目录
未链接。Windows、干净目标机器及 NativeAOT 由各自产品门验收。
