# ADR-0092：MIR Pass 运行与校验分离观测

状态：实现与独立源码审查完成；专项及首次正常完整门禁通过；已完成一次同90秒固定包诊断，结果见末节。

## 背景

既有 opt-in 编译阶段日志只能定位 pipeline 区段，不能区分 pass 运行和每轮 MIR 校验。缺少结束标记不能预判 validator 是根因。

## 决定

保留 run_pipeline，新增泛型只读事件观察入口，二者复用同一个执行与验证循环。MIR 定义固定事件类型，仅传递静态名称、从1开始序号、运行/校验与开始/结束枚举。观察者返回 ()，不接收 Program/Function，不决定调度、变换、验证与错误策略。

architecture §2 禁止的跳层编排回调仍禁止。本接口仅通知不可变诊断元数据，MIR 不依赖 driver，不引入时钟或 I/O；driver 负责 opt-in 记录，不将流水线拆为由 driver 逐 pass 调度。不得将此接口扩展为跨层编译控制或可变 IR 回调。

保持每 pass 后始终校验，失败原错误和短路行为不变，不输出虚假校验结束标记。最终验证和旧入口保留。默认 NoopObserver 的常量禁用分支不求值诊断名称；承诺无新增诊断时钟、格式化、I/O、堆分配，不承诺 dev 构建零调用成本。

## 输出与代价

沿用 StageTrace 的同一时钟和失败停写状态。pass 名和模块名各截取96字符并debug转义。每条flush，默认7个pass每模块额外28条记录；两项可选pass全开额外36条。日志开启会扰动耗时，只能定位区段，不可当性能基线。写入失败和进程终止均可导致结束标记缺失，必须结合退出状态判断。

## 验证边界

测试覆盖两类pass的成功/失败、重复名称序号、失败后不执行下一pass、原错误与产物一致、默认不读name、有界转义、失败停写及既有CLI compile-only哨兵。不会把哨兵复制成功当作运行时或外部项目兼容验收。

## 本轮验证记录

MIR observer 4/4、driver StageTrace 6/6、CLI compile-only 4/4 首次通过，零失败。后者只编译小fixture并复制哨兵，不运行Python应用或dotnet。随后首次正常 `make check` exit 0：fmt、严格clippy、workspace、Release、1647项C#零失败/跳过、228项CP基线、202项e2e及原30项ignored全部通过。门禁原日志SHA256为 `74d70856aeb3ccf708bdb92355ce6372c725203f70aa641a1684ed6a541d850e`。该门禁阶段尚未执行本轮固定chardet诊断，不预报结果或优化收益。

## 固定包的唯一第二层诊断

候选 `fe1e765f48ed90ea09820fb45bd2217bcc5bcbf8`（tree `82e26630e906bc6b58204b2d8c22af0162193b31`）准备CLI一次exit0，0.127392秒；按原锁定501文件、入口及原90秒上限，只编译一次。CP复用先前已核原流，本轮0 CP、0 JIT，不安装或改上游源码。

实际90.003929秒超时，SIGTERM退出-15，stdout为空、无产物。stderr共986条单调阶段事件、492个已闭区间。未闭区间为 `chardet.langbulgarianmodel` 的pipeline，以及第7个 `float_chain` 后的 `pass-validate`（89.391954秒开始）；该pass的run已结束，未进入link/emit/write。

只累计不相互嵌套的已完成pass子区间：run共0.043786秒，validate共81.497053秒；未完成末次校验不计入，外层pipeline也不重复相加。这是带逐条flush日志的开发构建诊断，存在观测扰动，不是无扰动性能基线或优化收益。它支持优先调查校验内部成本，但尚不能证明某处线性扫描就是唯一根因；没有移除校验、跳过pass或改变预算。

结果JSON SHA256：`7a079ee89ee3b5038f33528473e17e75f93332e21d354d1ab7d03d33d3a35242`；stderr SHA256：`5ef42aa6bb0803d65fd2952682d3a70687f80a781226dd7da6377c81eddad252`。未发布、未获得项目运行结果，不改变历史5/20或宣称main/全库/AOT通过。
