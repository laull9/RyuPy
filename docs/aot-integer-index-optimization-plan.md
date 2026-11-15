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

## 异构审查后的主代理核实

审查有条件支持上述语义范围；以下源码事实作为实施依据：
- `operations.rs` 的 `DynOp::Le.code()` 确为4，`expr_ops::compare_values` 发射
  `DynRichCompareValue` 后由条件位置转Bool。匹配使用枚举code与真实管线测试，不散落魔数。
- MIR `Body` 没有 `function_locals` 字段；它属于HIR。MIR准入应仿照既有累加器扫描
  FunctionLocalsValidate/Ensure/Sync调用，并排除env/cell/EH/Suspend/MakeGenerator/product。
- while原始构建为条件/体/else/exit，体直接回条件，不能照搬ForRange五块latch模板。
  SimplifyCfg可合并空else；识别当前默认优化阶段真实输出，允许非entry嵌套预头。
- 异构列表仍是合法精确PyList，字典也可能合法支持整数赋值，不能写成必抛TypeError的负例。
  非匹配路径应同次CP差分其真实行为，用户对象的__le__/__setitem__/__iadd__次数及顺序不得改变。
- 原溢出样例limit=long.Max不满足limit<Count，无法覆盖快路。应使用短列表、j=1、limit=1、
  step=long.Max，快路一次合法写后得到9223372036854775808；并保留守卫失败的大bound测试。
- PyList数值背板写bool可能经Items物化分配；因此“无分配/无任何异常”不是一般PyList的事实。
  首次仅限紧凑bool或普通object背板，或明确沿既有存储物化合同并在ADR登记资源失败边界。

## 最小ABI的运行时完整循环备选

可评估一个内部编译器ABI `GuardedListSetFalseLoop(list, start, bound, step) -> object?`：
Runtime内部逐层类型/值域/存储守卫，失败返回null且无写入，成功执行完整原生long循环，
返回规范最终整数（溢出用既有任意精度加法）；整数结果永不null，因此无需新建结果对象或out ABI。
MIR保留全部静态准入证明，在原preheader接一个调用与null分支：失败原cond，成功赋j后原正常出口。
这将类型守卫、Count和原生下标细节封装在Runtime，不需分别新增多个类型检测/Count/SetItem ABI。
它仍是完整热点切片，必须确认调用前读取局部不提前触发LoadChecked/协议、副作用不重复、
内部原生循环不丢常用观察与错误位置；未通过该备选的审查前不实施。

## 本轮审查结论与交接修正

2026-11-15最小ABI异构复审支持进入下一轮候选；本轮按用户要求停止在设计阶段，没有新增RuntimeFn或Pass。
主代理补充以下硬约束，取代审查报告中过强的完全保真/无分配表述：
- 精确long守卫必须使用`value is long x`；`PyInt.TryI64`同时接受bool和int，不能直接代替。
- 最终Box及正溢出BigInteger构造会分配，可能遇资源失败；只能对循环内有效背板写入说明无逐项装箱。
  资源失败时的局部j/Line写回差异需单独ADR接受，不能以“绝不抛异常”跳过。
- 无名临时槽也可能跨块读取；必须穷尽实际Statement/Terminator读集合，拒绝出口或环外仍读取的被跳过定义。
  不从name=None推断活性。普通函数参数身份限制按方案执行，不能凭未实现的Pass声称已证明。
- `_numeric`为PyList私有字段，PyOps不能直接访问；拒绝该背板的逻辑应在PyList内部helper实现，
  对codegen所需的public编译器ABI保持人工维护及ABI计数测试，不能为一条快路公开存储字段。
- RuntimeRefs/方法解析/stackify分派按实际模块核对，四参数Dynamic固定ABI的枚举穷尽匹配全部同步。
- null比较可复用现有BinOp::Eq和Constant::Null，但一个新call/分支仍有成本；不声明零开销。

下一轮先落手写e2e pending和真实原始sieve源管线结构断言，再实现与评测；
普通对象/字典/异构列表各按其真实协议验证回退，未绑定顺序、别名、零轮、overflow、locals/cell/EH、
外部容器与错误栈分别覆盖。当前没有性能收益验收。
