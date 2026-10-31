"""CPython 侧的计时外壳：以 `__main__` 身份执行基准脚本，并在退出时写出执行耗时。

用法（由 `measure.py` 调用，不直接面向用户）：

```bash
python -B cpython_harness.py <统计输出文件> <基准脚本>
```

与 RyuPy 的 `RYUPY_STATS`（`PyStats.cs`）对应：`main_ms` 只计脚本本身的执行时间，
不含解释器启动；再记录各代 GC 次数，便于和 .NET 的 gc0/gc1/gc2 对照。
外壳只多导入 `runpy`（启动时已基本加载），对墙钟时间的影响在 1 ms 以内。
"""

import gc
import json
import os
import runpy
import sys
import time


def main() -> None:
    stats_path, script = sys.argv[1], sys.argv[2]
    # 让脚本看到与直接运行时相同的 argv / sys.path[0]
    sys.argv = [script]
    sys.path[0] = os.path.dirname(os.path.abspath(script))
    start = time.perf_counter()
    try:
        runpy.run_path(script, run_name="__main__")
    finally:
        elapsed = (time.perf_counter() - start) * 1000
        sys.stdout.flush()
        collections = [generation["collections"] for generation in gc.get_stats()]
        with open(stats_path, "w", encoding="utf-8") as handle:
            json.dump(
                {
                    "main_ms": elapsed,
                    "gc0": collections[0],
                    "gc1": collections[1],
                    "gc2": collections[2],
                },
                handle,
            )


if __name__ == "__main__":
    main()
