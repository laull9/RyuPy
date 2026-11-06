# F0 SpecialForm基础合同

所有源先在固定CPython 3.12.13以`-I -S -B`独立双跑，冻结stdout/stderr/退出码；golden不从候选输出生成。当前新用例仍先标pending，是否转正由完整候选门禁决定。没有ClassVar支持声明，也不声明Any完整class/metaclass行为。

## 八个支持候选

- `metadata_annotations`：类型分类、稳定metadata、真正立即求值注解。
- `raw_keys`：归一化前原始键、TypeVar、安全别名。
- `shared_lru`：共享128项LRU、Optional内外两项、Literal隔离。
- `saved_root`：保存根与模块导出重绑、热/冷消费相位。
- `invalid_arguments`：已知非法tuple与完整错误消息。
- `equivalent_alias_keys`：结构相等键与内Union存活时外Optional重建。
- `error_message_supported`：通用str非法下标消息、emoji/ASCII错误repr截断。
- `native_alias_lookup`：独审原源逐字保留；原生别名等价暖key不消费`_GenericAlias`。隔离进程直接写该全局，不要求先读取尚未导出的私有helper。

## 六个完整CP正例保留pending

- `unsupported_helpers`：真实helper回调，有限实现冷消费明确NI。
- `unsupported_mutation`：CP合法alias元数据写入。
- `unsupported_forward_callbacks`：ForwardRef、合法标量和自定义哈希回调。
- `unsupported_type_factory`：独审完整原eviction.py，既有三参数type工厂问题不算F0容量失败；真实容量已用TypeVar独立验证。
- `error_message_boundaries`：完整第二轮原源与golden不变，额外保留Python孤代理字面量的全局repr差异；不能用支持子集覆盖该失败。
- `native_alias_helper`：完整第二轮原源与golden不变，额外要求读取/保存/恢复私有helper导出；缺少导出是独立既有边界，不代表真正暖key比较失败。

C#直接构造CLR字符串的合同和Python源码字面量端到端合同分开解释；C#通过不能消除完整Python孤代理用例的pending。所有旧源、旧golden及第一次失败流继续保留。
