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

MIR 对省略任一非 variadic 参数的调用选择当前函数对象与运行时 Binder；完整实参保留
直调。带标注函数的默认值可以随后换成其它 Python 类型，因此现阶段所有已有默认值
函数使用 Dynamic 参数与函数体；同模块函数值的属性访问、别名、传参、返回和容器
暴露也触发对应函数降级，覆盖给原无默认值函数新增默认值的情况。模块字典、注册表
与顶层 locals 等可见反射入口额外使发布函数降级。

## 代价与后续

省略默认值调用增加函数分派和 Binder 成本，带默认值的 typed 数值核心也暂时进入
Dynamic。后续恢复静态能力需要跨模块的封闭性证明，或守卫后的双核心特化；当前先
保留真实 Python 对象和参数语义。尚未测量默认值热点的端到端性能变化。

同一构建的类型模式计数：

| 样本 | Static | Dynamic |
| --- | ---: | ---: |
| `tests/e2e/phase1/m1_1_functions.py` | 3 | 0 |
| `tests/benchmarks/fib.py` | 3 | 0 |
| `tests/benchmarks/calls.py` | 0 | 6 |
| `c2_function_defaults_static.py` | 0 | 6 |

新增定向类型结构测试确认：带默认值 typed 函数 Dynamic，无反射且只直接调用的
无默认值数值 helper 仍为 Static/Int。既有 ty 和 codegen 结构保护均保持原断言。

跨模块的原无默认值 typed 函数仍有已复现缺口：其它模块通过模块属性取得它、设置
字符串默认值后，typed thunk 会抛类型错误。原始用例保留在
`tests/e2e/phase3/c2_function_defaults_cross_module_pending.py`，辅助模块为
`c2_function_defaults_exported_helper.py`。单模块模式分析尚未证明所有跨模块暴露路径。

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
| ty 完整单元测试 | 41 通过 |
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
