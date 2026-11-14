# CPython桥分层测量：源码复核后的实验计划

状态：规划，未执行新的性能实验，未修改产品。固定CPython3.12.13/NumPy2.3.5。
既有短样例34.37ms对143.64ms仅为不同执行域端到端基线，不能全归为GIL或初始化。

## 已证源码事实

- PythonObject.Invoke使用MakeTuple和可选kwargs字典，native rp_op(5)为PyObject_Call；当前无Vectorcall。
  MakeTuple分配的是CPython元组，不应误称每次在此新建托管参数数组。
- CPythonHost.Enter每次先在gate内更新计数，释放gate后rp_gil/Drain/leaseDepth++；
  Lease.Dispose执行Drain、depth--、rp_gil_release后再更新计数。没有基于depth的轻重入短路。
  这证明边界调用存在，无法证明发生GIL争用或给出各层耗时。
- bridge_calls是Enter计数，包括回调、关闭观测等，不等于外部Python函数调用数。
  active_leases是关闭后状态快照，不能由退出时0推导运行中峰值重入深度。
- 公开配置是container_transfer: synchronized/proxy，不能把内部SynchronizeContainers布尔属性名写到发布配置。
  两种模式的容器对象行为不同，必须为每种模式先过同一程序的结果/身份/别名oracle，不能盲目互换。
- Dispose无条件CaptureModulePaths，即使RYUPY_CPYTHON_REPORT为空也采集所有模块路径；
  WriteReport才检查环境变量。这是可能的关闭期开销，需测量后再决定是否门控，不能先宣称它主导差额。
- 报告写出前Roots/objects/transfers/handles已经清理；live_handles_at_shutdown另在Finalize前捕获。
  这些计数不能冒充运行期峰值或包装总量。late_releases非零应单列调查，不直接解释为非法用户访问。

## 原报告的归因限制

参数数量、关键字形态、重入深度变化同时影响求值/封送/回调/分派，回归斜率只能作为该工作量的边际成本。
空Python函数/标量属性仍含Python协议和结果封送；不能称为已隔离的纯PInvoke。
纯import包括启动、初始化、导入和关闭；不能直接等于startup，更不能用A/B/C总耗时差额命名单一层。
中间阶段程序与原程序不同，原样例保持独立端到端结果。没有采样栈或受控instrumentation证据时，
不下“tuple、GIL或异常构造占主导”的结论；也不预设亚毫秒、纳秒斜率或固定100ms门限。
原报告某些输出值描述与m4_1源码不同，所有oracle须同次直接运行生成，不手写字符串替代。

## 首个可执行实验

在当前sum竖切的构建/计时窗口释放后，由主代理统一独占执行：

1. 冻结已验证CLI与完整内嵌闭包、显式synchronized provider、锁定解释器及NumPy载荷。
   用仓库外私有distribution复制避免原环境路径泄漏；同一原m4_1_numpy_objects.py在两域退出0、双流严格一致。
2. 独立三个源级探针：仅导入、导入后数组构造、同一业务内核多次执行。
   这三个程序各自与CP同次比较，不能当成原样例的层级拆分或直接互减归因。
   长任务选N=1/10/100等多尺度且不触及数值溢出，验证输出、视图别名和回调副作用。
3. 主表无RYUPY_STATS/RYUPY_CPYTHON_REPORT，直接进程墙钟与wait4 RSS，1warm5交替。
   报告轮单独开启，严格解析实际字段、模块路径归属、关闭状态与同步失败；失败样本阻断。
   独立阶段标记版本可用stdout边界+显式flush由父进程读时间，标记开销和调度误差分列，
   只用于粗定位“用户代码前/体内/体后”，不能据此给GIL、native或模块观测精确计时。
4. 对短样例和长任务各选一次无报告外部采样（若平台允许sample/Instruments），
   将采样分布定位到CaptureModulePaths、native call、同步图、回调等，采样自身另跑不混入主表。
   若仍无法区分，下一实验才在外部临时受控源码副本加入口计时；保留未插桩对照与开销，不进默认产品。
5. 若模块关闭观测确认为热点，另开完整小竖切：无报告时不采路径、有报告时保留诊断和错误合同，
   重新验证atexit、生命周期、模块归属负例及真实provider搬迁，不直接在测量阶段修改发布实现。

## 成功与记录

退出码、完整双流及副作用必须同CP；stderr若有预期诊断也逐字节核验，不强制为空而抹掉合法输出。
同步模式、平台、JIT/AOT、原位/隐藏输入搬迁分别登记。报告指标缺失直接失败，不用默认0。
RSS按平台单位折算；若未来测onefile进程树，逐时刻求同时RSS，不能累加各进程高水位。
所有构建树与诊断记录外置清理，显式保留的原始结果也放仓库外；仓库只登记必要来源、方法与审定结论。
