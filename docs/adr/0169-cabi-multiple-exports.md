# ADR-0169：固定 C ABI 的单库多导出

状态：实现候选；本机真实 JIT / NativeAOT 搬迁、错误路径与完整 make check 通过，等待主代理审查整合。

## 决定

将已存在的单模块、单库、固定 `cdecl (i32, i32) -> i64` 绑定扩展为一个模块的
1–64 个公开函数。版本 1 声明保持原合同；版本 2 使用 `exports` 数组，单函数记录为
`name`、`symbol`、`signature`。两种版本分别验证必需字段、拒绝混用单导出字段，
继续拒绝未知字段、重复公开名字、保留元数据名字、错误签名及超额导出。
同一 C 符号可以显式绑定两个公开名字，得到两个独立的一等函数对象。

选择多导出是为了让自有 C 库能提供一组实际操作，而无需为每个函数单独打一个应用。
此次保持现有运行时函数工厂、参数边界、模块注册、resolver 和固定签名，
无需新增通用类型语言、动态 FFI 解释器或 Python C API。

## 分层与部署

- driver 将各导出分别分配为 Dynamic 全局槽，所有槽归属同一个真实模块和空初始化器；
  codegen 只消费显式 MIR ID 记录。原源码来源凭据按新增模块集合校验，多个函数仍只增加一个模块。
- codegen 在构造 Resolution 之前检查一库、一模块、一初始化器、完整且互异的导出槽。
  每个函数有唯一 wrapper/PInvoke 方法名、独立严格参数工厂和错误中的公开函数名。
  多导出先创建全部函数对象，再写全部公开槽，保持发布顺序。
- 生成模块只注册一次，resolver 只登记一次；JIT 仍只搬一份冻结 shared 到
  `_internal/native/v1/b0/library.*`。函数的 `binding_id` 是逐导出身份，部署目录的 `b0`
  是既有单库布局；使用清单的 `relative_file` 寻址，不能由函数编号推导部署名。
- AOT 为每个不同的实际 C 符号登记精确 `DirectPInvoke library!symbol`，重复符号去重；
  `NativeLibrary` 只登记一份 archive。任何声明中的符号在静态链接时缺失都导致构建失败。
- v1 保持产物清单 schema 3；v2 使用 schema 4，生成模块的来源由 `binding_ids` 列出
  全部函数，`native_bindings` 按声明顺序列出每个实际 PInvoke/委托/导出槽。
  模块公开名列表继续遵守通用报告的排序合同。

## 错误、取舍与成本

JIT 继续惰性解析每个符号。shared 里缺少第二个函数时，该调用抛 ImportError，
已经可用的首函数及同符号别名继续工作。参数绑定、严格实际整数检查和 signed32 范围检查
发生在原生调用之前，错误消息使用当前公开函数名，保留调用帧。
缺库和坏映像继续经过已有的窄 loader 异常边界。

代码体积和初始化成本随公开函数数量线性增长，故限定 64 个导出；每次调用仍使用现有
参数数组/严格检查，没有增加性能特化。实测此次关注正确性与部署，未采样吞吐或启动成本。

本轮实际宿主为 macOS arm64，同宿主 JIT 与静态 NativeAOT 已验。
Linux x64 和 Windows x64 的新增多导出执行仍待各自实机验证。
签名扩展、多个库、传递 native 依赖收集、字符串、指针、回调、所有权和 wheel 接入另行设计。
`--resources` 与 `--native-binding` 仍在 CLI 与 driver 两处互斥；绑定发布向 AOT publisher
传入的 ResourcePlan 仍为 None，这是接下来的独立部署缺口。

验证方法、初始失败与复现命令见[多导出验收](../../tests/aot/binding_multi/README.md)。
