# 数值列表观察边界诊断

固定 dev 3f8edd8c，产品源码不变。先测 host 的 fresh/首元素已观察/object 三种表示，
对 Insert、DelItem、IndexOf 首尾命中/未命中、Copy 分列增量分配与操作耗时。
3 个大小 × 3 表示 × 6 操作 × 5 次 = 270 行；预热不计入。
源码使用真实数值生产者分别测试单次插入、单次删除，并以原 list_ops 作控制。
固定 CP 3.12.13 双流 oracle、JIT/AOT 语义及每项 CP/AOT 各12次新进程。
仅诊断当前基线，无候选收益、无暖态产品或跨平台结论。静态 MIR 不能替代动态命中。
源码 Contains 根协议已逐槽观察，host IndexOf 的结果不能直接推广到 Python in。
