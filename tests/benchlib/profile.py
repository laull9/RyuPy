"""采样剖析：用 `dotnet-trace` 采集一个基准的 CPU 采样，汇总自身 / 包含耗时最高的函数。

```bash
python3 tests/run_benchmarks.py --profile sorting      # 或 make bench-profile BENCH=sorting
```

依赖 `dotnet tool install -g dotnet-trace`（默认装在 `~/.dotnet/tools`）。采集配置为
`dotnet-sampled-thread-time`，输出 speedscope 的 evented 格式：每个线程一串「进入帧 O / 离开帧 C」
事件，相邻事件之间的时间属于当时的调用栈。

- **自身（self）**：栈顶的真实帧，即真正在执行的函数；
- **包含（total）**：栈上出现过该函数的时间（同一栈里重复出现只计一次，避免递归重复计数）。

只统计栈上有 `RyuPy` 帧的时间，排除 .NET 后台线程（tiered JIT、GC、finalizer）的空转；
`CPU_TIME` 等伪帧只表示线程状态，归到它下面最近的真实函数。
结果写在 `<target>/bench/profile/<基准>.speedscope.json`，可拖进 https://www.speedscope.app 查看火焰图。
"""

from __future__ import annotations

import json
import os
import shutil
import subprocess
from collections import Counter
from pathlib import Path


def find_dotnet_trace() -> str | None:
    found = shutil.which("dotnet-trace")
    if found:
        return found
    candidate = Path.home() / ".dotnet" / "tools" / "dotnet-trace"
    return str(candidate) if candidate.exists() else None


def collect(dll: Path, out_dir: Path) -> Path:
    tool = find_dotnet_trace()
    if tool is None:
        raise RuntimeError("未找到 dotnet-trace：先运行 `dotnet tool install -g dotnet-trace`")
    out_dir.mkdir(parents=True, exist_ok=True)
    trace = out_dir / f"{dll.stem}.nettrace"
    subprocess.run(
        [
            tool,
            "collect",
            "--profile",
            "dotnet-sampled-thread-time",
            "--format",
            "speedscope",
            "-o",
            str(trace),
            "--",
            "dotnet",
            str(dll),
        ],
        check=True,
        stdout=subprocess.DEVNULL,
        stderr=subprocess.DEVNULL,
        env=dict(os.environ),
    )
    return out_dir / f"{dll.stem}.speedscope.json"


def _short(name: str) -> str:
    """去掉模块前缀与参数列表，保留 `类型.方法`。"""
    if "!" in name:
        name = name.split("!", 1)[1]
    name = name.split("(", 1)[0]
    return name.replace("RyuPy.Runtime.", "").replace("RyuPy.Generated.", "")


# speedscope 里的伪帧：标记线程状态（在 CPU 上 / 在非托管代码里），不是真实函数
PSEUDO_FRAMES = {"CPU_TIME", "UNMANAGED_CODE_TIME", "Threads", "(Non-Activities)"}


def summarize(speedscope: Path, top: int = 25) -> list[str]:
    """把 evented profile（O/C 事件流）折算成每个函数的自身 / 包含耗时。"""
    data = json.loads(speedscope.read_text(encoding="utf-8"))
    raw = [frame["name"] for frame in data["shared"]["frames"]]
    names = [_short(name) for name in raw]
    self_time: Counter[str] = Counter()
    total_time: Counter[str] = Counter()
    measured = 0.0
    for profile in data["profiles"]:
        if profile.get("type") != "evented":
            continue
        stack: list[int] = []
        last = None
        for event in profile["events"]:
            at = event["at"]
            if last is not None and stack and at > last:
                _attribute(stack, raw, names, at - last, self_time, total_time)
                if any(raw[i].startswith("RyuPy") or "!RyuPy." in raw[i] for i in stack):
                    measured += at - last
            last = at
            if event["type"] == "O":
                stack.append(event["frame"])
            elif stack:
                stack.pop()
    if measured == 0:
        return ["没有采到包含 RyuPy 帧的样本（程序太短？）"]
    lines = [
        f"采样时长：{measured:.0f} ms（只计栈上有 RyuPy 帧的时间）；火焰图：{speedscope}",
        "",
        "| 自身 % | 函数 |",
        "| ---: | --- |",
    ]
    for name, value in self_time.most_common(top):
        lines.append(f"| {value * 100 / measured:.1f} | `{name}` |")
    lines += ["", "| 包含 % | 函数 |", "| ---: | --- |"]
    for name, value in total_time.most_common(top):
        lines.append(f"| {value * 100 / measured:.1f} | `{name}` |")
    return lines


def _attribute(stack, raw, names, delta, self_time, total_time) -> None:
    """一段时间记给栈顶的真实帧（自身）与栈上每个不同的真实帧（包含）。"""
    if not any(raw[i].startswith("RyuPy") or "!RyuPy." in raw[i] for i in stack):
        return
    real = [i for i in stack if raw[i] not in PSEUDO_FRAMES and not raw[i].startswith(("Process", "Thread"))]
    if not real:
        return
    self_time[names[real[-1]]] += delta
    for name in {names[i] for i in real}:
        total_time[name] += delta
