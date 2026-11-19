# C2 typed 核心与通用核心方案

- 日期：2026-11-16
- 状态：原始只读设计；首个实现切片已进入 [ADR-0214](adr/0214-generic-default-function-companions.md)，以下保留完整后续方案。
- 前置：[实时默认值候选](cil-function-defaults.md)、[ADR-0208](adr/0208-cil-execution-libpython-kernel.md)
- 目标：跨模块的原无默认值 typed 函数可以新增任意 Python 默认对象，同时保留已证明的 typed 调用
- 实施时机：cbb92535 后已开启首切；准入、冷缺省分派及尚未覆盖边界以 ADR-0214 为准。

## 1. 决定

采用**一个 Python 函数对象、一个 canonical 函数身份、两个构建期 CIL 核心**。
现有 typed MIR 核心继续承接已证明的静态调用；同一份 HIR 通过现有动态降级再构建
通用 MIR 核心。公开函数对象的 thunk 先完成真实参数绑定，再选择核心。

推荐将通用核心作为同一 `Program.functions` arena 中的私有 companion，并用显式
角色字段关联 canonical `FnId`。这样复用现有函数 Pass、调用验证、链接与方法分配。
伴生核心拥有独立局部 arena 和 CFG，Python 元数据及模块状态由 canonical 统一持有。

首切先闭合当前跨模块 pending。已有默认值、类方法与同模块函数值逃逸的 Dynamic
决策继续保留。随后再为这些函数恢复有守卫的 typed 特化，分别验收语义与性能。
这条顺序保留现有验收结果，也把“增加通用能力”和“恢复特化收益”拆成可审查变更。

## 2. 当前真实编译链

`ryupy-driver/src/linked_compile.rs::compile_linked_traced` 冻结源码图和模块环境计划，
逐模块调用 `ryupy-driver/src/lib.rs::compile_module`。每个模块依次执行：

1. `lower_module_with_native_exceptions`：解析结果进入单份 HIR。
2. `ryupy_ty::check_module`：得到 `ModuleTypes` 的模式、签名、局部/表达式类型与模块槽类型。
3. `ryupy_mir::build_program`：在一组 arena 中预分配函数、类、模块槽及函数元数据槽，再建各函数体。
4. `passes::run_pipeline`：函数 Pass 遍历所有 `Program.functions`，每个 Pass 后验证完整 MIR。
5. `trace_product::link` / `link_with_origins`：逐 unit 分配全局 ID 并改写全部引用。
6. codegen 预分配全部 CIL 方法，创建函数 thunk、共享签名与模块访问器，再发射核心方法体。

`dump.rs` 也直接调用类型检查和 MIR builder，应与 build 入口选择同一候选合同。
`trace_product` 有单独编排与准入；首切明确排除已有 trace V1 产品和生成器/协程特化。

双核心生成应位于 HIR/类型到 MIR 的构建阶段，完成后再运行正常 Pass 和链接。
driver 通过默认关闭的 Session 候选选项传入选择，build/dump 共用；CLI 仅显式转发该选项。
driver 记录选择与失败原因，语言语义和核心选择事实归 ty/MIR。
codegen 消费已经明确的核心关系与守卫合同，继续保持只依赖 MIR。

## 3. MIR 表示与 FnId

建议新增中性的角色记录，具体命名在实现时确定：

```text
Ordinary
TypedWithGeneric { generic: FnId, thunk_contract: ... }
GenericCompanion { canonical: FnId }
```

canonical 保持原有 typed 参数、返回类型与 `FnId`；普通静态 `Callee::Fn` 继续指向它。
先预分配所有 canonical ID，再统一分配 companion ID，最后构建两组函数体。现有 HIR
`FuncId -> FnId` 映射始终指向 canonical；另有内部 `FuncId -> generic FnId` 映射。

