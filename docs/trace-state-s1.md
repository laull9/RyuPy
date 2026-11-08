# S1 未接线 traceback 事务内核

本单元仅新增未来可复用的 internal C# 类型及直接调用这些类型的 xUnit 测试。没有改动
`PyBaseException`、`PrepareRaise`、`Attach`、`Frames`、`PushFrame`、编译器 RuntimeFn 或发射。
不是 C# 解释器，没有切换开关，也没有修复当前严格 traceback 差分失败。
Bare、注入、RestoreHistory、异常链与真实宿主生命周期仍不在此单元。

## 两项实施审查更正

此前冻结的迁移 008、009 S1 附录和 016 终审均保留原文，不将历史稿改写成已通过实现。
010 的共享 epoch 合同继续适用。实施独审 017 识别并经协调者授权修正以下遗漏：

1. **出生资格不能跨成功 Explicit 保留。** 任意成功 Begin 都将 `GrantValid=false`；
   NativeFresh 另置 `GrantUsed=true`，Explicit 不冒充出生 grant 的使用。失败 Begin 两位均不变。
   因此旧稿“Explicit 已建立 unfinished F，出生 grant 仍可在终结后首次 Fresh”正例撤销。
   正确的可达重试是首次无 F 时因无效 scene/request 失败，再用同 grant 合法 Fresh。
   unfinished-F 拒绝只证明不改变当前已失效资格。空 H 旧对象也不能重新 Fresh。
2. **接收组合及空段。** 合法组合只有 Exclusive Forward/Suppress（包含 root）、
   Inclusive 真实 activation Handle、Exclusive Root Terminal。首次空内段 Forward 合法，
   如 native 本地错误的 scene=[M,P]、边界 P，或无 Python 帧的 root。
   最近 Forward 后以新 R 重包同一不前进边界拒绝；相同最近 R 完整参数仍 NoOp。
   后续 Handle/Suppress/Terminal 可以终结 F，不添加假 host/module 帧。

## 状态与权限

- `PyTraceState` 实例即唯一 owner。`TraceSnapshot` 是完整不可变状态；所有拒绝保留相同快照引用。
- H、scene 外到内排列。每次 Receive 将新遍历段前缀拼到最新 H，不跨请求历史去重。
- scene 在 Begin 防御复制，activation 为正、同 scene 唯一。边界同时验证 epoch、位置、ID。
  同名递归不同 ID 不合并；新 scene 不能因深度相同使用旧 activation。
- Q 是 owner/epoch 内正且递增的请求号。同最近 Q、来源和完整 scene 的 Begin 重送 NoOp，
  不重新打开已关闭 F，不清收据。不同 Q 不能覆盖 unfinished F。
- R 从 1 连续递增，仅保存最近一次成功 Receive 的完整事件与结果版本；更老 R、跳号、
  参数冲突、旧 Q 拒绝。新 Begin 清旧收据。Forward 继续 F，其他合法组合关闭 F。
- BirthGrant 只按该 owner 持有的实例身份校验，消费/失效只记录在 owner 状态，不修改 grant。
- authority 私有 seal 发行不可变 epoch 和 owner/from/target/version 许可。seal 工厂拒绝伪造；
  私有构造和身份校验是内部封装，不是抵抗恶意反射的安全边界。
- IssueNextEpoch 只生成当前后继，AuthorizeRotate 只以当前 epoch 为目标。Rotate 只发布 owner，
  保留 H，清请求/收据/出生资格。共享 epoch 永不消费，无已消费集合。两个 owner 可以先后迁至
  同一 epoch。新 epoch 的发行不影响尚未 Rotate 的旧 owner 收尾当前 flight。
- Rotate 要求无 unfinished F、精确匹配 owner/from/version/authority/current target。
  失败不修改 owner、authority 或其他 owner；成功后旧许可因 from/version 不符失效。

所有验证、数组复制及 next 构造在单引用发布前完成；分配抛错也不会先修改消费位。
调用模型为显式串行，不承诺并发安全。不读取 CLR StackTrace、环境变量或全局异常注册表。

## 资源范围

每个 owner 保存一个 H、一个最近请求 scene、一个 pending 游标、一个最近 receive 收据和固定资格位。
H 按实际提交帧增长，不因显示名重复去重；scene 按该请求帧数分配，关闭后最多保留最近一份以验证
Begin 重送。构造时临时 ID 集合 O(scene)，接收复制 O(新增段+H)。无跨请求收据列表、无 epoch
注册表。外部显式保留的旧快照继续持有其历史，这是不可变快照的空间成本。
计数采用有界整数并在可溢出发布前拒绝；未引入可配置的测试专用伪状态/计数回绕接口。

## 验证门及证据边界

`PyTraceStateTests` / `PyTraceStateEpochTests` 直接测试实际类型，不用反射构造内部状态，不打印预设
期望代替执行。拒绝逐次比较完整状态引用；readonly 容器和历史快照另测别名与变更隔离。
`InternalsVisibleTo` 只授权测试程序集一处，不扩大任何产品类型的公开 API。

`PyTraceStateArtifactTests` 读取本次真实运行时 PE：

- 遍历旧类型的基类、接口、字段/属性/事件/方法签名、泛型约束、方法局部、异常 catch 区域及 IL
  token 引用；递归解析 TypeDef/TypeRef/TypeSpec、MethodDef/FieldDef、MemberRef、MethodSpec、
  StandaloneSig，拒绝旧类型引用新内核。检查新类型无 `.cctor`，无新内核 ModuleInitializer。
- 检查新类型全部 internal；比较固定旧产物与本次产物的全部公开/保护类型、成员签名、
  修饰符和泛型约束。此项普通运行未指定旧 DLL 时明确 Skip，不计 ABI 通过。
- 这是上述 PE 引用/签名范围的证据，不检测反射字符串/外部程序集调用，不证明完整 ABI、
  语义兼容、零成本、JIT/AOT 体积或性能。ABI 比对不覆盖常量值、默认参数、全部自定义属性或
  类型引用的程序集 scope。新类型加入程序集可能增加未裁剪产物大小。

专用 default full-check 在旧完整门之前以固定旧源码
`bd5d7dbe1f09bd3ba649e9b1c1928845784e4fe8` 和同一 SDK 独立 Release 构建基线/候选。
`trace_state_s1.py` 保留命令、每步原流/退出码、两 DLL/哈希、发现列表、精确执行名、TRX、PE/ABI
JSON。它必须提供旧/新产物路径，验证测试加载 DLL 字节相同，要求精确预期案例全执行、零失败/跳过。
旧完整 make/check 和所有项目/Native 门没有删减；独立 S1 失败仍最终硬失败，不用其通过掩盖旧红项。

本次源码交付阶段尚未运行 .NET/Rust 构建或任何产品测试。静态 Python 语法/清单核对不能代替编译。
实际执行结果必须由后续固定提交 hosted CI 给出。既有严格 traceback 和旧 driver 负向期望失败
仍单列；固定 20 项 10/8/2、十项历史通过与七个 Native 门口径均不改变，accepted 仍为 fb0553。
