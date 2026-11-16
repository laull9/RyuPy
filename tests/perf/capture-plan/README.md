# 不可变捕获计划 Runtime 原型（未接编译器）

此分支仅探索捕获时缓存类函数与 owner、调用时传入已保存 receiver 的成本。
不改 MIR、RuntimeFn 或 codegen，不能声明产品已获得性能改善，也不合入 dev。

`run.py 仓库外新目录` 使用固定 .NET 10.0.403，四次独立构建 JIT/NativeAOT，
每次 PIC=1/0 各一个进程；JIT 再分正常分层和禁用分层两个独立进程配置。
每进程在同站点至少三秒预热后计时二十轮，四臂正反交错。禁分层仅作诊断，
不得当作默认产品配置；预热时长不自动证明稳态，仍需审查逐批漂移。
每批十万次调用，参数提前装箱。记录毫秒与当前线程分配；只覆盖普通单态实例单参函数。
late 臂是现有延迟绑定 PIC，仅用于无参数副作用的成本参照，不是正确求值顺序方案。
bound 臂是 e2d2667c 单数组捕获方案，plan 臂是新不可变计划。
构建日志、完整进程双流、退出码、源码 SHA/tree、入口摘要及发布目录保留为 CI 证据。

语义由 Runtime 合同测试检查，默认完整门禁外另用 PIC=0 定向运行计划合同；编译器实际求值顺序、完整 Python 差分、星号/关键字、
多态退化、特殊容器、真实项目、跨平台和干净机器搬迁仍须独立验收。
缓存仅有界持有类/函数/owner，不持 receiver；弱引用测试不代表所有对象全局无滞留。
未认证函数每次调用仍使用独立参数数组，防止任意 Body 保存数组或重入破坏前次参数。

## 首轮原型观察与测量修订

8c8ee698 隔离 CI 43856241940 完整门禁与 PIC0 合同通过。四轮 NativeAOT
PIC1 相对 bound 臂耗时降低约49%–51%，每调用分配80降为40字节；仍比
无实参副作用的 late 臂慢约88%–93%，因此不得晋升产品。JIT 原始批次出现
明显后段加速，原两批预热不足，不能把其总中位数解释为稳态性能。
本次仅延长诊断预热并加入禁分层对照，不修改 Runtime 和编译器。

任意 PyFunction.Body 可保留实参数组，因此当前ABI下不能通过共享scratch或
ArrayPool减少该40字节。消除此数组需要独立的标量调用入口及明确创建者合同，
并维护默认值、签名错误、模块owner、闭包、super和回退行为；产品尚未实现；下节只描述内部手写委托实验。

## 内部标量能力实验（仍非公共ABI/编译器实现）

原Body及其Target保持不变，内部弱键表一次登记明确创建者提供的标量委托，
计划miss时读取能力。总位置参数含self为1–4且无星号才可使用；其它情况回退。
owned函数从原Body.Target取得定义时context，不按当前模块名重找环境。
专用friend程序集仅供此host消费内部入口，不修改公共函数构造器或注册方法。

scalar臂使用手写静态委托，与其它臂拥有等价返回身份；不证明编译器emit、
签名错误、来源帧或真实Python业务路径已经接入。无能力plan臂继续保留，观察
新增判据成本。注册是内部可信创建者合同，不是对任意Body进行逃逸分析或等价验证。
弱表测试覆盖值闭包回指弱键的回收；站点缓存仍有界强持有函数及其既有闭包。
未来生成的标量委托应为静态core入口，只使用真实function/context，不引入额外receiver捕获。

合同测试补充四个arity、原始数组回退、同站点重入与重绑、super异常恢复、
owned globals/builtins/cells/元数据身份、弱键循环回收及重复登记拒绝。
完整Python差分、错误来源与动态默认值绑定仍是后续编译器接线验收，不以本host替代。

## AOT 成本分解与受限优化

继续保留late/bound/plan/scalar完整调用基线；新增scalar_split只把无super热路
与有EH的原调用分开，scalar_narrow再使用context/function/self/arg专用委托。
全部捕获、arity、star、UsesSuper与receiver late守卫保留；不改原公共调用入口。
capture_only只捕获不调用，invoke_only复用预先捕获的目标只调用。
后两者不具完整调用语义，不能与产品速度等同，也不能简单相加推算完整调用成本。
窄委托臂使用另一等价函数实例；所有臂仍为手写host，不是编译器发射证明。

CI默认/feature完整门禁及PIC0合同之外，保留四构建轮与正常/禁分层JIT、AOT。
每进程160批记录，共24进程3840记录。必须逐臂报告，不能挑最佳轮代替全部轮次。

## 保留守卫的冷热拆分实验

scalar_capture_split与scalar_narrow使用同一narrow类、receiver及CallExperimentalScalar2，
只用独立站点改为CaptureMethodPlanSplit；capture_split_only单独观测捕获，不是完整调用。
新入口保留每次完整MRO/布局检查、精确实例、外来实例、词法状态、版本、名称及shadow-slot
判据；只把miss/计划分配拆到NoInlining冷函数，热入口与相同布局遍历加内联提示。
未按GlobalVersion缓存布局资格，因为ExceptionFactory等setter不保证递增该版本。
两个入口共享同种缓存实现但使用不同站点，原入口及原八臂保留。
四构建轮、24进程、每进程200批，共4800记录；同一narrow对象的两完整调用臂逐轮对照。
内联提示只是候选，需要核机器码和实测，不能承诺收益；不改产品编译器或默认入口。

## 冷热拆分与内联策略的 2×2 消融

原十臂 legacy driver 保留，另在独立进程执行 isolated 六臂：original、legacy_split、
c0i0、c0i1、c1i0、c1i1。四个因子臂使用相同语义：C 控制 miss 是否分离至 NoInlining 冷函数，
I 控制 capture 与布局 helper 的 NoInlining/AggressiveInlining 策略。I 是两个内联提示的组合，
不是保证发生内联；必须查看实际机器码。original 与 legacy_split 是旧实现参照，不应冒充显式 I0 因子。

六个 isolated 臂各有同形 NoInlining driver，接收 site/receiver/value/count 参数，循环内直接调用固定入口。
同一 receiver/function/value，共用完整窄调用合同；不同站点防止跨臂缓存污染。
每进程三个秒组合预热、20 批、正逆顺序，四轮构建，48 进程共 7680 行。
report 每行新增 driver 字段，原始流与8份入口及其散列保留。不跨进程把 legacy 与 isolated
的绝对时间当作配对结果。仅同进程同批做因子对比，保留四轮与禁分层异常，不只报告最好一轮。

这是 Runtime 机制实验，不改默认入口或编译器，不能宣称产品吞吐收益。
完整布局检查继续每次执行，不用 GlobalVersion 替代未受保护的布局字段。