companion 的 Python 参数槽、局部值与返回值采用 Dynamic；即使原标注为 None，也返回
真实 Python None 对象。它另收 actual `PyFunction` 尾参数，用作帧与定义来源。
`name`、源码位置与限定名描述同一 Python 定义，`codegen_name` 用 canonical ID 与核心角色
生成唯一内部符号，链接时仍加 unit 前缀；Python 名字保持原值。

companion 禁止拥有 `function_global`、`defaults_global`、`signature_global`、模块初始化
入口和第二个私有函数身份锚点。它也不能成为 `Rvalue::MakeFunction`、模块导出或类
方法发布的目标。唯一函数对象始终绑定 canonical thunk。

当前签名缺失还被用于识别模块入口，需要显式更新两处合同：

- `source_provenance.rs` 的 CodeSource 入口/成员分区目前以 `signature_global` 是否为空区分。
- `validate_dynamic_origin.rs` 当前要求带 Dynamic owner 的函数自身拥有 signature。

新验证按角色分区：模块入口、canonical Python 定义、generic companion 分开核验。
companion 必须归属于同 unit 的有效 canonical，来源继承该 canonical，帧必须携带
actual self；禁止循环关联、交叉配对、多个 companion、跨 unit 共享与伪造身份。

`link.rs` 已预先建立完整 `fn_map`，足以重定位两种核心。新增角色记录中的 peer/owner
`FnId` 必须在该映射建成后回填；函数体中的调用仍走 `link_values.rs::remap_rvalue`。
局部变量与 BlockId 留在各核心自己的 arena。新增验证用两个有同名函数的模块，
检查 companion、CodeSource、调用目标和身份锚点全部重定位到正确模块。

相比在 `Function` 内嵌第二份 `Body`，伴生 FnId 的验证边界更清楚，也能直接复用现有
`MirPass::run(&mut Function)`。内嵌 Body 需要改写每个 Pass、程序摘要、验证器、转储
和 codegen 的遍历入口，首切工程面更大。

## 4. 复用动态降级，避免复制整个模块

从 HIR 再降级一次，而非把 typed MIR 的 i64/f64 槽机械改成 object。动态运算、真值、
参数传递、异常与局部未绑定规则已经由现有 builder 实现。

应先提取“分配好共享上下文后，构建一个函数体”的 helper，再对选中的 HIR 函数调用
普通模式和 GenericBody 模式。类、模块槽、签名、导出和 def 执行点只分配一次。
禁止完整 `build_program` 两次再从第二份 Program 拼函数，那会产生两套模块初始化、
默认值求值、类身份与元数据槽，也容易把第二份全局 ID 带入第一份程序。

建议通过统一的 `LoweringTypes` / `CoreTypeView` 查询层覆盖当前函数的类型视图：

- 普通模式读取原 `ModuleTypes`。
- GenericBody 的当前函数模式为 Dynamic；形参、局部、表达式和返回读取 Dynamic。
- HIR 不复制、不修改；兄弟函数的 canonical 映射与共享模块槽物理类型继续使用原表。
- GenericBody 调用策略与结果类型另作显式选择，见下一节。

当前模式查询散布在 `expr_values.rs`、`module_names.rs`、`build_print.rs`、`build_loops.rs`、
`build_dynamic_try.rs`、`build_lexical_class.rs` 与 `dynamic_origin_build.rs`；返回类型又在
`build_context.rs` 和 `build_flow.rs` 直接读签名。实现必须统一这些查询，避免只有参数
变成 object，return/循环/异常分支仍按原 typed 表插入 Unbox。

首切每个 companion 只新增当前函数的动态查询视图和 CFG，避免每个函数复制整个
`ModuleTypes` 或重跑全模块类型检查造成二次方构建成本。

## 5. 通用核心如何调用其它函数

当前 `call_builder.rs::build_user_call` 会依据被调函数的收敛模式和静态签名生成直接调用。
只将当前函数模式改成 Dynamic，仍可能把通用值拆箱后调用兄弟 typed 核心，重现类型拒绝。

首切采用明确的 GenericBody 调用策略：所有 HIR `ExprKind::Func` 用户调用从 canonical
真实函数对象走现有 `call_published_function` / `CallValue`，返回值保持 Dynamic。
它的 thunk 同样拥有通用核心。递归、相互递归和经过别名的调用统一遵守这个规则。

