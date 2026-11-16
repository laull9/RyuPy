# 数值列表插入保留存储候选

固定base86c8b97b。此前 run43771491778 的host在未变动Insert实现上显示65536槽单次插入
fresh分配3145808字节，object1048600字节；这只是定位，不能直接声称产品收益。
候选仅针对精确double的原盒保存，不变更bool背板、异构插入、宽下标或原索引回调合同。
真实producer静态MIR、定向Runtime合同、完整默认make check、固定CP3.12.13
对照base/candidate JIT/AOT双流以及删除实际输入和构建树后的搬迁。
四轮AB/BA/BA/AB重建；insert_once、insert_many、原list_ops各CP/A/B每臂12冷进程。
insert_many重复头插专门暴露16B槽移位相对object8B的风险，不能只凭单次收益验收。
六种执行顺序每轮各两次；原计时/RSS及独立分配统计保留。
共享runner、新进程启动/包装开销、每臂四构建，非暖态或跨平台；静态MIR不等动态命中。
