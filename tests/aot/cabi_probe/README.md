# 固定 C ABI 三平台机制原型（真实native执行待CI）

仅自有 `(i32,i32)->i64`；无 Python binding、模块/CLI/schema、通用 resolver、wheel、CPython C API。

## 与 emitter 的固定接口
- ModuleRef `ryupy_cabi_probe`，entry `ryupy_demo_add_i32`。
- `RyupyCabiProbe.dll`，公开 `RyuPy.CabiProbe.Program.Main(string[])->int`。
- emitter 接唯一预建空 outdir，完成真实 readback 才写 DLL/runtimeconfig/metadata.txt。
- 5 输入 `(0,0),(-1,4),(MAX,MAX),(MIN,MIN),(MIN,MAX)`，输出 `0/3/4294967294/-4294967296/-1`，各行；C reference 与两托管来源完全同值。
- C 先扩大到 int64 再加，无 signed overflow、pointer、所有权、错误码、编码、回调。

## 编排与预算
- Linux x64 / macOS arm64 / Windows x64 分别实际宿主；不跨编译冒验收。
- 每 host 从 harness 首命令总 1800 秒；每工具/C/emitter 构建 180 秒，唯一 AOT publish 600 秒，执行 30 秒，都受剩余总预算。
- 复用现 platform_runner：POSIX 进程组、Windows Job，超时杀子树且回收；无重试/补样本。
- 一次 C reference + 一次 JIT shared + 一次删除输入后的 AOT static native。无性能测量，无 AOT shared 第二 publish。
- cargo 保 argv0 代理名，不 resolve 到 rustup；Windows 需 workflow 已建立 MSVC 开发环境。独立workflow已接线，真实native实测待CI。
- C 源三次构建同一字节：static 无 dllexport、shared 有导出宏、reference 有 main 宏。Windows /MT，`.lib` 真 lib.exe archive 不拿 `/LD` 产生的 importlib 代替。

## 证据与搬迁
- 逐 archive 成员检查 ELF relocatable / arm64 Mach-O object / AMD64 COFF 头形状，拒 Windows short import object；这不是任意 .lib 的完整二进制认证，须结合本次实际编译、链接、搬迁运行；保 static archive 原字节作独立审计，不是运行依赖。
- 保存工具 raw logs、metadata readback 原文、C/DLL/shared/archive/native SHA 与大小、符号和依赖表；仅强可达 Main 调用，不 broad root。
- JIT 共享库放被调程序集同目录，验证 CLR 默认加载机制，不宣称 `_internal` resolver。
- 唯一 AOT 薄宿主选 DirectPInvoke(ModuleRef)+NativeLibrary(archive)，真实 SDK ILC 日志且 stderr 空；没有 PublishSingleFile 冒替代。
- publish native 单独搬到 relocated；删本次 source/shared/IL/host/build/原 publish/参考exe。保非可加载 archive 证据与 raw logs；只运行搬迁 native，空 PATH、无 DOTNET_ROOT、无动态库搜索 env、无相关共享库。
- 依赖表不得出现 probe shared；系统 libc/系统 DLL 仍是系统依赖，不叫全静态/无 OS 依赖。宿主已有 SDK，不冒干净容器证明。
- 非公开机制原型，不修改现 driver manifest 或承诺 native dependency closure；hash 与结果为独立实验记录。

## 待核事项与失败规则
- emitter 已冻结为 workspace example：`cargo build --locked -p ryupy-codegen-cil --example cabi_emit_probe --target-dir <owned>`，只用原 Cargo.lock/vendor，无第二依赖图；产物 `debug/examples/cabi_emit_probe[.exe]`。
- 发射器真实PE readback专项1/1与独立生成通过；尚无C参考/JIT共享库/ILC静态库执行证据，不把metadata通过当native调用成功。
- 任一 metadata/readback、archive、输出、ILC、平台工具、超时失败保原日志/result，不切换实现、不补测、不扩大 root。

## 冻结前收口
- 记录实际 cc/cl 与 ar/lib 版本双流；cl 无输入允许已知 0/2 且必须出现编译器版本标识。
- host 命令行及项目均禁 Directory.Build.props/targets，禁默认 Compile，只纳入 Program.cs；三类编译/ILC/ILLink 警告均视错误。
- Windows 三条 cl 命令显式 /utf-8；中文源码注释不依赖宿主代码页。
- 正式工具位置 tests/aot/cabi_probe/{probe.py,probe.c,test_probe.py}；已接线，但不新增公开Python绑定能力。

## CI与范围

独立工作流 `aot-cabi-probe.yml` 使用实际Linux x64、macOS arm64、Windows x64宿主，
固定.NET10.0.403/Rust1.99.1；每host32分钟step包30分钟内部预算，job40分钟含安装。
旧AOT广路径触发的回归本轮保留，不是新增C ABI模式。仅新的机制probe每host一次
C reference、JITshared、AOTstatic，没有公共Python语法/CLI参数/manifest新schema。
所有原始构建输出和非可加载静态archive留CI artifact，Rust emitter-target缓存不上传，
不把raw logs或二进制入Git。自有C样例固定LF与UTF8，不运行未知第三方库。

本地首次验证：发射器PE roundtrip专项1/1、生成器真实输出readback通过，纯archive
工具5/5；完整make check exit0为523 C#、228固定CP基线、202e2e+30ignored。该门禁
不含本原型C参考/JIT共享库/AOT静态链接执行，后者须真实三平台CI证明。
