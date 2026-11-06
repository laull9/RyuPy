# 模块裸名删除的完整合同

当前共15份完整源。01/02/03/06/08/09/10/11 已在候选002真实PE/JIT中退出0、
stderr为空、stdout逐字等固定CP golden，因此解除pending；其余7份保留完整源和pending。
最终CLI又在cases-final-001重编全部15源，8通过/7边界不变；正常完整门禁也已通过。
独立生命周期与性能验收另外登记，不能由此推断公开loader或项目通过。

准备阶段原11例全部pending，固定CP3.12.13双轮通过；后来09/10与11分别有独立双轮
CP窗口。旧CLI的13份首轮红灯和候选001的7通过/6边界均保留，候选002再验全部14份。
实际失败：04嵌套tuple del E0100、05函数内class E0100、07默认__dict__运行AttributeError，
另3个初始boundary明确拒绝。第15份boundary_deleted_exception_base逐字保存pe-001原组合源：
同模块可删ValueError又作用户异常基类，真实PE在类定义处明确NI；此前global scopes已通过，
后续异常目标/实例检查未执行。其golden来自先行完整CP原流，正常CP基线会再验新清单。
没有裁剪源、改golden或拿新增窄例替代原失败。

## 首批完整 CP 候选（不承诺全部属于本切支持域）

| 目录 | 完整源覆盖 |
| --- | --- |
| `01_module_binding` | 标注整数全局删除、函数后读 NameError、同类型重绑、None 是有效绑定 |
| `02_global_statement` | 函数 `global del`、重复删除、未曾赋值名字、错误后副作用停止、全局多目标部分生效 |
| `03_builtin_fallback` | `len` / `range` 遮蔽删除后回退、标注数值不能锁死类型、builtin 本身不能由裸名 del 删除 |
| `04_multiple_targets` | 从左到右、多目标中途失败不回滚、重复目标、builtin 缺绑定、嵌套目标列表 |
| `05_saved_definitions` | 已保存函数/类保身份，原名字调用和基类求值失效，callee 在实参前报错，重绑生效 |
| `06_import_aliases` | 顶层清除 import 临时名字，原模块/函数别名保持身份，重导入恢复相同对象 |
| `07_external_visibility` | 同一模块对象外部属性和字典可见性，旧 from import 引用保留，新 from import 失败及恢复 |
| `08_instance_dictionary` | 显式实例模块真实删键、外部注入名字删除、旧值保留、重绑、builtin 回退和重复删除失败 |

首批完整候选中的 `04_multiple_targets` 包含嵌套 tuple 删除，
`05_saved_definitions` 包含函数内 class，`07_external_visibility` 包含默认模块 `__dict__`；
这些是本切之外的额外边界。保留原源所有功能和原 golden，不以本切实现保证它们全部转正。
尤其不能为默认模块虚构真实字典或镜像，不能为通过本例放开函数内 class 或嵌套删除守卫。

前 7 例不选择实例模块。第 8 例仅有 `instance_module = subject`，调用方入口捕获异常；
subject 源内没有 `try` / `except`，不要求松开 B2 的其它 AST 守卫。目录里的所有辅助模块
是入口实际导入的完整依赖，不另算独立用例，也不使用假 loader 或测试名字特判。

## 新增独立窄例（CP与候选002实际PE已验证）

- `09_module_definition_phases`：全部 class 语句位于模块作用域；保存函数/类 alias，
  删除后的调用目标与构造目标先于实参失败、基类先于类体失败，之后重绑和旧对象身份。
- `10_external_attributes`：默认模块在同一观察者中属性读取和新 from import 看到删除，
  保存的 from import 引用不变，外部赋值恢复；不读取或宣称支持默认模块 `__dict__`。

这两份是单独命名、完整的新程序，不是对04/05/07裁剪尾部后宣称原源通过。
最初保持pending，后来真实PE通过才转正。新证据目录 `cp-design-002` 保存事先写好的 stdout 提案，之后获准只对
这两份新程序各运行双轮，共 4 进程；全部退出 0、stderr 为空，双轮 stdout 逐字节一致且
均与提案相同，才把原样输出写入各自 `stdout.txt`。执行前后输入哈希不变，未失败重跑。
首次 `cp-design-001` 及原 11 份源和 golden 均经 SHA256 复核完全不变；不是对首次窗口补采。
该CP窗口本身没有运行CLI/JIT；实际PE证据在独立cases-candidate-002，不据CP成功冒称实现。

11_range_binding是独审发现文本range快路旁路后新增的完整独立源。模块和函数内for在
删除前遮蔽、删除后builtin回退、函数重绑、global删除与再次lambda重绑时均读当前绑定，
并检查旧alias和自定义调用次数。cp-design-003只对该源双轮，实际PE整份通过；原13源不动。

## 保留的完整边界

- `boundary_cell_nonlocal`：共享 cell 的 nonlocal 删除、空 free cell 的 NameError、
  空本地 cell 的 UnboundLocalError、同一 cell 恢复。不能用模块删除实现冒称完成。
- `boundary_class_namespace`：class 读名字可回退模块，全局存在不意味着 class namespace
  的 del 能成功；类体错误相位和原全局值不变。
- `boundary_static_custom_builtins`：默认静态模块自定义 `__builtins__`，模块已有 frame 与
  后定义函数的 builtin 归属不同，字典后续修改可见。没有实例模块选项，仍单独 pending。

