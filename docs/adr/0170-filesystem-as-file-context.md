# ADR-0170：文件系统资源 as_file 与原生生成器上下文

状态：实现完成；专项、macOS arm64 NativeAOT 搬迁及最终完整 make check 通过。

## 决策

`importlib.resources.as_file(path)` 首切接收已有 `PyPath`，返回真实
`PyGeneratorContext`，由现有 `PyGenerator` 在进入时产出原 Path 对象。
不复制、不解包、不删除、不探测路径存在性。目录与不存在路径都能进入，
这是 CPython 3.12.13 的 `pathlib.Path` singledispatch 分支实测行为。

资源来源继续由 ADR0057 的 `files(anchor)` 决定，`as_file` 不增加资源发现、
动态模块加载或部署规则。AOT 继续使用显式 `--resources` 的外置资源锚点。
路径构造、读取与 IO 异常沿现有 Path 设施执行。

直接复用 `PyGeneratorContext` 的单次恢复、异常注入、PEP479 与装饰器重建，
避免另造生命周期近似对象。为使公开协议与实际状态一致，上下文将
`gen`、`func`、`args`、`kwds`、`__doc__` 保存在真实可写实例字典中；
进入按顺序删除 `args`、`kwds`、`func`，重复进入报对应 AttributeError。
重建读取当前公开参数，`gen` 重绑会改变后续恢复对象，字典不是状态快照。
公开方法固定接收者，保留共享 `__func__` 与 `__self__`，相等及字典键按接收者/函数身份判断；`__exit__` 接受
`typ/value/traceback` 关键字，并沿用 Python 函数的 self 参数计数。
这些补全也服务已有 `contextlib.contextmanager`。初始实现新增上下文 SetAttr/DelAttr 两条覆盖；
as_file 调度入口为 internal。评审修订再加入 `PyOps.ContextMethod(object,string)`，
对应独立 HIR 节点与 RuntimeFn，保持原分层。精确 ABI 清单与合成反例同步扩为 1722，
旧 1719 条及 V1 专属十二条合同保留。

评审修订：隐式 with 按类型绑定 enter、exit，再调用 enter；不读取实例字段或
`__getattr__`。已有普通类的继承、staticmethod/classmethod 和 property 复用现有绑定设施，
原生生成器上下文从固定方法表读取。显式属性访问仍支持实例覆盖。ContextDecorator
每次调用公开 `_recreate_cm`，随后复用统一上下文协议，保留替换的副作用、异常、
返回上下文和清理行为。D008 在已支持对象协议内关闭；任意自定义描述符与类级反射仍待后续扩展。

## 错误与边界

正常退出返回 False，原文件始终保留。异常退出按既有生成器上下文协议
传播原异常，包含 StopIteration、RuntimeError 与 KeyboardInterrupt 的身份。
对耗尽生成器、提前退出、重复退出、缺参/多参/重复关键字与错误类型另有差分；混合错误先报告未知/重复关键字，
再报告位置参数过多，与CPython一致。

无首位置参数时保留 CP singledispatch 的 TypeError；Path 分支的后续参数
由原生 `_` 生成器工厂绑定。非 Traversable 的 int/str/None 保留 CP 的缺少
`is_dir` AttributeError。具有 `is_dir` 的其它载体明确报 NotImplementedError，
不调用用户协议制造空结果，也不自动转为 Path。

自定义 Traversable 与 zip 临时解包/退出删除仍待实现，保留独立 CP 正期望 pending。
zip 用例当前首先停在 zipfile.ZipFile 缺失，尚未到达 as_file；自定义 Traversable
实际到达 as_file 并明确拒绝。完整 singledispatch 注册/反射、上下文类型类级反射及
任意用户对象的通用上下文设施扩展都留给独立竖切，本切交付文件系统 Path 分支。

## 验证与成本

先写 pending，固定 CPython 3.12.13 校准四个目录用例，再实现并转正 Path 与
真实包资源两个通过域；自定义 Traversable/zip 两个用例保留 pending。
专项 C# 七项加已有资源与上下文十九项，共 26 项通过；另有两项新增 ABI 合同测试。

真实 NativeAOT 在已安装 .NET SDK 的 macOS arm64 宿主发布，完整 bundle 搬迁，
删除本轮源码、JIT/托管构建目录与原 dist 后，从另一个 cwd 执行。相同程序覆盖完整
Path 协议和 `files(pkg).joinpath(...)`，CP/JIT/搬迁 Native 三者 exit/stdout/stderr
逐字一致；删除搬迁数据后的直接读取分支报 FileNotFoundError，exit 1。
首次发布的成功路径已通过，负例测试最初被 fixture 的 `is_file` 断言提前截断；
修正测试入口为直接读取后，第二次完整发布搬迁与负例通过。第三次收窄 CLR
可见性、补齐错误接收者后重验通过；第四次补齐方法相等/hash/字典键协议后重验通过；
第五次补齐混合错误绑定优先级后重验通过。成功记录中的
`publish_count=1` 表示该次 runner 调用一次，整个开发过程共五次发布。

无 SDK 目标环境、Linux、Windows 尚待各自真实验收。本切没有性能测量；
上下文新增实例字典与绑定方法包装只作用于上下文冷路径。

运行方式、证据及公共文档整合条目见
[独立说明](../notes/resources-as-file.md)。

最终完整门：fmt、严格 clippy、Rust workspace 全部通过；C# 3009 通过、2 项旧基线门
明确跳过，固定 CP 基线 720 通过、目录/单文件自检 723 通过，编译 e2e 443 通过、
284 pending/ignored。旧四项 frame 证明实验仍按项目 Makefile 在独立门运行。

完整门执行中的修正记录：首轮在公开 ABI 精确计数处停止，收窄内部入口并登记
两条必要覆盖后通过；次轮 driver 遇到 macOS 默认临时路径的 /var 符号链接及
旧 ForEach 测试专用 Python 未指定。以当前 worktree 的普通 target/check-tmp 和
RYUPY_FOREACH_PYTHON=固定3.12.13纠正运行环境，未修改 driver 实现。补充绑定方法
相等/字典键断言期间有一轮使用旧 Release 对新用例失败；固定最终源码并重新构建后，
上述最终完整门 exit 0。完整失败记录保留在本地原流，最终结果另存精简门禁记录。