实现时检查以下边界：

- callable 在显式实参求值前冻结；之后只读取该对象的实时默认值，不重新读取名字。
- `*args` / `**kwargs`、重复绑定与错误顺序沿用现有展开和 Binder。
- 通用调用不读取 companion 的函数槽，也不直接指向兄弟 typed 方法。
- Python 全局重绑定通过当前发布对象生效；旧函数别名继续调用原代码与原元数据实例。

后续可以为完整实参且绑定身份已证明的通用到通用调用生成直接 CIL 调用，但需要
显式 generic 目标映射、actual owner 透传与独立结构证明。首切选择现有动态调用链，
使静态性能主要由原 typed 调用路径保留。

## 6. thunk 绑定与核心选择

thunk 保持一份，由 canonical 函数对象持有。顺序是：

1. 对完整位置实参且无关键字的形状，可复用现有无 Binder 快路。
2. 其它形状调用一次 `PyBinder.Bind`，读取 actual self 的实时默认值状态。
3. 保留绑定后的原对象引用，执行无副作用的精确类型守卫。
4. 满足 typed 语义准入合同才拆箱并调用 typed 核心；其它值原样传通用核心与 actual self。
5. 直接返回通用核心的 Python 对象。typed 返回的物化由其明确的返回来源合同处理。

类型匹配不能调用 `PyCast.ToInt64` / `ToFloat64` / `ToBool`：这些入口会接受其它数值类型，
其中还会调用用户或 foreign 协议。守卫使用精确 CLR 盒类型，匹配失败本身没有副作用：

| typed 槽 | 首切匹配 | 进入通用核心的例子 |
| --- | --- | --- |
| I64 | 精确 boxed Int64 | bool、BigInteger、float、str、原生或 foreign 数值子类 |
| F64 | 精确 boxed Double | int、bool、用户 `__float__` 对象 |
| Bool | 精确 boxed Boolean | 其它可真值对象 |
| Str | 精确 CLR String | Python 字符串派生对象 |
| Dynamic / OpaqueStr | 保留原对象 | 按核心语义准入决定 |

现有 `IsBoxedI64` / `IsBoxedF64` 已提供相应 CIL 表示；codegen 可机械发射同类测试。
禁止捕获 typed 执行中的 TypeError/OverflowError 再重跑通用核心：函数体可能已经产生
副作用、消费迭代器或改变异常状态。

### 精确类型匹配之后还需要语义准入

类型匹配并不能解决所有 Python 可观察行为。例如 `def identity(x: int) -> int: return x`
接收大整数盒后，typed 拆箱再装箱会改变原对象身份；浮点参数原样返回也有同样问题。
`x is y`、将参数放进容器、回调参数、返回输入别名都需要保原盒。

因此公开 thunk 的 typed 分支另需 MIR `TypedInvocationContract`。首切可支持纯参数
透传：编译期证明所有正常返回均是同一个参数且没有对象观察/用户副作用，typed 核心
正常完成后返回原绑定对象。未知返回来源先走通用核心。后续再扩纯数值叶、保盒
materialization 及分支来源追踪，逐族验收，保留未准入函数的静态直调性能。

现有 D003 登记了静态 i64 溢出边界。公开动态调用若要达到 CPython 任意精度语义，
还需要在执行前证明数值区间，或直接走通用核心。首切通用能力不能靠执行中溢出后
重新执行来实现。旧静态已证明调用继续遵守已有 D003；改变该合同另切。

## 7. 模块槽、默认值、闭包与函数身份

两核心共享一份 module-var `GlobalId` 映射和 bound flags。通用核心读取 typed 模块槽
时按既有规则装箱；源码 `global` 写入对应原共享槽。双核心生成不改变槽的物理布局，
也不创建第二份模块环境。模块属性 setter 的已有类型边界保持单独登记。

