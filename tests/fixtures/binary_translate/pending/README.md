# 通用调用展开的既有重复关键字边界

`duplicate_keyword.py.pending` 是独立审查原源的逐字保留，不属于本轮已通过的三份公开应用，也不计入原生通过数。

精确边界：显式 `delete=` 与 `**{'delete': ...}` 重复时，CPython 3.12.13 在调用展开阶段抛出 `TypeError`，消息包含 `got multiple values for keyword argument 'delete'`。RyuPy 既有 `FlattenArgs` 未在展开阶段拒绝重复名称，进入 translate 绑定后错误地先报告 `translate() takes at most 2 arguments (3 given)`。精确 bytes 与 bytearray 的直接方法、保存方法、根描述符六种入口均可复现。

本轮 translate 支持普通直接参数及既有无重复展开；不以局部方法补丁宣称通用 `**` 展开完整支持。此原源留待下一次通用调用展开竖切，不能改成断言当前错误结果的假通过测试，不能删掉重复关键字行为来转正。

CP/JIT 双流、退出码及差分证据保存在：

- `/workspace/shared/ryupy-binary-translate-reference/duplicate-keyword-standalone.py`：原源。
- `/workspace/shared/ryupy-binary-translate-reference/standalone-cp.stdout`、`standalone-cp.stderr`、`standalone-cp.status`：CPython 结果。
- `/workspace/shared/ryupy-binary-translate-reference/standalone-jit.stdout`、`standalone-jit.stderr`、`standalone-jit.status`：RyuPy 结果。
- `/workspace/shared/ryupy-binary-translate-reference/standalone-diff.txt`、`standalone-diff.status`：六入口差分。
- `/workspace/shared/ryupy-binary-translate-reference/standalone-sha256.txt`：原源与证据哈希。
- `/workspace/shared/ryupy-binary-translate-reference/REVIEW-IMPLEMENTATION.md`：独立审查结论。

程序捕获并打印异常，因此双方进程退出成功不表示语义相同；必须比较完整输出。
