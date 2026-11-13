# 源码 catalog v2 独立验收

基线 `cd678640`，2026-11-13，macOS arm64 / .NET 10 / CPython 3.12.13。
实现合同见 [ADR0174](../adr/0174-versioned-source-catalog-probe.md)。

## 逐 API 范围

fixture：`tests/e2e/phase3/fixtures/catalog_colorsys_api.py`，先 pending，实测通过后转正。
独立 Rust integration test 消费该 fixture；fixtures 目录不参与普通单文件 e2e 发现。

| API | 实际场景 |
| --- | --- |
| rgb_to_yiq | 九组 RGB：黑白灰、三原色、三组混色；与反向转换往返 |
| yiq_to_rgb | 九组往返、亮度负值/超范围截断、TypeError |
| rgb_to_hls | 同九组 RGB，max/min 分支与无彩色，TypeError |
| hls_to_rgb | 往返，十种 hue（负值、扇区边界、1/2）×高低亮度/零饱和度，TypeError |
| rgb_to_hsv | 同九组 RGB，无彩色和通道最大值分支，TypeError |
| hsv_to_rgb | 往返，十种 hue×有/无饱和度，TypeError |

另验重复 import 对象身份。共 55 行完整 stdout 与固定 CPython 比较，颜色转换值 round
至九位小数。原源编译路径没有手工 C# 颜色函数、Python 子进程执行或解释器回退。

## 可重放命令

在 worktree 根串行执行，runtime build 不并发：

```sh
export CARGO_TARGET_DIR="$PWD/target" RUSTC_WRAPPER= TMPDIR="$PWD/target/tmp"
export RUST_TEST_THREADS=1
mkdir -p "$TMPDIR"
export RYUPY_FOREACH_PYTHON="$(uv python find "$(cat tests/pinned-python.txt)")"
export PYTHON="$RYUPY_FOREACH_PYTHON"
make runtime
cargo test -p ryupy-driver --lib source_probe -- --nocapture
cargo test -p ryupy-driver --test catalog_colorsys -- --include-ignored --nocapture --test-threads=1
make check
```

专项测试显式选择既有 OrdinaryDynamicPilot，默认发行策略保持原值。JIT 差分加入
常规 cargo test，真实 ILC 测试独立 ignored，由上述命令显式串行运行。
AOT 测试在搬迁后删除源码/out（含 build），设置空工具 PATH，移除 Python 环境变量，
其它 cwd 启动原生程序；真实 ILC 输出和 55 行差分成功后才清理临时验收树。

## 探测 CLI

```sh
cargo run -p ryupy-cli --features ryupy-driver/ordinary-dynamic-module-pilot -- \
  probe application.py --aot -I explicit_sources -o target/probe
```

报告在 stdout 及 `target/probe/application.source-plan.json`。默认不带 pilot feature 时，
colorsys 报 catalog_unavailable 并解释策略门；这是保留原有授权选择的有意行为。
`import queue` 会读取内嵌 queue 原源以给出依赖缺口，但绝不创建编译权限 Token。
动态导入候选保留 operation、literal_target、reason，不运行用户表达式。
用户 colorsys.py 优先，版本与 provenance 均留空/false，不继承 catalog 能力。
未准入 queue 若依赖根中已有的 threading.py，报告 source_detected_unselected 并继续
解析其 imports；星号导入中同类来源也如此。该状态明确区别实际缺文件和已选编译来源。

单线程测试设置用于避开既有 ordinary_dynamic_pilot 并行临时目录时间戳碰撞；
保持原测试源码和断言，完整执行所有常规门禁。

## 本轮实测结果

2026-11-13，按上述固定环境完成最终复验，所有命令退出码均为 0：

| 检查 | 实测结果 |
| --- | --- |
| `make runtime` | Release runtime 构建成功，未并发构建 |
| `make check` | fmt、workspace clippy `-D warnings`、Rust workspace 测试及全部常规门禁通过 |
| .NET runtime 测试 | 3009 通过、2 跳过、0 失败 |
| 独立 CPython 基线 | 721 通过、0 失败 |
| Python-only 多文件用例自检 | 724 通过、0 失败 |
| 编译器常规 e2e | 444 通过、284 ignored、0 失败；沿用现有四项 frame 证明实验隔离规则 |
| 来源报告专项单元测试 | 9 项通过，含用户原字节隔离、搜索根顺序、父包固定、环与传递缺口、动态别名、未选用户源码递归解析 |
| CLI 阶段诊断回归 | 6 项通过；语义失败只留下来源报告，没有 PE 产物 |
| colorsys 独立复验 | 2 项通过：JIT 与真实 NativeAOT 搬迁各复核全部 55 行输出 |
| pilot feature CLI clippy | 全 targets、`-D warnings` 通过 |

真实 CLI 分别生成可用闭包、queue 缺依赖、动态调用不确定、用户同名源码覆盖、
默认策略拒绝 catalog、未选用户源码六类报告。stdout 与落盘 JSON 一致；入口中
特设的异常和写文件代码均未执行。`source_detected_unselected` 案例继续报告用户
源码中的传递缺口，同时保留 null provider 与 false provenance。

原始日志在本 worktree 的 `target/catalog-make-check.log`、`target/colorsys-final.log`、
`target/catalog-feature-clippy.log`、`target/catalog-validation-env.log`；CLI 实际产物在
`target/catalog-probe/`。这些本地日志随构建目录保留，可提交的复验入口为本节命令与测试源码。

## 边界

完整标准库尚未完成；旧四模块 tag 未核实、queue 未接入、动态反射不确定、C# 模块
逐 API 覆盖、其他平台与干净目标机验收都保留为独立工作。
报告是静态发现产物，最终成功需结合编译诊断、optional-sources.json 和 AOT artifact 清单。
