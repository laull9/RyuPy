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
编译侧已接线私有内存声明与真实 PE/driver 回归；公开声明、冻结 native 输入、
manifest 扩展、JIT `_internal` 搬迁和 AOT 同链路尚未完成。旧无 binding 路径不新增调用。

既有真实 C ABI CI 只有微型 emitter + C 库，不用它替代新 Python 边界验收。
本阶段不增加字符串、指针、回调、所有权、errno 或 CPython C API/wheel 承诺。

冷组件独立完整门禁已 exit0：538 C# / 228 固定CP基线 / 202 e2e + 30 ignored。
本次仍仅WIP组件验收，未将Python绑定父目标勾为完成。


## A 编译接线与发现的调用顺序问题（当前 WIP）

固定 wrapper 的真实 PE 检查为5项；故意缺失的库通过真实生成 DLL 验证三项私有 driver，
覆盖模块身份/重绑、参数边界、注册冲突和真实末异常类型/调用帧。这不代表成功调用 C 或 B 部署完成。
首轮 PE 结构绿后，真实 loader 暴露 DllNotFoundException 的引用来源错误：它应引用
System.Runtime.InteropServices 10（另两类型仍 System.Runtime），已按实际 reference pack
和 facade metadata 修正并新增身份测试；不直接绑定 CoreLib。初版临时目录在并行时碰撞，
以 PID/时间/原子序号独占创建修复；帧测试同时加强末 Type/Overflow/ImportError 检查，
不沿用早先只看帧的假阳性。

实参回调重绑模块函数还暴露原方法调用先求参数、后查属性的错误。91efcfd 的有限修复对实际
PyModule（包括 alias/形参）在参数前捕获属性值，私有 token 在 PIC 缓存前分流。
缺属性先于参数报错；非可调用值仍在参数之后报错。非模块保原接收者/PIC，不按绑定名特判；
Global 动态 callee 显式快照。此为正确性修复，不宣称免费：普通路径增加类型 guard，模块路径
有 token 与参数数组成本；已有精确列表证明才消除捕获，真实 matmul MIR/CIL 的
NumericList Create/ReadCheckedF64/AppendF64 仍命中。固定36热进程成本观测随后完成，见末段负结果。

普通三模块 CP 差分首跑前64行相同，末尾源码 def 的重绑后身份读取仍有旧静态 Func 债。
完整原正期望保留为 `static_identity_pending.py` 及独立 ignored driver；core 仅不包含该末行。
普通实例 descriptor 的提前 lookup 也未在本切修复，不能称通用调用顺序/静态函数重绑全闭合。
新增运行时5专项已通过且零 warning；MIR3项/PE1项结构检查、原 binding driver3项已通过。
完整组合门禁首轮停在新增注册函数8参的 clippy检查，现合并可选绑定上下文为7参，未禁警告；
后续完整组合门禁及成本/三平台 CI 已执行，见末段；不把局部专项当终态。

组合门禁第二轮已过543 C#与228 CP，停在旧MIR/CIL快照；独立原e2e随后202通过/30 ignored。
21份快照只在保存旧版并独立审核后迁移：去新增捕获/全局快照及局部编号差异，重映射
branch/Leave/Switch/EH端点后全部结构相等；with的MIR 7函数/33区域及CIL20方法/36EH均保。
生成全部候选时使用的 INSTA_FORCE_PASS/INSTA_UPDATE 仅用于取证，不计门禁通过；
迁移后正式完整门禁明确移除这两个变量。旧业务源与断言未删除。

成本固定合同见 [MODULE_CALL_COST](../MODULE_CALL_COST.md)，新工作流仅精确工具/fixture路径
自动触发与手动运行，不把共享runtime每次变化当无限追加测量授权。


最终本地正常完整门禁第三轮 exit0：**543 C# / 228固定CP基线 / 202 e2e +30 ignored**；
普通模块driver另1 passed/1静态身份旧债ignored。正式命令无INSTA强制或更新变量，
8项配对工具轻测通过。此为WIP私有声明/有限调用语义验收，公开B部署仍未完成，固定36结果见下文。


91efcfd的固定36成本与原Linux/Windows/macOS/CABI回归已真实完成；[成本证据](../../../docs/validation/aot-module-call-cost/README.md)
保留matmul JIT变慢和模块调用分配翻倍的五个诊断旗，不以CI绿宣称无回归。
当前仍未main推广；下一表示小切先去token，未知callable独立数组不能为跑分快而复用。


## 双值表示候选（本地完整门禁通过，新成本待验）

当前候选以匿名 receiver 快照、独立 Bool 与 Dynamic 目标代替私有 token；没有新增 CFG。
每个调用相对旧 token 最多增加2个局部和2条语句。实际模块 GetAttr 仍先于参数，缺属性与
不可调用值的报错顺序不变；非模块走原 PIC。独立标志不从 None/list 等目标值猜测。
未知 callable 的固定参数数组仍每次新建，避免函数体保留数组或重入时被共享 scratch 覆写。
这只移除 token 表示，不能据此预称分配或吞吐已改善。

局部来源验证采用写入杀事实、合流交集及 EH 空入口，锁 receiver/flag/target 同源且未重写；
生成器入口 reload 后必须重新建立证明，不改变生成器布局。NumericList 保持原工厂支配与
匿名局部唯一读写证明，仅已证明 typed 分支同时省略两个 helper。新增生成器真实差分覆盖
恢复后捕获及参数 yield 暂停期间重绑；原 core 和完整静态身份长尾正期望未修改。

实际专项：运行时8/8、MIR pair5/5、capture3/3、NumericList7/7（后两筛选有重叠）、
PE1/1、真实结构driver2/2、普通/生成器CP差分2/2、私有binding3/3；静态身份旧债仍1 ignored。
首次Rust编译发现切片/未绑定变量、Operand比较及测试限定符拼写问题，机械窄修后通过，
未放宽断言。完整正常门禁第二轮 exit0：546 C# / 228固定CP基线 / 202 e2e +30 ignored，
fmt/clippy/Release均通过；配对工具11项通过。首轮停在预期旧快照，18份迁移独立规范审核
保81处捕获语义、目标/实参、CFG/EH与方法数量，生成候选仅取证，最终命令明确移除
INSTA_FORCE_PASS/INSTA_UPDATE。旧PIC结构断言仅迁移到精确captured入口并保原site快照。
不把旧91ef成本当本候选结果，新36性能组仍未启动。
