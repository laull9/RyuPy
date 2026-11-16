# ADR 0086：MappingProxyType 的真实只读活代理（有限范围本地已验）

- 状态：真实公共注册、有限源应用及首次正常完整门禁本地通过；未发布。
- 基线：27acfa966196407a05eca6f93211e2c0fdca3ea4。

## 背景与范围

锁定 tomli 2.0.1 原入口在动态 except 候选 79d 上 CPython、编译分别成功，JIT 因 `types` 缺失失败；原错误没有源帧，不能将静态导入位置冒充实测失败行。本切实现可复用的 `types.MappingProxyType`，不提供空模块或项目特判，也不承诺完整 types、tomli、AOT 或性能通过。

现 `IPyNativeMapping` 专供 dict 派生存储；类型判断、JSON、哈希和关键字展开会直接访问底层 Dictionary。只读代理不能借此接口冒充 dict，也不能将构造输入复制成快照。

## 设计

独立 `PyMappingProxy` 强引用底层对象，保留后续修改、值别名、嵌套代理层与用户回调。精确 dict 读取保直接路径；原生 dict 派生及普通映射使用自己的下标和方法，尤其不能跳过 defaultdict 缺键工厂或将 get 改为下标回退。

构造允许 `mapping=`，按总参数数量优先检查；结构准入不调用用户方法。类级 `__getitem__ = None` 可以先构造，读取才报不可调用。支持范围为 dict、既有 defaultdict/Counter、普通映射、字符串及嵌套代理；range 在 CPython 合法，但本体相关协议尚未闭合，本切明确 NotImplementedError 并保留正确参考，不谎称它不是 mapping。

`get` 即使省略默认值也向底层传两个参数；keys/values/items/copy 每次查找并直接返回底层方法结果。哈希按 Python 3.12 逐次委托底层，不缓存、不按代理身份或内容重算。只读限制仅指代理没有写入口，不深冻结值，也不是阻止底层用户代码副作用的安全沙箱。

类型使用稳定 PyNativeType；构造该类型本身会写入原生短名表，不能以“尚未导出 types”宣称没有全局影响。A 阶段的 Accepts 曾过渡接受已有 Enum.__members__ 载体；B3 已统一为真实 PyMappingProxy，并删除旧载体。每个 Enum 仍缓存同一 members 代理，别名值身份保持。ABC 仅按真实载体/类型判断 Mapping，不冒 MutableMapping 或 dict。

## 分阶段与边界

1. A：内部载体、构造、读取方法、hash 和类型身份。当时不注册 types、不切换旧 Enum 行为；未闭合的迭代、contains、比较、union 保持明确内部阶段未实现。
2. B：真实惰性迭代、getitem 回退与 contains 早停；公开 raw iterator 身份与 CLR 内部适配分开。闭合反向游标、dict/kwargs 消费、union、ABC 和 Enum 迁移，逐项独审。
3. C：以上协议闭合后最后注册 types，并进行有限真实源差分与完整门禁。

本切不改全局 Compare 的 bool ABI。显式比较槽保底层原结果；非 bool 比较表达式的身份仍是旧边界，保留 CP 正例，不宣传完整富比较。原 dict 迭代的更严格结构版本行为也不借此宣称全部对齐。

字典字面量 `{**proxy}` 当前在 HIR 就未支持，保留正确参考，不默扩编译器。函数 `**proxy` 的消费者方案须保 keys 列表准备、重复键在相应 getitem 前拒绝、非字符串键合并后检查；跨实参表达式与展开交错的旧调度边界单列，不将局部运行时桥接冒充整个调用框架已修复。

## 性能取舍

代理只增加一个保存底层引用的对象；精确 dict 现有非代理热路不改成接口分派。普通方法仍采用现有原生函数绑定，回调路径不得通过缓存方法、复制映射或预取所有值改变语义。新 kwargs 桥接保留原有两个直接返回快路；已有展开慢路增加一次 O(k) 的 CLR 参数扫描，即使最终没有 proxy 也支付该扫描成本。只有 proxy 参与时才进入新的合并 helper，原非代理展开主体不变。目标字典首次及外部结构修改后会扫描现有键，不能称所有普通展开零成本。BitOr 的原内建叶子抽为薄方法，以便代理两侧运算槽各调用一次后复用，不重放用户槽；其他 BinaryCore 签名和运算不变，但原 BitOr 增加薄调用及整数类型检查成本。SetUnion 仍可能执行原有键 hash/eq，不能称叶子没有用户回调。当前没有新的性能测量，不宣称零成本或收益。

