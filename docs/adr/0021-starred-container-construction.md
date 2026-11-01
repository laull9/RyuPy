# ADR-0021：星号容器保持最终类型与流式集合插入

- 状态：已接受（缓存hash复制明确未支持）
- 日期：2026-11-01

## 背景与决定

旧通用星号展开先建list，却把该list直接当tuple/set结果返回。tuple可以用可变构建列表累积，
最后一次冻结为tuple；set不能最后统一转换，因为hash副作用/不可哈希错误必须发生在后续元素
或yield之前，普通iterable还要求每个next后立即hash/插入，不可先拉取整个序列。

MIR容器构建独立于通用ExprBuilder。star set在第一个star前求全部前缀元素，再MakeSet；随后
普通元素立即SetAdd，星号源用SetExtend流式迭代。临时容器引用随现有生成器帧保存，恢复不重新展开。
这保留CPython当前前缀建集合边界，避免每个元素创建环境或中间列表。尚无速度倍率测量。

## 缓存hash的明确边界

CPython从set/frozenset展开可复用源缓存hash，而目前HashSet<object>公开API不能读该hash。
.NET10实测UnionWith对空/非空目标都重新计算hash，复制构造虽能复用但无法完成非空目标通用合并。
不能把额外用户__hash__调用当作无害性能差异：它可能有副作用或在源构造后改成抛错。

本切对set/frozenset源包含用户实例或绑定PythonClass的用户异常（递归tuple/frozenset）在复制前
显式NotImplementedError；检测不调用hash/equality。普通iterable用户键照常逐元素hash，内建键
集合源正常支持。这里登记的是未支持，不是接受静默语义偏差；期望行为保留pending差分。
后续若引入缓存hash可复用的集合存储，应独立ADR、完整容器回归和性能测量，不在此切宽改HashSet。
