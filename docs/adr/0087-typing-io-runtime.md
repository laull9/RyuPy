# ADR-0087：typing IO 三类的有限名义运行时

- 状态：公共注册与 29 项专项、四项真实应用分别通过；首次正常完整门禁已通过，未发布。
- 基线：167e9c16e7ff70ceef36c2d9aa41a3a547842960。

## 背景

锁定 tomli 2.0.1 固定入口在候选 53bc95b 的 CP、编译成功后，JIT 实际报无法从 typing 导入 BinaryIO。该原错误没有帧，静态源码位置不能充当唯一运行位置；不承诺本切足以通过项目。既有 pyfiglet、tabulate 的结果也只覆盖对应 Linux JIT 固定入口，历史 5/20 不变。

## 决策与边界

以三个稳定真实类及其方法、property 和普通单继承行为闭合，不导出假的 typing form 或 io 别名。固定 CPython 3.12.13 源码中方法/getter 的正文为 pass，合法调用真实返回 None；仍必须执行正确绑定，不增加参数类型转换。根没有 ABCMeta，允许零参实例化。

IO、BinaryIO、TextIO 的裸实例遵循空 slots；普通无 own slots 后代可拥有字段。通过根在创建起禁止分配实例槽，加真正 ApplyDataclass 装饰入口首写前拒绝，证明根不能形成 PIC 槽缓存或快构造计划。不为任意宿主内部污染增加 SetSlot/PIC 命中热分支。

不在本切实现 Generic、AnyStr、IO 参数化别名或完整继承/函数反射。涉及缺失结构的公开观察明确未实现，保留 CP 合法正例；不会返回假的 object 祖先、空参数或占位类型。普通类已有描述符、函数内类等边界不借此全面改造。

普通后代模块来源只在类创建冷路径读取真实最内模块执行来源及其注册 getter 当前 __name__；不以文件名、cwd、可变函数 module 或外层帧猜测。未知来源不能污染构造、字段、身份 hash，只限制确实需要该元数据的观察。

## 成本

无编译器或新增 RuntimeFn，数值 ABI 不变。三个类、28 函数、8 property 为模块固定对象；后代只保存有限布局/来源信息，不建立无界全局子类强引用表。慢槽分配与构造入口检查仍有成本，不能称零性能影响；尚无性能测量。未完成类的 object.__new__ 旁路在 AllocateFromObject 冷入口复用检查，不修改所有 PyInstance 构造。DeclareDataclass 也供普通注解类使用，只登记信息、不激活/分配槽；它保持原路，避免误拒合法注解派生。

## 验证计划与已知事实

唯一 CP 参考：10 组、exit 0、stderr 空、0.114288146 秒，无超时。脚本 SHA256 b5c847a8fbaa4212e0f56814490c37e62df2715b87e41b4a7c0f050921ed1605，stdout SHA256 a5803680f8bfe9fbba749b126d6b8156cdd010254d59fa373dabbec5eaf5c597。这些是参考观察，不是 RyuPy 测试通过数。

内部布局/绑定/来源专项后，三根一起公开注册，再以四个默认进入门禁的 CP/JIT 应用覆盖根调用、绑定和 property、普通派生、真实跨模块重绑。泛型等合法长尾独立 pending。制定计划时完整门禁尚未执行；本切不因此承诺项目、AOT、性能或公开发布。

## 内部阶段首次验证

12 个既有运行时文件窄接点与 4 个 helper 已接线，typing.Build 尚未导出三根；没有编译器或 RuntimeFn 改动。生产源审中修正定义期 __qualname__ 假写、派生实例泛型同名普通字段的读取优先；callable 对本族实例只检查真实类型链 __call__ 槽存在，不触发用户调用或读取实例伪槽。非族原行为不改。

首次 runtime Release 编译成功，0 warning/error。随后三份内部测试首次 29/29、0 failed/skipped，包括真实方法表/绑定、零槽与 property 优先、未完成构造、注解声明与真正 dataclass 装饰区分、名义关系、元数据边界及真实当前模块槽来源。该内部阶段四个真实 CP/JIT 应用仅完成源码审查，尚未执行；当时公共导出与完整门禁仍待。内部测试不是 typing API 已对用户可用的证明。

## 公共注册与应用阶段

三根在工厂全部完成后统一注册。公共专项首次28/29，唯一失败是测试宿主未执行既有原生模块注册，真实Import找不到typing；仅按既有PyTypingTests补宿主初始化，保Same断言，第二次29/29通过，生产注册逻辑未为此改写。

四个应用首次3/4，CP均成功；绑定/property应用唯一E0100是既有delattr未实现，JIT未启动。保原全文为builtin_delattr.py.pending，仅将已知八个property的删除改为显式del分支，全部删除错误及其它断言不变，精准复验1/1通过。另三个应用首次即CP/JIT通过，覆盖真实根调用、普通带注解派生/context和跨模块重绑/定义时模块快照。另将异常with第三参数的原完整CP非None正例保为with_traceback.py.pending，本切不扩大HIR。

首次完整 make check 已通过：fmt、strict clippy、Release 零 warning/error、1526 项 C#（0 failed/skipped）、固定 CPython 3.12.13 基线 228 项、workspace 与 202 项 e2e + 30 原 ignored；四项新应用也在完整门禁中全部通过。日志 SHA256 93d04283c21505f34bdac8a23d259c1ca2d782579d5f79a34acad2ec3124810a。未追加项目、AOT或性能执行，不更新历史项目分子。
