# ADR-0146：JIT空资源子包发现与Static词法归属稳定

- 状态：实现候选，局部Rust门已过；新Runtime/产品/完整同HEAD门未验，不登记项目晋升
- 日期：2026-11-10

## 决策与原问题

新增未初始化同basename空包会令既有正确Static→原生partial→TypedDict路径在全Registry扫描时歧义。故不能先扩大默认图，再用“pyfiglet都是Dynamic”回避通用回退。

新增冷`CreateLegacyCodeSourceV1(string,string,PyModuleGet)`，只对已通过MIR实际module.init/自身signature/getter关联校验的Legacy源码单元发射；旧三参CreateCodeSource、旧host错名及Unknown仍保持旧扫描。新V1实际signature帧的TypedDict取不可变词法初名，Dynamic/ModuleMain/Instance/generator实际来源语义不改。它不是当前可写function.__module__，Static rebound及定义前globals改名仍是既有不足。无hidden owner、热参数/返回ABI变更或每调用新增对象；没有性能测量。

## 唯一默认扩域

现build编排显式给模块加载层传`!aot_locations`，仅JIT build/run发现。原公开硬图/include及AOT图不自动增加；AOT复制仍显式resources。没有新用户开关。

发现一次只读完整硬图，识别模块作用域importlib.resources/别名的有限`files`拼写和单一literal位置参数或anchor关键字；这不是运行时绑定证明。调用、求值、副作用和异常次序原样保留。只补原图缺名且直接父已为真实普通包的同目录直接子包；现resolver必须选择同一lexical路径且仍为Package，保原搜索根和.py遮蔽，不canonicalize、不递归新包。

可选候选只读一次bytes；BOM、前两行任何coding声明（含UTF8）、解码/读取/解析失败及非空AST body均软跳过。docstring/pass不算空。私有诊断不污染Session，接纳时登记同一已读文本，不把missing/未知/非空候选变成硬依赖。包仅注册，初始化仍由真实import触发；runtime父__path__/cache/失败重试规则不放宽。

## 验收与明确边界

原Static完整输入/oracle不改；其unused函数现在使默认/include两臂都为3模块，子包仍未缓存。真实PE需核Static void core、自身signature→pkg source、新V1实际冷创建、direct call及data仅ldftn。旧host与实际Module语义分别保。

原锁定pyfiglet 1.0.2入口无include默认JIT必须stdout逐字11B `6  _     _\n`、stderr空、exit0；旧缺包control保留同输入改正向门。AOT显式include/fonts清单及搬移/隔离/缺字体门保留。额外通用资源+Static碰撞fixture沿现部署工具真实Native，缺资源不得回退源目录。源码不能因新测试失败改成项目特例。

最终同HEAD default/pilot完整check、全20默认JIT及旧10逐ID（含progress）、既有source/S2/Legacy/Instance/Trace/Native均独立保护。新pytest/Rust冷helper或编译通过不能代替产品成功。namespace/zip/custom loader、动态name、递归包扫描、完整import/fromlist、TypedDict元类/Static owner及其它九项目不在范围。性能/AOT优化尚未开始。
