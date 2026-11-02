# 固定 C ABI 的 Python 入口：A/B 有界实现

状态：三平台底层机制已经[验证](../../../docs/validation/aot-fixed-cabi/README.md)；
本文的新 Python 绑定尚未完成，不是已发布的 CLI/JSON/API 或任意 wheel 支持。

## A：真实模块、函数和窄错误边界

- 编译器使用三个已有 ID（module/init/export）的显式记录，复用 `ModuleDef`、Dynamic
  导出槽和现有 import/get/set。没有伪 `.py`/源码 SHA、通用 FFI 解释器或新的 MIR i32 类型。
- Python 可见函数是真实 `PyFunction`，固定 `left, right, /`，无默认参数。所有用户调用
  读取公开绑定，不把用户名称替成静态 PInvoke；保存 alias 与后续属性重绑须保原身份。
- 专用 `CreateI32Binary` 复用 `PySignature`/`PyBinder`；仅绑定错误由此 PrepareRaise，
  body 在 catch 外。签名仅在创建函数时分配，不改变所有原生函数的热路径。
- `CheckedI32` 只接受实际 CLR int/long/BigInteger 表示，拒 bool、浮点、文本、None 和
  用户 `__index__`/`__int__`，不调用转换协议；先范围比较后转 signed32。左校验先于右，
  但普通 Python 实参表达式仍全部先求值。类型/范围分别 TypeError/OverflowError。
- 固定 wrapper 仅 native call 周围捕获 DllNotFound/EntryPointNotFound/BadImageFormat，
  映射 ImportError，正文仅逻辑库名/符号；不暴露 CLR 原消息中的绝对路径。不吞其它CLR错误，
  不承诺错误 ABI/内存破坏可恢复。类型、范围、绑定和加载错误都须保 caller 帧/context。
- 注册前只读核 Registry/Cache exact/后代冲突，原生模块先存在；不覆盖现有名字。
  初始化全部成功后最后写公开函数槽。生成模块初始 `__file__` 真缺失，后续显式写入可见。
  沿现单线程初始化边界，不增加整个 import 系统线程安全承诺。

## B：显式构建输入与 `_internal` 部署（尚未接线）

只允许自有已知 ABI 的首个数值案例，普通 JIT 必须真实 shared，AOT 优先真静态 archive。
声明/输入冻结/manifest 来源与 native 角色须分别明确，不能把库伪装为普通数据资源，
不能从符号存在或 hash 推断任意库 ABI 安全；不自动搜集第三方传递依赖。

每个生成程序集统一登记一次 resolver（JIT/AOT 都登记），使用真实 owner Assembly，
不急切探测/加载 shared。首次 JIT 调用仅从冻结的应用根
`_internal/native/v1/<id>/<library>` 绝对路径加载；未知名/错误 owner/缺库明确拒，
不返回零让 CLR 继续裸名探测。成功句柄至少保活到进程结束，不承诺可卸载 ALC。
首入口仅 standalone `dotnet generated.dll`，不把 dotnet 安装路径、cwd 或公开 `__file__`
当部署根；不承诺任意嵌入 host。祖先 symlink/reparse 保守拒绝，非恶意文件替换沙箱。

不能用 `!RuntimeFeature.IsDynamicCodeSupported` 判断 AOT：普通 JIT 也能通过开关关闭。
.NET10 官方源码支持统一 SetDllImportResolver 冷登记，不调用 Load；AOT DirectPInvoke
绕过回调。因此不需要新增可改 mode 开关/双入口，但仍必须实测同 DLL 的 ILC 以及
无 shared 搬迁、JIT 开关关闭且有同名诱饵仍 fail-closed，源码核对不等于此实证。

## 当前实际检查与未完成项

runtime helper/注册 partial/专用 factory 与 resolver 专项 **15/15、0 skip**，Release 构建通过。
首次编译仅 CS1591（新公开类缺 XML summary），补一行文档后原专项通过；未删断言。
这些测试证明冷 API/注册/严格转换/错误准备，不证明生成 Python 入口或真实 C 调用。
编译侧真实 PE/动态身份与私有 driver 回归仍在隔离；公开声明、冻结 native 输入、
manifest 扩展、JIT `_internal` 搬迁和 AOT 同链路尚未完成。旧无 binding 路径不新增调用。

既有真实 C ABI CI 只有微型 emitter + C 库，不用它替代新 Python 边界验收。
本阶段不增加字符串、指针、回调、所有权、errno 或 CPython C API/wheel 承诺。

冷组件独立完整门禁已 exit0：538 C# / 228 固定CP基线 / 202 e2e + 30 ignored。
本次仍仅WIP组件验收，未将Python绑定父目标勾为完成。
