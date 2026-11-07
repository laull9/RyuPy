# ADR0135：自动 P1 模块内严格 ForEach 局部累加器实验

状态：由已恢复 a485 基线重建，默认关闭。本轮功能与项目验收进行中；旧 f064 的验收结果不转记为新结果，不作性能或默认晋升判断。

## 背景与范围

真实模块环境把 range 也恢复为当前 globals/builtins 的真实读取和调用。生成的是通用
IterStart/IterNextOk/IterValue，而不是按拼写跳过 Python 调用的计数循环。原累加器
matcher依赖独立latch；SimplifyCfg会折掉ForEach空latch，故原优化不命中。
不能仅放宽五块约束，更不能恢复按range拼写绕过绑定、缓存全局值或扩大P1结构族。

新增 Session.experimental_foreach_accumulator，默认false，无公开CLI模式。只有编译
feature ordinary-dynamic-module-pilot、原实际计划automatic=true且InstanceGlobals
与新选项同时成立才插入新pass。旧显式instance、entry/package、class、完整签名、固定
绑定与AOT不因本切扩大。driver build/dump共用纯选择函数，reason只是审计字符串；link
不再决定优化。dump本身的单文件入口角色不冒充普通imported模块。

新选项与experimental_direct_accumulator同开在读输入/写产物前明确拒绝。MIR流水线
底层另保direct优先的防线，不能叠加两种表示；既有pipeline wrapper统一传新选项false。

## 最小表示与证明

新ForEachNumericAccumulator插在SimplifyCfg后、首次DeadValues前，保留simple local
目标的原证据。它不改旧NumericAccumulator matcher，复用既有holder RuntimeFn ABI，
不新增runtime类型、缓存或直接typed算术。

候选限定入口P、header H、body B、出口X四个不同角色及唯一TracePop/return epilogue。
P无前驱，H仅P/B两前驱，B仅H，X仅H；B直接回H。P中原IterStart定义唯一iterator，H
恰一次IterNextOk并以其bool分支，B以同iterator的IterValue及普通local目标赋值开始。
Next/Current/callee/args/global读取和Line的次序、次数、位置均不变。

body尾部唯一DynBinary(Add,inplace=true,total,rhs)紧邻唯一total回写；tmp全函数仅该
回写使用。total的循环内唯一读是此lhs，唯一写是回写；递归数组及ZeroCallFrame等隐式
operands都参与exhaustive读取。普通+不是+=，必须拒绝inplace=false和其它运算。

total/目标不是cell、context、owner或协议临时槽。整函数拒env、EH、Suspend/生成器、
FunctionLocals、Cell、LoadChecked/MakeUnbound及Unpack协议。序列for目标有命名隐藏
local，不能只凭name.is_some称其为源码simple target；按实际UnpackSequence/Ex/Check
ABI保守拒绝，不消费内部名称拼写。

种子绑定按entry顺序跟踪最终写入，只接受参数/非null常量经Use/Box得到的肯定绑定；
任意Call、MakeUnbound、LoadChecked或“曾有Assign”都不是证明。不把未绑定lhs读取推后
到RHS回调之后。其它初始化形状保原路径，不推断参数注解是实际long。

X只接纳return total搬运与已知epilogue。非空else、break/continue分支、多入口、多出口、
后续循环、中途读/逃逸或更新后副作用保原路径。源尾部无条件continue/空else若归一化
后与普通循环完全同形，则以实际CFG证明为准，不假装仍能从MIR判断原语法。

变换仅为：P全部原语句后Create(total)，原Add/回写处Add(holder,rhs)，唯一X消费前
Value(holder)回写。原Current即使结果没用也保留；后续DeadValues只能删除已无观察的
纯Use/Box，不得删除Current调用。

## 运行时与观察边界

当前holder保初值和慢路返回对象的原引用。仅实际long+long且不溢出时延迟物化；bool、
BigInteger、float/custom及溢出在原加法点走原inplace动态协议。失败保旧绑定，不回滚
用户__iadd__已经改过的对象；外部初值alias不是禁止条件。空循环必须返回原seed，慢路
返回已有整数盒也必须保身份。普通+=与普通+、mutable alias、副作用、异常位置不能混同。

严格“不逃逸”指当前局部绑定与holder不可被现有受支持观察机制看到，不是完整Python
frame反射保证。当前_getframe/settrace/traceback locals尚不支持。未来开放callback读取
或写入caller frame locals时，必须在观察点物化或禁用本优化，并重审旧ForRange holder。
不能随P1预检放宽而静默继承该证明，也不为尚未实现的能力发明全局运行时开关。

当前未支持caller frame、traceback locals、trace、asyncio、严格右子类反射优先及
range用户__index__。新矩阵将这些CP断言与native既有边界分别登记，不计CP通过；
本切不修通用二元协议/标准库/前端，更不修super或NoReturn。

## 持久验证与后续门

- 新运行MIR真实源与独立合法变异，先validate输入再核matcher原样拒绝及输出validate；
  非法IR被validator先拒不能充当matcher拒绝证据。另核del、隐式读、EH/cell/yield/解包。
- driver测试默认off、feature-off、真实automatic门、旧explicit-instance、AOT/fixed、入口
  与包/class/full-signature及direct组合，在实际linked MIR核精确holder调用。
- tests/fixtures/foreach_accumulator_reconstructed使用完整可执行新源，逐case独立固定CP
  oracle、同P1只切新bool及真实PE执行；旧失败、前端NI、planner未选与matcher拒绝分账。
- 同源同工具off/on逐方法PE审计，核Create/Add/Value命中、负例拒绝、原迭代和名称/callee
  位置；独立Current可变/抛错原生探针覆盖未使用item的真实调用。源码MIR不替代PE证据。
- 完整make check与固定20两配置各一轮，九protected逐ID保持；progress显式super NI仍
  独立阻断promote。默认off全门不能代替实验on项目入口覆盖。

没有计时或分配采样，不宣称速度收益、默认晋升或新增项目分子。资源与噪声验证需另行
固定协议。回退须停止候选进程并恢复完整源码与匹配工具/产物，不能热切存活globals/owner。

## 本轮来源与结果隔离

基线为实际恢复的a48574b0100acb0097a69e137d42239e14ea1b0d，不能称为旧f064恢复。
部分已保全原文按独立清单核SHA；其余实现、测试和文件属于新重建。只对核验的单文件
陈述历史字节一致，不从片段hash推导旧整树完整。每次构建/测试保全原stdout、stderr、
退出状态、源码与工具身份；失败不覆盖，最终通过数只来自本轮实际执行。

独立重建复审发现原保全analysis把合法MIR的Array种子也递归视为已绑定，超出本实验
只允许参数/非null常量经Use/Box的范围。本轮改为保守拒绝，并为合法空数组、嵌套数组
增加独立红测；不据此声称发现真实Python输出回归。此文件最终实现属于改进后的新源，
不能继续把analysis最终hash说成历史原文一致。已知语义失败的可运行fixture仍须满足
同样的holder结构断言；仅精确前端NI没有PE，单列诊断核对，不充当matcher拒绝。
