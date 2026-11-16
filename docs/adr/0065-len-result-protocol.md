# ADR-0065：len 的冷长度结果协议

状态：有限实现、专项及首次正常完整门禁本地通过；未发布，不代表项目或性能通过。

## 问题与边界

len 已通过完整动态实参建立与严格单参绑定，不存在此前 int/abs 的首参吞余参问题，故本切
不改编译器或 ABI。真实缺口是 PyOps.Len 的用户 __len__ 结果直接交给 PyCast.ToInt64：
负机器整数可直接返回，结果对象的 __index__ 未执行，错误采用不对应长度协议的拆箱文案。

已有 bool 冷真值协议另有有限长度校验。不能仅从现 bool 实现推断 CP len 的边界，应先用
固定参考确认可共享的语义。无长度对象的 len 报错、bool 默认 true，以及 __bool__ 的优先
仍归各入口，不混进共同的结果转换器。

## 有限设计

共享冷校验返回真实 long。actual bool/int/long 直接处理，不先构造 BigInteger；大整数
先负值检查再正上界，合法值最后窄化。非整数结果调用类型级 __index__ 一次，不退到 __int__、
不递归调用返回对象的 index。len 取实际长度，bool 只检查转换后结果非零。

exact Enum/list/tuple/dict/view/set/frozenset/string/有限str子类/bytes/bytearray 的 Len
分支及 bool 的 primitive/container switch 保持原样。只转换隐式协议结果，直接
obj.__len__() 仍返回用户原对象。用户正文异常不 catch/reprepare；本层自造的绑定、缺槽、
结果校验错误仅在对应冷点 PrepareRaise，保调用现场与 ambient context，不加通用调用 catch。

len 的自身绑定保持关键词优先于数量，修正其专用消息；不改共享 Single、关键词检查或
FlattenArgs。任意 Mapping、跨 ** 重复名、复杂展开时点/异常帧仍为独立旧债。
__index__ 返回 bool 或真实整数子类的弃用警告域保正期待与明确未实现边界，不能静默当完全
支持；原 bool 已声明的 NI 文案保持。操作名参数只选择该 NI 文案，不改变转换算法。

## 参考与验证计划

固定 CPython 3.12.13/64位一次进程、30秒上限、8组参考实际 exit0、空stderr，0.037993秒。
脚本 SHA256：0d0f68e5956608efe1c35e99fdc0c9650af8fe0fa720773c0b2a6f9bfb925545。
stdout SHA256：fa40dbe8a6d5781674f068eddf5f39138e5c2d1675b55bfcc64c676a8c8f178d。
独立核验确认 len/bool 对直接及 index 的负巨大值均先 ValueError，正超大值 OverflowError，
直接 bool 合法，索引只调用一次。关键词错误优先于 arity；用户异常身份/context及已返回的
长度正文不出现在校验错误帧中。该参考不是候选执行，也不是性能数据。

候选计划以10项C#、原bool12项及3项driver（1MIR结构＋2CP/JIT应用）验证，再运行正常完整门禁。
不新增公开观察API，不改变项目或ignored分母。已恢复的pyfiglet源码大量使用普通list/str的
len，但未发现自定义长度槽；这是热路回归对象，不是本切新项目受益或首阻断证据。

## 首次候选专项

运行时首次22/22通过（新len10＋原bool12），零失败或跳过；Release构建零warning/error。
新len driver三项首次通过（1 MIR结构＋2 CP/JIT应用），原bool driver三项一并通过。
原双流与退出码真实比较，未重跑参考或项目；当时正常完整门禁仍待。

## 最终本地门禁

首次正常 make check 已真实 exit0：fmt、strict clippy、workspace、786 项 C#（零失败/跳过）、
固定 CPython 基线228项、e2e 202通过＋30原ignored全部通过，无快照迁移或额外测试重跑。
日志 SHA256：9b0dbdd2032db9dae9af601cb14ca0e759422450ed134602021986d7fbd4e67f。
没有修改 Rust 编译器/ABI、AOT/性能工作流或全局 PyCast/调用展开；没有新项目复测、原生发布
或性能结果。警告正例 pending 不计候选通过，历史项目分子与已知性能负项不改变。
