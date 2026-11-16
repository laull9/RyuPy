# 先准入的既有records实体键实验

不改产品/ABI/守卫/default/dev。固定产品a1419ac5，与此前诊断可比；后续5d36只文档，不声称最新提交。本轮原records字节不改。

先分别默认CLI及同feature CLI off/on执行native MIR/CIL。off无RecordsTryIncrementMaterialized；on零命中保存完整结构与明确admitted=false，停止后续昂贵门，绿色工作流不等于命中/语义/性能已通过。命中必须唯一，保留summarize中的tuple、两get/add/set原fallback及真实CIL helper。不为通过修改matcher。

仅命中后构建独立diagnostic CLI，原records JIT/AOT与CP完整双流相等并精确核240000 fast/0 fallback。复用既有五fixture及审计器，default新基线也逐例JIT/AOT实证，callbacks/subclass旧差异必须仍精确匹配，不匹配即失败不得沿用旧已知结论。普通实验控制/启用同CLI同Runtime；诊断专用Runtime不进入正式性能。语义保留原tuple身份、回调/溢出/异常和回退；已有探针预算不改。不称无分配键或布局优化。原语义脚本附带30条小fixture冷样本单列，不与正式记录混表。

四轮独立feature CLI，构建顺序off-on/on-off/on-off/off-on，同一CLI两个wrapper仅flag不同。原records与dict_ops各CP/A/B12次六排列，288条正式冷进程计时；每轮A/B标签的off/on映射单独保存。另轮分配/GC和搬迁沿既有脚本，16份实际AOT清单；不把控制臂代表所有基准，无暖态/显著性结论。CWT/版本守卫可能抵消重复查找节省；tuple及其object数组仍会物化。完整原件分400MiB归档，需核全部分片/总hash才能称二进制原件核验。
