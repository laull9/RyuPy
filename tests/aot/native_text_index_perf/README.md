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

2026-11-12，候选 `3700df16`（产品树 `b8b2fc1a`，本地同树 `eefab816`）完成 CI [43328258397](https://github.com/laull9/RyuPy/actions/runs/43328258397) 两个 attempt。每轮原件的 440 次正式/预热运行及 10 次独立 RSS 运行均逐项核完整输出、退出和 stderr；统计重算一致，两臂 runtime 源差异只有 `PyText.cs`，源码前后哈希不变。

| 负载 | 首轮中位 before→after ms | 第二轮中位 before→after ms | 结论 |
| --- | --- | --- | --- |
| startup | 12.61→13.28 | 11.01→10.82 | 首轮变慢未复现，不能声称无启动风险 |
| hi | 28.75→29.28 | 24.21→24.31 | 无可靠收益 |
| longhi100 | 77.98→75.21 | 73.54→71.67 | 未达预设稳健 3% 阈值 |
| longhi800 | 2115.26→1946.93 | 1921.73→1815.15 | 两轮中位耗时降低 7.96% / 5.55% |
| paragraph | 55.33→54.56 | 46.91→47.09 | 无可靠收益 |

longhi800 的配对 before/after 比 95% bootstrap 区间分别 `[1.0841, 1.0891]`、`[1.0507, 1.0607]`；只支持该极长行场景，不代表一般应用提速。startup 首轮区间 `[0.9337, 0.9997]`，第二轮 `[0.9602, 1.0664]`，保留两轮，不选择性丢弃负向结果。

Native 入口两轮均只增 8B；完整 dist 分别增 217B/225B，含调试文件，allocated bytes不变。RSS仅10ms独立采样，不作精确内存优势；构建缓存条件不同，不作构建加速结论。Nuitka未重测。

同产品完整 `make check` 通过：C# 3053通过/5跳过，e2e 454通过/283忽略。新源码孤立代理字面量与反向切片独立代理合并缺口另留 pending，基线与候选输出相同，不冒充完整Unicode兼容。
[binaryornot回归](https://github.com/laull9/RyuPy/actions/runs/43328258313)：8宿主+8无Python/.NET隔离容器输出及7依赖控制通过。
[Mac/Windows回归](https://github.com/laull9/RyuPy/actions/runs/43328258236)：各平台两入口共12次原位/搬迁、553字体、receipt及平台负控通过；不是无运行时干净机证明。候选保存在独立验证分支，不因CI绿色自动合入dev/main。

官方ZIP SHA256：
- A/B attempt1：`bb39f8ddc8a5a8e62fc3655b4e66d1b5254e29b040f7f0436851f53ede38e5e4`
- A/B attempt2：`9f9f9335d9f26a8cfc1b750d1803c74fb5c21e5ad726b2ceec8910cc61891a82`
- binaryornot：`d24a8b1875e1e6f7a838ac5d4cd0ae18963ab3d86d4463ef50795e38b26cb3b3`
- macOS：`f7e82daa32a9877d2b55d954e27745dab6115330a76a9d18cb705da636b45673`
- Windows：`7e0640faf947eef3b267a4d58aacafa1a1ef0201e225419bb8d4111cae0b8aa3`

GitHub artifacts保留3天；下载原件已另行保存审计，不以CI摘要代替原流。
