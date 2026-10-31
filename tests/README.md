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
| `cases/errors/unhandled_zero_division` | 未捕获异常的非零退出与消息 | pending（M2.2） |
| `cases/errors/unhandled_chain` | `raise ... from ...` 的异常链 | pending（M2.2） |
| `cases/errors/missing_module` | `ModuleNotFoundError` | pending（M2.7） |
| `cases/harness/args_env_stdin` | 框架自检：参数 / 环境 / stdin 传递 | pending（需 sys.argv 等，M2.7 / M3.4） |
| `cases/harness/multifile` | 框架自检：入口导入辅助模块 | pending（M2.7） |

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
make e2e-all                                         # 含 pending，看进度
```

可以通过 `--python /path/to/python3.12` 或 `PYTHON` 指定解释器。默认检查 Phase 1–3 与目录用例；
Phase 4 需要显式选择，环境缺少 NumPy 时直接失败，不静默跳过。

基线脚本让每个程序在独立临时工作目录运行，避免文件测试污染仓库；关闭 pycache 写入，不修改
用例源码。这只是 CPython 侧用例自检，不等于 RyuPy 支持。

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

2026-10-31 在 macOS arm64 上运行，解释器为环境中默认 `python3`（当前 3.13.12）；
扩展基线需要 CPython 3.12 + NumPy 2.3.4，命令见下：

```bash
uv run --no-project --python 3.12.13 --with numpy==2.3.4 python tests/run_python_baseline.py \
  --phase phase1 --phase phase2 --phase phase3 --phase phase4
```

这是用例自检结果，不能计入 RyuPy 的项目覆盖率或扩展可引入性验收。
