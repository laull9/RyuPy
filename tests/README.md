# 测试用例与基线

分三类，都在 `make check` 里跑：

| 位置 | 形态 | 说明 |
| --- | --- | --- |
| `tests/e2e/phaseN/*.py` | 单文件 | 一个文件一个用例：无参数、期望成功退出、stdout 与 CPython 逐字节一致 |
| `tests/cases/**/case.txt` | 目录 | 有清单：入口、参数、环境、标准输入、工作目录与错误路径期望 |
| `tests/projects/` | 项目 | 固定项目集：编译项目入口并对照 CPython 基线，产出机器可读结果 |

未实现的用例标 `pending`：单文件用例在文件首部注释区写 `# ryupy: pending`，目录用例在清单里写
`pending = true`。Rust e2e 默认跳过 pending；`make e2e-all` 会运行并报告差异；跑通后 e2e 会打印
`[可转正]` 提示去掉标记——pending 只是待办，不是通过。

## 目录用例格式

`tests/cases/<分组>/<用例>/case.txt` + 同目录的入口文件。目录里其它 `.py` 是**辅助文件**
（被入口导入的 fixture），不会被当成独立用例；有两个以上 `.py` 时必须显式写 `entry =`。

```text
entry = entry.py                 # 默认 entry.py，或缺省时唯一的 .py
pending = true                   # 未实现，RyuPy 侧跳过
args = --name value "a b"        # 命令行参数（支持引号与反斜杠转义）
instance_module = example       # 可重复非空；仅选择图内模块的RyuPy实例能力，CPython忽略
env = KEY=VALUE                  # 可重复
stdin = 12\n34                   # 支持 \n \t \r \\ \" 转义
cwd = work                       # case（默认：隔离工作目录）| 相对用例目录
expect_exit = 0 | nonzero | 1    # 默认 0
expect_exception = ZeroDivisionError: division by zero
expect_cause = ValueError: source
expect_stdout = exact | ignore   # 默认 exact（与 CPython 逐字节比较）
timeout = 30
```

错误路径只比较**退出码、异常类型、消息子串、cause 行**与 stdout；traceback 的栈帧属框架差异，
不参与比较。异常消息按子串匹配，因为 CPython 的同一异常可能带额外上下文。

清单写错时两边都会立刻失败：CPython 基线（`tests/run_python_baseline.py`）与 Rust e2e 都会先用
CPython 校验声明的退出码与异常，再用同一份清单跑 RyuPy。声明与 CPython 不一致时没人能「通过」。

现有目录用例：

| 用例 | 覆盖 | 状态 |
| --- | --- | --- |
| `cases/errors/unhandled_zero_division` | 未捕获异常的非零退出与消息 | 已转正（M2.2） |
| `cases/errors/unhandled_chain` | `raise ... from ...` 的异常链 | 已转正（M2.2） |
| `cases/errors/missing_module` | `ModuleNotFoundError` | 已转正（M2.7） |
| `cases/harness/args_env_stdin` | 框架自检：参数 / 环境 / stdin 传递 | pending（需 `sys.argv` 等，M3.4） |
| `cases/harness/multifile` | 框架自检：入口导入辅助模块 | 已转正（M2.7） |
| `cases/harness/pipeline` | 多文件数据处理：继承、模块状态、异常恢复与 `with` 清理 | 已转正（M2.9） |

两个 `harness` 用例在 RyuPy 侧要等模块系统与 `sys` 才能转正，但它们的价值已经兑现：CPython 侧
自检证明框架确实把参数、环境变量、标准输入与多文件入口传给了子进程。

## 命令

```bash
python3 tests/run_python_baseline.py                  # 用例本身是否成立（Phase 1–3）
python3 tests/run_python_baseline.py --phase phase4  # 需 NumPy / sqlite3 / zlib
make python-baseline                                 # 同上（默认阶段）
make python-extensions                               # 扩展基线
make cases-selftest                                  # 只跑框架自检（不编译 RyuPy）
make e2e                                             # 正式差分（跳过 pending）
make bench                                           # CPython vs RyuPy 基准（不进入门禁，见下文）
make e2e-all                                         # 含 pending，看进度
```

可以通过 `--python /path/to/python3.12` 或 `PYTHON` 指定解释器。默认检查 Phase 1–3 与目录用例；
Phase 4 需要显式选择，环境缺少 NumPy 时直接失败，不静默跳过。

e2e 默认串行执行：用例按名字排序，输出行带 `[已跑/总数]` 进度，结果文件也与顺序一致。
需要提速时可显式传 `cargo test -p ryupy-e2e -- --test-threads=8`，此时输出按完成顺序排列，不再保证稳定排序。

