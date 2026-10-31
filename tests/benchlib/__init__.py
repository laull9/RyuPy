"""RyuPy 基准框架（`make bench` 的实现）。

模块分工：

- `suite`：发现 `tests/benchmarks/*.py`，解析文件头的 `# bench:` 元数据；
- `measure`：构建 Release CLI 与基准 DLL，逐次启动子进程计时并采集进程内统计；
- `report`：汇总中位数 / 波动 / 几何平均，输出 Markdown 表格，保存与对比 JSON 结果。

入口是 `tests/run_benchmarks.py`。基准耗时随机器波动，不进入 `make check`。
"""
