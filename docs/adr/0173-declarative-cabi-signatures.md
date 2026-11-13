# ADR-0173：面向 NativeAOT 的声明式 C ABI 签名与只读借用

状态：已实现；专项证据与复现方式见[独立验收](../../tests/aot/binding_abi/README.md)。

## 目标与选择

在 ADR0169 的单库多导出与 ADR0171 的资源组合发布之上，引入版本 3 声明。
重点是 AOT 程序的通用、静态、可审计外部调用，而非运行时 FFI 签名解释器。
复用 C# 的严格编码、BigInteger、GCHandle 和 .NET 静态 PInvoke；每个导出在编译期
生成精确机器签名。禁止 Reflection.Emit、运行时任意签名 JIT、业务符号名特判及隐含封送。

本轮选择完整交付标量与输入借用，暂缓 opaque handle。资源所有权是下一轮独立竖切，
必须将创建、失败回滚、销毁与调用期保活一起完成，再开放 Python 对象入口。

## 版本与声明

```json
{
  "schema_version": 3,
  "module": "abi",
  "library": "abi_c",
  "exports": [{
    "name": "read",
    "symbol": "read_data",
    "signature": {
      "calling_convention": "cdecl",
      "parameters": [
        {"name": "text", "type": "utf8", "ownership": "borrowed_call", "length": "usize"},
        {"name": "data", "type": "bytes", "ownership": "borrowed_call", "length": "usize"},
        {"name": "flags", "type": "u32"}
      ],
      "returns": "u64"
    }
  }],
  "target": {"rid": "osx-arm64", "shared": "libabi_c.dylib", "archive": "abi.a"}
}
```

- v1/v2 继续只接受原字符串 `cdecl_i32_i32_i64`，保留原工厂和转换行为；
  v3 必须使用签名对象。混用版本字段、未知字段、null 封送字段、未知类型明确报错。
- 单声明仍为一个模块和一个库，1–64 个导出。每个签名有 0–16 个逻辑参数。
- 参数名必须为唯一、有界 ASCII 标识符；所有参数均为位置专用且必填，无默认值和 varargs。
- 同一 C 符号可有多个 Python 别名，各自独立函数身份；逻辑类型序列和返回类型必须相同。
  名字可不同，`utf8` 与 `bytes` 别名合同不可混用；冲突先于原生文件读取被拒绝。
- `cdecl` 唯一。按现有同宿主 RID 支持 Linux x64 glibc、macOS arm64、Windows x64 MSVC。
  本轮新增 ABI 的实机证据来自 macOS arm64，Linux/Windows 留待各自 CI/实机验收。
- JIT 只选择 shared，AOT 只选择 archive。库路径、冻结、resolver 与发布事务保持原合同。

### 类型合同

| 声明类型 | C 合同 / CLR PInvoke 类型 | Python 入参 / 返回 |
| --- | --- | --- |
| i8 / u8 | int8_t / uint8_t；sbyte / byte | 实际 int，精确范围 / int |
| i16 / u16 | int16_t / uint16_t；short / ushort | 实际 int，精确范围 / int |
| i32 / u32 | int32_t / uint32_t；int / uint | 实际 int，精确范围 / int |
| i64 / u64 | int64_t / uint64_t；long / ulong | 实际 int，精确范围 / int |
| isize / usize | intptr_t / uintptr_t；nint / nuint | 宿主指针位宽的实际 int / int |
| f32 / f64 | float / double；float / double | 实际 float / Python binary64 float |
| bool8 | uint8_t；byte，值仅 0 或 1 | 实际 bool / bool |
| utf8 | const uint8_t* 紧接 size_t；nint + nuint | 实际 str，严格 UTF8，只读调用期借用 |
| bytes | const uint8_t* 紧接 size_t；nint + nuint | 实际 bytes，只读调用期借用 |
| void | C void / CLR void | 仅返回，Python None |

这里的 bool8 明确为 `uint8_t`，不采用 C `_Bool` 或 CLR bool 默认封送。
`isize/usize` 固定映射 intptr_t/uintptr_t，不推断 C long、wchar_t 或平台 typedef。
所有支持的目标目前为 64 位；跨位宽交叉编译继续拒绝。

