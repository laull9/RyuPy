# M3.10 SLP 首轮前置公平三臂

2026-11-01，一次预定两tier×3进程×7样本。所有语义/资源/时间/JIT通过，但**未建立packed相对同guard scalar的速度净益**；A→C去盒收益不能冒称SIMD收益。未自行追加救测。

## 三臂、来源与公平性

- A：保存的NumericList最终CLI ac88cf9a…编译的scalar baseline。
- B：当前CLI `--diagnostic-scalar-slp`，与SLP相同精确类型/ISA守卫、最终每lane Box和容器，算术用typed scalar。
- C：当前CLI默认packed。只是本轮候选默认，不代表最终项目策略已经批准。

三臂使用相同最终NumericList runtime0347f3878919bae370decd00533d6f322126285baf3f4bee5cd32ef04400f0a5，binary-hashes.json逐项核artifact及harness实际DLL。基点9023f21，baseline/source.patch与candidate/source.patch分别包含tracked+untracked生产完整改动，含候选诊断flag。性能源码单独归档，不在生产patch里。

保存目录：/tmp/ryupy-m3_10-slp-baseline-xlKt3V、/tmp/ryupy-m3_10-slp-candidate-XdWjc0。二进制只留/tmp，归档不含CLI/DLL/bin/obj。

## 有限命中与语义

slp_pairs.py普通Python函数循环30000次，每次fresh list或tuple输出两条同构独立6步乘加链；参数已求值为locals，第三元素scalar尾项保原。两lane共享只读b/c，不共享中间结果。首/末bool及自定义__mul__通过同list_pairs函数验证守卫失败；计数/身份类型/最终结果校验在time外。

固定13 --validate21轮与--diagnostic30000轮通过；三臂完整--diagnostic均与固定13一致；C额外HWoff语义通过，不增加计时分组。2000payload及异常顺序/IEEE特殊值由父driver已有结果提供，本轮不冒称微负载独自涵盖这些。

MIR：A无vector/ISA/精确floatguard；旧FloatChain不把连续两条独立链整体优化。B/C各3 ISA与30 exact-f64 guard（包括原fallback），B无vector算术；C三个函数各6 packed算术，总18算术/9 pack/6 extract。故B→C才隔离packed；A→C主要含减少装箱与去动态分派。

matmul/Nbody A/C完整MIR、CIL逐字相同，hash保留，未重复这些项目性能或重新报告收益。本轮仅SLP有限形状，不代表项目命中率。

## 时间 ms/30000次输出

每进程3次预热，7正式样本中位数，再跨3进程取中位数；ABC/BCA/CAB轮转。全部逐样本/逐进程数据保留。默认预热不等于无后台tier或PGO完全稳定。

|场景|default A/B/C|disabled A/B/C|C/B default /disabled|
|---|---|---|---|
|float_list|10.099 /4.254 /4.280|8.791 /4.301 /4.619|+0.62% /+7.39%|
|float_tuple|7.336 /1.075 /1.239|4.213 /1.078 /1.260|+15.31% /+16.95%|
|scalar_tail|11.485 /5.347 /5.286|7.995 /4.829 /4.854|-1.13% /+0.53%|
|first_bool|12.618 /8.538 /8.981|8.891 /8.526 /6.890|+5.19% /-19.19%|
|last_bool|9.992 /8.554 /7.932|9.420 /8.372 /8.170|-7.27% /-2.41%|
|first_custom|21.720 /18.500 /18.553|11.903 /11.025 /11.834|+0.28% /+7.34%|
|last_custom|9.547 /17.594 /21.138|14.790 /11.638 /13.120|+20.14% /+12.73%|

默认last_custom B/A+84.28%、C/A+121.40%，明确保留；不能只看纯float去盒收益批准整体默认。纯tuple B→C约多5.49ns/输出（default）和6.09ns/输出（disabled），这不是仅xmm出现即必然加速。

## 独立资源

6个独立资源进程，每场景3预热+3样本，JSON等全部资源采样后输出。时间进程不读分配/GC/JITcounter也不开disasm。
两tier结果一致，B/C所有场景分配完全相同：
- list A12,960,064→B/C5,760,064B。
- tuple A10,560,024→B/C3,360,024B。
- scalar tail A14,160,064→B/C6,960,064B。
- first/last bool A12,960,064→9,360,064B。
- first/last custom A15,833,920→12,233,920B。

每lane最终盒和输出容器保留，packed自身没有额外分配收益；上述减少全是两臂共有的去盒/纯链降低。

## 实际JIT

独立6个JIT进程保留default全部Tier0/OSR/Tier1与disabled FullOpts。C三个函数的快边确有各3条vmulpd、3条vaddpd，3条vunpcklpd pack与1条vunpckhpd高lane提取；不是xmm清栈冒称SIMD。未见FMA，第三元素仍scalar。

禁tier代码字节：
|函数|A|B|C|
|---|---:|---:|---:|
|list_pairs|7121|7294|7243|
|tuple_pairs|7076|7204|7153|
|tail_pairs|7173|7124|7088|

C比B略小，但本次计时仍没有可靠packed净益；数据不能归因“代码大小必然决定速度”。pack/extract、最终Box/容器与失败守卫仍有成本。保完整fallback机器码与方法hash，不挑最优版本。

## 复现与终态

三臂编译命令分别为保存旧CLI build、当前CLI --diagnostic-scalar-slp build、当前CLI build；全用同runtime。资源harness以--diagnostic初始化真实输入，CreateDelegate调用真实run/reset/check。run_three_arms.py ROOT resources/time/jit依序执行，目录必须不存在；summarize.py聚合全部原始样本。
本轮脚本沿用前轮通用三臂框架但只运行micro；残留未执行matmul分支不表示重新跑了原matmul。HWoff仅validation。

资源6+时间18+JIT6全部exit0，18:34:10 UTC JIT终态后立即释放父窗口。之后仅只读分析/文本归档；是否唯一修正或保实验由父决定，不自行第三方操作、提交或重测。
