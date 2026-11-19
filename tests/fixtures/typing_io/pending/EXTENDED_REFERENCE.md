# typing IO 扩展正例的来源与边界

typing_io_extended.py.pending 是从已执行的唯一 CPython 3.12.13 参考整理的正期待草稿。本组合文件没有独立执行，也没有运行 RyuPy、SDK、编译器或测试；不能把原参考通过写成本文件通过。没有修改生产代码，也没有把 NI 当作预期结果。

## 原始证据

- 原记录目录：/workspace/shared/ryupy-typing-io-validation/reference/。
- source.py SHA-256：b5c847a8fbaa4212e0f56814490c37e62df2715b87e41b4a7c0f050921ed1605。
- stdout SHA-256：a5803680f8bfe9fbba749b126d6b8156cdd010254d59fa373dabbec5eaf5c597。
- result.json 记录 exit=0、timeout=false；stdout 为10组781行、stderr 为空。独立复核见该目录上一级的 independent-reference-review.md。
- 本准备只读取这些已存在的原文件并复核源码/stdout 的 hash，没有新增 CP 执行。

## 断言映射

1. 三根 __bases__/完整 __mro__/__orig_bases__/__parameters__、重复元组身份及未参数化 get_origin/get_args：stdout 第1–12行（第1组）。Generic 的实例/继承归属：第735–743行（第10组）。
2. IO[bytes]/IO[str]/IO[AnyStr] 非类、origin/args/parameters、read 共享身份、缓存身份和开放替换：第643–658行（第7组）。别名调用的真实 IO 类型、无字典/弱引用槽/__orig_class__：第660–685行（第8组）。
3. AnyStr 的 TypeVar 身份、约束、bound、方差和 get_origin/get_args：第713行（第9组）。
4. BareIO 的字典、弱引用槽、extra 写入、继承 read，以及 SlottedIO 的继承、参数元数据、无字典/弱引用槽、写入 AttributeError：第593–612行（第6组）。
5. HookedChild 的钩子顺序和完整 MRO、BadHooked 未知关键字 TypeError 及失败前 enter 事件：第633–638行（第6组）。类/函数名称保持原参考一致，使完整异常消息可直接引用。
6. PayloadIO 的参数元数据、单个约束 TypeVar 的来源身份和参数化别名元数据：第728–729行；bytes/int 实例保存来源别名：第730–733行（第9组）。本草稿把原输出中相同的来源别名表示整理为相等性断言，属于参考派生的组合，未对本断言形式作独立执行。
7. IO[bytes]/IO[str]/IO[AnyStr] 作为 isinstance/issubclass 目标时的真实 TypeError 类型、原文和 args：第745–768行（第10组）。

## 保持 pending

- 泛型对象、完整反射、参数化派生 slots 与继承钩子等尚未纳入本切支持声明。当前29项专项通过不代表本文件或上述扩展域通过。
- 文件保留 .py.pending 后缀，默认只发现 .py 的 gate 不会执行；顶部 pending 标记再次说明未实现状态。不得改成 NI 预期、捕获导入失败后跳过，或计入当前通过数。
- 本文件是有限延期证据，不新增完整 Generic 框架承诺；不涉及 ParamSpec、TypeVarTuple、多参数泛型或自定义元类。
- 若后续要转正，须先明确授权与执行窗口，再对本组合文件在固定 CP 下独立确认、按原期望做 RyuPy 差分及适用门禁，不能直接沿用原参考的 exit0。
