# ADR0125：模块裸名与 global 删除

- 日期：2026-11-06。
- 状态：功能、正常完整门禁、最终真实PE与CIL独审通过；唯一性能观测完成，时间回退风险未关闭，未推送。
- 开发基线：7bea9816bb5ad1800e9f711fd26a6ba60c20b021。

## 原项目驱动的范围

原 Markdown 的 find_spec('html.parser') 失败不能靠给 native html 填 source metadata
解决：实际 native html 只有 escape，没有 html.parser、unescape 或 entities 实现。
不能把不存在的解析器配上假的 SourceFileLoader，也不全量替换标准库。

因此保留原项目入口，另用固定 CPython 3.12.13 未改的 html/__init__.py、entities.py、
parser.py 与 _markupbase.py 四文件闭包，显式 --include-module html.parser 与
--instance-module html.parser 真实编译。旧 CLI 的实际第一条诊断是 _markupbase.py:20
的 `del re`。这是通用模块绑定能力的缺口，而不是 Markdown 名称特判。

本切只实现模块裸名/global 删除。公开 loader、完整解析器及后续 regex/super 等能力
独立推进；闭包复现也不等于默认原项目通过。

## 选择

具体合同见[模块删除](../module-deletion.md)。HIR 精确收集 may_delete，定义与异常
静态身份证明服从公开绑定；类型层不把删除目标当读取，MIR 分别清 typed flag、动态
哨兵或实例真实字典键。codegen 只消费 MIR，并按实际使用惰性添加四个精确 RuntimeFn
引用，不给所有模块引入实例状态。

只扩当前可正确表达的语义。默认自定义 builtins 和 __name__/__doc__/__annotations__
删除先明确 E0100，不用复制属性字典掩盖默认存储限制。实例路径沿 B2 的真实 owner。

## 发现并修复的边界

独审在候选成功用例之外发现默认元数据初值/定义协议未闭合、自定义 builtins 普通键
回退漏守卫，以及文本 range 快路绕过被删/重绑名称。分别补明确边界和真实 callee
降级，并加独立单测。range 再加完整独立源，不修改首批源或黄金输出。

首轮 Rust 专项的新增 codegen 测试漏写基线既有 __run_main 期望项；保留失败日志，
核实基线生产代码之后仅修测试预期，没有把原有方法从实现删除。

综合真实PE首轮实际执行了嵌套函数/方法/生成器global del，随后在同模块可删ValueError
作为UserError基类处明确NI。源码、双CP成功、真实失败及未运行的后续部分完整保留；
原源逐字进入boundary_deleted_exception_base，不能以另分模块的新suite覆盖旧失败。

## 验收记录

- 正常make check最终通过：2377 C#、861 Rust、5项清单自测、476 CP基线、
  476 cases selftest、334编译e2e，146 ignored；11542份源码前后逐项相同。
  第一轮因旧driver测试仍要求module/global del拒绝而失败；两份原源逐字转正，
  其余cell/静态局部/嵌套目标负例不变，14项driver专项后重新完整门禁通过。
- 最终CLI SHA256为3f9a2ba685011930b6ce1872693127f16e3797b36d44355bb653480c2d0a046b；
  Release runtime为c8be21bec4939eaecc4fde175576b49b0908b796eee562d9e467da329c0fca57。
  正常门禁feature合并改变CLI字节后，重新编译全部15份完整源：8份严格通过、7份边界
  保留。原source/golden未裁剪；原组合异常基类源也逐字保留在pending清单。
- 新的真实独立模块suite使用相同实例正文及相同Host，全部原默认断言保留但作用域
  明确分开。最终82/82生命周期检查通过，stdout与固定CP完整核心逐字一致；不把
  同模块的旧组合NI改记为通过。Host只触发真实Python生成正文，没有模拟del。
- 四个附加边界均实际执行：cell、class namespace、tuple目标的E0100断言通过；eval
  在删除后首次名字回退明确NI，但冻结runner预写了另一段消息，保留整窗failed。
  独审确认这是期望文字错误，不改生产错误消息、不重跑，也不声称四断言全绿。
- 最终8个正例PE与候选002各只差COFF时间戳，raw metadata与所有CIL相同。
  runtime全728类型、1921字段不增，5112旧方法体/5165旧方法定义保留，恰增4个helper。
  明确映射Roslyn有限闭包编号后，旧ABI、locals、EH与规范指令全相同；3533 raw CIL
  hash及4835 token变化原样归因，不称原始字节相同，也不由结构推出时间无回退。
- 01虽有int标注，实际main动态消费沿旧规则使全局为object；8普通完整例不充typed
  删除的真实CIL证明。另在最终独立纯PE确认Int64/Double/Boolean/String与四个bool
  检查，重复删除NameError；common/class与typed A/B三对无del完整PE仅时间戳不同。

唯一20进程/288暖样本完整完成，无补采；40默认配对分配相同，但默认module_bare
+4.002%/+185.153%、dynamic_defaults +23.124%/+2.126%、生成器+22.518%/+14.675%
及其余不利全部保留，未可靠归因、不称噪声、不称无回退。新首次builtin fallback整段
分配3,103,080B；完整绝对值、20个Main与12额外冷段见[性能报告](../module-deletion-performance.md)。
B2及ModuleSpec/find_spec等历史风险不因本切通过而关闭。

## 干净提交后的原项目结果

功能提交 6502e10 的原 docopt/Markdown 各一次 CP 与 compile 均通过。首次两次 JIT
在新增 RLIMIT_FSIZE=4MiB 条件下实际 SIGABRT，原流完整保留，未写成成功。
一次单独校正只移除该新增限制、各重执行同一 PE 的 JIT：docopt exit0 且 16 字节
stdout 等 golden；Markdown exit1，375 字节 stderr 仍是 htmlparser.py:39 的
find_spec 原 NI。没有重编或重跑 CP，不把观测变化说成已证明底层内存失败原因。

未改四文件闭包单次 compile 的旧 del re 首错消失；新首错为 html/parser.py:21:25
的 re.compile 被环境名字 compile 守卫拒绝，另有 super 等共 21 条 E0100。
无产物、无 JIT，未将窄闭包或原 Markdown 计为通过。完整身份和失败保留见
[模块删除合同的原项目记录](../module-deletion.md#原项目与未改标准库闭包复验)。
