# 资源、as_file 与 C ABI 整合验收

2026-11-13，主分支 macOS arm64，固定 CPython 3.12.13。
真实 runner：`tests/aot/binding_resources/run.py`，23 步符合预期，见 result.json。
v1/v2 各成功发布一次，v2 另做真实缺符号链接失败；四类构建失败均保留旧 dist。
应用通过 as_file 读取部署数据，保持 Path 身份；正常/异常退出保留资源、传播原异常。
原输入及构建路径失效后换 cwd，搬迁原生双流与 CPython 逐字一致；缺数据副本报 FileNotFoundError。
原始现场：`target/integrated-binding-resources/binding-resources-yz4rt1lp/`。

整合树 make check exit 0：fmt、strict clippy、Rust workspace 全通过；
C# 3009 passed / 2 skipped；CPython 基线 720；目录自检 723；编译 e2e 443 / 284 ignored。
日志：`target/integrated-final-check.log`。
SDK 仍安装于宿主。Linux/Windows 新组合合同及无 SDK 目标环境待验。
