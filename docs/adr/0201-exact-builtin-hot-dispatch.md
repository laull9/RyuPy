# ADR-0201：精确内建值的热点分派

状态：已接受，2026-11-15在macOS arm64完成逐片配对、26项保护及完整门禁。

## 证据与范围

原始records、comprehensions、list_ops与generators经真实NativeAOT构建，独立sample诊断轮
核对退出/双流，再用同次dSYM与加载地址符号化。短进程采样仅用于定位调用路径，样本数量有限，
不发布各层耗时百分比。实际命中元组键比较、集合方法通用分派、生成器比较与帧管理。
本轮选择前三项可闭合的局部规则，保持生成器状态机、异常上下文和帧生命周期。

## 三个切片

1. 精确PyTuple两侧相等比较直接遍历object数组，避开外层通用协议探测及IReadOnlyList访问。
   外层同一引用直接成功；元素依次保留身份捷径和完整PyHash.Equal，回调/异常/短路仍可观察。
   tuple须先比较共同前缀再比较长度，本次同时修复旧精确tuple提前按长度返回的副作用缺失。
   派生tuple继续走既有协议。
2. 已知0/1/2个位置参数的PIC入口，在普通对象PIC路径之后为sealed PyList/PyDict/PySet增加
   append/pop/get/add直达。正文仍用原容器方法，未知方法、错误数量、关键字、星号及派生
   布局保持原分派。尤其一个语法实参可以是星号标记，必须检查后才能绕过FlattenArgs。
   显式禁用PIC时仍走原路径，不新增缓存、scratch或强引用。
3. CompareValue两侧精确long时直接比较并返回规范bool单例，跳过foreign/普通对象协议探测。
   不做减法判大小，覆盖long两端；bool、BigInteger、浮点、foreign及用户富比较保留原路径，
   用户比较返回的非bool结果仍在条件出口才求真值。

## 兼容与取舍

不新增RuntimeFn/public ABI、CLI选项或值表示。全部守卫由CLR精确类型决定，不依赖变量名、
用户标注或不稳定缓存。保留普通对象方法缓存的原热路顺序，避免让实例PIC命中承担新分派。
无新增生命周期、缓存容量或释放策略；所有分配几何按实测登记。资源错误沿原容器正文传播。

## 验收

手写pending先行；同次CPython3.12.13差分、C#边界、NativeAOT删除输入搬迁及NumPy保护，
每片冻结旧新CLI进行原始输入独占对照，最终26项和make check均通过。
records/comprehensions/list_ops/generators最终耗时分别下降15.86%/12.85%/10.14%/5.49%；
RSS整体接近持平，bigint追加10轮仍增加1.76%，作为本批的局部性能代价保留后续优化。
接受三项默认实现，全部不利样本、精确测量协议和平台范围见
[验收报告](../aot-hotspot-dispatch-optimization.md)。
