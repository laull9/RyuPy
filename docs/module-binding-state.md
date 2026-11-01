# 未绑定全局名字与模块属性

本页描述 M3.7 的未知名字与绑定状态增量，不代表完整动态名称查找或所有内建回退。

## 名称解析与诊断

HIR 先按现有局部、闭包、类体、模块绑定与已实现内建规则解析。真正未解析的普通名字暂存为内部节点；整个模块的函数体与默认值表达式降级后，统一登记动态模块槽并改写读取。不会通过扫描所有 AST Name 把局部参数、属性或延迟注解误注册为模块导出。

因此 Python 3 兼容分支中未执行的 `unicode` 不再是编译期 E0106；真正执行时按普通未知名字抛 NameError。未知槽首次绑定为 None 后是已绑定的 Python None，不与 CLR null/PyUnbound 哨兵混淆。局部赋值的词法作用域规则不变，未赋局部读取仍是 UnboundLocalError。

固定 CPython 3.12 中已知而未实现的内建名称，在没有用户绑定优先命中时继续明确 E0100。**此处不是完整 builtin fallback**：如果已预登记同名模块槽（包括 global 声明或死分支赋值），除下述 Ellipsis 常量外，当前未绑定读取仍不会回退到内建；固定 CPython 3.12 内建名称的缺值分支（包括已实现的 len 和未实现的 locals）明确 NotImplementedError，表示尚未实现模块槽到内建的回退，不伪装成普通未知名字 NameError。正常已解析内建调用和已绑定用户槽不受此冷分支影响。`len` 前读后赋和未实现 `locals` 同名槽回退由独立 `m3_7_builtin_shadow_fallback_pending.py` 保留，这些未实现回退不可视为正确兼容。

本轮新增 Ellipsis 单例的窄回退（固定 3.12.13 差分与第五轮聚合门禁已通过）：同名模块槽保为 Dynamic，未绑定时读取返回真实单例，已绑定用户值（包括 None）保持原值；外部模块属性缺失仍抛 AttributeError。源码 `...` 直接取单例，不受 `Ellipsis` 名字重绑影响。此规则不将其它同名内建槽全部降为 Dynamic，也不宣称完整 builtin fallback 已完成。

## 静态值与绑定位

可静态定型的模块变量保留 i64/f64/bool/string 等原槽位，另有显式 bool 全局字段记录绑定状态：

1. 每次模块入口（包括初始化失败后的重试）先把绑定位置 false。
2. 值转换与 SetGlobal 成功之后，才把对应绑定位置 true。
3. 内部读取先调用 CheckGlobalBound，缺失时 NameError；随后立即将值冻结到同类型临时量，避免后续子表达式调用改变模块槽，错误地延迟读取。静态函数不为此装箱。
4. 写入协议在独立 MIR 处理中加入，再进行生成器降级，因此普通和可暂停路径一致。

动态槽继续使用现有值哨兵，不另增加绑定字段。现有类型分析在模块入口整体为 Dynamic 时会把模块槽统一动态化，以容纳 BigInteger 等值；此增量不删除该正确性规则，也不宣称混合动态模块已全部去装箱。更精确的逐写入分析是后续优化工作。

## 模块属性协议

MIR 的 ModuleExport 明确携带名字、值槽和 ExportGuard（None / ValueSentinel / BoundFlag）。链接阶段同时重映射值槽和绑定位；链接后验证值槽存在、值哨兵仅用于 Dynamic、绑定标记存在且为 Bool 并且不自指值槽，错误以 E0399 阶段诊断阻断。CheckGlobalBound 的 bool/string 参数及 void 结果使用也在 MIR 验证；后端只执行此协议，不根据 Python 名称猜测是否绑定。

模块 getter 对缺失值抛 AttributeError，而不是内部名字读取的 NameError；因此 hasattr 为 false、getattr 默认值生效，from-import 可进入原 ImportError 路径。外部 setter 先遵循静态异常导入保护、完成类型转换与存储，然后设置绑定位。None 是可导出的真实值。

## 成本与证据

每个静态模块变量增加一个 bool 字段；成功写入多一次标记，读取多一次检查和同类型局部保存。并未为所有模块值引入对象装箱，也未声称这项正确性检查免费。后续消除冗余检查必须以支配关系和跨调用可观察写入为依据。

验收文件：`m3_7_unbound_globals.py`、目录 `m3_7_unbound_exports`、`m3_7_static_global_bound.py`、目录 `m3_7_module_retry_bound`。静态 driver 测试检查整数槽与函数保持 Static、绑定位重置/成功存储后置位、读取检查及无 Box；types/MIR 快照保留结构。四条新增用例均已通过固定 CPython 3.12.13 与编译程序的真实差分并转正；三项 driver 测试通过，包含未实现内建回退的运行时明确拒绝。完整工作树门禁仍由 M3.7 聚合执行。

隔离实测见[全局绑定位性能记录](notes/2026-11-01-m3.7-global-bound-performance.md)：
真正 i64 只读循环每百万次访问，默认 tier 中位数 1.246→1.551ms，禁 tier
2.093→2.741ms，分别约增加 0.305/0.648ns 每读；两组均有正成本，不能全归噪声。
前后每次函数调用分配恒为反射返回的 24B，未增加逐轮装箱。含 global 写入的动态
函数另作对照，不作为 typed 检查的成本证据；原始 types/MIR/CIL 与全部进程数据已归档。
