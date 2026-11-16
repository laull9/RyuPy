# C2 函数默认值元数据首切

- 日期：2026-11-16
- 对应：[ADR-0208](adr/0208-cil-execution-libpython-kernel.md)、[C2 路线](cil-execution-roadmap.md#c2函数元数据动态对象与反射)
- 状态：原生默认值反射与真实参数绑定候选；高级反射整体待办
- 产品版本范围维持 Python 3.9–3.15；本切同次 oracle 为 3.9.25、3.12.13、3.13.3

## 真实行为

`PyFunction.__defaults__` 返回位置默认值的真实 tuple 或 None，`__kwdefaults__` 返回
仅关键字默认值的真实 dict 或 None。重复读取保留容器身份；赋值保留所传容器身份；
None、空 tuple/dict 与删除保持各自的可观察状态。删除可重复执行，非法类型赋值先报
TypeError，旧状态继续有效。`__dict__` 中写入同名键不会覆盖这些真实成员。

默认值仍在 def 定义位置求值；后续调用从当前函数实例读取。可变默认对象继续共享，
关键字默认字典的原地增删、覆盖及其它别名写入在下一次调用生效。超长位置默认 tuple
按 CPython 从右侧匹配参数。`functools.wraps` 保留自己的默认值，转发调用消费被包装
函数的当前默认值；注解字典继续使用已有实时身份合同。

实现使用冷弱表记录状态。第一次读取或写入默认值元数据时，为该函数建立专属默认值
数组键；共享空数组和共享签名的函数保持实例隔离。函数实例字段布局保持现有形状。
Binder 仅在真正省略的参数槽查当前 tuple/dict，不缓存字典值，也不修改共享签名。

完整实参保留原直调。缺省调用先冻结公开callee，再按源码顺序求值并冻结每个实参。
对legacy普通顶层Dynamic函数、总位置形参1–3个且缺失尾参均有定义默认值的纯位置
调用，实参结束后调用`FunctionDefaultsUnobserved`：实际callee须与对应MakeFunction
发布的私有锚点引用相同，且Defaults没有冷状态。成功臂才读取此callee的当前原数组，
保默认对象身份并直调原Fn，actual owner仍是此callee。失败臂用同一callee与实参对象
调用Binder，两臂汇入一个结果；代码不重读名字、不重新求实参。

只读默认元数据也使守卫失败；相同签名或相同Body的其它函数对象均不能通过身份守卫。
嵌套Env、实例模块、生成器/协程、kwonly/variadic和宽签名继续运行时绑定。带标注函数的默认值可以随后换成其它 Python 类型，因此现阶段所有已有默认值
函数使用 Dynamic 参数与函数体；同模块函数值的属性访问、别名、传参、返回、容器
暴露及类方法发布也触发对应函数降级，覆盖给原无默认值函数新增默认值的情况。模块字典、注册表
与顶层 locals 等可见反射入口额外使有Python参数的发布函数降级。零参函数保持原模式，
默认值无法为它新增形参，已有Static帧与ABI保护继续有效。

## 代价与后续

已观察默认值的调用继续承担Binder成本；未观察的窄位置调用增加身份/冷状态守卫，
避免原函数分派和绑定数组分配。带默认值的typed数值核心也暂时进入Dynamic。后续恢复静态能力需要跨模块的封闭性证明，或守卫后的双核心特化；当前先
保留真实Python对象和参数语义。

初版与冻结5d36基准的26项完整配对功能全部通过，calls墙钟93.877→115.866ms
（+23.42%），分配372078056→420428832字节；attrs分配增加4.8MB，全量墙钟
几何平均增加1.74%。这些初版回退触发本次守卫快路，守卫修复后的完整配对已完成，calls 的 48 MB 额外分配消除，墙钟本轮 -1.59%；
所有单项变慢与整体 +0.55% 结果见[首轮集成验收](cil-execution-first-slices-validation.md)。

同一构建的类型模式计数：

| 样本 | Static | Dynamic |
| --- | ---: | ---: |
| `tests/e2e/phase1/m1_1_functions.py` | 3 | 0 |
| `tests/benchmarks/fib.py` | 3 | 0 |
| `tests/benchmarks/calls.py` | 0 | 6 |
| `c2_function_defaults_static.py` | 0 | 6 |

新增定向类型结构测试确认：带默认值 typed 函数 Dynamic，无反射且只直接调用的
无默认值数值 helper 仍为 Static/Int。既有 ty 和 codegen 结构保护均保持原断言。

跨模块原无默认值typed函数新增异型默认值的原始缺口由[ADR-0214](adr/0214-generic-default-function-companions.md)
闭合：冻结多源码图中的可发布无env/状态机Static参数函数拥有一份通用伴生体。
canonical继续持唯一函数对象/签名/defaults，Binder成功且实际补齐缺省参数时才使用通用体；
完整供参typed方法和完整位置thunk保留原ABI/CIL。通用体内对兄弟/递归函数按真实函数
弱键显式分派，无签名/名字猜测、共享签名变异或环境开关。原用例转正为
`tests/e2e/phase3/c2_function_defaults_cross_module_pending.py`。

该首切不扩大公开完整供参的typed边界；单源码图经未知外部扩展反射取得函数的完整边界、
恢复已有Dynamic函数的typed特化另切。通用体只复制准入的函数CFG，模块槽、类和def
求值仍共享，未用伴生的单模块产品不初始化冷CWT。

`inspect.signature`、`__signature__` 的真实 Signature/Parameter 消费、装饰器解包、
code/frame/traceback 和 FastAPI/Pydantic 消费者待后续切片。Runtime 当前没有原生
inspect 模块；`__code__` 仍明确报未实现。字典派生布局的 `__kwdefaults__` 存储明确
报未实现。Python 3.14+ 延迟注解另立合同。本切未修改 CPython 桥或加入 Python 胶水。

## 验证记录

macOS arm64 当次验证：

| 门 | 结果 |
| --- | --- |
| 三条新增用例 × CPython 3.9.25/3.12.13/3.13.3 对原生 JIT | 9 项 stdout 与退出一致，三版本之间无本切差异 |
| 三条新增用例对固定 CPython 3.12.13 的原生 NativeAOT | stdout 与退出一致 |
| Runtime 完整 C# 测试 | 3352 通过，3 项既有跳过 |
| MIR 完整单元测试 | 423 通过，4 项既有跳过 |
| ty 完整单元测试 | 42 通过 |
| codegen 完整单元测试 | 70 通过，3 项既有跳过 |

三条用例是 `c2_function_defaults_dynamic.py`、`c2_function_defaults_static.py`、
`c2_function_defaults_typed_changes.py`。所有 Python 预期当次由指定解释器运行生成，
JIT/NativeAOT 输出使用仓库外 TemporaryDirectory 并清理。NativeAOT本次验证产物执行，
删除输入搬迁、onefile、Linux/Windows与剩余 Python 版本保留后续平台门。

Runtime 完整命令：

```sh
PYTHON="$(uv python find 3.12.13)" dotnet test runtime/RyuPy.Runtime.Tests -c Release --no-build --nologo -v q
RUSTC_WRAPPER= cargo test -p ryupy-mir --lib
RUSTC_WRAPPER= cargo test -p ryupy-ty --lib
RUSTC_WRAPPER= cargo test -p ryupy-codegen-cil --lib
```

首次未提供 PYTHON 的 Runtime 全套有两项 TextWrap oracle 环境失败；指定绝对路径后
重跑完整套件得到上述结果。完整 `make check` 由集成门统一执行。


## 未观察默认值守卫的定向门（2026-11-16）

新增`c2_function_defaults_guarded.py`先pending、差分成功后转正，覆盖零提供参数、
可变默认对象身份、实参中只读/替换默认值、公开函数重绑与嵌套Env独立实例。
Runtime守卫另验证共享签名但不同Body、同Body重建def对象都失败。
`function_origin_structure`现锁callee先于实参、守卫晚于实参、私有source锚点、成功臂
唯一默认数组读取、原Fn尾owner、失败臂冻结对象/参数与共同结果；原Static ABI/CIL、
alias和MakeFunction发布断言保留。宽/kwonly/variadic/Env/实例形状明确保绑定路径。

当次定向结果：原四条默认值用例CPython3.12.13/JIT一致；新增守卫用例还对照
3.9.25/3.12.13/3.13.3并在macOS arm64 NativeAOT执行。MIR全套425通过/4既有跳过，
codegen全套70通过/3既有跳过，Runtime全套3366通过/3既有跳过，driver来源/普通动态/
资源保护三套11项通过。最终产品、性能和完整门由主集成重新冻结验收。

## ADR-0214伴生核心验证

新增跨模块正例覆盖None/bool/float/str/dict/tuple/大整数原对象返回、可变默认多次返回与
容器别名、元数据读写身份、兄弟/递归转调、同名不同函数体重定位、实参期间默认变更与
callee重绑、体副作用一次、重复绑定和删除后缺参错误。固定CPython 3.9.25、3.12.13、
3.13.3同次oracle均与JIT一致；macOS arm64 NativeAOT搬迁后删除输入和build仍逐字一致。
新增MIR配对/来源/独立发布负门3项、Runtime冷注册/默认值合并保护7项通过；真实CIL门
逐段对照typed方法和完整实参thunk前缀相同，私有通用核心及唯一PyFunction构造正向存在。
codegen全库70通过、3个旧忽略；最终全门和配对性能由主代理在集成冻结后验收。

最后冷包装审计补回未登记`PyBoundMethod`的原`InvokeBound`上下文；仅实际已登记的
plain函数目标使用通用入口，注册函数仍接收同一owner。新增独立Runtime正例验证普通
UsesSuper方法上下文及接收者不变、注册目标身份不变，3项通用核心门通过。

普通`@dataclass`构造的反射回归另有两模块正例：其make_box收敛Dynamic、无typed伴生体，
结构门锁运行时类调用与单一核心。固定CP3.12.13、JIT、删除输入/build后的搬迁NativeAOT
均通过，保可变默认别名及原类身份；见ADR-0214异构审查反证记录。

伴生体中的range循环使用既有原生range值及通用迭代协议，避免旧Dynamic i64计数优化
向通用参数插入Unbox；typed及普通Dynamic计数路径保持。新两模块range正例通过三版本
CP oracle、JIT和删除输入/build后的搬迁NativeAOT；driver foreach整组6门通过，见ADR-0214。
