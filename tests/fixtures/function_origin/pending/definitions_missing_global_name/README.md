# 真正缺失模块全局名：保留未完成源

`definitions.py` 逐字复制首次正常CP-first应用源，未加注释或改写：
首次失败应用保留的 `definitions.py`（SHA256 `7c0cec6d844365f2f6cd600ae15f9ab21bab0908746f2279c4978e36f331925e`）。

该次固定CPython退出0并输出`missing-name True`；RyuPy在HIR因`del __name__`报告E0100（byte1514..1522），没有构建成功或执行JIT。不将CP结果、runtime单元或旧namedtuple参考计作本源Ryu通过。

这是既有del Name语法旧域，当前前置不扩大实现。`case.txt`持pending标记，原源码保持不变。当前仅归档，不由活跃 driver 运行；在通用del Name获得独立实现并复验之前保持pending。

活跃应用以`__name__ = None`覆盖真实None捕获，标签明确改为`none-name`；它不等于键缺失，不能代替此pending源。其余两个已绿应用与完整异常链不改。
