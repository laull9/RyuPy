# 固定上游测试移植

本目录保存选定上游测试的可独立编译入口，测试来源、版本与许可不可省略。
它们补充而非替换 `tests/projects` 的固定 20 项目入口，不能增加通过率分子或缩小分母。

| 入口 | 上游版本 / 来源 | 保留范围 | 未声称覆盖 |
| --- | --- | --- | --- |
| `wcwidth_core.py` | wcwidth 0.2.13 `tests/test_core.py`；MIT，完整许可在 `licenses/wcwidth-0.2.13.txt` | 空串、ASCII/NUL、日文、长度截断、控制字符、希伯来组合、cafe 组合重音、组合包围共九场景；保留逐字符和总宽度断言 | 发行元数据、全部 Unicode 表完整性、emoji 文件矩阵、所有 Unicode 版本及 pytest 插件 |

`termcolor_force.py` 已验证选定子集：来自 termcolor 2.4.0 `tests/test_termcolor.py` 的
`test_color`（black/red/green/blue/white）、`test_on_color`（on_red/on_blue）、
`test_attrs`（bold/dark/underline/blink/reverse/concealed），保留原 ANSI 断言，
以 `force_color=True` 替代 pytest 的 TTY monkeypatch；另加颜色/背景/双属性顺序、
no_color 优先与整数文本三场景，共 16 场景。完整 MIT 许可见
`licenses/termcolor-2.4.0.txt`。不覆盖全部颜色别名、环境变量矩阵、TTY/cprint 与 pytest 插件；
固定 CPython 3.12.13 与 RyuPy 的强制 ANSI 路径已通过；不代表 termcolor 完整测试集通过。

用 `make upstream` 执行：先由固定 CPython 3.12.13 运行，再编译相同入口并通过
.NET 执行，比较退出码、stdout、stderr。临时产物只写自己的临时目录，结果保存在
`target/upstream/results.jsonl`。源码缺失或任何失败都会返回非零，不静默跳过。

源码按项目集既有固定下载流程准备，`make upstream` 不自行联网、不下载或更换版本；
因此这不是无需源码的普通 `make check` 子项。聚合里程碑额外记录此结果，CI 没有准备
上游 sdist 时不能声称已经执行。此脚本不调用 CPython 来运行 RyuPy 编译产物。

## texttable 1.7.0 布局子集

`texttable_layout.py` 来自上游 `tests.py`，保留以下九个原测试的输入、assert 和 `clean`：
`test_texttable`、`test_set_cols_width`、`test_exceeding_max_width`、
`test_exceeding_max_width2`、`test_exceeding_max_width3`、`test_exceeding_max_width4`、
`test_obj2unicode`、`test_combining_char`、`test_combining_char2`。
完整 MIT 许可见 `licenses/texttable-1.7.0.txt`。

仅将固定期望文本的 `dedent`/`u_dedent` 在移植时预计算为相同字符串，未修改布局、空格、
换行或组合字符期望；不依赖 pytest，直接调用原测试。覆盖基础横纵对齐、固定列宽、
两种宽度配置入口、长词/行换行、None 文本、组合附加符与中文显示宽度。
固定 CPython 3.12.13 和真实 RyuPy 编译执行九场景均通过。

明确未选入 `test_texttable_header` 的浮点/指数/precision 格式场景，当前字符串百分号
格式仍不支持这些转换，后续应保留独立未支持验收，不能据此称完整 suite 通过。
也未覆盖 `test_user_dtype`、可选 cjkwrap 的 `test_cjkwarp`、`test_chaining` 等其余原测试。
此结果只增强已有 texttable 固定入口证据，不增加 20 项目分子或缩小分母。
