# ADR-0162：以真实 sortedcontainers 入口驱动通用依赖竖切

- 状态：实施中；不宣称项目通过或 ABC 可继承已实现
- 日期：2026-11-12

## 背景与决定

普通 MI 接入后，固定 sortedcontainers 2.4.0 从编译拒绝推进到实际运行时
`ModuleNotFoundError: No module named 'traceback'`。这是暴露后续旧缺口，
不是已经证明 C3 产生错误结果。固定入口、sdist SHA256 和黄金输出保持不变。
不能逐次用空模块、空 ABC 或绕过保护让下一个错误出现，随后称为兼容改善。

本轮先实现可以独立与 CPython 差分的标准库行为：bisect、starmap、operator
所用运算、有限 traceback API、sys.hexversion 和 _thread.get_ident。
各 API 必须有真实行为和失败路径；模块其余属性保持缺失，不冒充完整标准库。
项目仍以原始包全部导入、原始入口执行和完整 20 项回归验收。

## 已知完整失败链，而非只看第一个异常

锁定源直接依赖 traceback、bisect_left/right/insort、itertools.starmap、
operator 的比较/加法/增量加法、sys.hexversion 和 _thread.get_ident。
除 traceback 已在原 CI 实际失败，其他导入缺口最初由源码与注册表确认，
应与后续真正执行的失败区分。

类协议方面还有独立而更大的缺口：

- MutableSequence 等当前是仅参与类型判定的 PyNativeType，不支持作为用户基类。
- 普通 MI 的 C3、查找 owner 和 super 共同以 PyClass 为权威；直接接受任意
  native base 会丢失祖先和方法，不是正确的兼容实现。
- KeysView/ItemsView/ValuesView 需要真实继承初始化、_mapping、长度、迭代、
  成员判断和实时反映映射变化。原 SortedDict.items() 即依赖这些。
- 原源码三种 view 含空 __slots__；不能简单忽略，否则无字典类会获得字典。
- 自定义 __new__、decorator、SortedDict 的 dict 布局与上述类族需要共同验证。

## 下一竖切的架构约束（尚未实施）

引入无特殊存储布局的 ABC 类族时，必须维护唯一公开类型身份，以及统一的
类拓扑、方法 owner 和 C3 权威。不能用隐藏代理类把 __bases__/__mro__ 悄悄改写。
继承混入方法、抽象约束、nominal/structural 类型判定、现有泛型订阅及空 slots
布局须一起验证；其他 native 特殊布局继续明确拒绝。

`m3_8_abc_inheritance.py` 是已在固定 CPython 验证的 pending 合同，覆盖上述
核心语义，不计入已实现或编译 e2e 分子。后续应再验证真实项目的 key 分派、
live views 和 SortedSet 的 set/sequence 双协议。

## 首批已落地行为的边界

sys.hexversion 与既有版本目标 3.12.13 final 0 一致，不宣称 CPython 实现身份。
_thread.get_ident 返回当前实际 CLR 线程的非零标识，在存活线程间唯一；数值
不应跨进程比较，也不冒充 OS native id。没有新增 Python 线程创建、锁或 GIL 支持。

## 验收口径

局部 API 差分通过不能增加项目通过数。pending、桥接、改源副本、忽略项及
只通过导入均不计为原生项目成功。完整检查前不提交为已验证候选；main 不变。

## 依赖层本地验证（2026-11-12）

最终 `make check` 通过：C# 2913 通过、2 项既有未执行；编译 e2e 406 通过、
280 项忽略/pending。新增 5 个真实差分入口；ABC 与旧算术反射优先问题仍是
明确 pending。public ABI 仍为 1718。此处不是新的完整 20 项矩阵或 AOT 验收。

原 sortedcontainers 2.4.0 sdist 的 SHA256 与 lock 一致，固定 CPython 原入口输出
逐字等于原黄金。修后原包编译及实际 JIT 已越过导入缺口，但在 sortedlist.py:85
`SortedList(MutableSequence)` 明确拒绝运行时 ABC 基类，仍为 failed，不增加通过数。

第一轮完整检查暴露的特殊比较分派与精确 PE 桥接签名问题已修复，旧保护断言保留；
另经固定 CPython 实测修复 traceback 无活动异常时的 limit/chain 副作用顺序，
最终完整检查针对修后源码重新执行。没有以此前局部绿灯替代最终回归。
