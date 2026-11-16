# ADR-0215：原生 async with 与真实 traceback 冷视图

- 日期：2026-11-16
- 状态：显式原生协程候选；同步 with 的同根清理错误修复
- 接续：[ADR-0208](0208-cil-execution-libpython-kernel.md)、[ADR-0210](0210-native-coroutine-state-machine.md)

## 决定与范围

`native-coroutines` Cargo 特性继续默认关闭。开启后，普通原生动态函数中的
async with 复用现有显式 await 委托状态机、逻辑异常路由与 finally 分派。
没有事件循环、async for、新的 Python helper 或 CPython 字节码执行。

上下文表达式只求值一次，按类型槽先绑定 enter、再绑定 exit，最后调用 enter。
两个已绑定方法保留到结束，实例同名字段与进入期间的类型重绑不会替换本次 exit。
异步 enter 与两条互斥 exit 路径分别保留 AwaitContext；只有缺失 __await__ 时产生
上下文专用错误，用户 __await__ 内部抛出的错误及非法迭代器错误保持原身份/内容。

enter 成功后才进入保护区；as 赋值放在该区内，再执行其余上下文和正文。
同步 with 同时采用这一保护区修复，并复用已有属性、下标和平坦解包目标。
解包或 setter 失败调用已经进入的 exit，外层上下文接管内层 enter 失败。
return/break/continue 等正常离开路径 await exit(None, None, None)，忽略返回值的真值。
异常路径先标记已经进入 exit，再 await exit(type(error), error, traceback)，
仅在 await 完成后求一次真值；退出再次失败或取消不会重复调用 exit。

## 原生 traceback 传播协议

已有 `PyBaseException.Frames` 是首次抛出时的全栈文本快照，不能反转后冒充 traceback。
新链条独立记录真实传播接收，旧 Frames 文本与 TraceV1 owner 保持原合同：

- 源码显式 raise 使用 RaisePrepareExplicit，开启新传播并把旧链作为尾链。
- 普通 catch 在弹栈之前使用 TracebackReceive，接收者至失败点之间的实际激活进入链。
- 生成器的真实 physical catch 在 Retry 弹栈之前使用 GeneratorTracebackReceive，
  用独立存储载体精确定位当前逻辑生成器激活。
- bare raise 的当前指令帧不新增节点；跨出调用者时仍按真实接收增加外层节点。
- 内部 finally 与匹配 ambient 使用 HandlerEnterGuard，保留异常上下文 token 与传播状态；
  真正匹配后用 EnterHandler 或 TracebackHandled 结束本次传播。
- 同一异常再次 throw 到暂停的生成器/协程属于新传播，旧链头保留为稳定尾链。

PyGenerator 的真实 PushFrame/PushFrameWithOrigin 仍直接位于 MoveNextCore，
之后独立绑定冷激活。未启动 throw 保留该函数激活，并从受信定义元数据取得 def 首行。
正常首次 next/send 不读取这份冷元数据。TraceV1 接收规则与 PE 权限边界未放宽；
native 冷入口复用已有 Attach 边界拒绝进入 V1，不直接读取受保护绑定字段。

## 可观察对象与明确边界

`error.__traceback__` 与 exit 第三参数共享同一真实链头；跨 await 保留头节点身份。
节点开放 tb_frame、tb_lineno、tb_next。tb_next 可设为本域 traceback 或 None，
类型错误与循环链明确拒绝。tb_lineno 冻结在传播点，f_lineno 冷读活动槽，
暂停/返回后读取弹栈时保存的末行。

frame 只开放 f_code 与 f_lineno；code 只开放 co_name 与 co_filename。
代码身份只由已登记 CompiledCodeSource 的受信 signature 提供，缓存还核对实际文件来源。
未知宿主手造共享 signature 无权声明代码身份，f_code 明确 NotImplementedError。
CPython 字节码偏移、co_code、f_locals/f_globals/f_back 等字段明确 NotImplementedError。
这些视图没有把 CIL 字节伪装成 CPython co_code，也不承诺完整 frame/code 反射。

本切 native 异常的 __traceback__ 头只读：赋值和删除统一 NotImplementedError，
用户异常实例字典不能覆盖真实数据槽；用户类同名覆写也明确拒绝。
foreign 异常优先走原 provider 协议。cause/context 的其它既有写入边界保持原范围。
可写头、with_traceback 与完整 traceback/frame 反射后续单独实现。

## 分配与性能保护

普通调用不分配 frame/code/traceback 对象。frame 并行缓存只在真实异常接收/观察时创建，
同一栈深度后续调用通过 Push/Pop 清槽取得新的激活身份；同一生成器跨恢复共享原 frame。
generator-owner 表在第一次真实生成器激活时才分配，普通产品不分配或写清该表。
SetLine 保持原热实现；f_lineno 在冷读取/弹栈时同步。

Push/Pop 的空缓存条件检查仍有成本。冷热路径、普通 calls/recursive 与完整固定性能集
由主集成阶段配对测量，本切不发布性能收益或默认晋升结论。

## 验证

手写输入见 [tests/coroutines](../../tests/coroutines/README.md)，当次固定 CPython 3.12.13
生成预期，JIT 与 AOT 产物在仓库外并清理。覆盖：

- enter/body/exit 真实挂起，多上下文、return/break/continue、解包/setter 失败清理。
- BaseException 取消注入、enter 取消、内层 enter 失败、exit 抑制/不抑制、
  exit 再次取消、退出结果真值失败及原异常链。
- throw/close、清理仍产出时的 GeneratorExit 合同与后续恢复。
- 同一 traceback 头身份、准确调用链、显式/bare 重抛、同异常重复 throw、
  未启动 gen/coro 的 def 首行、同深度新调用与同一生成器恢复的 frame 身份。
- tb_next 类型与循环拒绝、未知 code 来源、只读头及字典覆写边界。
- TraceV1 七个 PE 反伪造门；正常帧观察后重复 Push/Line/Pop 的零分配断言。

本机 CPython 3.9.25、3.12.13、3.13.3 的六组新源码输出一致。
macOS arm64 NativeAOT 门保留删除输入和整个构建目录后的中文空格搬迁。
其它平台、onefile、事件循环取消 API 与真实 IO 生命周期另行验收。