基线脚本让每个程序在独立临时工作目录运行，避免文件测试污染仓库；关闭 pycache 写入，不修改
用例源码。这只是 CPython 侧用例自检，不等于 RyuPy 支持。

## 性能基准（`make bench`）

基准不进入 `make check`（耗时随机器波动），但每个基准的 stdout 都与 CPython 逐字节比对，
输出不一致或运行失败时脚本以非零码退出。

| 位置 | 作用 |
| --- | --- |
| `tests/benchmarks/*.py` | 一个文件一个基准；文件头 `# bench: category=… / desc=… / variant=标签:K=V / pending=M3.x` |
| `tests/run_benchmarks.py` | 入口：过滤、次数、保存 / 对比、剖析 |
| `tests/benchlib/suite.py` | 发现基准、解析元数据、按类别排序与过滤 |
| `tests/benchlib/measure.py` | 构建 Release CLI 与 DLL，交替运行 CPython / RyuPy，`wait4` 取峰值内存 |
| `tests/benchlib/cpython_harness.py` | CPython 侧计时外壳：只计脚本执行时间与 GC 次数 |
| `tests/benchlib/report.py` | 中位数 / 波动 / 几何平均，Markdown 表格，JSON 保存与对比 |
| `tests/benchlib/profile.py` | `dotnet-trace` 采样，汇总自身 / 包含耗时最高的函数 |

类别：`startup`（只测启动）、`static`（全标注静态路径）、`numeric`、`dynamic`、`objects`、`containers`、
`strings`、`algorithms`、`control`、`project`，以及 `phase3`（闭包 / 推导式 / 生成器 / 装饰器，pending）。

```bash
make bench                              # 全部基准 + 资源表（分配 / GC / JIT / 内存 / 编译耗时）
make bench BENCH="dynamic strings"      # 按名字子串或类别过滤
make bench-quick                        # 1 次热身 + 3 次计时
make bench-list                         # 清单与 pending 状态
make bench-save NAME=before             # 另存结果
make bench-compare NAME=before          # 与已保存结果逐项对比
make bench-profile BENCH=sorting        # 热点函数表 + speedscope 火焰图（需 dotnet-trace）
make bench ARGS="--no-cpython --runs 9" # 其它参数见 python3 tests/run_benchmarks.py --help
```

口径：墙钟（含进程启动）取中位数，「执行 ms」是进程内计时（RyuPy 的 `RYUPY_STATS`、CPython 外壳），
二者之差约等于启动成本；波动 = (max − min) / 中位数，超过 10% 会标 ⚠。结果保存在
`<target>/bench/results/`（`latest.json` 每次覆盖），不同机器的结果不要直接比较。

新增基准：写一个只依赖已支持特性、输出确定的脚本，CPython 侧耗时建议 50–300 ms；依赖未实现特性时
标 `# bench: pending=M3.x`，默认跳过，`--include-pending` 会尝试编译并在跑通时提示「可转正」。

## 结果记录

每次 e2e 运行都会重写 `target/e2e/results.jsonl`，一行一个用例：

```json
{"name":"phase1/m1_0_print_int","status":"passed","detail":""}
```

`status` 取值 `passed` / `failed` / `pending` / `unexpected-pass`。`pending` 不计入通过数，
`unexpected-pass` 表示该去掉 pending 标记。编译失败不会被算成通过，`detail` 里带诊断摘要。

## 项目级运行

`tests/projects/` 固定至少 20 个纯 Python 项目（版本、来源、传递依赖、入口、上游测试范围），
详见 [compatibility.md](../docs/compatibility.md)。运行方式与结果格式见 [projects.md](projects.md)。

## 本次基线记录

2026-10-31 在 macOS arm64 上运行。解释器固定在 `tests/pinned-python.txt`（3.12.13，经
`uv python find` 解析），工具链为 .NET 10.0.403、Rust stable：

- 用例自检：Phase 1–3 的单文件用例与 6 个目录用例全部通过；
- 项目基线：20 个项目全部记录成功（`tests/projects/baselines/`）；
- RyuPy 侧：M2.9 时 e2e 通过 50 项、pending 21 项；20 个项目全部 `unsupported`，
  第一项阻塞逐项目写在 [compatibility.md](../docs/compatibility.md) 的矩阵里。

扩展基线需要 CPython 3.12 + NumPy 2.3.4，命令见下：

```bash
uv run --no-project --python 3.12.13 --with numpy==2.3.4 python tests/run_python_baseline.py \
  --phase phase1 --phase phase2 --phase phase3 --phase phase4
```

这是用例自检结果，不能计入 RyuPy 的项目覆盖率或扩展可引入性验收。