整数只消费运行时 int/long/BigInteger，拒绝 bool、浮点、字符串与 `__index__` 等钩子。
越界抛 OverflowError，类型错误抛 TypeError。u64 返回超过 signed64 时使用 BigInteger，
不会截断；小整数结果沿既有缓存路径物化。

浮点只消费实际 Python float，不自动接受 int、bool 或 `__float__`。
f32 使用静态 `conv.r4` 舍入，再在返回时提升为 binary64；有限值变成无穷时抛 OverflowError。
保留负零、次正规数、NaN 与正负无穷；NaN 载荷位模式没有承诺。
bool8 返回值出现 2–255 时抛 ValueError，避免错误状态被吞成 true。

## 借用、长度与异常安全

`ownership: borrowed_call` 在 v3 中唯一且含义固定：只读、同步调用期间有效、不转移所有权。
`length: usize` 是编码后字节长度，作为紧随地址的独立机器参数展开，由 wrapper 自行提供。
Python 无法传独立长度或地址；缓冲返回、可写缓冲、持有指针、任意裸指针均被声明检查拒绝。

1. 在进入 C 和固定任何地址前，依声明顺序完成全部参数的类型、范围和编码检查。
2. str 使用 `UTF8Encoding(false, true)` 产生自有数组，无 BOM、无追加 NUL；嵌入 NUL 原样保留。
   孤立 UTF16 代理项抛 UnicodeEncodeError；补充平面字符按 UTF8 编码。长度以字节计，绝非码点数。
3. bytes 复制快照，不暴露 Python 对象的底层存储。即使一个对象传入两次，也各自独立快照。
4. 所有转换成功后，以 `GCHandleType.Pinned` 固定数组。空输入使用非空的一字节哨兵，长度为 0。
   地址始终供 C 消费，Python 不获得句柄或地址值。
5. 构造帧时部分 pin 失败立即释放此前句柄。生成 wrapper 的最外层 finally 负责确定性 Dispose；
   成功、缺符号、缺库、坏映像及返回物化异常均经过同一释放路径，Dispose 幂等。
6. 内层只捕获三个既有 loader 异常并转 ImportError；返回转换在窄 catch 外执行，保留协议错误类型。
   参数/返回错误保留调用帧与当前 Python 异常上下文。

这是受信原生代码边界：C 必须遵守只读、长度与调用期限制，不得保留地址、异步访问或释放借用内存。
进程崩溃、原生非法写入和违反 C ABI 的库属于原生代码错误，托管 finally 仅承接可展开的异常路径。
当前没有 callback、C++ 异常跨界、结构体、可变参数、native-owned 输出、errno/last-error 自动捕获。
后续每一项都需独立、版本明确的合同，禁止静默推断。

### 成本与 AOT 路线

每次调用包含绑定数组、预转换数组、缓冲快照及短期 pin；成本与声明长度/字节数线性相关。
最大 16 个缓冲展开为 32 个机器参数，CIL 栈上界为 34；真实 C fixture 覆盖该极限调用。
选择快照是为了稳定所有权、编码与别名边界，本轮未测性能，不宣称零拷贝或吞吐提升。

AOT 仍逐实际符号生成 `DirectPInvoke library!symbol` 并去重，archive 只登记一次。
签名与调用指令均静态可见，ILC 编译后直接运行，无运行时探测/生成签名或回退解释器。
利用 C# 成熟设施承接转换和生命周期，后续性能优化必须保持常用结果、身份和副作用合同。

## 分层与清单

- MIR 的 `native_abi` 定义封闭类型、参数序列和签名验证，不依赖 CIL 或 driver。
- driver 严格解析 JSON，归一化旧版本，检查所有权与长度；显式签名随绑定槽传给 codegen。
- codegen 只消费 MIR 类型和显式 ID，生成 CLR primitive PInvoke、wrapper、释放区域与实际 PE 报告。
  内部未验证合同同样拒绝，整个流程不按业务模块/符号判断实现。
- runtime 负责严格 Python 值转换、固定帧和返回物化；不新增语法、provider 或 RuntimeFn。
- 清单保留声明签名对象，并对照同次实际 PE 的完整机器签名。v1/v2 保持 schema3/4，
  加资源保持5/6；v3 无资源使用7，加资源使用8。布局层保留已有资源验证与原子安装策略。

既有清单身份字段沿原发布合同生成；本轮没有新增普通文件摘要或外置 hash 验收流程。