`FunctionDefaults` / def 发布只执行一次，默认值和注解只求值一次；同一 `PyFunction`
保存默认值、注解、任意属性和 module 元数据。thunk 每次绑定查询 actual self，
companion 没有副本或缓存。模块导出、字典、装饰器和旧别名看到同一个对象。

首切范围为 legacy 模块中当前仍为 Static 的普通顶层函数，排除 env/cell、内部嵌套
定义、实例模块、生成器、协程、类定义与 trace V1 产品。当前这些复杂函数大多已经
Dynamic，可继续使用既有唯一核心。

后续扩展到闭包时，thunk 从 actual self 读取同一 Env cell 数组，两核心共享 cell 身份，
每次调用自己的局部槽独立。实例模块还必须从同一 `PyFunctionOwner` 传入真实 context，
不能用 TLS、公开模块名或静态镜像推断环境。新角色的隐藏参数顺序显式验证为
Python 参数、env、context、actual self。

帧在被选中的核心内只压一次。generic 用 actual self 的定义来源；typed 使用原有
源码签名载体。thunk 不新增 Python 帧，helper 的 CLR 名字不进入 Python traceback。
recursion limit、异常 cause/context 和 finally 清理必须按实际调用次数验收。

### typed 直调的绑定身份

双核心不会使模块函数重绑定自动安全。对能证明目标身份稳定的直接调用，保留 typed
直调。无法证明稳定时，先冻结实际 callable，再求实参，最后比较它与 def 成功发布的
私有身份锚点；命中进入 typed 调用，否则调用已冻结的真实对象。参数副作用导致
公开名字变化也不能重新选择目标。

现有 `specialization_identity_global` 已承担特化身份锚点合同，可复用其规范扩展，
由 MIR 生成并验证发布事实。元数据默认值更新保持函数身份，完整实参 typed 调用
无需因此失效；省略参数继续走真实 Binder。

## 8. 与全项目函数逃逸证明的比较

全项目证明可作为后续体积优化，当前不适合作为通用能力的唯一前提。源码图已冻结，
但函数可能经模块属性、sys.modules、函数字典、类字典、动态 getattr、闭包/回调、
foreign 消费者或原生扩展获得。仅扫描显式 `FuncId` 引用和 import 名单覆盖不全。

有界的闭包证明可以确认：模块未导出目标、没有反射取得路径、没有 callable 逃逸、
没有动态名称构造、没有 foreign/native 回调入口且没有运行期重新导入能力。需要驱动
冻结的完整闭包事实及可验证的 MIR 调用合同，未知观察边归入“可能暴露”。它适合
省掉已证明私有 helper 的 companion 或身份守卫。

对可能暴露的函数仍生成双核心。这样无需将全部导出函数改为 Dynamic，也无需等待
全生态逃逸分析才能修复当前跨模块默认值问题。

## 9. 体积、构建与运行成本

为 K 个原 Static 函数新增 K 个通用方法；每个逻辑定义仍只有一个 signature、一个
thunk 和一个函数对象。增加的是通用 CFG/IL、守卫、必要的原对象返回信息及链接关系。
产物报告增加显式 core role 与 canonical ID，分别列 typed/通用实际 MethodIndex，
继续从实际发射结果取数据。通用操作会引入额外 runtime helper 可达性，NativeAOT 原生体积增长可能超过原方法
字节增量，必须实测。包含模块、类和默认值的初始化成本保持一次。

构建成本约增加所选函数的第二次 lowering、各 Pass 和验证工作；不承诺固定倍数。
运行期函数对象与默认值数量保持原值，绑定后的守卫增加分支；完整位置调用继续可
复用原形状快路。通用核心内部经 thunk 调用还有数组/PIC成本，后续单列优化。

应记录 typed/通用方法个数、实际 CIL 方法体字节、PE 大小、NativeAOT dist 大小、
编译时间和峰值内存；输出写仓库外，普通产物不使用摘要校验。性能对比至少包含
直接数值 helper、默认值热点、函数别名调用、跨模块调用、异型默认值及原先的不利样本。

