# ADR-0066：普通类创建期 hash 槽及有限容器准入

状态：有限候选已完成本地验收：16项C#、4项真实CP/JIT差分及正常完整门禁通过，未发布。

## 问题与范围

原普通类没有在最终 namespace 中物化 Python 的隐式 `__hash__ = None`：类体自己定义
`__eq__` 时可能错误继承身份 hash。已有显式 None 又被当作可调用值，报出错误的 TypeError。
修复不能只让 hash(x) 拒绝：CLR 空字典/集合可能不调用 comparer，查询会绕过新 None 槽。
此外，dataclass 在冷类完成之后装饰；新增 None 若只按键存在性判断，会破坏 frozen 自定义
等值类的生成 hash。

本切只处理已经完成普通类体的单继承对象及其现有 dataclass 装饰域；复用既有完成事实与
类属性版本缓存，不扫描反向继承网络。String、Enum、异常与专用 native 布局保持旧分支和
守卫，C# 手造未完成类也不被新资格封死。Hashable ABC、任意元类/描述符、完整富比较不扩。

## 实现与成本

- 在既有 FinalizeOrdinaryClass 所有原验证之后、类装饰器之前，仅 own eq 存在且 own hash
  缺失时写真实 None。eq 即使非 callable 也算存在。继承 eq、类后 eq 写删都不重新抑制 hash。
- 普通实例隐式 hash 只查实际类链一次，None 给准备过的 unhashable TypeError；缺失沿身份
  hash，非 None 沿原 InvokeBound。用户正文异常不 catch/repackage，实例同名字段不干扰隐式槽。
- 只为已知 object 根 hash 提供类/实例/super/别名的真实绑定；不把所有 native callable
  泛化为 descriptor，不把 object eq 全局注入隐式方法。
- 容器仅在可能短路的空表路径，对普通实例检查原始 None 槽，不执行用户 hash/eq/getattr。
  非空继续 Of，不为正常键先算一次 hash 再交 CLR 重算。dict.pop 必须先判断 Count==0，
  保 default 或同键 KeyError；fresh、clear、删除至空三种历史一致。keys/items 视图沿既有
  Contains/TryGet；frozenset membership 的独立旁路也接同一准入。
- dataclass 先完成 field 占位属性的移除/替换，再于生成 eq 前记录 own hash/eq：None 与
  own eq 同时存在按非显式 hash 处理（即使 None 手写），frozen 可生成字段 tuple hash；
  None 且无 own eq 保不可 hash。现 mutable 默认值守卫自然识别新 None，不调用默认对象 hash。

exact 数字/字符串等 hash 主分支保持。空容器精确键新增至多一次 PyInstance 类型排除；普通
空表键额外一次缓存槽查找，非空不叠加此查找。显式 root 绑定时才构造方法包装，直接身份
hash 不经包装。类完成和 dataclass 仅冷时多做 own 字典判断。没有性能测量，不宣零成本或提速。

## 明确保留的边界

`collections.abc.Hashable` 仍不导出；原 `m3_6_hashable_pending.py` 不转正。
`eq=False`、`unsafe_hash=True`、dataclass 继承和完整 field(hash=...) 仍按旧范围。
`__hash__` 返回 -1/大整数的归约、非整数错误文本、空 CLR 容器对有效用户 hash 的漏调用、
成功 pop/setdefault 的重复 hash、缓存 hash 复制及反射比较优先均非本切全部解决。
正确期待保留 `tests/fixtures/hash_slots/hash_return_and_empty_lookup.py.pending`，该文件未执行，
不计候选通过。None 准入不等于完整 Python 容器协议，内部 IsHashable 更不等于 ABC。
Python set.clear 当前未开放，source 差分仅保 set 的 fresh/nonempty/deleted 三态；dict 的
四态和空 pop 三态完整保留，不为夹带测试增加 set.clear API。

## 固定参考与验收计划

唯一 CPython 3.12.13 进程，28秒 TERM 加2秒强制上限，实际 exit0、空 stderr、无超时，
0.141188 秒；13 个核心断言组及独立 boundary 观察完成，均经独立原流核验。
脚本 SHA256：386258757a34fe7855dfa140150f8f74329e84b7831977b05a07a78702aef694。
stdout SHA256：b3ff47f5ff4ba87296025108d3313b0802404fdb4b4cd3d79418546bdf8a3929。
时间仅表示限时执行完成，不是性能结论；boundary 参考成功不等于 RyuPy 支持。

本轮以16项新C# Facts、既有object/构造/dataclass相关回归及4项真实CP/JIT应用验证，
完整用户异常链9项投影不删帧。随后正常 make check。未修改编译器/ABI、工作流、AOT/性能工具，
不运行20项目或新增项目分子；历史pyfiglet仅先前751afef固定Linux JIT入口通过。

## 首次专项与保留失败

新运行时16/16首次通过，Rust driver首次编译通过；真实source首轮3/4，containers在跨三次
操作复用同一异常的context身份断言失败。原CP exit0，候选exit1；本切未改PyErrors/Attach，
原Prepare仅在Context为空时赋ambient、Attach保已有Frames，复用异常仍带第一次hash现场。
这是已存在的重复raise异常状态债，不改通用异常系统或删context断言。

首次完整source逐字保为reused_hash_exception_context.py.pending，SHA256：
0778f4619850ea203dd22a8f0dadf5171e990ccce241297c2d9700b23aa98681。
活跃source仅把shared异常构造移到每轮操作之前，仍锁每次同一对象、当前context和一次回调；
独立完整error.py九项用户链保持。第二轮4/4真实差分通过，零ignored；首失败原流保留，
pending不计通过。该阶段正常完整门禁尚待，不作项目/AOT/性能结论。

## 完整门禁终态

2026-11-04 正常 `make check` 首次完整运行 exit0：fmt、严格 clippy、workspace 测试、
Release 构建（零 warning/error）、802项C#（零失败/跳过）、228项固定CP基线、
202项e2e及30项原ignored均通过。没有快照迁移或新增项目/AOT/性能运行。
完整日志 SHA256：4f20c6882cb77639b5a14f602b36c68126786d70fe9024670ee9cd28b2c2de69。
候选仍未公开发布，以上不改变历史5/20项目统计，也不代表本轮重新验证pyfiglet。
