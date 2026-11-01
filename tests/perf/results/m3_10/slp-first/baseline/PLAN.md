# SLP 两lane有界验收预案（仅准备，未运行）

基线保存当前已门禁的NumericList CLI ac88cf9aa8999327da5628037e74e857653580a894837a05e57f88431f3fdbbe，与最终runtime0347f3878919bae370decd00533d6f322126285baf3f4bee5cd32ef04400f0a5。
完整source.patch包括tracked+untracked crates/runtime；独立保存，不覆盖前一轮候选。

## 有限源码

slp_pairs.py：普通无注解函数循环30000次，输入参数已求值为locals；每lane是6步同构独立mul/add，最后构造fresh list或tuple。第三尾项a-d仍scalar。两lane共享只读b/c输入不共享中间值。每次结果容器确实构建，不按用户SIMD API手写替身。

七场景：float_list、float_tuple、scalar_tail、first_bool、last_bool、first_custom、last_custom。同list_pairs函数既命中又失败，防止只测永远成功独立函数。自定义__mul__按原lane顺序调用一次，返回float使剩余算术合法；计数每次count，检查保留最终两个float值、容器类型、尾项。结构/错误顺序/IEEE反例另由生产e2e锁，不用此微负载替代全部语义。

固定13 --validate为21轮，--diagnostic为30000轮仅语义（待正式窗口执行）。所有输入构造/reset/验证在time外；同进程3次预热+7次正式。最终容器/每lane最终盒仍真实分配，不能把packed arithmetic宣称零分配。

## 一次前置公平三臂（计时前决定，不是事后救测）

A保存旧scalar baseline；B新CLI --diagnostic-scalar-slp（同SLP guard/最终Box/容器，typed scalar）；C新默认packed。三臂独立目录、同runtime，default/禁tier两组，各3进程×7点，ABC/BCA/CAB轮转；只一组预设，最多父授权一次具体根因修正，不无限救测。每次保存stdout/stderr/exit/wall，不能只报最优点。

正式计时前必须实际MIR/CIL确认Vector128/ISA guard及scalar fallback、尾项保留；不能只凭源码认为命中。scalar基线使用保存旧CLI，不当前重编。若runtime变动先报告并统一两方DLL；不混入旧新runtime成本。旧FloatChain拒连续独立链，A→C可能包含去盒收益；B→C才隔离packed算术，不把A→C全部归SIMD。HWoff仅语义验证，不新增时间分组。

资源与actual JIT各自独立进程：真实allocated bytes/GC/JIT计数；default全部Tier0/OSR/Tier1、禁tierFullOpts，核实际packed mul/add而非只出现Vector128类型，核ISA失败原路径/无FMA/顺序，统计代码体积、static命中/未命中、pack/extract开销与每lane最终盒。只看xmm清栈不算packed命中。候选若只增加guard却无packed机器码不能称SIMD收益。

原matmul/Nbody仅比较MIR/CIL与语义，若未变化不重复大量进程；整M3.10最终suite另统一一次。结果只代表30000次容器输出微负载，不外推真实应用整体。
