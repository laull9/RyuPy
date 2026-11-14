# sum流式迭代：实现与验收

2026-11-15，默认启用。BuiltinSum通过一次迭代器创建逐项取值并立即执行普通二元加法，去除累加前的全量物化。

## 行为与边界

生成器yield与元素__radd__严格交错，加法失败后停止取下一项；列表追加在同次迭代中可见。
空输入保持start对象身份，自定义start使用__add__。不引入原地加法、数值去装箱或新运行时ABI。
内部object数组零复制地包装成PyTuple后使用既有游标，保留原入口的接受域。

沿用现有参数解析，已登记两项既有偏差：str/bytes/bytearray start缺少CPython专门拒绝；
位置与关键字重复start缺少特化报错。见semantics.md §4.15，后续独立修复。

## 语义与交付验收

- 9项定向C#测试通过：真实sum入口、有状态迭代器与加法回调，覆盖严格交错、早停、start身份、活列表、普通加法及内部数组/元组/浮点/大整数。
- 源E2E即时CPython3.12.13/JIT差分通过并转正。正式CLI真实NativeAOT在源码和构建目录隐藏、空PATH、中文空格路径搬迁后退出码与双流一致。
- 迭代器抛错和加法回调抛错两类未捕获异常比较完整stderr，仅规范本次源路径、对应源码回显与caret；退出码、stdout、帧顺序、行号、函数和异常消息一致。
- NumPy2.3.5的int64/float64数组迭代求和、空输入start，固定CPython与显式随包provider NativeAOT对照通过。全部私有发行/NumPy/source/build输入隐藏后，空PATH搬迁双流一致。这是对象桥功能验证，另行开展桥接性能测量。
- 完整make check退出0：CPython基线744通过；C#3134通过/2跳过；编译e2e470通过/284忽略，Rust全套及严格Clippy通过。

## AOT性能对照

固定CPython3.12.13、Apple M5/macOS arm64/.NET10.0.403。分页版冻结CLI与流式sum候选采用1warm5交替采样，墙钟/RSS无插桩；分配和GC来自独立统计轮。
六项hello、bigint、containers、strings、comprehensions、generators的双流、退出码和发布目录搬迁均通过。

| generators | 旧sum | 流式sum | 同轮CPython |
| --- | ---: | ---: | ---: |
| 墙钟中位数ms | 416.85 | 336.40 | 112.68 |
| 峰值RSS MiB | 83.63 | 21.91 | 14.78 |
| 累计托管分配MiB | 185.06 | 161.42 | — |
| 独立统计轮GC暂停ms | 69.68 | 1.53 | — |

其余五项对照未见明显变化。独立统计轮反映该进程的GC状态，墙钟还包含运行时调用、分派和启动成本。

## 当前完整26项

冻结候选（aa9fa3a8+sum源码）另跑原始完整26项及PIC关闭变体，全部通过，排除startup的CP/Ryu几何平均1.1290×。
按本轮中位数，速度不高于CPython为13/26，RSS不高于CPython为3/26，同时满足为2/26。
hello波动49%、containers波动12%保留标注。固定版本、直接脚本协议和原输入保持，编译期沿现有默认配置。
当前逐项速度和内存目标继续推进，下一优先项为推导式内部追加与sieve容器构造。
