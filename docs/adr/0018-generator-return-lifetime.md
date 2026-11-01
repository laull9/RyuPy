# ADR-0018：生成器终值由触发完成的操作立即消费

- 状态：已接受
- 日期：2026-11-01

## 背景

ADR-0017 的 PC -1 已能区分 yield None 与正常结束。扩展 return value 时，如果在生成器里
缓存一个“待领取终值”，for/list 结束后的 next 可能错误拿到之前应被消费的值，并延长对象寿命。

## 决定

MIR 完成边设置 PC -1 并返回原终值；暂停边仍返回本次 yield 值。运行时恢复核心通过 out
参数交付仅属于本次操作的终值，立即关闭并释放帧；普通 MoveNext 丢弃 out，Python next
仅在本次结束且没有 default 时抛 StopIteration(value)，之后再次耗尽为 None。

正常完成的 StopIteration 在恢复核心的 PEP 479 捕获区外构造，不能误转 RuntimeError。
非 None 终值在异常 args 中保留为单个原对象，tuple 不展开；None 的 value 为 None、args 为空。
不增加生成器常驻终值字段，也不为普通迭代额外分配结束异常。
完成异常构造不能提前调用终值的 __str__；消息属性在格式化时才按当前对象内容计算，
一次 Render 只读取一次消息，不跨多次格式化缓存，避免副作用或可变对象内容失真。
仅未捕获异常的报告边界捕获消息格式化错误并显示 `<exception str() failed>`，显式 str 调用仍抛原异常。

## 边界

此切不扩展 send/throw/close 或跨暂停异常清理。一般异常构造的现有 ABI 仅携带首参，
尚不能保真无参、显式 None 和多参数；不能把内部完成异常的正确性冒称所有 StopIteration
构造已兼容。后续完整构造应保留实参数组，而不是继续用字符串或单值补丁。
