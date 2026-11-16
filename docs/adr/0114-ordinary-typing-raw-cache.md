# ADR0114：普通 typing 根的原始键缓存前置

- 日期：2026-11-05
- 状态：有限域专项、独审、有界性能及正常完整门禁完成；原项目干净提交复测完成

## 证据与分切

原 Markdown 3.5.2 的 ClassVar 导入首错尚未解除。C1 未验收候选引入的参数检查暴露
旧普通根 normalized Intern 的暖命中缺陷：先缓存 List[key]，再将 ClassVar 改为 list，
同一原键应暖命中，而新原键 List[(key,)] 应重新消费冷检查。直接把检查挪到旧池 miss
仍会错误复活归一化身份，不能作为修复。原 C1 回归、CP 对照与 WIP 独立保存，不合入。

本 F1a 只迁移 List、Dict、Set、FrozenSet、Iterable、Iterator、Sequence、Mapping、
MutableMapping、Collection、Container 十一根。Tuple 与 Callable 各自真实装饰器边界
留后切，之后才恢复 ClassVar；不借单一项目导入特判，也不将暖 R1 通过当 C1 完成。

## 设计

十一根共享一个 receiver/原始 parameter 的128项 LRU，None 与 NoneType、不包装与
显式 tuple 保原键身份；冷建直接 new alias，不再进入归一化 Intern。真实 receiver
保存根不受公开 List/Dict 导出改绑影响。hash 桶与双向节点平均 O(1) 定位/触碰/淘汰，
淘汰用已存 hash，冷失败不占位或驱逐成功项。静态方法组只初始化一次，不逐次造闭包。

新可实例化缓存只复用 F0 安全键的 RequireKnown/Equal；F0 对应两方法由 private
变 internal，原算法/静态池与 SpecialTypeArgument 函数体不变。整个 cached 调用的
PyTypeError 走一次 uncached 回退，NI/ValueError 不吞。当前 exact list/dict 仍提前
NI，没有执行真正 unhashable hash，不能将此结构或 C# guard 测试称为完整回退验证。

冷正文按序逐参检查，再消费 _check_generic 与准确 arity，再消费构造/采参 helper。
暖命中绕过正文 globals，但键 hash/eq 仍消费自己需要的 helper。保留已有安全不可变
树的惰性稳定 metadata；嵌套 Generic 专用载体或 alias 内 tuple 的旧采参缺陷明确 NI，
不静默输出空 parameters。普通类叶不任意 getattr，未知合法回调/动态采参另切。

## 验证与边界

17份 CP3.12.13 完整源双跑均成立；8支持候选严格 CP/compile/JIT 匹配，16新 C# 与
162旧 typing 回归通过。独审4原源和2独立源首次 CP 双跑12/12、compile/JIT 各6/6。
独立覆盖满池后160冷失败不占容量、真 LRU、保存根冷建、普通类描述符不消费，以及
安全嵌套 alias 的元数据稳定。每次固定源、CLI/runtime 与实际加载身份，保全部原流。

九份完整源保持 pending：完整 helper 保存/恢复、非类型参数、hash/eq 回调、tuple
派生、冷 helper 回调、raw alias 消费、ClassVar 新 raw tuple、eq TypeError、动态冷
parameters。完整 helper 源旧模块删除 NI 未改，另建无私有读取/删除的独立暖支持源。
未知域 NI 不是 CP 成功；完整 ClassVar、helper 替换、元数据 mutation、前向引用未完成。
原 Name-based alias hash、ABC origin 晚绑定及其他旧池关系不纳入本切修复。

## 性能

与 F0 固定同 CIL/host 的公开整函数路径：14旧路径加 List/Dict 热/160键冷轮转；
默认与禁 tier 两配置各唯一 ABBA，8进程720样本32.52秒。固定8192热身/固定次数，
全部 checksum、输入与实际加载身份通过，无重跑挑样本。不能声称热身保证 tier 稳定。

List/Dict 热默认 -43.61%/-26.95%，禁 tier -44.68%/-26.31%；分配分别688→536B、
728→600B。List/Dict 冷默认 +4.65%/+11.44%，禁 tier +7.75%/+48.55%；Dict 冷仍
1008B，List 冷默认960B不变、禁 tier 960→928B。保留配置差异，不据此推断 C1 成本。

旧14路径 B/op 不变，但默认 listgetitem +77.16%、Generic +11.75%、冷 Optional
+14.19%、get_args +19.48%、type(Union) +20.64%，禁 tier 普通构造 +17.26% 等
不利样本均保留。每侧仅两个独立进程且存在离散，不作普遍提速/无回归或因果结论。
F0 冷 Optional 已增848B的历史代价仍未关闭；不能由本轮热路径收益掩盖。性能归因
和有界优化另跟进，不无限等待。全部原始数据和解释随恢复证据保存。

## 验收顺序

只转正上述8支持源，另9完整源保 pending，正常 make check 后才作功能提交。随后用
干净提交固定原 docopt/Markdown 入口各唯一复测，不拼接旧矩阵计数。六件备份采用
小于32MiB标准 tar.xz，纳入 C1 WIP 和全部旧证据，独立实际恢复后才交付。

正常 make check 唯一运行 exit0，2130C#/315CP/244e2e通过，75ignored。旧236通过与
66ignored逐名保留，准确新增8通过/9pending；非e2e Rust775通过、另1旧ignored。
CP基线与315条CP-only自检不计Ryu支持率。10874源内容及模式与冻结门禁快照一致，
runtime实际SHA未变；约115秒、未触及低空间界。独审首解析器漏进度编号空格的
失败原流保留，修正审计解析不重跑测试，也不改源/golden。

干净 b5e6279 原项目各唯一复测：docopt0.6.2 CP/编译/JIT全过，CP四条860B原警告
保留；Markdown3.5.2 CP/编译通过，JIT exit1首错仍为ClassVar缺失。两份3187包源、
20根路径次序、原入口/golden及工具/clean前后审计一致，无重试，不增加项目分子。
CP-only与完整e2e相差的四项为phase4 NumPy两项/sqlite3/zlib显式排除，非编译失败。
门禁独审001漏进度数字前空格、002误用e2e总数作CP-only总数，原审计失败均保留；
003更正解析完成，非生产或测试失败，不进行重复全门禁。
