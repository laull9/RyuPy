# ADR-0094：模块捕获pair校验的两处局部查询

日期2026-11-04。状态：独立源码审查与首次11项专项通过，首次正常完整门禁通过，已完成唯一同预算编译与冻结产物JIT对照，结果见末节。

## 背景

ADR-0093首切已经落地checked local查询。其同90秒候选诊断仍超时，但完成原langbulgarianmodel并推进至langrussianmodel；共同窗口jpcntx pipeline约16.29秒降至7.75秒。这是单次截断窗口观察，不是稳定基准，也没有定位module_pairs内部成本占比。

## 决定与边界

仅将validate::local_decl开放为pub(crate)，供兄弟模块复用，并替换validate_module_pairs的两处local扫描：Facts::transfer中的赋值目标声明查询，以及captured DynCallMethod的valid_dest检查。不修改其他scan、pass或默认校验覆盖。

transfer仍先kill再查询：即使目标非法，也不能漏杀旧事实。is_cell/name/param/env排除、事实建立、同源与支配证明、flag歧义、meet、worklist、EH根、Suspend input kill及固定点全部保持不变。消费者仍先检查ABI与facts.valid，再执行当前statement的transfer；诊断顺序保持原样。

复用的helper按la-arena 0.3.1 raw范围检查后索引，非法ID返回None，foreign同raw仍读取目标arena对应声明。无缓存、无额外分配、无生产双执行。

transfer查询发生于最终诊断遍历及工作队列每次实际到访；不是固定两遍，循环可能重复，不可达块可能只遍历一次。单查询O(L)变O(1)，不承诺剩余瓶颈归属或具体加速倍数。

## 验证计划与现状

新增独立测试覆盖真实transfer边界/foreign、非法dest仍先kill键和依赖、cell/named/param/env排除、结果写回target前先验证pair、下一次使用拒绝、Dynamic/非法/非Dynamic/Eval结果槽、完整ABI/来源/目标membership错误Vec顺序。继续使用既有helper原scan oracle和module_pair_tests的diamond/loop/reload/ABI回归。

该设计阶段只完成隔离准备和格式化。随后接线首次11项专项通过（新增6、原module_pair 5），固定预期未修改；随后首次正常make check exit0，fmt/严格clippy/workspace/Release、1647 C#零失败跳过、228 CP基线、202 e2e及原30 ignored通过；原日志SHA256 `cc1ad2393b0cd9bc68bd61cc3e014a4402a1ca5a1a1c409d0cfd789e2b2e81d2`。该门禁阶段同90秒诊断尚未执行。固定预期据原代码推导，失败须保留分析。父任务授权的执行顺序为独审、专项、fullgate，合格后一次同90秒；未通过前不声称生产验收或包兼容性改变。

## 唯一同预算编译与冻结产物运行

候选 `e5744c4b996e8faf2ca49253f14ea119598c4914`（tree `071c2ebdec120c38295f5575ac62cbe419a1b36c`）的CLI单次准备exit0，0.537033秒。原锁定chardet5.2.0的501源码文件、入口与golden不变，保原90秒编译上限、同开发构建及诊断日志；未改变优化pass、验证频率或运行引擎。

编译一次exit0，实际1.618965秒，无timeout。44模块的1682条阶段标记构成841个闭区间，全部闭合并完成link/emit/write；精确产出程序DLL、RyuPy.Runtime.dll与runtimeconfig三文件。已完成不嵌套pass-run累计0.091813秒、pass-validate0.357857秒；旧窗口在90秒被截断，不计算稳定加速倍率。它证明此有限候选在同预算内完成原入口编译，不意味着已解决所有编译瓶颈或运行兼容性。

编译结果SHA256 `91c9098415534f887b06ac50f5f9c933dc4b98fbbcd109216745cb4389c04961`，stderr SHA256 `2618909426545d3961fba97287cf02fea7e8a14549f2660a32377bc599070fdd`；源、候选、工具、全部事件和三个产物已独立核验。

其后仅使用已核冻结产物执行一次JIT，30秒上限、最多2秒终止清理，无重编译或新CP。复用原CP3.12.13 exit0的19字节golden和空stderr。JIT实际0.214848秒、exit1，无timeout，stdout为空、stderr精确46字节：`ModuleNotFoundError: No module named 'codecs'` 后接LF。原流没有帧，不能指定唯一源行或宣称后续bytearray regex等路径已执行。JIT结果SHA256 `68cd22e8b28781a2103ea1d86ac365829c8701da9fc7b010d0eb406186611e40`，stderr SHA256 `d6bbb0bc993ed55f7c614e9b36e733a180cfdef12ec63bea8140dfd75ab687e1`。

因此当前结论是编译预算阻断解除、真实运行首阻断为缺少codecs，固定入口仍未通过。候选未发布，不改变历史5/20，不冒main、全库、AOT、跨平台或正式性能基准。
