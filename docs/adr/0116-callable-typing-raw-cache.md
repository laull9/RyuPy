# ADR0116：typing.Callable 独立原始键缓存与真实参数转换

- 日期：2026-11-05
- 状态：有限域专项、独审、有界性能、正常完整门禁及原项目干净提交复测完成

## 来源与范围

ClassVar未验收候选的暖/冷原键回归按普通根、Tuple、Callable分解；本切基线是已验收
c8721627与实际F1b DLL。仅修Callable外层与独立inner raw LRU、真实alias类型和必要
_GenericAlias直接父关系，不恢复C1、不把Markdown首错假称已解除。ClassVar导出仍缺。

五个生产触点包括Callable专用partial、Subscribe分流、alias TypeName、metadata名称表
及PyBuiltinTypes两处有限关系。322保护运行时文件逐字不变；SpecialTypeArgument、
GetArguments、GuardHelper原函数体及其它alias算法不变。没有替代原生类型登记或完整MRO。

## 外层、冷相位和缓存

外层先验证exact二元tuple，已知非tuple内建值给出CP确切TypeError；可能有__class__
及len/iter副作用的未知实例或派生仍NI，完整合法源pending。exact list参数每次调用
含暖命中均只快照一次，随后形成inner raw pair；tuple/scalar原形不抹平，list与同值
exact tuple可以命中同键，原list后续修改不污染缓存。原始键在None转换前保身份区别。

inner复用真实raw hash/eq、失败不插入及128项LRU；独立于普通/SpecialForm/Tuple/
Generic池。静态方法组仅初始化一次，冷构造不进入旧normalized Intern。新增池最多
多持有128条强引用，不承诺即时析构。暖命中绕过正文，但每次hash及非同身份等值键eq
仍消费自身helper，不能称暖缓存不读任何全局。

冷正文先对result调用_type_check，再对args逐项仅_type_convert；不能把args误作
result的严格类型检查。None转NoneType，裸Generic/Union/Optional/Literal可按CP
透传；字符串ForwardRef、Ellipsis参数表达式与未知采参保NI。然后按次序消费
_CallableGenericAlias、_TypingEllipsis、_collect_parameters。空args仍有result，
所以此处不同于空Tuple，构造阶段仍消费sentinel。Generic[T]、内部嵌套tuple及动态
参数采集完整行为保持pending，不用静态类型拒绝替代真实helper次序。

Callable alias类型改为真实_CallableGenericAlias，补已证_GenericAlias直接父边，
保isinstance与issubclass原本为真的关系；其它类型负路径也覆盖。原同名、数值、异常
关系早返，原生登记优先，不声称完整typing类型工厂/元类反射已支持。

## 真值与独审

25原合同源CP隔离双跑全部成立，新增独立write-only helper源也双跑成立，共26源。
基线26源均未严格通过；其中3个旧编译边界保留，余下在JIT暴露缺口。候选原11主中
10个严格通过，saved_receiver_warm_helpers因读取未导出的_CallableType失败，整源
原文保pending；不为求绿新增假helper或删断言。新窄源仅在独立进程写helper、不读/
删除/恢复导出，单独CP/compile/JIT通过。最终11支持、15完整pending。

新增18 C#和198旧typing/metadata回归通过。独审复用4原源和1独立负类型源，CP双跑
及固定runtime唯一compile/JIT均严格通过；第6完整探针前段暖等值/身份短路通过，
后段真实eq/hash helper改绑执行仍NI，因此整源保pending，不计第6严格通过。
所有原始setup失败、测试草稿错误、旧基线失败及不支持完整源保留，不覆盖证据。

## 性能与验收

旧22公开整函数源/CIL/迭代数保持不变，新增4 Callable及4真实类型层级路径。
CP19源各双跑、A7/B17必需断言与共同30路checksum通过；额外旧private helper完整
源仍失败并记pending，审计器误要求直接CIL call的失败原流也保留，修正为核公开
CallBuiltin及原AST后只续跑未执行部分，没有重建/重跑A或改变源/CIL。

默认与禁tier各唯一ABBA，8进程1200样本约51.41秒，无超时或重跑。每侧每配置只有
两个独立进程，固定8192热身不保证tier稳定，不作显著性或全面无回退结论。
Callable热默认/禁tier -4.36%/-23.86%，880→872B；冷 -2.50%/+18.14%，两配置
1136→1256B（+120B）。冷路径新pair/原键与真实检查的成本不被热收益掩盖，未进行
额外重跑归因。args读取默认+33.70%/禁tier-14.18%，0B不变；get_args为+6.68%/
-5.32%，208B不变。四层级公开路径96B不变，真实父关系isinstance -3.94%/+13.49%，
issubclass +29.10%/+22.82%。这部分A旧同名关系与B真实父边成本不同，不能声称
原共有分类器完全无回退。

旧22路径B/op中位数未增加，但TypeVar hash +13.64%/+4.22%、Optional冷+10.06%/
+1.58%、List冷+5.15%/+4.08%、禁tier Dict热/冷+8.80%/+9.87%等全部不利保留。
F0 Optional冷+848B、F1a/F1b旧风险仍开放。新Callable池最多持有128条，短时分配
不是长期驻留量测量。详细30路径与配置离散随恢复证据保留。

只转正11支持源，15完整源仍pending；正常make check唯一运行exit0，120.018秒，
2166C#/361CP/263e2e通过、102ignored，非e2e Rust775通过和1旧ignored。旧339项
（252通过/87ignored）逐名状态保持，只新增11通过/15ignored。361CP-only独立核验，
只排除phase4四项，不计Ryu支持率。11019源size/hash/mode与预先冻结快照一致；独审
两次实际观测均在门禁结束后，不虚称有独立运行前观测。门禁中运行时SHA保持不变。
独审初版误要求性能host stderr为空，原失败保留；v2改核30条正常摘要原字节/hash，
没有重跑产品测试或计时。唯一门禁、全部负性能与原始失败完整保存。

## 后续

原docopt与Markdown在干净功能提交各唯一复测，不能用桥接/导入成功增加项目分子。
后续再接回保存的C1 WIP，重验普通List原暖键与新raw tuple冷键，以及Callable args
仅转换/result严格检查的区别，不能盲目cherry-pick旧通用guard。
恢复采用已验证固定F1a基包加单一当前累计增量；每件小于32MiB，独立仅由两包离线
恢复全部历史原证据、源码快照与Git/C1，后续替换固定增量ID而非形成无限旧增量链。

干净68220e7/tree c050179的原项目各唯一复测：docopt0.6.2 CP/编译/JIT均0，
stdout16B同golden，四条SyntaxWarning共860B保原流；Markdown3.5.2 CP/编译0，
JIT1仍为57B ClassVar ImportError。63输入、两份各3187包源、原入口/golden与20搜索
root次序及实际DLL经独审核准，不重跑、不增加项目分子。锁表实际21项，其中原合同
选20包，humanize4.9.0未加入此次运行；独审初版误设20锁项的失败与修正均保留。
功能提交相对门禁仅三份结果文档变化，生产、测试源和golden字节不变。
