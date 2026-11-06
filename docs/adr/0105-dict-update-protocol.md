# ADR-0105：真实 dict 派生前的必要更新基础

日期：2026-11-05。状态：有界基础已验证，正常完整门禁通过；未远端发布。

## 依据及拆分

79a04f7的原docopt固定入口仅余Dict(dict)基类E0100；list阻断移除并不表示项目运行通过。
2276fdf恢复及备份后，先用固定CP3.12.13核真实dict合同。dict.__init__是更新而非清空，
fromkeys须调用派生构造，来源快路资格与iter槽有关，不能机械复制list布局。

独立四原源在CP、compile、JIT均exit0但输出不同：generator失败丢已提交前缀；第二pair
非法前多执行后续next且错误漏元素序号；mapping误走iter；update零参/kwargs被拒绝。
所以先独立修基础，再接真实布局。该切不开放dict派生，不重写哈希表，也不改全局比较ABI。

## 更新合同

- update先绑定零或一位置参数，任意kwargs是数据键；多位置参数在消费前失败。
  位置来源成功后才写kwargs，保覆盖值而不移动原键顺序。构造器共用正文而保原构造文案。
- 外pairs来源只iter/next，不求len/hint；逐项完整物化inner pair、验证长度2、立即写入，
  再取下一项。坏pair带当前#序号，不继续外源，先前成功写入保留。
- 普通mapping先读取keys属性。属性读取的AttributeError才回退pairs；keys方法调用失败
  不回退。generic keys先完整物化，再逐key取值写入；exact list keys活读原列表，getter
  修改keys会影响后续访问。self-iterator的运行时适配器先还原公开对象再选择mapping协议。
- inner generic按CP经过iter、游标的iter/长度提示、完整耗尽，不以N+1解包替代。
  inner迭代中的TypeError换为标准转换错误，不保被替换异常；调用者ambient异常仍保留。
  RuntimeError等用户异常身份、cause/context和用户帧不换写。
- generic物化复用ADR0103长度提示验证及至多4096额外预留，不截断实际消费；巨大合法hint
  的资源失败时点不承诺与CP逐分配等价。

## 支持域与旧路径

新增generic mapping/pairs只接不会调用用户hash/eq的精确标量键。目标也按结构版本复核，
每次用户回调后、写入前检查；自己的安全写入只更新已核版本，不每项扫描整个目标。
普通用户键、tuple/bytes等未列域是CP合法但当前明确NI，不说成非法，不计CP一致通过。

exact dict/native mapping保持旧raw路径及已有custom-key成功测试；同一raw存储self更新
直接无操作。该历史路径仍可能重hash，并未解决已存hash合并、popitem、压缩或重入缺陷。
不能将新generic守卫的保证外推给旧raw。mappingproxy继续既有独立受限合同。

dict构造既有kwargs能力不缩；新update kwargs入口保目标守卫。root dict描述符、派生
布局、fromkeys、缺键钩子、视图扩展以及全部dict方法另切，不能由本次更新成功推断可用。
重复**关键字的旧公共展开边界不在本次binder声明中消失。

## 表示及成本

PyDict字段、sealed与普通实例布局不变。只分离新协议冷路；mapping回调不能使raw早退
也分配C#捕获闭包。已知无keys的精确list/tuple可跳缺失异常，派生业务keys不能跳过。
外源不再全量快照；inner和generic keys仍按Python可观察合同物化。没有用户键全局缓存，
不以缓存hash规避可变hash语义。每次target guard是调用内值类型，只持目标和版本，按ref传pair helper，不装箱或逃逸；
首次写入或外部结构变化后会检查现有槽位，自身安全写入不重扫。

## 验证记录

原四失败源、九份派生CP草案及第十份keys/ambient补充均独立保存。派生合同只是后续参考，
不计Ryu支持。新六driver应用先CP冻结，四原业务源不改；新增协议应用的adapter只追加，
旧整源/golden仍另保。完整异常链单列，不仅对比最后错误文本。

首稳定Release b55f87ccb545153d310293768e3331e56c8b534399c8ee25071e710d057a2b31
前后源码hash一致，零构建warning；全C#1939/1939（新增12），六应用CP/JIT全过。
独立审查先CP实测加静态定位mapping不可hash键验证过早，尚未build时修复，不能假造旧
Ryu失败流；最终原反例等三补充源CP/compile/JIT均0且stdout逐字一致、stderr空。

## 性能审查与收敛

首候选同CIL/host ABBA四次exit0，原流完整保留。exact dict source约0.973–1.032倍，
普通属性/分配及get/set/len未见新回退；但list pairs慢23–59%、tuple慢27–102%，
generator n8慢159–204%且多576B/op，不能宣称整体改进。减少外源快照并不自动等于高效。

第二候选仅隔离已知精确载体：list每次重读Count，tuple直接下标，共用inner验证和提交；
sealed generator跳过必缺keys的异常查找，其next仍保原帧/ambient协议。guard改无装箱
局部struct并按ref写回，扫描用既有只读槽，无新字段、缓存或哈希表示。CP独立验证inner
回调append/shrink外list，以及generator不能设置keys；新C#对应三项回归。
第二稳定Release 8f2262547e26457d0d26fee309d208b9ac2f136e67d64f707809acb0fcd3f528
前后源码hash一致，零warning；全C#1942/1942、原六应用及独立四源全部严格复验通过。
同CIL/host第二ABBA四次exit0，32旧/47候选案例、每例7样本，共1106个原样本：

- 小generator原约3μs额外已消除，更新640→376B/op；大generator覆盖仍慢7.2%、构造
  慢3.7%，各少6336B/op。不能因内存下降说所有case都更快。
- 大list三类更新/构造仍约慢13–19%，clear/overwrite更新2072→0B/op；
  tuple小例约慢6–9%，大例约慢9–21%，分配不增。此剩余成本不隐藏、不靠平均抹掉。
- raw dict来源、普通对象及dict基本操作未见明显新分派回退；字典来源的少量时间改善
  仍受共享CPU波动影响，不宣称普适倍速。每run、迭代、B/op和GC原记录均保留。

版本v1/v2各自完整封存，同一host/generated只换runtime；旧不支持mapping/kwargs只报
候选绝对成本，不算新旧倍速。最终第二候选首次正常完整make check通过：1942 C#、228 CP基线、202 e2e和30原ignored；
fmt/clippy、全Rust/多文件自检及六driver应用全过。没有迁移旧成功断言或新增ignored。
本切未另跑docopt，无新项目通过，真实dict派生仍待下一竖切。
