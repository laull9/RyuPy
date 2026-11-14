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
- 运行：结果写 `target/e2e/projects/projects.jsonl` 并打印汇总表；`--only id,id` 只跑指定项目。
  M2.9 起 runner 会把项目源码与传递依赖的 `third_party/projects/<包>-<版本>/`
  （含 `src/` 布局）加入模块搜索根，因此「找不到第三方包」不再是统一阻塞，
  首个编译 / 运行错误就是该项目的下一项阻塞。

`make projects-baseline`、`make projects` 是对应的快捷命令。完整项目运行不进入 `make check`：
它需要事先准备的官方源码；提交门禁继续离线可跑，但包含项目门禁本身的离线失败注入测试。

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


## P0：按项目 ID 的保留与默认晋升门禁

`tests/projects/protected.json` 是显式审阅的保护合同，不是第二份可变选集。它记录固定20个ID、
manifest/lock/入口/golden的hash、当前九项成功集合及历史回归阻断。任何新通过只报告，
不会自动改写该文件。版本、依赖与入口仍来自原 manifest；禁止裁剪源、缩分母或重录答案求绿。

- **保留门**：tomli、docopt、tabulate、texttable、chardet、wcwidth、colorama、termcolor、binaryornot
  每一项都必须 native。丢一项同时增加另一项仍失败，不能只比较总数。
- **默认晋升门**：保留门通过，而且历史默认通过的 progress 必须恢复 native。
  db866ead 的真实默认矩阵是 **9 native / 8 unsupported / 3 failed**；progress 因显式 super
  失败，是开放回归，不是 XFAIL。保留门可以通过而默认晋升门必须失败。
- 两门都运行完整20项原入口并保留每项状态。通过只涵盖这些入口，不等于完整库兼容、16/20目标
  或最终产品发布验收；性能、AOT及其它发布条件另验。显式字体/模块实例配置不混入默认结果。

```bash
make projects-fetch                    # 仅首次准备；门禁本身绝不联网或升级
make projects-preserve                 # 本候选 runtime + runner 构建，独立完整20项
make projects-promote                  # 再跑完整20项，progress未恢复时非0
make projects-preserve PROJECTS_STORE=/absolute/path/to/prepared-store
make projects-promote PROJECTS_OUTPUT=/absolute/path/to/new-evidence-directory
python3 -B tests/test_project_gate.py    # 离线框架单测，已进入make check的python-baseline
```

源码 store 结构沿用 fetch：`_dist/<lock中的归档名>` 和 `<pypi>-<version>/<归档原顶层>/…`。
可复用已准备目录；门禁逐归档核对 lock SHA256，再逐文件对照**全部解压源**，拒绝缺失、
增加、修改或内部链接。只核对被固定项目实际要求的包，lock 中额外登记的包不会改变20项分母。

门禁使用现有 Rust `projects` runner/driver，不复制编译器逻辑；每项唯一输出目录和同轮 run-id，
父进程监管编译+JIT期限并终止整个进程组。CP 使用固定版本、`-S -B`、全部原源搜索根；编译只用
本项目及manifest明示依赖，Session使用默认值。CP与JIT stdout按原字节比较原golden，stderr原样
保存（例如docopt的上游SyntaxWarning，不强求CP与JIT stderr一致）。当前监管实现只验收POSIX。

默认证据写入 `target/e2e/project-gates/<随机ID>/`，可指定不存在的目录。包含：
`run.json`（候选HEAD+工作树、工具hash、合同、完成标记）、`sources.json`、`matrix.json`、
`summary.json`，以及每项目CP/运行器/编译诊断/JIT原流与产物。未完成只有partial或invalid摘要，
不能把它当有效矩阵；已存在目录一律拒绝，不覆盖或复用旧结果。退出码0只表示所选门通过，
1表示完整有效矩阵未达所选门，2表示证据/输入/运行器无效。`summary.json`始终分别列出两门结果。

运行前后核对候选文件、源与工具，允许未提交候选，避免“提交前不能测自己”的循环。直接调用
`tests/run_projects_gate.py --runner …`时须提供该候选的真实构建产物；工具hash只标识所用二进制，
不声称证明它由某份源码构建。标准make目标先构建避免误用陈旧产物。hash/run-id用于一致性审计，
不是对同权限恶意篡改的安全保证。

未知/重复/缺失ID、错误模式/旧run-id、半矩阵、缺source/golden、错CP版本、CP失败、无诊断内部编译
错误、运行器失败、信号/超时、截流、任何项目stdout不一致、写盘失败都不能返回绿。编译的正常
源码诊断仍记unsupported；正常Python运行异常仍记failed且不增加分子，受保护/历史阻断按ID判定。
`make projects`仍是进度报告，不是这两个门；其--only定向输出不得拿来宣称默认准入。

CI目前只随既有make test执行离线门禁测试；网络项目任务尚未接线。未来若单独接CI，应先获取并
核验锁定归档，再构建候选、运行完整门并保存所有证据；progress未恢复前不得把promotion改成可忽略。