### 留给 catalog/provider 整合的接入点

本分支基于 cd678640；主树 96dfeb13 的 source_probe/scan 与 colorsys catalog 由主代理整合，
本轮不修改 source_catalog。当前 binding 构建会设置 `Session.fixed_binding_build`，
`optional_sources::enabled` 和 `default_aot::eligible` 均据此关闭可选源码路径。
所以本轮通过的是声明 ABI、显式源码闭包与资源组合；catalog/provider 联合发布仍需下一步接通。

1. **计划入口**：`binding_build::build_internal` 在 `BindingPlan::read` 之后调用
   `compile_linked_locations`。在这次源码计划中接入统一 provider 选择，先冻结用户源码、
   catalog 源码与显式原生模块身份，再产生最终 LinkedProgram；禁止仅移除策略门就宣告完成。
   同名冲突继续经过 `BindingPlan::check_modules`，一个模块只允许一个 provider。
2. **发射交界**：`SourceModules::capture` 必须捕获最终实际源码图，然后 `declare` 追加
   原生模块与绑定槽，最后调用 `fixed_bindings::emit_source_with_report`。保持此顺序，
   新 catalog 源码参与来源位置和资源 consumer 校验，生成原生模块独立登记。
   AOT 若选择或裁剪源码，必须随最终图投影模块 ID，再捕获并追加绑定，不能沿用裁剪前编号。
3. **报告交界**：`BindingManifest::new` 已同时接收源码摘要、同次实际 PE 发射报告与
   BindingPlan；`BindingManifest::render` 是联合报告的入口。整合时显式传入同次冻结的
   catalog/provider 记录与来源计划，列出选择理由、活动/延期状态及依赖；实际源码和生成
   原生模块继续分列。不要在渲染时重新扫描来源，或把旧会话的 optional-sources 报告复用进来。
4. **部署交界**：沿 `aot::publish_binding` 与现有资源扩展、OwnedAttempt 安装事务携带
   联合报告。普通构建调用的 `optional_sources::write_report` 当前未接入 binding 分支，
   整合时需把报告写入本轮候选包并随 dist 原子安装，失败保持旧 dist 和报告共同可用。
5. **联合验收**：增加同一入口调用一个已验收 colorsys API、一个 v3 缓冲 ABI 导出、
   再读取资源的差分测试；核对 provider、来源模块、绑定槽和机器签名，覆盖 JIT/AOT 与
   移走源码/catalog/原库及换 cwd 的搬迁。补同名 provider 冲突和失败保旧 dist 的负例。

## 许可与依赖边界

没有新增 NuGet、Rust 或原生第三方依赖。运行时复用现有 .NET/BCL 的 PInvoke、GCHandle、
UTF8Encoding、BigInteger；.NET runtime 使用 MIT 许可，实际分发继续随 SDK/runtime 现有许可证。
测试 C 源码是项目自有恒等/字节读取/计数夹具，ctypes 仅用于固定 CPython 的独立参考测试。

SQLite、OpenSSL 等后续能力应复用成熟原生库或既有 C# 库，不重写数据库引擎和密码算法。
接入前逐库固定版本、公开 C API、错误模型、许可/NOTICE、依赖闭包与每个 RID 的发布输入。
当前单库 ABI 没有自动传递 native 依赖收集能力，也未新增这些库的 API 支持。

## opaque handle 的准确下一步

为 handle 另立类型身份与专用声明：创建/销毁符号、失败哨兵、allocator 配对、线程约束和错误保存时机。
生成静态 PInvoke 的 SafeHandle 子类，提供显式 close/context manager 与终结兜底，ReleaseHandle 调用
已绑定的唯一销毁函数。调用期间使用 SafeHandle 保活或成对 DangerousAddRef/DangerousRelease，
杜绝 close 与调用并发造成提前释放。创建成功但包装失败必须释放；use-after-close、重复 close、
空句柄、异常中途退出均有明确行为，并禁止用户自行构造/数值化任意地址。

验收必须包含真实 C 分配/释放计数、分配失败、包装失败回滚、调用失败、返回转换失败、显式关闭、
GC 兜底及 JIT/NativeAOT 搬迁。完成上述整条生命周期后，才接入 SQLite/SSL 类资源 API。
