# 项目级运行（M2.0）

固定项目集的**验收路径**：编译项目的入口样例、真正运行、并与 CPython 基线逐字节比较。
编译成功、`import` 成功都不算通过。

## 选集

`tests/projects/manifest.tsv` 是唯一事实来源，TAB 分隔 8 列：

| 列 | 含义 |
| --- | --- |
| `id` | 项目标识，也是 `entries/<id>_sample.py`、`baselines/<id>/` 的目录名 |
| `category` | 用途分类：通用库 / 数据结构 / 配置解析 / 文本与数据处理 / 命令行工具 / 终端与文件自动化 … |
| `pypi` | PyPI 包名（来源） |
| `version` | 固定版本，不做「最新版」跟随 |
| `import` | 顶层导入名，用于说明阻塞发生在哪个模块 |
| `entry` | 入口样例，相对 `tests/projects/` |
| `test_scope` | 上游测试范围（当前阶段只跑入口样例，逐 API 的移植测试在 Phase 3 补） |
| `deps` | 传递依赖，`name==version` 逗号分隔；`-` 表示无 |

20 个项目覆盖 6 类用途，全部是纯 Python：依赖 C 扩展的项目放到 Phase 4 的可引入性矩阵，
不进这个分母。入口样例必须与包名不同名（`<id>_sample.py`），否则入口脚本所在目录会先命中
自己的文件名，`import` 到的是样例本身。

## 三个命令

```bash
python3 tests/fetch_projects.py          # 抓 sdist 到 third_party/projects/（不进版本库）
python3 tests/run_projects_baseline.py   # 用 CPython 记录基线到 tests/projects/baselines/
cargo run -p ryupy-e2e --bin projects    # 用 RyuPy 跑同一入口并对照基线
```

- 抓取：`lock.tsv` 记录每个发行包的 URL 与 sha256；sha256 与上一次不一致时直接失败（上游被替换
  属于需要人工确认的事件）。源码目录不进版本库，`--refresh` 才重新下载。
- 基线：每个项目写 `baselines/<id>/stdout.txt` 与 `meta.txt`（解释器版本、日期、退出码、最后一行
  stderr）。基线是「答案」，失败说明入口样例或环境有问题，不是 RyuPy 的能力声明。
- 运行：结果写 `target/e2e/projects.jsonl` 并打印汇总表；`--only id,id` 只跑指定项目。

`make projects-baseline`、`make projects` 是对应的快捷命令。这两步不进入 `make check`：
它们依赖联网抓取的源码，而提交门禁必须离线可跑。

## 状态口径

| 状态 | 含义 |
| --- | --- |
| `native` | 编译 + 运行通过，stdout 与 CPython 基线逐字节一致 |
| `unsupported` | 编译被拒（明确诊断），属于已知未实现能力 |
| `failed` | 编译通过但运行失败或输出不一致 |
| `not-evaluated` | 缺基线或入口样例，未运行 |

`native` 才进入兼容性分子。`unsupported` / `failed` 的 `detail` 字段就是「下一项阻塞」，
兼容性清单按它排序。

## 结果文件

```json
{"id":"six","pypi":"six","version":"1.16.0","status":"unsupported","entry":".../six_sample.py","scope":"...","deps":"-","detail":"[E0100] Error: 不支持普通 import"}
```

一行一个项目，字段固定。`docs/compatibility.md` 的项目矩阵与本文件同源，更新时两边一起改。
