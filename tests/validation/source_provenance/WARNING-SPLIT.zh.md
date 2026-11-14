# warning 模块名观察的能力分域

原 `fixtures/warning_names/` 组合不支持，不能把它当正例跳过或声称修复完成。两个文件及原完整CP观察保持不变：entry SHA `0ff4f380120ca831a157195b93ff41d1fb4e209886687aa192ec69f94e5f8052`，subject SHA `ebeed0d0e1ef7c5414ddacc8ab8f6e7f108907a6741ab857b8d7bec7be0e6412`；原394字节stdout SHA `ea9fe57d634209e66e17f0ce674d765e166a2a67275a20996f8397ec7509b04f`。原始身份清单、旧债及其它fixture不变。

## 两个独立正向输入

- A `fixtures/warning_names_legacy/`：默认Legacy。只从原subject移除remove_name定义，并从原entry移除name-missing观察块；相邻分隔空行随块移除，其它源字符及全部四类、构造与str回调顺序不变。
- B `fixtures/warning_name_missing/`：显式 `--instance-module subject`。subject只有events、原issue和原remove_name；entry逐字保留原name-missing块，直接使用内建DeprecationWarning/UserWarning。两文件无ClassDef，不以另一个异常子类代替NameString。

`warning-split.json` 是执行前冻结的小合同，包含两输入源码SHA、原完整CP输出、新输入预期及以下映射。它的待执行状态描述的是冻结时点。固定CP3.12.13（可执行文件SHA `021044895e95be79dc2f110367607e684119afbc8ce75f6f0eec94844e0acec7`）随后对每个输入仅运行一次 `-B -S entry.py`，自然exit0、stderr空、完整stdout与预期逐字相同；A stdout SHA `6c397379bda6564ee49a66f0e5a7c876ad93c2a3727a6437dd48cd17796b3e1c`，B stdout SHA `f46856310cd09d6ce59b168278ec5ab0d64b50f40e7c7158a4275fa8e242611f`。这两次只为新输入取oracle，不替换原组合oracle。

## 原观察逐项映射

1. `none-suppression 0 []` → A：None先抑制消息构造和已有Warning.str。
2. `none-category TypeError []` → A：类别校验先于None抑制。
3. `name-default object 0 []`、`name-always object 1 []` → A：普通name对象不调用str/bool/eq。
4. `name-default integer 0 []`、`name-always integer 1 []` → A：整数name按<string>。
5. `name-default list 0 []`、`name-always list 1 []` → A：list name按<string>。
6. `name-default string-subclass 1 []`、`name-always string-subclass 2 []` → A：读取原NameString不可变payload，不调用override。
7. `name-missing 1 []` → B：删除真实模块name，default抑制、always累计一条，events为空。
8. `context-before-message constructor 1 ['constructor']` → A：构造恰一次，过滤使用构造前模块名。
9. `context-before-message existing-str 1 ['warning-str']` → A：已有Warning.str恰一次，过滤使用str前模块名。
10. `WN done` → A和B各自结束标记，不证明同一输入共存。
11. 四个class与global删除同模块共存 → 原组合两个明确负例；不能从A+B推导支持此组合。

## 实际构造链与不能组合的边界

NameObject是普通PyClass/PyInstance；NameString的Legacy HIR走runtime_bases::candidate/ClassBase.Runtime，真实str经PyRuntimeBases.Validate的精确str分支，再MakeClassCore设置HasStringLayout。方法按原顺序发布后终结；CallValue→PyClass.Invoke→PyStringLayout.Construct→独立不可变PyStringInstance.Value。warnings通过TryGet读取payload，不执行name的用户协议。

两个DeprecationWarning派生在Legacy是编译异常类，具有真实CLR异常工厂。MakeClass携工厂，Invoke先创建并绑定PythonClass，再调用存在的自定义__init__；Stringified的已有异常经PyRepr调用其原__str__。warnings先读取所选帧的活name，再执行这些回调，副作用顺序和次数均由A保留。

Instance的ClassBase.Runtime首先经过通用基类守卫。str在该守卫放行，但InstanceMakeClass→PyModuleInstances.CreateClass的普通基类校验拒绝它。后面的两个builtin DeprecationWarning基类也不在Instance普通类域，且会在更早的通用守卫拒绝；原运行没有到达它们，不能称已执行。B完全没有此class路径，但仍需真实PE/JIT证据。

源码参考：`crates/ryupy-hir/src/{class,runtime_bases,class_bases,module_deletions}.rs`，`crates/ryupy-mir/src/build_class.rs`，`crates/ryupy-codegen-cil/src/{assembly,literals,stackify_runtime}.rs`，`runtime/RyuPy.Runtime/{PyClassOps,PyClasses,PyCallOps,PyStringLayout,PyInstanceClass,PyRuntimeBases,PyWarnings,PyRepr,PyReprObjects}.cs`。

## 原组合的两种必需负例

`contract.json` 将原组合放入独立 `boundary_cases`，正向 `cases` 不再包含它。

- Legacy：实际build自然exit1、stdout空，最后stage必须是subject的HIR开始。仅投影诊断前符合固定格式的stage计时行与唯一已知subject绝对路径，其余完整E0100诊断须逐字匹配“默认静态模块暂不支持删除__name__的初始绑定与元数据协议”及原1–40行展示。不得存在生成DLL，不运行JIT。
- 显式Instance：实际build正常成功，先核图、模式、产物/Runtime身份和固定PE来源链，才运行JIT。仅接受自然exit1、空stdout和精确一行 `NotImplementedError: instance classes currently require an ordinary user class or object base`。任何其它错误、stdout、超时、启动失败或崩溃都硬失败。

`ci/warning_split.py` 只负责上述小合同和精确拒绝，沿用原Recorder、gate、bootstrap和固定PE读取器；不是新验证框架。固定PE表A/B的函数定义、实际行号与模式分别从新AST登记。原组合Instance仍使用原PE表，Legacy拒绝无PE。原历史运行42656097144/42660971300及完整原流SHA留在冻结合同，不能冒充当前候选的结果。

## 下一hosted窗口预算与未执行范围

- CP：11个正向输入 + 1个原组合完整控制 + 1个host控制 = 13。
- 新build：13个正向臂 + 2个原组合负例build尝试 + 1个host = 16。负例中只有Instance产生PE，Legacy必须编译拒绝。
- 新JIT：13个正向臂 + 1个Instance负例 + 1个host = 15；Legacy为0。
- 旧host对照另外1build/1JIT；旧Runtime与Session构建预算保持原合同。
- Native独立作业保持既有native_consumers的1CP/1publish/3run，不把它当A/B自身Native覆盖。本次未另加A/B Native发布预算。

本次只新增2次CP与纯Python文本/AST/合成验证器自测。没有本地新Rust/CLI/runtime构建、实际PE、JIT、Native或工具安装。A/B在当前候选的产品结果均待运行，原组合完整支持仍为否；不改变固定项目兼容率，不作性能结论。
