# C ABI 加显式资源的 AOT 验收

公开组合为 `build --aot --resources ... --native-binding ...`，设计见
[组合发布 ADR](../../../docs/adr/aot-binding-resources.md)。本目录独立于 `binding_multi`，
复用其自有 C 源和 CPython ctypes 参考入口、现有 AOT 布局解析与资源清单验证器。

```sh
export CARGO_TARGET_DIR="$PWD/target"
export RUSTC_WRAPPER=
mkdir -p target/task-tmp
export TMPDIR="$PWD/target/task-tmp"
export PYTHON="$(uv python find "$(cat tests/pinned-python.txt)")"
export RYUPY_FOREACH_PYTHON="$PYTHON"
make runtime
cargo build -p ryupy-cli
cargo test -p ryupy-driver --lib binding
cargo test -p ryupy-driver --lib resources
cargo test -p ryupy-driver --lib aot_layout
"$PYTHON" -B tests/aot/binding_resources/run.py \
  --ryupy target/debug/ryupy --out target/binding-resources-evidence
make check
```

应用最初标记 pending，先补 CLI 用例确认旧实现因互斥失败，再实现并转正。
`pkgdata/numbers.txt` 的两个数经过 `files(pkgdata).joinpath(...).read_text(encoding='utf-8')`
交给两个真实 C 符号。程序还检查包 `__file__` 与 `__spec__.origin` 一致。
`single.py` 使用 v1 单导出声明并读取同一数据；两个程序都直接对照固定 CPython 3.12.13 原双流。
本 fixture 暂用 `read_text()`，不依赖并行实现中的 `as_file()`。

验收包含：

- v2/schema6 的两个实际静态符号、单份 archive 输入、真实源码 consumer 与原生生成来源分列。
- v1/schema5 的资源与单导出组合；Rust 结构测试额外覆盖 v1/v2 的空 files 位置声明。
- 缺资源、资源误指原生生成模块、错误绑定签名、真实静态缺第二符号的失败 attempt 留证。
  每次逐字比较旧 dist 全部文件与权限，再执行旧程序与原 CPython 对照。
- JIT resources 参数继续拒绝；编译后改变原数据不影响已成功发布的快照。
- 两个成功 dist 搬迁，原源码/动态库/静态库与 managed/SDK/build 原绝对路径全部失效，
  换 cwd 运行仍与 CPython 一致。发布文件集合精确匹配清单，无源码/托管 DLL/shared/archive。
- 另一个 cwd 放置同名诱饵包数据，搬迁副本删除实际资源后仍抛完整 Python FileNotFoundError。

runner 在输出根下新建独占目录，保留 `result.json`、`logs/`、失败 attempt、全部输入与搬迁结果。
普通文件直接比较字节，没有新增 SHA/MD5 检查。SDK 仍安装于宿主；实际平台和执行结果见下方记录。

## 本轮记录

宿主 macOS 27.0.1 arm64，.NET SDK 10.0.403 / runtime 10.0.14，CPython 3.12.13。
Linux x64、Windows x64 分支尚待各自实机执行；最低 macOS 版本另行验证。

转正后的完整真实验收为
`target/binding-resources-final-evidence/binding-resources-30b54pc7/result.json`：
`status=passed`，共 23 步，包含预期失败步骤。v1/v2 各一次成功 NativeAOT 发布，
另一次缺第二符号的真实失败发布；原始双流、错误日志和搬迁文件都保存在该目录。
主日志为 `target/binding-resources-real-final.log`。
v2 程序输出为 `随包数据`、`38 24`、`True`、`binding-resources-ok` 四行，与 CPython 完全相同。

实现前的 CLI 测试在旧互斥上确实失败，见 `target/binding-resources-pending.log`。
首轮真实流程使用了未重建 CLI，缺资源虽失败，但诊断没有文件名而使验收停止，
见 `target/binding-resources-real.log` 与
`target/binding-resources-evidence/binding-resources-v9_rzgs8/logs/missing-resource.stderr`。
重新构建 CLI 后同一缺资源判据通过；第二轮完整 23 步和上述转正后的最终轮均 passed，
没有放宽诊断、搬迁或保旧包断言。

无资源绑定旧流程复验为
`target/binding-resources-compat-evidence/multi-vkyn88ur/result.json`：passed，19 步，
包含 JIT/AOT、多导出、搬迁、静态缺符号及原错误路径；其主日志为 `target/binding-resources-compat.log`。

`make runtime` Release 零警告、零错误；绑定 driver 专项 18 passed，
全部 driver 库测试 181 passed / 4 ignored（`target/binding-resources-driver-all.log`）。
完整 `make check` 首轮 exit 0，见 `target/binding-resources-make-check.log`：
fmt、strict clippy、Rust workspace 全通过，C# 3000 passed / 2 skipped，
固定 CPython 基线 716 passed，Python-only 目录自检 719 passed，
编译端到端 441 passed / 282 ignored。C# 测试项目构建有既有分析器警告，未改运行时或其测试。
转正后的真实发布完成后，再运行最后一轮 `make check`，仍 exit 0、数量相同，
日志为 `target/binding-resources-make-check-final.log`。

公共 TODO/roadmap/stdlib/semantics、`as_file()` 与其运行时测试留给主代理整合；
本次未修改这些文件。资源/目录事务原有预算与崩溃恢复边界见 ADR。
