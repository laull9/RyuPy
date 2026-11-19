# exact set.clear：有限绑定与迭代状态

准备时仅完成源码和唯一 CP 参考；随后三个新应用 CP/JIT 与一项结构检查首次通过。
提取 pending 未单独执行，不计候选通过；参考 exit 0 本身不等于新 fixture/JIT 通过。

## 已有真实 CP 原流

来源父任务唯一 CP 参考执行，准备基线 HEAD `48f42f3`；完整私有执行记录另归档。
固定 CPython 3.12.13，单次墙钟 0.03213083199807443 秒，exit 0、无超时、stderr 空，stdout 74 行。
本目录保留以下原源码与原双流，不覆盖或用新结果替换：

- `reference.py.pending`：原 `source.py`，SHA256
  `62d0d7d51311cde077c86bb32b26b5359e59a0ffd5aeacf6f59aef28c537baeb`。
- `reference.cpython.stdout`：完整原 stdout，SHA256
  `c56e0958e02054dd8848b6afaca30ea7be8cb6ebdcd5b76eaf9f8d89ef2f9a24`。
- `reference.cpython.stderr`：原 stderr。
- `reference.result.json`：标记 summary 的结果摘要，非原始 JSON；保版本、各 SHA、退出、时间与预算，
  不公开解释器路径或私有证据位置。原 JSON 仍在私有验证归档，不覆盖。

`reference.py.pending` 原字节含版本校验，没有为了后续代码或 pending 标记改它。
原参考的 active 标签只是采集分类，**不是本次实现承诺**。通用展开旧债后来经源码审查明确
独立 pending；不能从原参考抹掉，也不能据 active 标签将其纳入本次已通过分母。

## 三个应用与一个结构检查

- `bindings_and_identity.py`：direct/saved 零到二参、普通关键字优先、空/非空星号展开；
  身份、别名、空/重复 clear；用户键的 hash/equality 在 clear 中不执行。
- `evaluation_and_iterators.py`：参数异常身份、已发生修改保留、star 失败顺序、接收者重绑、
  保存方法，活 iterator 的 size 错误、错误持久性与真实耗尽。末项已取不能提前等于耗尽。
- `error_chain.py`：保存方法绑定错误穿过两层函数，保已有 ValueError context 与全部用户帧；
  finally 打印集合，证明失败没有 clear。该链不在唯一74行参考中；随后自身CP-first及JIT已通过。
- `crates/ryupy-driver/tests/set_clear_structure.rs`：一个真实源码结构检查，正常前中后端流水线，
  核普通 direct/PIC、保存和展开路径及参数回调顺序。不手造 MIR/PE，不当作应用执行。

前两个应用输出分别应匹配 `bindings.reference.stdout` 的 21 行与
`evaluation.reference.stdout` 的 30 行；它们是从唯一 CP 参考按明确行号抽取的原字节。
完整行范围及标签说明在 `extraction.txt`，不是这两个新源码已经执行过的证据。
新 driver 先核 CP 与该合同一致，再 build/JIT。异常链的行号取自新源码，首次 CP-first
必须核正确，否则停止在 CP 阶段，不能据 JIT 调整期望。

每个应用独占 root；保 source、CP 双流/退出、编译诊断/结果、JIT 双流/退出，成功失败都保留。
普通应用比完整 stdout/stderr/exit；错误链仅剥路径、源码展示、caret、空行，保全部用户帧、
行号、错误类型/消息与链分隔。版本探测与实际应用执行分开计数。
新 driver 的版本检查和三个应用均用 `-I -S -B` 隔离 site、环境与字节码缓存；
不设置会被 `-I` 忽略的 PYTHONHASHSEED。成员与迭代用固定整数，用户键使用固定 hash，
不存在依赖随机字符串 hash 顺序的输出。

## 通过域与 A 方案

本切只实现 exact set 的 clear 与保存方法，不扩根描述符或 set 子类布局。
独立 set iterator 保原确定快照；已 finished 后永远耗尽，size 错误粘住；
在活态下先检查 Count，再检查非空 clear epoch，最后才读 snapshot。
空 clear 不改变 epoch，非法调用不触发 clear，不修改 iterator 状态。

CP 实测 clear+同 size 重填的新成员会被 iterator 读到，包括已经取出旧 singleton、
但尚未确认 Stop 的情形。当前 A 方案对此明确 NotImplementedError，不让旧 snapshot
悄悄返回陈旧成员/提前 Stop。此边界不计兼容通过，必须保正确 CP pending。
无 clear 的 remove/add 同 size 保既有 snapshot 欠账，不扩大新 NI，也不宣称本切修复它。

## 独立 pending

`boundary/*.py.pending` 是自包含、尚未独立执行的源码，不在三个应用分母内：

- `same_size_clear_refill.py.pending`：三种真实 CP 正例，期望新 2 或相同成员继续被读出，
  不断言 RyuPy 的 NI 为合法 CP 结果。来源参考 58–65 行。
- `keyword_expansion.py.pending`：duplicate keyword、非字符串 keyword 完整消息，及任意
  mapping.keys 的真实副作用/LookupError。来源参考 10–11、20–21、26 行。
  这些是 PyCallOps 的旧通用展开欠账，本切不改通用 binder，不弱化诊断或删除副作用。
- `root_and_subclass.py.pending`：根 descriptor 的正确/错误 receiver、参数与 set 子类继承、
  override、root 绕过。来源参考 66–73 行，不扩通用 descriptor/特殊布局框架。

旧 tomli `83c` 证据仍为 CP0/compile0/JIT1 单行无帧 set 无 clear；本通用修复不等于唯一现场定位，
不等于 tomli 项目通过，不改变固定项目统计。

原CP输出中StopIteration后的尾空格是print原字节，原流及精确抽取不裁剪；源码/文档仍按常规空白检查。
