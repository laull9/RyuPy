# ADR-0207：CPython 产品中的编译源码随包位置

- 日期：2026-11-16
- 状态：已实施，完整门禁与 macOS arm64 两真实项目三模式搬迁验收通过

SanBudget 使用 `Path(__file__).resolve().parent` 创建 SQLite 数据库。编译 JIT 原先保留
开发机绝对路径，源码删除后数据库连接失败。此行为影响使用源码相邻资源的通用项目。

在显式 CPython 产品中，已编译模块的原源码作为数据部署到
`python/ryupy_compiled/<模块路径>.py`；包保留 `__init__.py`。该目录不加入 CPython
搜索路径，导入仍由既有编译模块注册与回调执行。诊断来源保留原位置，公开初始
`__file__` 使用新的 `BundleRelative` 合同，以 `AppContext.BaseDirectory` 解析。
JIT、NativeAOT、onefile 解包后的程序集根共用此位置。

原 `ExecutableRelative` 的 NativeAOT 约束保留，纯 RyuPy 构建沿用旧位置。
驱动负责选择及复制源码，MIR 仅承载路径，codegen 只发射位置模式，Runtime 只解析
已冻结的随包相对路径。本切不自动复制任意业务数据；非源码资源继续由显式资源计划收集。

同轮真实项目验收还暴露出 `str.title()` 缺失。本 API 在已选 CPython provider 中调用
真实 `builtins.str.title`，保留 Unicode、参数和错误协议；原生执行域继续按已有能力报错。
安装 wheel 的合法 include 头文件仅登记为构建文件，不进入运行期载荷；越界仍拒绝。
macOS 原生边解析仅检查目标 arm64，排除架构标题和真实 LC_ID_DYLIB 自身标识。

验收包含代码生成模式断言、路径逃逸负例、绿色头文件闭包与越界负例，以及原 SanBudget
编译类、真实窗口/按钮/SQLite 在删除输入后搬迁执行。各执行模式的最终结果单列于
`tests/general_projects/README.md`。
