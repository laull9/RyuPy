# zlib/gzip 原生 API 验收记录

日期：2026-11-13。模块决策见 [ADR0176](../adr/0176-zlib-gzip-native-compression.md)。
公共 ADR0175 由主代理维护；本记录只验收压缩模块，联合 HTTP/base64/SQLite 应用由主代理整合。

## 环境与隔离

- worktree：`t3-6202a292-623e403633f3cc26`，分支 `t3/zlib-gzip-stdlib`。
- macOS 27.0.1、arm64；.NET SDK 10.0.403 / net10.0；CPython 3.12.13。
- Cargo 输出与临时目录均放在本 worktree；`RUST_TEST_THREADS=1`。
- runtime、e2e、NativeAOT 按顺序执行，没有同时启动另一组运行时验收。
- 本轮未修改 driver 部署、公共 TODO、roadmap 或 stdlib.md；未增加其它模块。

```sh
export CARGO_TARGET_DIR="$PWD/target/zlib-gzip"
export TMPDIR="$PWD/target/zlib-tmp"
export RUST_TEST_THREADS=1
export PYTHON="$(uv python find 3.12.13)"
export RYUPY_FOREACH_PYTHON="$PYTHON"
mkdir -p "$CARGO_TARGET_DIR" "$TMPDIR"
make check
cargo build -p ryupy-cli
"$PYTHON" tests/compression/verify_interop.py \
  --ryupy "$CARGO_TARGET_DIR/debug/ryupy" \
  --out target/compression-interop --aot
```

`RYUPY_FOREACH_PYTHON` 是既有 driver 夹具自己的版本选择器；只设置 `PYTHON` 时，
它会使用 PATH 中其它 Python，导致精确版本检查失败。本轮最终完整检查显式设置两者。

## 实际结果

| 验收 | 结果 | 本地日志 |
| --- | --- | --- |
| 新 e2e 初始 pending → CP 差分 | 3 成功、0 失败、3 可转正；随后移除 pending | `target/zlib-e2e-pending.log` |
| 最终 `make check` | 退出码 0；fmt/clippy 成功，Release runtime 构建成功 | `target/zlib-make-check.log` |
| Python 门禁自测 | cases 9、project gate 22，全部通过 | 同上 |
| 固定 CP 基线 | 721 通过、0 失败 | 同上 |
| C# 全量 | 3034 通过、2 跳过、0 失败 | 同上 |
| Rust 工作区（排除独立 e2e crate） | 1230 通过、14 忽略、0 失败，含 doc-tests 汇总 | 同上 |
| e2e 的 CP 端 | 727 通过、0 失败 | 同上 |
| e2e 的 RyuPy 端 | 447 通过、284 忽略、0 失败、0 pending、0 可转正 | 同上 |
| 最终真实 JIT/NativeAOT 双向互操作 | 5 步退出码均为 0，结果 `passed` | `target/zlib-interop-final.log` |

正式 RyuPy e2e 中，`stdlib/compression_values`、`compression_errors`、`compression_files`
三项全部通过。既有忽略项与 C# 跳过项保持原状；本轮没有运行四项旧反向 frame 证明实验。

### C# 专项覆盖

`PyCompressionTests` 有 8 个执行项（3 窗口 Theory + 5 Fact），覆盖：

- 三容器 × 十一级别，空正文、.NET 反解压、每一个截断前缀及首流尾随数据。
- gzip 多成员、零填充、CRC32/ISIZE、可选头、FHCRC、损坏 DEFLATE。
- MTIME 四字节、浮点截断、非法时间、CRC32/Adler32 标准向量与链式种子。
- 参数错误先于创建文件、父路径为普通文件时的 `NotADirectoryError`。
- 二进制/文本读写、append 新成员、flush、重复 close、独占重新打开验证句柄释放。
- 65 MiB 压缩炸弹、65 个 1 MiB 成员的累计限额，均抛 `MemoryError`。

`PyCompressionAbiContractTests` 另有 1 项真实 PE 合同测试，锁定 7 条新增公开异常签名，
并拒绝缺项、改签、重复与未知项；所有既有 ABI 组的完整增量允许表同步更新。
压缩与文件帮助类保持 internal。完整 C# 门已包含这些专项。

### CPython 双向跨解压与真实搬迁

最终证据目录：`target/compression-interop/compression-rvwu5luv/`。
目录中的 `result.json` 记录环境、命令及每步退出码；`logs/` 保存原始 stdout/stderr。
这些是本地验收产物，复验会生成新的独立目录，脚本与夹具纳入版本管理。

- CPython 生成 66 个输入容器：2 种正文 × 11 种 level × 3 种 wbits。
  JIT 和 NativeAOT 分别成功解压；另覆盖 CP 的 optional-header、拼接成员和损坏载荷。
- CPython、JIT、NativeAOT 各生成 78 个输出，由 CPython 逐项反解压并检查原始正文。
  其中覆盖显式 `mtime=0/1/1234567890/4294967295`、当前时间和 gzip 文件。
- JIT/Native 的 stdout、stderr 与同次 CPython 执行完全相同。
- 真实执行 `ryupy build --aot`，将产出的 `.dist` 拷贝到 `搬迁 native bundle`。
  删除本轮源目录、JIT 构建和原 NativeAOT 构建目录，使用新的 cwd 与新输入目录。
- 清除 PYTHON、RYUPY、DOTNET、COREHOST、COMPlus 前缀环境变量，PATH 指向空目录，
  直接启动搬迁后的本机可执行文件，退出码 0，78 个输出仍由 CPython 成功反解压。

## API 与交付边界

- `zlib.compress/decompress/adler32/crc32` 与 `gzip.compress/decompress/open` 已逐 API 验收；
  `gzip.BadGzipFile`、`zlib.error` 和 `MemoryError` 有真实异常构造、捕获和类型身份。
- level 真传 `-1..9`；压缩 wbits 为 `15/-15/31`，解压另支持 `0/47`。
  小窗口明确抛 `NotImplementedError`，非法窗口/级别抛 `zlib.error`。
- 显式 MTIME 写入容器；gzip 整体输出字节、OS/XFL/FNAME 与压缩块布局按 ADR0176 允许差异。
  负数/非有限/超范围时间戳明确拒绝，异常类别与 CP 的差异已登记。
- gzip 文件支持真实磁盘上下文、追加、迭代与有限 strict 文本 I/O；
  seek、truncate、text tell、非 strict 编码错误策略明确拒绝。
- 每次解压/每个 gzip 文件累计输出上限 64 MiB；FNAME/FCOMMENT 各 1 MiB。
  解压器单字节输入适配优先保证截断与成员边界，吞吐量优化留待性能任务。
- CRC32 当前独立实现，不依赖其它分支尚未合入的 binascii 类型；后续合并可共享内部算法。
- 本轮只验证 macOS arm64；Windows/Linux 的原生产物与联合 HTTP→base64→gzip/zlib→SQLite
  应用留给主代理后续整合验收。本地提交交付，不推送、不建 PR、不派评审。
