# Enum auto有限域独立Native图

独立有界job复用既有reusable workflow入口，主controller保持600行。
默认CLI无pilot，独立构建与artifact，不改变Decimal/tzinfo的输入或统计。

fixture来源绑定本轮四份合同：整数/bool/BigInt与非单调历史、别名、赋值期读取及方法默认参数、
两类独立history、Flag非负位历史、CP3.12 warning record/ignore/error与失败类发布边界；
另含普通非Enum类namespace/实例属性写和方法调用。全部stdout以固定CP3.12.13逐字为准。
warning及异常路径也必须在真实ELF执行，不能将JIT成功代替Native。

不涵盖混numeric+str/None生成、自定义_generate_next_value_、module auto重绑、tuple或全MI。
这些域外边界由产品负合同明确诊断，本图不会把它们静默改为正例或宣称全面支持。

复用既有纯执行合同但输出独立目录：CP1、双PIC JIT2、真实ELF1/原位及搬迁2；
冻结源码/CLI/runtime/CP/SDK与实际PE、校验manifest、归档dist、隔离源并finally恢复，
对照完整退出/双流与dist快照。Native发布上限600秒，验证25分钟、job40分钟。
检查器只是拒绝坏证据，本地CP或checker通过不等于产品/Native通过。

另加独立非-O语言检查：直接和函数默认参数中的__debug__为True，try内assert False仍抛出，
消息副作用只发生一次。即使.NET Release也不能删除这些行为；不声明CP完整-O模式支持。

本图独立runner显式固定PYTHONOPTIMIZE=0，并记录python-mode.json；实际CP argv另存原件。
不会继承宿主的优化环境，旧Decimal/tzinfo runner不变。
