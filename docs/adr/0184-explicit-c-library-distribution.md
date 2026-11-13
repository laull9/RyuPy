# ADR-0184：显式 C ABI 动态库与依赖分发

状态：显式动态库路径已实现，macOS arm64 JIT/NativeAOT 搬迁通过；ctypes 编译桥同平台 JIT/NativeAOT 搬迁与缺库负例通过。
日期：2026-11-13。

## 决定

沿用 ADR-0173 的通用声明式 C ABI，在 v3 `target` 增加 `linkage: "dynamic"`，
让 JIT 与 NativeAOT 使用同一份 shared 输入。NativeAOT 的机器签名仍在构建时固定；
动态库经已有绝对路径 resolver 在首次调用时加载，不生成运行时 IL。
缺省行为保留 JIT shared、AOT archive；AOT 也可显式 `linkage: "static"`。

`dependencies` 是最多 64 个相对输入路径，入口库与依赖都检查文件类型、目标架构，
持有打开句柄并复制到同次候选包。入口部署名沿旧合同固定；依赖保留原文件名，
大小写不敏感重名与入口覆盖明确拒绝。每个非系统传递依赖都须列出。

普通文件遵循 AGENTS：跳过 SHA/MD5。必要构建路径登记来源、大小、打开对象身份、
元数据及真实机器类型；历史报告字段形状保留，摘要值为空。签名正确性沿同次 PE 报告验证。

## 加载合同

本轮选择显式依赖清单而非猜测闭包。编译器不执行目标库、不解析任意插件搜索逻辑，
也不修改第三方 ABI。ELF 依赖须采用 `$ORIGIN`，Mach-O 须采用 `@loader_path` 或
相应相对 rpath；不改用户 PATH、LD_LIBRARY_PATH 或 DYLD_LIBRARY_PATH。
库作者负责 ABI、系统依赖版本、符号可见性、rpath、许可与发行授权。
遗漏依赖在实际调用时以 ImportError 失败；无系统库名搜索回退。

同宿主 macOS arm64、Linux x64 glibc、Windows x64 MSVC 保持原目标集合。
动态分发首先实测 macOS arm64；其他平台必须独立验证加载器合同后发布。
NativeAOT 懒加载与 DirectPInvoke 的区别依据
[.NET 官方互操作说明](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/interop)。

## 类型与所有权

直接声明支持 13 种标量、void 和只读调用期 utf8/bytes 借用；签名限制沿 ADR-0173。
任意裸指针、按值结构、可写/返回 buffer、回调、varargs 和 native-owned handle 的
直接声明会被严格诊断拒绝。C++ 先提供 `extern "C"` 适配边界。

复杂 C ABI 采用 CPython provider 的 ctypes，并将适配模块、动态库与依赖一起打包。
适配者明确 Structure 布局、argtypes/restype、回调生命周期、错误模型和 allocator/free 配对。
指针由适配模块内部管理；输出复制后立即 finally/free；回调异常在返回 C 后重抛。
该通用路径复用 CPython/libffi；不要求每个第三方库进入 RyuPy 产品代码。

编译回调可传播纯单继承的用户异常类。桥按真实 PyClass 身份生成 CPython 异常类型，
保留 base、module/name/qualname/doc 元数据；同名类各有身份。CPython 侧构造或继续派生的异常
返回时，按真实 MRO 中最近的已映射类调用原编译 ExceptionFactory，保留 CLR typed catch 和实际 CP 类型。
导出前整条继承链须无自定义构造、方法、类字段，实例须无自定义字段，否则明确 NotImplementedError。
成功导出后该 PyClass 的 SetAttr/DeleteAttr 明确拒绝修改，CP 对应类型设置 IMMUTABLETYPE；
这是受限的纯继承导出合同。未导出的原生类保持原行为。

## 验收

[独立套件](../../tests/aot/c_library/README.md) 包含：无产品专用分支的 zlib 1.3.1
`compressBound` / `crc32_z` 直接签名；自有入口→相邻依赖的两层动态库；
CPython 同次差分；JIT/NativeAOT 搬迁、原输入/构建根删除、清工具 PATH、缺依赖负例；
ctypes 的结构、指针输出与成对释放、编译函数回调和异常返回。

zlib API 以[官方手册](https://zlib.net/manual.html)及固定 1.3.1 发行头文件为准，
发行来源为 <https://zlib.net/fossils/zlib-1.3.1.tar.gz>，许可随发行 README。
网络下载只发生在独立验收驱动，产品构建与运行均无隐式下载。
