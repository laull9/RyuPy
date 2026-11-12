# 真实 pyfiglet Native 文本索引优化对照

本门使用同一 GitHub hosted Linux runner、同一当前Release编译器，分别绑定指定基线与候选runtime，公开构建同一锁定上游pyfiglet项目。每臂执行独立Native receipt审计，不以托管微基准代替真实应用。`BASE`与workflow的before-source checkout必须一致；基线须含此前裸return的Python None修复，不能用更旧失败产物规避paragraph。

## 负载与完整正确性

- startup：`--version`，隔离不渲染时的新进程启动路径。
- hi：常见短输入。
- longhi100 / longhi800：标准字体、宽20000的长行压力，不能代表所有用户输入。
- paragraph：宽80、十遍自然多词短句，保留正常换行/单词处理路径。

每个负载先以固定CPython 3.12.13获得完整stdout黄金；计时前隐藏项目源码。各臂4次预热和40次正式样本，按样本序号交替before→after / after→before。每次进程exit 0、stderr为空、完整stdout与CP逐字节相等才接受。保留每次argv、原流、退出和wall，不丢弃离群值。计时期间没有编译器、oracle、CPU profiler或并行构建。

这是**文件缓存已预热的新Native进程**，wall包含启动、渲染、输出和退出；不是系统冷启动、长期进程稳态吞吐或Nuitka比较。startup单独报告，不用长行收益掩盖启动退化。

## 统计与代价

保存所有样本、执行顺序、p25/median/p75/p95/min/max，以及同一序号的log(before/after)整对重采样：固定种子，5000次bootstrap，报告几何平均比和95%区间。区间只描述本次runner样本，不证明跨次/跨机稳定性。下界至少1.03才标记`improvement_candidate`；上界至多1/1.03标记`regression_signal`；其余`inconclusive`。这些标签不是自动发布许可，`passed`仅表示正确性和证据完整，必须综合全部负载与代价审阅，不能以微小中位数变化宣称提速。

同时保存单次CLI、两臂runtime与公开AOT构建wall，以及Native和完整dist大小。构建为before先于after，缓存/restore条件不同，**observational-not-controlled**，不能据此算构建加速。

RSS在所有wall测量结束后独立各运行一次，不混入计时分布。10ms `/proc` 后代RSS求和可能漏过短暂峰值并重复计算共享页，不是精确cgroup峰值；保留采样次数与原始记录，不把采样wall当A/B数据。

## 状态

这是待真实hosted运行的门；本地统计测试通过不等于优化收益。runtime优化、完整回归以及跨平台/打包验收由对应独立门提供，不由本脚本的绿色状态推断。
