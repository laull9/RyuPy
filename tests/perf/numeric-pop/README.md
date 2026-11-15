# 数值pop基线定位

固定bbcca490，无产品实现变更。宿主Runtime用三个长度、fresh/observed/object三种
背板输入各五次记录，仅测一次pop增量分配、ticks与背板退化；先预热三次。
该诊断不能等同产品AOT、驻留内存或稳定暖态吞吐。

独立源码pop_once/pop_all要求真实MIR数值producer标志。当前CLI与CP同时核JIT/AOT
输出，AOT每源码12新进程样本，另次stats记录分配，list_ops为普通列表对照。
只有一个当前基线AOT构建，无候选对照，不宣称优化收益。共享runner/启动与外壳
均在wall中，不把少分配直接推断为加速。没有新增C ABI、原生库接入或默认开关。
本轮不做搬迁/全make check验收，因为没有产品改动；后续候选须独立完成。
