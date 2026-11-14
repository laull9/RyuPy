# exact list四方法参数绑定回归与首失败证据

本目录只覆盖`clear/reverse/copy/pop`的有限参数绑定，不宣称整个list或索引协议兼容。
源码准备时未执行下面三个新应用，也未执行新standalone pending；真实验收由父统一进行。
不将预期行号、源码检查或旧baseline exit 0称作新版本通过。

## 三个应用与保留范围

- `active.py`：首次`repro.py`原文只删除两条`scope='boundary'`的report调用，其余字节不动。
  34 active组保留原成功/错误及副作用；yield组有两行，所以active输出35行，外加完成标志。
  为保持同源，两个未调用的boundary函数定义与原注释仍在；它们不属于active执行域。
  driver将新CP stdout与历史CP stdout剔除两条boundary后的完整字节相比较，再比较JIT双流与退出码。
- `success_and_order.py`：空/单元素、浅copy及元素身份、saved零到三参、普通小整数/bool pop、
  直接三参、位置/star/keyword/mapping全部求值副作用、展开异常优先及四方法跨yield接收者冻结。
  参数回调独立新建LookupError，只抛一次并检查异常身份，防止helper捕获重包。
  不加入合法自定义`__index__`成功或宽long索引修复。
- `error_chain.py`：前三方法捕获打印类型、消息和空context；最后模块ValueError handler触发pop绑定错误。
  8条完整用户链投影逐帧、逐类型消息冻结；只剥路径、源码展示、caret和空行，不丢用户帧或链标记。
  行号最初由源码计数冻结，随后首次 CPython 3.12.13/JIT 差分真实核验通过。

Rust driver固定CPython 3.12.13，每次独占临时目录；在比较前保存原source、CP双流/exit、
编译诊断与结果、JIT双流/exit，失败目录保留。使用默认优化的正常build与JIT，不手造MIR/PE。
三应用之外的compiler结构检查由独立文件承担，不能把结构检查当运行通过。

## 首次实际baseline，保持原字节

唯一来源：`/workspace/shared/ryupy-list-method-bindings-baseline/run/`，
运行基线`bb6b59078ae8030a11d7da661d42389db8c215e6`，固定CPython 3.12.13。
以下文件是那次实际执行的归档，不是新fixture的预期输出或修复后结果：

- `baseline.py.pending` ← `repro.py`，6122字节，原字节不加pending头。
  SHA256：`fdc2771a9ee446987dbcb456feff6f0d3d8f53ae0d35ab54f534e692c80df342`
- `baseline.cpython.stdout` ← `cpython.stdout`，3558字节。
  SHA256：`da50652979e937a7cef90b8f41520dc290c30a705f73c2a9f361d120963ffe98`
- `baseline.bb6.stdout` ← `ryupy.stdout`，3318字节。
  SHA256：`ce554a9b1ed7e7e98b99ac3288cfa230ef2e3d08b36e3060165133fe80457f8c`
- 同时保留`baseline.result.json`、CP/Ryu stderr和build双流，记录真实exit与工具/产物hash。

准备时三件原文件分别`cmp`一致并以`sha256sum`核对。
那次CP、build、JIT均exit 0，但`raw_equal=false`、`outcome=baseline-difference`。
原程序捕获错误后继续，退出成功不能替代语义对齐。它含2个未闭合索引boundary，
不得执行完整归档来冒充本绑定修复已全通过，也不得覆盖这份首失败证据。

## 两个域外boundary

`boundary/index_protocol.py.pending`和`boundary/wide_index.py.pending`是新提取、尚未运行的
standalone源码；pending不是认可偏差，也不是已完成。`boundary/original.*.stdout`只从上面的
首次实际原流抽取原行，属于历史证据，不是这两份新standalone曾经跑过的证据。

原CP实际结果分别是合法`__index__`返回10并记录`INDEX CALLED`，以及4294967296索引抛
`IndexError: pop index out of range`且不修改列表。原bb6实际结果分别是类型拒绝，以及截断后
错误删除首元素。后续需要独立处理ToIndex协议和PyList.Pop的宽索引范围，不能靠改期望消失。

## 首次候选验收

三个应用首次3/3通过，结构另1/1，新C#14项与既有numeric10项同批通过。
随后首次正常完整门禁通过（832 C#、228 CP基线、202 e2e＋原30 ignored）；
baseline与两个域外pending仍保持历史边界，不计修复成功域。

## 后续索引修复

ADR0071 已为上述两类问题接入独立 pop 转换/边界修复，并由
`tests/fixtures/list_pop_indices/` 的三个新CP/JIT应用验证。此目录的baseline和两个提取pending
仍作为前切历史证据保留，不覆写原输出；两份提取文件本身没有另行独立执行。