## 验证记录

- 一次 CPython 3.12.13 参考：28 秒执行及 2 秒清理上限；实际 exit0、空 stderr、无超时。113 行原流包含 84 项观察、26 项调用日志及版本/工厂/完成标志。它们是 CP 参考，不是 RyuPy 已通过测试。
- 参考脚本 SHA256：`4a35f941c2c29e3027887e4b3baa68b7c1125d4ca72eba0241cad15b88167009`；stdout：`232bc671702882801d9ac6f6c7d2834f8af9af94350319c5cfaf98c666dba099`。
- A 四文件经独立源审；必要尾修仅为帧/ambient 测试加入既有测试集合。首次 Release 专项 8/8，0 失败、0 跳过；包括旧 Enum 类型身份、底层方法替换、原异常帧/context、只读及 JSON 拒绝。
- A 阶段记录时尚未运行 B/C；其后 B1/B2 接线及首次 Release 合体专项 23/23（A 8、迭代 8、消费 7），0 失败、0 跳过。公开 raw iterator 身份、局部 StopIteration 状态恢复、getitem 回退、contains 早停、反向 dict 游标与 keys/getitem 消费顺序均有内部断言。
- 新 copy/update/** 消费只接受列明的基本键类型，其他键明确 NotImplementedError；该限制不改变 proxy[key]、proxy.copy() 的底层委托或既有普通 dict 成功路径。已发生的用户回调副作用不回滚。Counter 非空更新/减法仍走 items，空更新走 keys。
- B3 审中修正了提前检查右侧运算槽及异常 PythonClass 的 descriptor 漏检；最终按实际分派时点检查，两侧均返回 NotImplemented 后直接进入内建叶子，不重新执行槽。首次 Release 合体 34/34（31 项代理专项、3 项旧 Enum），0 失败、0 跳过。
- 显式 eq/ne 保原返回对象；有限普通槽支持不包含 descriptor 和严格派生反射优先，相关合法域明确未实现。新 exact dict union 仅非 inplace，基本键限制不改变旧 dict 更新；不能据此宣称 dict |= 已支持。
- C 仅在 PyNativeModules 登记真实类型，不预 Import、不冻结属性、不改变源码模块覆盖顺序。首次四应用 3/4：P1 CP 成功，Ryu 因函数内定义 class 的旧 E0100 在编译停止，未启动 JIT；原源逐字保存在 pending，测试仅迁为模块级 try/class，完整继承 TypeError 及输出不变。
- P1 第二次 CP/编译成功，JIT 揭示真实 object() 的 PyBaseObject 被错误归入未知 native NI；已精确修为非法 mapping 的 TypeError，并在原 8 项 core 专项增加真实载体断言、保 CLR 对象旧断言。复验 core 8/8、P1 第三次 1/1；其余三应用首次均成功，四应用已逐项通过，来源/动态值路径结构 1/1。
- 四应用包含 live views、迭代/反向游标、原生消费、用户回调与错误时序、真实类型/Enum/ABC/模块重绑及源码 shadow，均 CP 原流先守门后执行同源 JIT。pending 不纳通过计数，新增时序日志不是从旧 84 项参考推断通过。
- 首次正常 `make check` exit0：fmt、严格 clippy、Release build 零 warning/error、1497 项 C# 零失败/跳过、固定 CPython 3.12.13 基线 228、workspace 与 e2e 202+30 原 ignored 全部通过；本轮四项应用及一项结构在完整门禁中也再次通过。日志 SHA256：`0f86d592e1189c66b6bb8b1217c4e09bacc9ca779e12511494cda5bd1cc5d2b6`。
- 未执行新 tomli 入口、AOT 或性能组，未公开推送；不更新历史项目通过率。