## 10. 最窄可编译首切与文件清单

首切交付一条显式候选路径：当前 `cross_module_pending` 的无默认值顶层 typed 函数
拥有伴生通用核心；模块导出仍是原对象。字符串、list、bool、超 i64 整数通过同一
函数对象正确执行；同模块已证明的完整数值实参仍可到 typed 方法。

实现应拆成以下完整变更包。下面是预计必要文件，新增模块用于遵守 600 行限制：

| 变更包 | 主要文件 |
| --- | --- |
| 候选编排与 build/dump 一致 | `ryupy-driver/src/session.rs`、`lib.rs`、`dump.rs`，`ryupy-cli/src/args.rs`、`main.rs` |
| 显式核心角色、配对和准入 | `ryupy-mir/src/mir.rs`、新增 `function_cores.rs`、`lib.rs` |
| 从同一 HIR 构建单函数两次 | `ryupy-mir/src/build.rs`、新增 `build_function.rs` / `core_types.rs`、`build_context.rs`、`build_params.rs` |
| 统一当前函数动态视图 | `expr_builder.rs`、`expr_values.rs`、`build_flow.rs`、`build_loops.rs`、`build_print.rs`、`module_names.rs`、`build_dynamic_try.rs`、`build_lexical_class.rs`、`dynamic_origin_build.rs` |
| 通用调用与身份守卫 | `call_builder.rs`、`call_function_origin.rs`，新增 `core_calls.rs`；首切保持省略参数 Binder 规则 |
| 来源/角色/调用验证 | `source_provenance.rs`、`validate_dynamic_origin.rs`、`validate.rs`，新增 `validate_function_cores.rs` |
| 链接、转储与结构遍历 | `link.rs`、`link_values.rs`、`pretty.rs`；新增角色中的 FnId 统一映射 |
| CIL 方法与 thunk 分支 | `ryupy-codegen-cil/src/assembly.rs`、`callables.rs`，新增 `generic_core_thunks.rs`；必要的无副作用守卫引用 |
| 实际产物可追踪记录 | `ryupy-codegen-cil/src/emission_report.rs` 与相应 PE 结构测试 |
| 验证与文档 | 新增 MIR/ty/driver/codegen 定向结构测试、原跨模块 e2e、本文及 C2 实施文档 |

`Function` 新字段会要求其它源码结构测试的手写构造补齐显式 Ordinary 角色；这是
机械迁移，旧断言继续保留。源 provenance 与 fixture 构造不能靠可删的名称标记授权。

ty 首切可以沿用当前收敛结果，只为仍为 Static 的目标生成通用核心；不需要删除当前
`default_reflection_modes` 的保护。恢复带默认值函数的 typed 性能时，再单列独立的
“Python 通用签名”和“带前置条件的特化类型事实”，避免把注解重新变成参数限制。

## 11. 准入门

1. 先保留原跨模块 pending，新增其它异型对象、别名、默认值删除与关键字默认值场景。
2. 单函数结构证明：typed 原参数/运算保留，generic 全 Python 值槽为 Dynamic；通用内部调用不拆箱到兄弟 typed 核心。
3. 构造破坏角色关系、错模块 FnId、伪发布 companion、第二份签名/默认值、错 actual self 的负例。
4. 对实参/默认值副作用、重复绑定错误、异常和 finally 清理证明每次只发生一次。
5. 身份差分覆盖大整数、NaN、同一输入重复传入、输入装入容器、透传返回与可变默认对象。
6. CPython 3.9.25/3.12.13/3.13.3 当次差分，3.9–3.15 产品版本门保持原范围并逐版本验收。
7. JIT、NativeAOT、删除输入搬迁、onefile、平台和真实消费者分别记录；源码及发行归属继续由 C0 审计。
8. 原 typed PE/函数模式/运算保护、默认值热点与全门通过后，另作候选晋升决定。

本文所有新增角色、选项和接口均为拟议设计。当前源码与已通过反射测试保持本轮开始时状态。
