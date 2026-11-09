# ADR0143：普通 CLI 的有限 trace 默认迁移候选

日期：2026-11-09。状态：candidate06源码候选，尚待绑定本源的Rust与产品验收。
本地完成12份新源各一次固定CP3.12.13参考、纯Python脚本自测与同版官方rustfmt格式检查。
candidate05的真实MIR/driver库检查通过，但21项选择测试为10通过/11失败；首败定位为完整Bottom实参被缩掉。
该旧失败原流保留，candidate06修订不借旧统计宣称通过；driver测试、真实CLI/JIT/Native及完整门仍待同源验证。accepted `fb0553`不动。

## 已有证据与本次请求

CI012使用本地`78394809493f57017e18be0c843f64b0c29aaea4`、公开同树
`23512582d990227549eac994623e11f3e697411b`，tree为
`f7d112af3e6315d86cdfc0779eae0e05fbc38182`。
[唯一run42535351804](https://github.com/laull9/RyuPy/actions/runs/42535351804)最终failure：
显式A/B/HOT × JIT/Native × PIC1/0十二run及C/D四拒绝通过；两模式完整check与追加门通过，
640发现/401 compiled/239 pending保持。原无flag frames第一链仍多模块帧，因此独立门失败。
固定20逐ID为10 native/8 unsupported/2 failed；native是runner的默认JIT标签，不是20项NativeAOT。
CI012最终报告SHA256为`368ba72b1f12f3a64a7b886e803f1566cb3e5d563b88210153770850e6e2ddf5`。

本候选复用已有V1证明与runtime，不扩大构造/字段/C/D，也不移动原frames pending或改其430B源、行号、期望。
不是先测显式再改默认：本候选源码已接普通CLI build/run无flag，最终必须检验这份同源真实入口。

## 请求、所有权与不可回退点

- CLI build/run明确传`TraceEntry::StandaloneMain`。裸Session默认Unknown；普通dump继续旧观察路径。
  公开请求只表达正常Main执行意图，不保证Public函数无人直接调用，不保证任意CLR宿主/反射/复入。
- driver先用实际单源码图、entry0、无include/resource/fixed binding/显式Instance及真实Legacy计划过滤。
  同一HIR函数必须同时`is_generator`和`SourceDynamic`才值得分析；无需求不跑facts/prepare/新lower/Enter。
- `prepare_high`私有拥有实际Program/exports/生成器计划/必要来源/初始化合同及原警告。
  facts只在prepare本地消费，成功后释放，finish不重新求解；不机械延长无用facts生命周期。
- typed `NotInDomain`只代表合法能力未覆盖。Auto丢弃整个未lower候选，随后走原完整build/pass/lower。
  `RequireV1`仍转E0310；普通MIR错误保原Diagnostic，Warning不变成错误。
- `InvalidCompilerState`及源/Pass合同、实际ABI、lower、link、certificate或发射失败全部硬停，不能回Legacy。
  成功prepare按值move进finish，只消费同一Program一次，不Clone或重新build，不拼局部trace权威。
- link用本次实际proof选择，不从残留Session bool授信。无证书产物含V1动作也是硬错。
  现有Main Initialize→Enter(1)与完整certificate门不改，运行时不改。

## 四项分类义务

1. 未调用`identity(value): return value`的Bottom参数没有输入覆盖，是能力不足。
   不再用CLR Null冒充参数事实；完整已建立上界与实际IR矛盾才是内部错误。必要origins映射缺失返回错误，不索引panic。
2. 签名的default/keyword/variadic、合法closure/cell、额外普通handler、公开metadata和有限值形状，与owner/真实类型/身份分开。
   普通handler仍核实际Push/Pop/depth，合法depth+1恢复只用于域外完整性检查，不能授予V1能力。
3. 沿现有全量validator、源→Pass合同及defaults/初始化索引检查独立硬义务。
   能力拒绝记录后继续后续检查，不造Unknown Value或第二套资格求解器。
   Bounds逐字段区分Known/NotProven，映射独立全核；NotProven不用作fits上界，也不提前结束唯一high操作遍历。
   Resolver在同一次有界定义走查中先将每个已解析值交给原token/参数硬义务，再限制32项存储，第33个已知反证也不能丢掉；DirectFn、已知callee的DynCallValue/CallValue共用同一参数义务。
   空已知集合不作负反证；原有位置数组只沿实际Array/Use/Box传递约束，不新增数组heap能力。
   测试含前后/函数顺序、Bottom/未迁入摘要+坏token或缺映射、32/33集合及最后一项参数矛盾。
4. defaults一次索引先核真缺/重复/错角色/公开私有槽；实际opaque发布和receiver检查分开，预算不抹掉已检查前缀逃逸。
   127/128/129跨度及缺/重复组合保留。初始化原128遍历步包括终端常量检查，127局部链是该纯链形状的通过边界；
   128/129局部链只保域外，未放宽旧预算。HIR32项/1024轮仍完整拒绝，不部分授信。
   截断defaults的consumer按同一实际定义索引最多128步回看opaque身份，不能把尾alias的Eval/Return漏掉。
   初始化只有真实截断链的纯producer身份定位允许NotProven；同一前言走查仍核全部reset/Push/其它effect。
   超预算未解析的尾部不猜测正确或损坏，只拒正向资格；已建立合同与已解析反证始终硬错。

## 同最终源验收入口

- 原产品门新增A/B/HOT无flag十二run、真实`ryupy run`及PE/Main核验；原显式十二run、C/D四E0310拒绝保留。
  C/D无flag只核Legacy PE，不声称trace语义正确。
- 新`generator_default/default_gate.py`真实普通CLI无flag，与同候选`trace_legacy_control`测试example裸Session产物对照。
  后者不是发行CLI或历史旧CLI，没有新增生产Legacy开关。九份成功CP源要求完整CP流一致；unused identity仅核保旧trace债。
  rename/alias独立控制使用不同文件stem，dead-yield另要求V1与完整CP一致。
  原矩阵及新控制都核prepare/lower/旧build的begin/end；显式拒绝无emit/output-write。实际首败PE先归档，再做选路/PE核验；归档另败单独留证，不替换原compile/run首因。
- 新12份CP源单次参考：9个exit0、3个exit1，固定binary SHA
  `021044895e95be79dc2f110367607e684119afbc8ce75f6f0eec94844e0acec7`，参考清单SHA
  `8451d5ce17f11ca6163cb134188af4ca586191e098226c980affb63c0cf4e965`。
  CP事实不证明新Ryu选择正确，CI不重跑这12源的CP，不从Ryu输出制造golden。
- 原`generator_dynamic_gate.py`未改，原frames门必须真绿。项目runner和compiled e2e仍裸Session，绿色不能冒作Auto覆盖。
- 两模式make check、原追加门、640全名集合/401 compiled/239 pending、固定20逐ID10/8/2及progress都保留。
  旧4具名实验的原断言/隔离不改，证书/后改写/源/PC/槽/EH/Retry及S1/S2安全门继续必需。
  前置新增25项选择组合与driver4项的两模式独立进程精确清单。基础工具/源码身份或fmt失败现在跳过昂贵编译/测试并记skipped，
  保留原失败原流，不能计pass；这取代历史“fmt失败仍收编译反馈”的成本策略，不豁免任何门。

未测编译峰值内存、正常next/异常/getter吞吐、Native镜像尺寸或净性能。阶段记录不是正式benchmark；不声明零开销或收益。
同源首败保留原流并停止晋升，不换fixture、不删帧、不改golden、不把旧CI012结果倒灌为新默认通过。

当前候选修正通用defaults角色与真实getter必发布义务的区别；property保留的零写静态槽不授予读取能力。
选择测试先核合法事实/基底并验证目标错误，新增可选槽零写/越权读、先capture后删构造或发布、
原初始化顺序拒绝、已知receiver超窗缺/错/重复发布四组；整链守恒只覆盖实际build之后的pass边界。
这些修订仍待绑定新冻结源码的真实Rust及产品验收，不能沿用此前统计作为目标义务已覆盖。

本候选的真实Rust执行曾揭出统一参数投影回归：完整Bottom被过滤成缺位，再flatten缩短实际实参数量。
三调用入口现统一在独立负反证检查后先finish，再保形收集；Some(empty)保留位置，未完成None不能授信，
成功出口仍有缺位则报内部错。空集合本身不作负反证，后续已知坏值仍核；普通函数空输入/空上界和Bottom callee仍域外。
原ExceptionMatch的完整Bottom target合同不改，不新增helper成功域。新增四组保形/未知/后继坏值配对测试，
仍保留原21项与原B真实prepare/finish；新冻结的Rust及产品结果须另行验证，不能沿用前一候选的统计。
