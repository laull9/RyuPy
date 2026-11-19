# 通用接口目录与原生扩展产品门

本门使用两个不同名称、包布局和扩展模型的手写输入：

- `cedar_dock._atlas` 是传统单阶段 CP 扩展，提供 positional-only 参数、keyword-only 偏移、
  原对象回调与 `ValueError`。
- `violet_ridge.engine._nacre` 使用 PEP489 多阶段模块、独立模块状态和 PEP573 heap type。
  `Gauge` 的实例偏移、拥有的 Unicode 标签和模块状态都参与实际调用。

两份 `.pyi` 分别描述函数和类方法。应用入口和 `identity` / `increment` 回调由 Rust
静态编译为 CIL，扩展通过真实 CPython ABI 回调；每个产品要求恰好两次静态 core 调用、
两个 owner 析构及全部终态计数归零。包初始化文件仅有手写说明，stub 从不求值。

```bash
python3 -B tests/general_projects/interface_exports/run.py \
  --python /absolute/path/cpython-3.12.13/bin/python3.12 \
  --cli /absolute/path/ryupy
python3 -B tests/general_projects/interface_exports/test_macho_audit.py
```

默认构建两模型 × JIT / NativeAOT / OneFile 共六个产品。逐格核验实际分发清单中的
`interface_catalog`：精确 `PyInit` 本地导出、模块执行归属、stub 参数种类/默认值与
类方法声明。目录中的 `method_signatures_verified=false` 与 `machine_abi=not_derived`
保留当前证据边界；真实调用与失败路径另外验证本门覆盖的 Python API。

每个包同时部署一个与扩展同后缀的普通共享库及其 stub，另外部署仅有 `.pyi` 的模块。
二者的 `execution_provider` 都必须为空；真实运行进一步要求普通库导入得到 `ImportError`、
stub-only 得到 `ModuleNotFoundError`。这些判断依据安装文件、初始化导出和公开 schema，
生产实现统一按协议分派。

构建完成后删除实际编译用的 C 源码副本、应用源码、wheel、venv 和所有构建目录。
产品移到中文空格路径，在空 PATH、污染的 PYTHONHOME/PYTHONPATH 下运行；完整三流
与同次固定 CPython oracle 逐字节比较。OneFile 还要求从本轮私有临时目录加载并在退出后
自动清理解包目录。所有命令、目录和 Host 报告保存在仓外 `~/.cache/ryupy/validation/interface-exports/`。
成功与失败都记录 `TEMPORARY_REMOVED`。

本门实际构建 `.so` 后缀的 `MH_DYLIB`，与 `MH_BUNDLE` 扩展不同。
独立 Mach-O 审计器按真实 `LC_ID_DYLIB` 识别自身 ID：自身 ID 不是加载其他映像的边，
所有真实依赖仍要求闭合。小门对 `.so` dylib、bundle、普通 `.dylib` 做真实编译对照，
并确认 dylib/bundle 的未闭合 `@rpath` 加载边继续拒绝。
首轮 `20261117T090323.469708Z` 的 verifier 失败和清理记录保留；修复后的产品成绩单独登记。

当前手写扩展构建器支持 macOS/Linux，运行证据以实际平台登记；Windows 编译与搬迁另验。

2026-11-17 macOS arm64 / stock CPython 3.12.13：两个模型 × 三形态 **6/6** 严格三流差分通过，
接口目录和两类执行身份负例全部通过。真实 callback 共 12 次静态 CIL 调用、12 个 owner 析构，
各 Host 报告终态全零。所有原始输入和构建中间物已删除，OneFile 解包目录自动清理。
最终证据目录为 `~/.cache/ryupy/validation/interface-exports/20261117T090652.913212Z/`，
包含完整命令、逐格目录/Host 报告、六项结果与 `TEMPORARY_REMOVED True`。
