# ADR-0170：固定非优化模式的 __debug__ 语言常量

- 状态：有限域 JIT 正合同、诊断对照及独审通过；完整回归、Native hosted 另验
- 日期：2026-11-12

## 决策

__debug__ 是语言常量而非可遮蔽 builtin/global 槽。HIR 在词法/类/模块名字分派前降为已有 bool True；不引入运行时查询、ABI 或动态代码生成，不为 click 内联改源。
本切固定非 Python -O，现有 assert 继续执行；消息表达式只在断言失败时求值。Rust/.NET 的 Debug/Release 与 Python 优化模式独立，不能读取宿主配置偷偷改变常量。

若以后引入 -O，须同时定义 assert、常量、缓存键和产物 manifest，不得单独把 True 改 False。

## 绑定与验证

三组 tests/cases/debug_constant 在 CP3.12.13 与真实 JIT 双 PIC 精确通过，覆盖模块/函数/类/闭包/默认参数、条件式、assert 副作用和模块字符串属性注入。注入同名 False 后反射可见 False，直接读取仍 True，不借此扩大 globals() 能力。

HIR 对照已核 31 非法与 12 合法语法。普通名字赋值/删除、参数和导入绑定等必须诊断；obj.__debug__ 点赋值也非法，但属性删除/增强赋值及字符串 setattr 合法，必须按 CP 区分，不能一概拒绝。正 baseline 不夹入非法 Python。

Native 尚待 hosted，完整回归另验；真实 click 原入口和固定项目保护不由这三组替代。常量 lowering 没有新增分派，但未据此宣称已测得性能提升。
