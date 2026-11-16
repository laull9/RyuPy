# 原records用户态采样与粗边界诊断

固定产品a1419ac5；原records源码字节不变，原生二进制不改编译选项。两轮独立默认构建，每轮原程序与粗边界输入各CP/JIT/AOT每臂12次，共144纯计时样本与4实际SDK清单。所有命令显式native、实验关闭，沿用独立SDK10.0.403及绝对JIT host，固定CP3.12.13。无产品修改、无默认check/搬迁验收。

粗边界只在summarize聚合循环、sorted、report循环外加perf_counter；清除4个_phase_赋值与phase_times.append后，所有函数类AST必须等于原源码。原循环语句/顺序不改，计时数据在完整报告之后输出；每次剥离12计时行后的完整双流与CP/原程序相等。新增globals可影响优化，阶段不相加解释原程序、不拆小循环相减；summarize原有四次执行不是四次冷进程。

每轮正式计时、分配和host trace完成后，原AOT另做最多12新进程perf record cycles:u/499Hz/DWARF栈采样；每次先独立CP oracle并核完整原生输出/程序stderr。perf stderr与程序stderr隔离。无sudo perf、无sysctl/安全设置写入。权限/工具限制保留命令、退出和完整错误；停止后续采样但仍做另一输入语义，绝不把失败当有效样本。

保存perf.data、原报告、逐事件IP与调用栈、有效事件行数、ELF build-id/节表、原始AOT和现有独立.debug/.dbg文件/hash。不新增符号构建、不把静态存在符号当可解析样本；应独审核report/script非零退出、unknown比例、采样事件及函数归属后才能解释。两轮同二进制内重复不是24独立构建。短命启动/解析/GC/内联/未知栈、采样扰动和共享runner限制仍在，不按样本份额宣称纯helper因果。

RSS为峰值非活堆，分配另轮，不把采样轮wall当正式性能。若perf不可用，粗边界只判定聚合vs排序/报告范围，不证明GetAttr/哈希哪个主导。不启用旧records实验，不触碰provider/ABI。
