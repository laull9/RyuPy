# 整数下标优化：实际热点与实施准入

状态：规划。当前sum候选独占构建计时，本文无产品改动或新benchmark。
固定sieve源的i/j处于Dynamic上下文；j += i通过动态整数加法生成对象，随后下标再解包。
只加I64下标ABI不能消除其生产端装箱。动态字面量也已由build_literal/boxed_const变为Dynamic，
只在build_subscript检查operand_type==I64未必能命中flags[0]/flags[1]，须真实管线结构证明。

## 调研报告纠正

- PyList为sealed，PyListInstance是独立包装，不存在PyList的继承子类；is PyList已是精确该布局，
  无需GetType反射式判定。子类/foreign仍须按各自协议处理。
- 新I64入口的慢路若用GetItem(obj, index)触发C#原始装箱，会绕过PyInt.Box小整数缓存。
  用户对象可以观察key身份，必须用规范BoxInt，并保留已有Dynamic key的原引用。
- 数值F64守卫对bool不成立，也不提供原生i64参数ABI；只可借鉴守卫支配/写入失效的证明模式。
- 按原筛法的数学计数：N=2000000，所有p<=sqrt(N)质数的floor((N-p*p)/p)+1之和为4347778次写入，
  撤回约600万次。乘24字节为104346672字节的装箱上界估算，部分小整数命中缓存；
  该乘积不是实测分配量，实际总量仍读取独立统计轮。

## 推荐下一条完整候选

优先研究受守卫的动态while计数器小竖切，直接覆盖原始sieve内层：

    while j <= limit:
        flags[j] = False
        j += i

准入须从真实MIR证明循环拓扑、唯一计数器定义、循环不变bound/step、精确PyList及bool值，
无用户调用、反射观察、cell、异常区或侧退出。运行期验证j/limit/i均精确long且i>0；
任一失败完整回原CFG，不猜变量名、不截断BigInteger或bool。
快路内部原生long计数和原生索引写入，结束时按需物化最终j；零次迭代保留原对象身份。
加法必须检测溢出并正确物化/回退，绝不能unchecked环绕或把CLR OverflowException直接外露。
若依赖精确正步长与long bound证明溢出后循环必退出，也须证明最终j的Python任意精度值与可见性。
下标负数与越界沿原Python异常，尤其错误前j的物化及异常回溯/外部观察不得丢失。

如果需要新的SetItemI64 RuntimeFn，应作为这个完整循环切片的配套，而不是单独上线无热点命中的基础设施。
必须同步Runtime、MIR枚举/参数/返回验证、codegen引用与stackify分派及ABI结构测试；
codegen继续只依赖MIR，不引入HIR/ty。先限定一个Set写通道，Get另按实测需求推进。

## 准入与回退方案

该循环CFG变换属于重大编译器决定，实施前需异构审查证明，当前只是待审设计。
需要原始sieve真实源结构命中、非匹配/反射/cell/EH/非long/负步与溢出反例、CP同次差分、
真实AOT/搬迁和26项保护，所有机器产物外置清理。速度和内存收益尚未测量。
若无法形成小而闭合的证明，优先评估Runtime精确PyList+long key的窄分流，保留Dynamic key原引用与全协议慢路；
它可以减少分派，但不会移除加法装箱，须另作测量后取舍，不能借此宣称已解决主要分配。


## 待异构审查的更窄无异常快路

首个完整切片可进一步限定普通函数局部的非空循环：预头逐步验证flags精确PyList，
j/limit/i精确long、0<=j<=limit、i>0且limit<flags.Count；类型成立后才读取Count，不能非短路求值。
任一守卫失败回原CFG，因此零次迭代也沿原路径，保留原j对象身份。
循环体仅允许字面量False写入flags[j]和j+=i，无列表resize/回调/反射/cell/EH/product/侧出口。
flags、limit、i整个循环不变，j单一定义链且无中途观察。
由0<=j<=limit<Count证明整个快循环下标合法，避免为了错误时物化j再引入额外EH。
原生加法检测溢出：正常返回原生状态；正溢出后数学最终j>long.MaxValue>=limit，
应精确构造最终BigInteger并退出，不执行截断后的比较。结束物化j一次，别名仍指向原不可变值。
必须审查是否有外部locals/traceback观察或跨块活性使上述延迟写回不成立；有疑点拒绝匹配。
该收窄计划仍无实现、无收益保证；审查后再决定是否实施。
