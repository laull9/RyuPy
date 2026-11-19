# ADR0118：itertools.count 通用有限惰性数值迭代

- 日期：2026-11-05
- 状态：有限域专项、独审、有界性能、正常完整门禁与干净原项目复测完成

## 来源与最小范围

干净eed648d原Markdown3.5.2在ClassVar接入后，CP/编译通过而JIT首错为itertools.count
导入缺失。此次基线451ddbc，通用exact bool/int/float，不做项目专用编号器或导入空桩。
新增PyCount/PyCountBinding，itertools导出和PyObject.TypeOf各一个窄触点；不改共享
Binary/数值/迭代器热实现，不新增编译器RuntimeFn。真实稳定PyNativeType直接识别
专用载体，不按可伪造TypeName判断；公开模块重绑与同名native注册都不改变实例root。
TypeName用itertools.count保持缺属性/len错误，类型短名与模块元数据仍各自正确。

## 绑定与惰性状态

构造先总位置/关键字数量，再按关键字插入次序判未知或重复绑定，最后start/step数值
资格；不调用int/float/index或用户descriptor来强转。exact常见非数值内建拒TypeError，
未知用户数值协议明确NI，不声称CP一定拒绝。类级wrapper、派生、copy/pickle及完整
用户数值/complex/Decimal/Fraction另保完整CP正例pending，不能改成NI成功验收。

整数步长1（含True）且start可放入long时用窄游标，首项按CP规范化bool为int；long
上界先得到正确BigInteger下一值再切慢态，永不回绕或耗尽。其它情况保原start/step
引用，next先完成old+step，成功后才提交下一值并返回old；错误不产出old、不推进，
可重复尝试。Current保最近成功产物，不与未来状态混淆。任意精度整数的大小可增长，
常数个状态引用不代表数值占用常数空间。单一对象态设计备份未构建，不冒第二候选。

float逐次IEEE相加，禁止index乘step/FMA重排；bool慢态首项身份、混合int/float首项
类型、有符号零、NaN/inf与巨大int转float首项前OverflowError均覆盖。repr显示下一值，
只省略整数值1的step，float1.0仍显示；无限count不提供len/length_hint，也不物化。
显式实例iter/next、builtin next、for break、islice共享同一状态，零长度islice不消费。

## 证据与边界

四份完整公开主应用、三份安全完整pending与124条CP真值先做固定CP3.12.13双跑。
设计时额外包含有界重入尾段的原完整源两次CP SIGSEGV，原字节与失败完整隔离保存，
不安装正常门禁、不重跑、不声称唯一定位CP bug。另建无该尾段的独立安全源双跑过，
不冒原源通过。重入仍未取得可靠完成真值，当前exact实现也不运行用户数值回调。

旧候选四主CP与编译均过，JIT四源均缺count导入；安全custom源也编译过后缺导入，
标准数值/派生源编译在int基类E0100。copy/descriptor源CP exit0且stdout正确，CP自身
两条DeprecationWarning触发父runner空stderr假设而停止后续，原失败保留；仅按已有
设计原warning逐字比对（只规范化源码文件名）更正解释，没有重跑CP或改源/golden。

首候选24项C#和四份原完整CP/compile/JIT首次全过。独审完整绑定源的CP前置又发现
纯kwargs超量时应写keyword arguments，原生产和一条C#期望均错误；独审原失败源
不改，另存只修正CP预期后的完整源，双CP过而原候选唯一JIT红灯。只加纯kwargs
错误文字分支并修同Fact真值/组合，保原源/原golden及失败，不改数值/迭代算法。
该完整源作为第五主pending加入。B2重新24C#与五主CP/compile/JIT全部通过，独审
同一完整绑定源也全过；其它三独审成功源依据逐文件生产差异证明复用B1，不冒称
B2又重跑。三个完整边界不转正。所有产物只以固定CLI/runtime/source身份取证。

## 性能

预注册先12进程/400样本，正式前明确增补真正受共享改动影响的type(native)旧路径，
修订为12进程/440样本：七条已有公开迭代/整数/原生类型查询函数对A/B，两配置ABBA；
八条新增count仅B绝对成本，两配置各两进程。旧版本没有count，不能虚造A分母。
完整公开函数真实生成CIL，128项流逐项消费，不用数组替代；构造/首项另计，不混
热吞吐。全部负样本、分配和失败保留，正式矩阵唯一12进程/440样本、15.297秒全部完成，身份/初始化/checksum通过，没有
追加实验或重跑。七共同路径两配置B/op均与A相同；禁tier repeat聚合+15.61%、chain
+6.27%，单进程对照另有默认zip_longest+2.13%、禁tier native type+2.60%，全部保留。
默认tier样本明显换挡/离散，不能把平均负百分比当确定提速，不宣称统计显著或全域
无回归。count只B绝对成本：构造56B/首项144B；跨cache、BigInt、float和显式方法
完整128项函数分别9824/25224/11320/4008B，包含Python调用/循环，不冒零分配。旧十四函数体与全部B1预检原流保留；修绑定后只做B2两smoke，
覆盖修订又仅编译新的common模块、复用动态host和count PE并三smoke，不丢旧证据，
全部正式计时在修订冻结后唯一执行。TypedDict+512B及
F0/F1等历史风险独立开放，不因新API完成而关闭。

## 完整门禁

唯一正常make check130.017秒exit0：2226C#、398CP固定基线、398CP-only自检、292
编译差分通过；110e2e ignored=旧107+三新完整边界。旧394名称/状态保持，八新增
为五支持三ignored；其它Rust775通过+1ignored，不把ignored算通过。冻结11144文件，
实际Release库前后均95497a38，未低空间中止。两次CP标准warning与早期独审oracle
错误原流均保留，不混为生产门禁失败，也不丢掉真实B1红灯。

门禁后仅文档和四主源码首行说明注释改中文，执行体/AST与golden逐字不变，独立
源差异记录明确区分，不冒称这些注释与门禁快照逐字相同。

## 原项目与恢复

干净功能提交ed7bdb2d5d7b637ee8890338f72f5e661d7d3ea0上，两原固定入口各唯一
CP/compile/JIT尝试。docopt0.6.2三阶段exit0，stdout16B逐字等golden，JIT stderr空，
CP四条SyntaxWarning共860B保留。Markdown3.5.2的CP与编译exit0，CP stdout77B等
golden，JIT exit1、stdout空、stderr57B为ImportError: cannot import name 'overload'
from 'typing'。不重试、不改原源或入口；count缺失已越过，但Markdown仍未通过。
63输入、两个3187文件源树、20根次序与实际runtime/工具前后一致。不回写旧同提交
20项目矩阵，不声明完整库、其他平台或AOT。下一切研究通用typing.overload真行为。
恢复继续固定基包加最新累计包，保持每件小于32MiB、旧证据可逐字重建及独立实恢复；
不在新组合核验上传前覆盖删除旧备份，不推送远端。
