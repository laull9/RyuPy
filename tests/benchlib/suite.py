"""基准发现与元数据解析。

每个基准是 `tests/benchmarks/` 下的一个独立 `.py` 文件，文件开头用注释声明元数据：

```python
# bench: category=dynamic           # 类别，用于分组与按类别过滤
# bench: desc=300 万次动态函数调用    # 一句话说明输入规模与覆盖路径
# bench: variant=PIC 关闭:RYUPY_PIC=0  # 可选，可重复：同一 DLL 换环境变量再测一行
# bench: pending=M3.0               # 可选：依赖尚未实现的特性，RyuPy 侧默认跳过
```

元数据只在文件开头的连续注释区读取，遇到第一行代码即停止，避免误读正文里的字符串。
`startup` 类别（`hello.py`）只用来估计进程启动成本，不计入几何平均。
"""

from __future__ import annotations

from dataclasses import dataclass, field
from pathlib import Path

# 类别的固定展示顺序；未登记的类别排在最后、按名字排序。
CATEGORY_ORDER = [
    "startup",
    "static",
    "numeric",
    "dynamic",
    "objects",
    "containers",
    "strings",
    "algorithms",
    "control",
    "project",
    "phase3",
]


@dataclass
class Variant:
    """同一 DLL 的另一种运行配置（只改环境变量，排除编译差异）。"""

    label: str
    env: dict[str, str]


@dataclass
class Benchmark:
    name: str
    path: Path
    category: str = "misc"
    desc: str = ""
    # 非空表示依赖的里程碑尚未完成（例如 "M3.0"）。
    pending: str = ""
    variants: list[Variant] = field(default_factory=list)

    @property
    def is_startup(self) -> bool:
        return self.category == "startup"


def parse_header(path: Path) -> Benchmark:
    """读取文件头的 `# bench:` 行；格式错误直接报错，避免基准元数据悄悄失效。"""
    bench = Benchmark(name=path.stem, path=path)
    for line in path.read_text(encoding="utf-8").splitlines():
        stripped = line.strip()
        if not stripped.startswith("#"):
            break
        body = stripped[1:].strip()
        if not body.startswith("bench:"):
            continue
        key, sep, value = body[len("bench:") :].strip().partition("=")
        if not sep:
            raise ValueError(f"{path.name}: 元数据缺少 '='：{line}")
        key = key.strip()
        value = value.strip()
        if key == "category":
            bench.category = value
        elif key == "desc":
            bench.desc = value
        elif key == "pending":
            bench.pending = value or "pending"
        elif key == "variant":
            label, colon, assignments = value.partition(":")
            if not colon:
                raise ValueError(f"{path.name}: variant 需写成 '标签:K=V[,K=V]'")
            env = {}
            for item in assignments.split(","):
                k, eq, v = item.partition("=")
                if not eq:
                    raise ValueError(f"{path.name}: variant 环境变量需写成 K=V：{item}")
                env[k.strip()] = v.strip()
            bench.variants.append(Variant(label.strip(), env))
        else:
            raise ValueError(f"{path.name}: 未知的元数据键 '{key}'")
    return bench


def category_rank(category: str) -> tuple[int, str]:
    if category in CATEGORY_ORDER:
        return (CATEGORY_ORDER.index(category), category)
    return (len(CATEGORY_ORDER), category)


def discover(directory: Path) -> list[Benchmark]:
    """按「类别顺序 → 名字」排序返回全部基准。"""
    benches = [parse_header(path) for path in directory.glob("*.py")]
    benches.sort(key=lambda b: (category_rank(b.category), b.name))
    return benches


def select(benches: list[Benchmark], filters: list[str]) -> list[Benchmark]:
    """按名字或类别过滤；名字支持子串匹配。无过滤条件时返回全部。"""
    if not filters:
        return benches
    chosen = []
    for bench in benches:
        for item in filters:
            if item == bench.category or item in bench.name:
                chosen.append(bench)
                break
    return chosen