实现未覆盖的边界应保留明确诊断及完整源，不删除尾部断言换取通过。

## 固定 CP 事实和 golden

2026-11-06，在基线 HEAD `7bea9816bb5ad1800e9f711fd26a6ba60c20b021` 上，
使用 `/workspace/shared/ryupy-module-owner-validation/env.sh` 的 `$PYTHON`，确认版本
3.12.13。一次命令完成两轮，每轮全部 11 个入口加 16 个事实探针所在的一个进程：
24 个进程全部退出 0、运行 stderr 为空，两轮对应 stdout 逐字节相同。
每轮总预算 30 秒，外层上限 120 秒，实际约 0.17 秒/轮，无失败修期望重跑。
各目录 `stdout.txt` 为该次原样 stdout，没有覆盖任何已有 baseline。

新证据位于 `/workspace/shared/ryupy-module-delete-validation/cp-design-001`：
`results.json` 包含命令、退出码、耗时及输出哈希；`round-1` / `round-2` 保存原输出；
`facts.py` 和 `round-1/facts.stdout` 保存完整事实源码、异常属性、错误字节码相位与可见状态。
这些是项目工程证据，不属于 RyuPy 通过记录。

关键事实：

1. 模块 `DELETE_NAME` 和函数 `DELETE_GLOBAL` 都只删除对应 namespace 的已有键。
   缺失时为 `NameError("name 'x' is not defined")`，`error.name == 'x'`。
   即使 builtin 中有同名键也不删除它。删除后读才执行 builtin 回退。
2. 函数没有 `global` / `nonlocal` 的 `del x` 使名字属于局部，未绑定时是
   `UnboundLocalError("cannot access local variable 'x' where it is not associated with a value")`，
   `error.name is None`；模块已有 x 不受影响。本次实现不能误降级成 global 删除。
3. `del a, missing, c` 先删 a，在 missing 报错，c 原样保留；重复目标同理。
   该规则同时适用于模块与函数 global。None 是已有绑定，不能充当未绑定哨兵。
4. 已删除函数名的查找先于调用实参求值，已删除基类名的查找先于类体执行。
   不能让旧静态直调或旧 ClassId 绕过这个错误相位。保存的函数/类/import 对象仍保持身份。
5. 删除遮蔽 builtin 的整数、函数或类后，读恢复真正 builtin。只有仍可证明安全的普通
   typed global 可保静态槽位；可回退 builtin 的被删名字不能锁死为原数值/函数/类类型。
6. `del` 不移除对应 `__annotations__` 记录。这个事实只由额外 CP 探针记录，
   不宣称新增 B2 annotation 支持，也不要求本次扩大运行时注解范围。
7. 模块外部读缺失为 `AttributeError("module 'subject' has no attribute 'payload'")`；
   新 from import 为 ImportError，其路径相关尾部不纳入 stdout golden。已有 from import
   保存的对象不受删除或后来重绑影响。

## 真实PE验收合同

- 对完整候选按实际支持范围逐份编译真实PE；已在候选002完成，额外边界原源继续pending。
  独立窄例获得 CP 验证后也应整份运行，逐项对照 stdout、退出码、stderr、异常消息和副作用。
  不能用独立新例的通过替代或宣称原完整候选通过。
- 默认模块保留可证明的普通数值全局类型与未绑定检查；删除要清除实际绑定状态。
  01虽然有int标注，但实际main动态消费会沿旧规则把全局降为object，不据此称typed PE通过。
  纯typed真实PE结构与执行另验，不能用标注或MIR单测代替实际CIL。
  builtin 同名、函数和类删除时必须检查所有读取、直接调用、构造、基类和循环优化入口。
  本目录不是性能证据；IR/IL 类型和实际 .NET 吞吐应由父级单独核验。
- 使用真实编译后的实例执行入口建立 A/B 两份不同 globals。调用 A 的已保存删除函数，
  必须只删 A 的键；B 及两者共享的 builtin 字典不变。检查函数 `__globals__` 与宿主真实
  字典身份，不允许以假 namespace、假 loader 或 CPython `exec` 代替生成 PE。
- 实例直接执行重复初始化时，同一字典的旧键删除和重新赋值可见；旧函数仍归属原字典，
  已保存旧容器不被新初始化覆盖。中途删除失败只保留已发生效果，不自动回滚、换字典或
  重新执行另一份实例。该项与普通 import 失败重试创建新模块的既有规则分开。
- 多目标删除中途失败、缺失 builtin 名、仅外部注入的名字、保存函数和类的身份、跨模块
  对同一对象的观察，应在真实 PE 宿主中复核。边界源仍需明确拒绝，不能被宽泛 AST 放行。

## 现有框架限制与提议

现有 Python 清单解析器不接受 `timeout` 或 `expect_stdout`，尽管上层 README 提到了它们。
本目录只用 `entry`、`pending` 和既有 `instance_module`；外部 CP runner 负责总时间上限。
未修改 parser、全局 baseline 或已有清单。

普通目录 e2e 每例只有一次入口执行，不能表达同一真实 PE 的双实例与直接重复执行。
提议父级沿用既有生成 PE 的 C# 宿主验收，保留独立命令、完整源、PE/runtime 哈希和原日志；
若未来把这种生命周期纳入普通清单，应先扩展并测试双方 runner，而不是私加未知字段。
