# 单索引数值列表删除候选

固定 base 3f8edd8c。此前 baseline run43771491778 的 host 证实删除末槽会整表物化，
65536槽 fresh 分配2097208字节，object 背板0。该证据仅为host归因，不是产品收益。
候选仅单索引删除，不修改切片删除、Clear、Copy、Insert、根比较协议或C ABI。
真实数值producer静态MIR、专门Runtime合同、完整默认make check；固定CP3.12.13
对照base/candidate JIT/AOT双流和删除输入/构建树搬迁。
性能四轮重建顺序AB/BA/BA/AB，delete_once/delete_all/原list_ops三项，
每轮每项CP/A/B各12新进程，六种顺序各两次。计时/RSS与独立统计轮分列。
共享runner、每臂仅四构建、新进程含启动和外壳开销；不推断暖态或跨平台性能。
只在语义、身份/别名与控制回退验证充分后考虑产品合入。静态MIR不等动态命中。
