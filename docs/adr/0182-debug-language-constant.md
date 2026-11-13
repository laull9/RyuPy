# ADR-0182：固定非优化模式的 __debug__ 语言常量

- 状态：有限域 JIT/Native 正合同、诊断对照、独审与完整回归通过
- 日期：2026-11-12

## 决策

__debug__ 是语言常量而非可遮蔽 builtin/global 槽。HIR 在词法/类/模块名字分派前降为已有 bool True；不引入运行时查询、ABI 或动态代码生成，不为 click 内联改源。
本切固定非 Python -O，现有 assert 继续执行；消息表达式只在断言失败时求值。Rust/.NET 的 Debug/Release 与 Python 优化模式独立，不能读取宿主配置偷偷改变常量。

若以后引入 -O，须同时定义 assert、常量、缓存键和产物 manifest，不得单独把 True 改 False。

## 绑定与验证

三组 tests/cases/debug_constant 在 CP3.12.13 与真实 JIT 双 PIC 精确通过，覆盖模块/函数/类/闭包/默认参数、条件式、assert 副作用和模块字符串属性注入。注入同名 False 后反射可见 False，直接读取仍 True，不借此扩大 globals() 能力。

编译语法合法性对照已核 31 非法与 12 合法语法；syntax 仅新增该目标的窄合法性检查，未启用宽泛 semantic checker，也不承担类型/名字解析语义检查。普通名字赋值/删除、参数和导入绑定等必须诊断；obj.__debug__ 点赋值也非法，但属性删除/增强赋值及字符串 setattr 合法，必须按 CP 区分，不能一概拒绝。正 baseline 不夹入非法 Python。

Native 有限组合图与完整回归已实际通过（见后续实证）；真实 click 原入口和固定项目保护不由这三组替代。常量 lowering 没有新增分派，但未据此宣称已测得性能提升。

## 单模块编译审计增量

最终固定矩阵的 click 仍为 unsupported（del frame），不计项目通过。仅批准旧计划 click.core.compiled 的严格布尔 false→true，表示单模块可编译进展。历史 raw/hash 冻结不改；其余模块、字段、selector、ABI、reason 及来源身份逐项保持，缺失晋升或类型错误仍拒绝。此审计例外不修改项目成功计数，不表示真实 click 入口已完成。

## 后续实证（2026-11-12）

CI `43229286565` 在同候选上通过有限组合图的真正 Linux x64 ELF 原位与搬迁、CP/双 PIC 和双完整回归；整 CI 仍有旧 metadata 边界失败。该证据不扩大上述语义域或平台承诺。两批 JIT 配对性能保留全部样本，oop 的回退风险尚未排除，不能据功能通过宣称性能无回归。
