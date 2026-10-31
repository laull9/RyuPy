# Python 用例与基线

`e2e/phase1/` 为已实现用例。新增 `phase2/`、`phase3/`、`phase4/` 文件都以 `# ryupy: pending` 开头，默认 Rust e2e 会发现但跳过；实现对应语义后逐个去掉标记。每个测试覆盖一组关联行为，文件中的数据和调用顺序用于 CPython 差分。

## 先检查用例本身

```bash
python3 tests/run_python_baseline.py
python3 tests/run_python_baseline.py --phase phase2 --phase phase3
python3 tests/run_python_baseline.py --phase phase4
make python-baseline
make python-extensions
```

可以通过 `--python /path/to/python3.12` 或 `PYTHON` 指定解释器。默认检查 Phase 1–3，以及 Phase 2 的预期错误退出。Phase 4 需要显式选择，环境必须有 NumPy、sqlite3、zlib；缺失依赖直接失败，不静默跳过。记录实际解释器与扩展版本，不能把不同环境的结果混成固定版本验收。

`make check` 已包含默认 CPython 基线。扩展基线保持独立，不让未安装 NumPy 的普通开发环境误报语言回归。

基线脚本让每个程序在独立临时工作目录运行，避免文件测试污染仓库；关闭 pycache 写入，不修改用例源码。成功用例要求正常退出，错误用例按 `fixtures/errors/cases.json` 检查非零退出、异常类型、消息、stdout 与 cause。这只是 CPython 侧用例自检，RyuPy 的多文件与错误路径框架仍由 M2.0 完成。

## 用例分工

| 目录 / 前缀 | 覆盖 |
| --- | --- |
| phase2/m2_1 | 动态值、参数绑定、大整数与标注失配 |
| phase2/m2_2 | 捕获、异常链、裸重抛、finally 与控制流 |
| phase2/m2_3 / m2_4 | 可变别名、切片、哈希、字节、Unicode、迭代与内建 |
| phase2/m2_5 / m2_6 | 普通类、身份、缓存失效、单继承与用户异常 |
| phase2/m2_7 | 真实包、相对 / 循环 / 重复导入、global 与失败重试 |
| phase2/m2_8 / m2_9 | with 清理，以及容器 / 类组成的数值集成样例 |
| phase3/m3_0–m3_3 | 闭包、推导式、生成器协议与清理、装饰器 |
| phase3/m3_4–m3_7 | 标准库首批 API；覆盖范围以各文件实际调用为准 |
| phase3/m3_8 / m3_9 | 桥接 API 行为与特化失败的正确性；桥接执行路径还需宿主侧证据 |
| phase4/m4_1 / m4_2 | NumPy 导入、数组对象、基本调用，以及 sqlite3 / zlib 扩展 |

部分测试需要其它竖切的基础能力，例如参数绑定测试会使用容器。用例编号表示主要验收目标，不保证仅实现该编号的一小部分后就能转正，应按真实依赖检查。

多文件辅助包位于 `e2e/phase2/fixtures/`，只由入口导入。Rust 测试发现器排除名为 fixtures 的目录，不能把 __init__.py 或辅助模块作为独立 e2e。预期错误退出文件放在 `tests/fixtures/errors/`，当前成功用例框架不会误收集。

这些用例先铺主要成功路径和语义边界，不等同于完成每个标准库 API、第三方项目或部署组合的测试。后续还需加入诊断快照、缓存 / 桥接释放的宿主侧测试、缺失动态库与 ABI 不匹配的隔离环境负例。项目集的至少 20 个真实项目仍在 M2.0 冻结，不能用这些微型用例充当分母。

## 本次基线记录

2026-10-31 在 macOS arm64、CPython 3.12.13、NumPy 2.3.4 上运行全部四个阶段：53 个用例通过，包括原有 9 个 Phase 1 用例、41 个新增 pending 入口与 3 个预期错误退出。辅助包不单独计数，sqlite3 / zlib 来自该解释器环境。

复现命令：

```bash
uv run --no-project --python 3.12.13 --with numpy==2.3.4 python tests/run_python_baseline.py --phase phase1 --phase phase2 --phase phase3 --phase phase4
```

这是用例自检结果，不能计入 RyuPy 的项目覆盖率或扩展可引入性验收。
