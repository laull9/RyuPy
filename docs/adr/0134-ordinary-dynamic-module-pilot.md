# ADR-0134：普通动态源码模块的隔离环境试点

日期：2026-11-06。状态：本地S0/S1候选，默认晋升未获准；验收结果见独立实施报告。

## 决定与范围

默认关闭的Cargo feature `ryupy-driver/ordinary-dynamic-module-pilot`只改变编译会话的默认策略。
没有新增公开CLI运行模式。显式`--instance-module`继续映射同一InstanceGlobals；发行构建
不启用feature时仍是ExplicitOnly。取消隔离须另过产品、AOT、性能门，不允许把临时开关当
永久双语义保证。

HIR提供语法充分条件，复用完整实例AST预检。普通非包、非入口的实际源码图模块，必须至少
有一个def，全文没有class，每一个def都缺完整签名或含变长参数。嵌套及不可达def同样扫描。
完整签名闭包/生成器即使类型层会动态化也保守不选；不复制initial_mode，不让HIR依赖ty。
类型层单测用真正initial_mode交叉验证充分条件。lambda和genexpr维持已有动态规则。

except、相对导入、注解赋值、急切推导式与环境名字等边界沿用原实例预检。资格不符只保留
legacy计划，不向Session注入该预检诊断。无class/except也切断静态异常handler/基类身份
保护的自动消费者；自动计划仍有protected_exports属于编译器不变量错误，该断言不得覆盖
旧显式实例的类/native-base边界。没有项目名或模块名白名单。

AOT与固定原生绑定编排不自动迁移。显式实例+AOT原拒绝不删除。本包没有实现或验证AOT
模块环境迁移，不声称AOT因JIT通过即可交付。

## 层次与权威

模块图加载后一次形成计划，最终HIR名字解析前选定；build、dump和link消费同一计划函数。
dump的单文件是入口角色，不能把它误称build图内依赖模块。MIR及实际codegen发射报告保留
选择原因；普通JIT没有交付manifest，Session审计报告独立记录实际图、execution、reason及
compiled阶段。已有diagnostic-stages成功产物可另存此审计文件，但审计失败不得改变编译
结果或runtime复制。固定20运行器额外保存报告，不改变原P0项目ID、mode、golden或成功标准。

迁入者直接复用PyModuleInstanceState.Globals、执行context与PyFunctionOwner；普通import
和compiled loader进入同一实现。公开GlobalStorage为InstancePublic，生成物没有本族公开
static字段、getter/setter、bound flags或可观察的函数/defaults/来源单例。共享不可变签名
仍可static。实例get/set原分支直达唯一dict，不使用DynamicAttributes作镜像。

没有runtime/RuntimeFn/注册ABI改动，没有ContentEpoch、位置/值缓存、ambient owner、隐式
CPython桥接或super修补。未迁入模块保原ABI。函数原本Dynamic不代表顶层初始化原本Dynamic；
ModuleMain和全局槽的计算/分配成本变化必须单独实测。

## 持久验证与边界

`cargo test -p ryupy-driver --features ordinary-dynamic-module-pilot --test ordinary_dynamic_pilot`
重跑仓库完整CP/PE合同。新增普通import、同一subject的公开loader双实例/重执行、失败import
逃逸旧owner、callee先于args、默认值原容器及装饰器见证；原unbound_exports与owner/loader
消费者保持原文。每个目标断言真实自动计划；显式对照用ExplicitOnly和原选择表。

新发现的`function.__defaults__`读取/替换原能力边界保留完整失败源，并证明候选与旧显式
都在同一AttributeError失败；不删断言，也不把另建现有能力域正例算成该反射能力通过。
原ordinary_retry无def，不在自动族，继续按原显式能力验收。class-cell、zero-super、closures、
genexpr与cross_target/name_ordering原完整例及pending均保留。

候选正常门禁命令：

- `make check CARGO_FEATURES=ryupy-driver/ordinary-dynamic-module-pilot`
- `make projects-preserve CARGO_FEATURES=ryupy-driver/ordinary-dynamic-module-pilot`
- `make projects-promote CARGO_FEATURES=ryupy-driver/ordinary-dynamic-module-pilot`

上述三个门禁命令及Makefile直接Cargo构建/测试recipe统一传feature；间接bench脚本不消费
此变量，本轮性能不用这些入口。不得将feature-off通过算候选结果。
preserve逐ID保护九项，promote仍受原progress回归独立阻断；不改原super忽略参数旧行为。
源码/hash、实际binaryhash、runtime、成功/失败原流和选择覆盖须一起封存。

## 回滚与剩余工作

停止候选进程，恢复完整源码及匹配compiler/runtime/产物后重启。不能在已有函数/生成器存活
时把globals搬回静态槽或重放模块副作用。剩余legacy消费者包括入口、包、完整签名函数族、
class/except及其它实例AST边界、固定绑定和AOT；本包不承诺全部迁移完成。

性能单独冻结前后PE、同host同工具A/A与平衡顺序，区分新进程冷入口及进程内热kernel，保留
全部原始不利数据。没有稳定判定时不称无回退，不用probe耗时代替性能，不沿用旧5%阈值。
