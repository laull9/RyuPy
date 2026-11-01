# memoryview：一维连续 buffer 竖切

生产接点已齐套；原始用例与用户整数协议重入回归均已通过真实差分，用例已转正。
独立只读审查已确认重入及 traceback 阻断关闭；此子集完成不等于整 M3.7 已完成。
下列为固定 CPython 3.12.13 与 RyuPy 实际核验范围，不代表 chardet 已通过。

## 有限通用 API

支持 `memoryview(object)`（位置或 `object=`），输入为 bytes、bytearray 或本实现的视图；
一维连续格式 `B` 与 `c`，整型索引、步长 1 切片、迭代、单元素写、cast、release、with、
tobytes、tolist、格式/长度/shape/strides/readonly/obj 元数据及有限 buffer 比较/hash。

- `B` 索引/迭代产整数，`c` 产单字节 bytes；都共享 exporter 原始存储。
- `cast(format, shape)` 接受关键字；shape 未提供与显式 None 不同。多维 shape、其它格式与
  非连续切片明确 NotImplementedError；不以复制数据假装视图。
- `tobytes(order='C')` 接受 C/F/A/None：本子集布局中结果相同，但仍复制调用时实际字节。
- bytearray 的同长位置写入应立即反映到所有视图；readonly bytes 导出写入抛 TypeError。
- 活动视图锁定可变 exporter 长度，改变长度抛 BufferError；空操作仍允许。
- 切片/cast 独立持租约：父 release 不使孩子失效，孩子仍锁 resize；release 幂等，
  已释放的读取通常 ValueError。已计算的 readonly hash 保留；未计算的 hash 仍检查释放状态。

## 真实 buffer 与迭代协议不同

chardet 5.2.0 `charsetprober.py:122` 使用 `memoryview(buf).cast('c')`，以字节对象比较 XML
尖括号，再把切片传给 `bytearray.extend`。`extend(c_view)` 使用原始 buffer 可以成功，
`extend(list(c_view))` 则因元素是 bytes 而不是整数报 TypeError。
使用明确 `IPyBufferView.CopyBuffer()` 接口供原始字节消费者调用，不能只靠 `__iter__`。

`extend` 必须先消费并验证输入，再检查实际 resize：非法元素的 ValueError 优先于导出锁，
且不得继续消费该元素之后的生成器副作用。append 的整数转换也先于导出锁。
现有 resize 接点为 PyByteArray Append/Extend/DelItem 与 PySequenceOps 的 +=、*=；
`Bytes` 公开 List 的所有现有内部写入口已统一守卫，不能只保护新 memoryview 方法。
现有未支持的 bytearray.clear、bytearray 切片赋值和 memoryview 切片赋值不计作本切通过能力。

## 寿命与性能取舍

每个 view 直接保存原始 exporter、offset/length/format，不保存父 view；构造、slice 和 cast
本身不复制字节。原始复制 API 才分配输出；B 小整数迭代复用既有整数缓存，c 迭代需要单字节对象。
导出计数使用原子更新，显式 release 确定撤销当前租约，finalizer 最终撤销未显式释放的租约。

GC 释放时机沿 D001：不保证 CPython 引用计数销毁的立即性，包括
`memoryview(data).cast('c')` 中临时父视图的回收。因此未显式 release 的租约可能更晚解除，
但不能永久遗忘或通过复制绕过。显式 release 不依赖 GC；不为“解除锁”强制全局 GC。
尚未测量吞吐，不宣称新增协议天然更快。对 exporter 的 Python 可见别名语义不得以性能为由改变。

## 验证记录

`m3_7_memoryview.py` 与 `PyMemoryViewTests.cs` 覆盖共享写、独立孩子、释放/上下文、
raw c buffer、resize 消费与错误顺序、参数关键字、readonly、hash 与明确未支持布局。
固定 CPython 新基线 `/tmp/ryupy-m3_7-memoryview-cpython-final.log`；真实差分 unexpected-pass
记录 `/tmp/ryupy-m3_7-memoryview-reentry-results.jsonl`。专测 10 项、全 runtime 396 项通过，
Release 与 workspace clippy 全绿；`memoryview_traceback` driver 验证四种转换异常的真实退出报告。
对应日志 `/tmp/ryupy-m3_7-memoryview-reentry-all-runtime.log` 与
`/tmp/ryupy-m3_7-memoryview-traceback-driver.log`。
chardet 实际复测仍 unsupported：`codingstatemachinedict.py:10` 的 `TypedDict, total=False` 类关键字被拒绝（位于 TYPE_CHECKING 分支），不把新视图子集算作项目通过。

## 用户协议重入与错误边界

整数索引和值的 `__index__` 可以执行用户代码。读取在可表示索引转换后先检查原视图活性，
写入先检查索引边界、再转换值，实际写前重新检查活性，避免回调 release/resize 后使用陈旧存储。
切片分量按 step/start/stop 转换，转换期间独立持有临时buffer租约；即使回调释放原视图，
也不能改变exporter长度，合法连续切片仍可创建独立子视图。临时租约在成功或异常后都撤销。

bytearray 保留用户 `__index__` 异常；memoryview 的 B 元素转换按固定基线替换 TypeError、
ValueError、OverflowError，并清除被吞转换回调的traceback帧，其它异常原样传播。
`__index__` 返回 bool 的弃用警告路径暂明确 NotImplementedError，不静默吞警告。
