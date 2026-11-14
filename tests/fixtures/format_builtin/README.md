# 内建 format 的有界验证

`protocol.py`、`binding.py`、`text_integer.py`、`traceback.py` 是由 `crates/ryupy-driver/tests/builtin_format.rs` 显式引用的四个活跃 fixture。文件中的 `# ryupy: pending` 是首次验收前的历史标记，不会令该专用 driver 跳过它们。首次3/4、异常链窄迁后定向1/1，随后完整门禁中四项均通过，旧失败源仍pending。由父任务统一运行：先真实固定 CPython 保存原始双流和退出码，验证参考合同，再 build/JIT 比较；错误时保留现场。

`long_tail.py.pending` 是明确不自动执行的长尾正例合同。专用 driver 不引用它，`.pending` 扩展名也不参与 `.py` 自动发现。它不新增第五个活跃应用，不属于首切完成声明。文件本身尚未运行 CPython 或 RyuPy；不得将参考探针记录当作此文件已通过。

唯一CPython3.12.13参考的脚本/原流哈希与范围见[ADR0073](../../../docs/adr/0073-builtin-format-protocol.md)，原始证据另保恢复包，不依赖clone后的临时绝对路径。对应记录：

- 重复 `**` key：`duplicate_keys`，应抛 TypeError，消息 `format() got multiple values for keyword argument 'x'`。长尾程序捕获并断言此真实错误后成功；不以 NotImplementedError 冒充正确结果。
- 混合 `*` 后规格求值顺序：`mixed_star_order`，事件 source/iter/yield/stop/spec，结果 `2a`。
- 单字段 f-string 返回子类身份：`fstring_single_identity` 为真；`fstring_concat_identity` 为假。现有 f-string ABI 不在首切更改。
- 根方法：`object_root_identity` 为真、`super_default` 结果 `SUPER`。隐式内建兜底不等于显式根方法已实现。
- `String.__format__` 覆盖：本次参考没有对应记录，末段仅是待独立 CP 确认的语言预期，不标成本次已证。RyuPy 当前守卫拒绝此覆盖，首切不开放。

首切合法未支持的格式语法另由 runtime 单测验证明确边界。不得更改 CP 输出、删除用户异常帧或把上述长尾加入本切已通过计数。

## 首次执行与保留的旧异常链债

本轮首次四驱动实际结果为3/4：protocol、binding、text_integer通过真实CP/JIT差分（含str子类no-op身份与decimal4300位边界）；旧traceback源失败，不能计为4/4。原源逐字节保存在 `nested_cause.py.pending`，不被driver引用，不自动执行。原始CP与RyuPy证据及失败现场另保，摘要和哈希见ADR0073。

旧源在用户槽内raise并catch ValueError，再raise RuntimeError from cause。CP内层cause仅有槽第8行；RyuPy额外附加模块22、caller20、convert14三帧。外层完整链相同。这是保留的嵌套caught异常traceback债，本切没有修复或放宽它。

活跃 `traceback.py` 改为模块级先raise/catch并保存cause，然后用户槽raise RuntimeError from该cause；仍比较全部外层模块/caller/convert/槽帧、明确cause链及slot→finally输出。只迁移源合同，不删除任何比较帧，不改runtime traceback，新源定向1/1通过，随后完整门禁四项全部通过；原失败源不计绿。


`general_float.py`是阶段3新增的第五个活跃应用，仅显式g/G矩阵；297次format、成功290行。
首次真实CP→build/JIT原双流差分通过，不改变原四源与异常投影；正常完整门禁899C#/228CP/202e2e+30原ignored通过。
规格范围、真实后端限制及未支持项见ADR0074，不以本轮差分声称tabulate或性能通过。
