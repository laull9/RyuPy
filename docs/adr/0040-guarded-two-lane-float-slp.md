# ADR-0040：受守卫的两lane独立浮点SLP

状态：有界向量化已实现并通过实际语义/JIT/性能验证；未获净收益，两变体均默认关闭。

## 目标与非目标

前三切已让标量循环暴露原生F64及有界数值列表，但实际JIT只有scalar算术；
AVX栈清零不是数值SIMD，16B引用/bits交错槽也不是连续double数组。
严格matmul归约不能树化或改为FMA，因为会改变每步舍入、NaN与正负零。

本轮选择SLP：把同一原顺序区域的两个**独立**同形算术链配成两lane，
不改变循环次数，不跨lane归约，不重写全部容器为SoA。普通Python源码不调用SIMD API；
编译器生成真正的Vector128<double>局部与BCL packed运算，C#运行时不解释算术。
这不是通用循环自动向量化，更不是通用动态union ABI已迁移。

## 有界匹配

- 两lane各2–8步Add/Sub/Mul，无inplace；两段连续、相邻，不能跨Call、Line或写入。
- 依赖拓扑包含**有序**lhs/rhs，不能把Add/Mul按交换律规范化；外部输入已在原位置求值。
- 所有中间/末局部匿名、唯一静态定义且全函数读闭合。链间依赖、输出别名或旧共享值拒绝。
  两lane可共享只读输入，但每个最终算术结果仍为自己的新盒。
- 两末值仅各作为同一fresh MakeList/MakeTuple的不同实参位置；容器构造不移动。
  未配对的第三项留原scalar位置，其调用/异常不得为配对前移；不是改循环尾次数。
- 拒EH、cell、env、生成器与跨块候选；最多4对、16总尝试（含失败），
  原与扩张后128块/512局部预算，不作全程序SSA。

## 表示与控制流

新增MIR的V128F64仅无GC引用的私有局部，禁止进入参数/返回/global/field/env、
装箱为Python对象或通用动态ABI。validator独立验证Pack两F64、向量二元同型且仅三操作、
Lane索引字面量0/1与F64输出，并验证硬件支持真边支配intrinsic。
codegen只机械映射System.Runtime.Intrinsics.Vector128及泛型方法实例，
不依赖HIR/ty、不增加运行时逐操作解释入口。

先冻结已有Dynamic输入并actual-double守卫，再检查硬件支持。
全真分支按原依赖/操作顺序Pack、逐步packed Add/Sub/Mul，末端各Lane提取并Box一次。
两位模式即使相同或NaN也不共盒；不FMA、不重结合、不规范化NaN/正负零。
任一失败则执行原两lane顺序，lane0抛错禁止lane1；没有已执行前缀的重复回放。
慢路末结果可经独立临时Use写回，让既有FloatChain仍有机会优化各条标量链，
不因两分支公共结果槽而强行丢掉已有标量优化。

## 先验验证

pending fixture包含非对称Sub锁lane顺序、舍入/抵消、不FMA、±0/inf/NaN、
两个独立最终盒与共享旧值拒绝、失败lane0/lane1协议日志、参数求值顺序及第三尾项。
另对不同quiet-NaN payload按位比较同编译器原scalar结果，而不是用nan==nan作验证。
硬件intrinsic关闭时运行同一真实fixture，证明ISA假边保原语义；不新增该模式计时组。
实际JIT必须出现packed数值指令且无FMA，API名字或xmm/zmm寄存器本身不是证据。

## 性能预算与停止规则

以当前已验收scalar FloatChain/FloatLoop/numeric-list为固定基线，先真实命中及语义，
再一组两tier、三进程×七样本。前置明确三臂：A旧编译器、B新SLP相同守卫/物化但直接
标量计算（`--diagnostic-scalar-slp`）、C真正packed。旧FloatChain可能拒绝连续独立链，
因此A到C收益混有分链/去盒，不能全归SIMD；只有B到C用于隔离packed成本。
控制开关是会话显式选项，不关闭整个进程硬件；HWoff只验正确性，不充作性能控制。
纯float、首/末类型失败、未命中与scalar尾项控制保留；
Pack/Extract、两末盒、ISA/类型守卫和代码增长全部计入，不宣称零成本。
资源、GC与actualJIT独立采样。最多一次具体根因修正，不为intrinsic上线追有利统计；
不能净赢则保原默认并透明记录实验。整个M3.10最终门禁独立，不能凭该方案勾完成。

## 首次正确性证据

MIR结构/验证器、真实PE泛型MethodSpec与CLI控制开关测试已通过。
真实pair/asymmetric/recurrence/with_tail强锁向量节点，shared/observed保原路径；
packed/scalar控制各在硬件开启/关闭时均与固定3.12.13完整差分一致。
独立测试宿主注入2000组位模式，调用真实编译pair与scalar函数，覆盖不同quiet-NaN
payload、正负零、无穷、极值及操作数反序；HWon/off均逐bit通过。
首fixture误用未实现的math.copysign，改用repr锁零符号且保更强位模式验证，不扩math API。

日志 `/tmp/ryupy-m3_10-slp-integrated2.log`、`/tmp/ryupy-m3_10-slp-ready.log`：
PE、driver、payload、全workspace clippy/Release和转正后的标准e2e通过。
非向量程序不额外创建Vector程序集/方法引用。该正确性阶段本身不证明性能，
不把BCL API存在或PE重读成功当作JIT向量化成功；随后实际结果见下节。

## 实际结果与终态政策

前置三臂完整语义、6资源、18时间、6 JIT进程全部通过。C的三个函数各有
3条vmulpd+3条vaddpd、3次pack与一次高lane提取，无FMA；是真packed数值运算，
不是寄存器命名或清栈指令。B/C分配完全一致，A→C主要去盒收益不能冒称packed独有。

C/B：list默认+0.62%、禁tier+7.39%，tuple+15.31%/+16.95%，tail约持平。
默认末custom A/B/C 9.547/17.594/21.138ms，B/C失败路径都显著退化。
禁tier代码list A/B/C 7121/7294/7243B、tuple7076/7204/7153B、
tail7173/7124/7088B；稍小未转换成可靠速度净益。完整证据
`tests/perf/results/m3_10/slp-first/` 保留全部不利数据，没有追加救测。

决定：默认不插SLP pass，亦不创建向量引用；只有 `--experimental-packed-slp`
显式启用packed，`--diagnostic-scalar-slp`显式启用公平标量控制，CLI互斥。
会话API同时设置则scalar优先，结构/CLI/driver锁定该规则。
首次归档的C来自原型默认阶段，当前复现需显式packed flag；原始哈希不事后改写。
这是“有限自动候选与真SIMD已实现验证，但净收益不足默认关闭”，不是纯研究，
也不是通用或默认SIMD完成。没有具体错误根因，不浪费预留的一次修正额度。
