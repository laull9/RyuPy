# 通用 C ABI 动态库验收

入口固定为 `ryupy build app.py --native-binding binding.json`。产品路径不按库名或函数名分支。
声明类型及复杂 ABI 的选择见 [ADR-0184](../../../docs/adr/0184-explicit-c-library-distribution.md)。

v3 示例：

```json
{"schema_version":3,"module":"native_demo","library":"thirdparty",
 "exports":[{"name":"bound","symbol":"compressBound","signature":{
   "calling_convention":"cdecl","parameters":[{"name":"size","type":"usize"}],"returns":"usize"}}],
 "target":{"rid":"osx-arm64","linkage":"dynamic","shared":"libz.dylib","dependencies":[]}}
```

`usize` 此处依据 macOS arm64/Linux x64 的 zlib `uLong` ABI；Windows 的 C long 为 32 位，
必须另写精确声明。未知类型、裸指针、struct 和 callback 直接签名会在打开原生输入前明确拒绝。
复杂签名可写小型 C shim 降为支持合同，或选择随包 CPython provider 的 ctypes。

## 运行

```sh
make runtime
cargo build -p ryupy-cli
uv run --python 3.12.13 python -B tests/aot/c_library/run.py --ryupy target/debug/ryupy
```

`run.py` 仅在构建验收阶段下载官方 zlib 1.3.1 源码并构建动态库，产品运行离线，直接绑定 `compressBound`、`crc32_z`；
另编译自有 demo→helper 两层动态库，依赖采用 `@loader_path`/`$ORIGIN`。
Python ctypes 当次生成 stdout 预期，对照 JIT/AOT 的完整退出码、stdout 与 stderr。
构建完成后删除源程序、shared 原输入、zlib 源码、SDK 构建树，再切换 cwd、清 Python/加载器环境、
使用空 PATH 执行搬迁包。删除实际 helper 后必须加载失败并抛 ImportError。
临时目录由上下文退出时清理，仓库只保存源码；不生成文件摘要。
前置是 C 编译器、make、curl、.NET 10 SDK、固定 CPython；当前驱动支持 macOS arm64/Linux x64。

## ctypes 复杂 ABI

`complex.c` 公开按值结构参数/返回、malloc buffer/配对 free、C 函数指针回调。
`ffi_demo.py` 显式指定每个 ctypes 参数和返回类型；buffer 连续创建/释放 128 次，结束计数为零。
`ctypes_app.py` 是真正由 RyuPy 编译的入口，传入已编译函数供 ctypes 回调，覆盖正常回调及 ValueError。

以已冻结且可构建宿主的 provider JSON 为模板运行：

```sh
uv run --python 3.12.13 python -B tests/aot/c_library/run_ctypes.py \
  --ryupy target/debug/ryupy --provider /absolute/provider.json
```

驱动追加 `ffi_demo`、`ctypes` 模块，打包手写 facade 与自有动态库，执行 CPython/JIT/AOT 同程序，
再删除源码/SDK构建树并搬迁，清环境、空 PATH、换 cwd 后差分；删除动态库执行负例。
模板须提供正式发行来源、随包 libpython、桥 DLL 与 shim，禁止改用 Python 子进程执行产品入口。

## 状态

- 类型解析、动态 AOT 输入选择、显式依赖部署结构单测：通过（macOS arm64，2026-11-13）。
- 原生 JIT/AOT/搬迁与真实 zlib：通过（macOS arm64，2026-11-13）。
  demo→helper 两层动态依赖在清环境/空 PATH 下搬迁成功；删除 helper 两模式均以 ImportError 失败；
  zlib 1.3.1 两个公开 C API 在 JIT/NativeAOT 搬迁后与 CPython 当次输出逐字节一致。
  原输入、第三方源码与构建根均已删除，仓库外临时目录退出时清理。
- ctypes 编译应用桥：待验收，输入保留 pending。
- Linux x64、Windows x64、无 Python/.NET/SDK 干净机：待验收。
