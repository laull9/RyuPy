# v3 C ABI 独立验收

自有小 C 库通过统一声明接入，固定 CPython 的 ctypes 适配器与 RyuPy 执行同一 `app.py`，
逐字节比较完整 stdout。生产代码不使用 ctypes。合同见 [ADR0173](../../../docs/adr/0173-declarative-cabi-signatures.md)。

## 复现

在 checkout 根目录串行执行，避免并发构建同一个 C# runtime：

```sh
export CARGO_TARGET_DIR="$PWD/target"
export RUSTC_WRAPPER=
mkdir -p "$PWD/target/tmp"
export TMPDIR="$PWD/target/tmp"
export RYUPY_FOREACH_PYTHON="$(uv python find "$(cat tests/pinned-python.txt)")"
make runtime
cargo build -p ryupy-cli
"$RYUPY_FOREACH_PYTHON" -B tests/aot/binding_abi/run.py \
  --ryupy target/debug/ryupy --out target/abi-evidence \
  --python "$RYUPY_FOREACH_PYTHON"
make check
```

前置：宿主 .NET 10 SDK/NativeAOT 工具链、C 编译器与静态归档器。macOS 使用 cc/ar；
Linux 使用 cc/ar；Windows 从 MSVC 开发环境调用 cl/link/lib。新增 ABI 实机验收在
2026-11-13 的 macOS arm64、SDK 10.0.403、CPython 3.12.13 上执行；Linux/Windows 待补。

## 断言与证据

- 全部 13 类标量：10 类整数的上下界、越界和拒绝 bool/float/str/None；f32 舍入、
  次正规数、有限溢出、NaN/inf/负零，bool8 的实际 bool 入参与非规范返回拒绝。
- UTF8/bytes：空输入、非空哨兵、中文/补充平面、嵌入 NUL、FF/80、类型拒绝、
  编码失败；多个缓冲混合标量；0 参数 void、16 混合标量、16 缓冲/32 机器参数。
- 全部参数先校验：混合签名末参错误重复 128 次，真实 C 调用计数保持不变；
  `__index__` / `__float__` 禁止隐式调用，位置专用参数和参数数量错误明确抛 TypeError。
- 返回物化失败和三类 loader 失败都带缓冲重复 128 次；PE 单测确认外层 finally 覆盖
  PInvoke、窄 catch 和返回物化；C# 单测验证 Dispose 释放全部 pin、GC 后地址稳定和快照隔离。
- 14 项无效声明在原生文件读取/输出写入之前失败，覆盖未知字段/类型、null 封送字段、
  错误调用约定/长度/所有权、buffer 返回、void 输入、超额/重名参数、符号冲突和版本混用。
- 真正构建 JIT/NativeAOT 与 schema7；静态重复符号别名去重，缺符号 archive 交给真实链接器失败，
  失败后旧 AOT dist 仍运行正确；显式资源与 v3 同次 AOT 发布得到 schema8。
- 源码、shared/archive 输入、构建目录整体移走，换 cwd 运行搬迁 JIT/AOT/资源AOT，
  完整差分仍通过。原 v1/v2 与资源组合使用各自 runner 回归。

runner 将每一步命令、退出码、宿主与搬迁路径写入独立证据目录的 `result.json`，
逐步 stdout/stderr 保存在 `logs/`；证据在本 worktree 的 target 下，不加入版本库。
所有断言成功才去除 pending 标记。此 fixture 位于 `tests/aot/`，需显式运行上述 runner；
`make check` 覆盖新增 Rust/C# 单测和项目既有门禁。

## 本次验证记录

2026-11-13 在上述 macOS arm64 环境完成：

- 完整 `RUST_TEST_THREADS=1 make check` 退出 0：fmt、clippy、runtime 构建、Rust 单测、
  固定 CPython 基线 721 项、C# 3025 项通过/2 项跳过、CPython 全用例 724 项、
  RyuPy e2e 444 项通过/284 项既有跳过。日志：`target/abi-make-check-serial.log`。
- 首次完整门禁遇到既有 `ordinary_dynamic_pilot` 的并行时间戳临时目录碰撞；
  该专项串行 3 项通过后，串行重跑完整门禁成功，未修改无关测试实现。
- v3 runner 最终复跑退出 0：`target/abi-evidence-verified/abi-__twuoal/result.json`
  的 `status` 为 `passed`，含 14 项声明负例、JIT/AOT/资源组合及三类装载失败。
- 原 v1/v2 多导出与资源组合 runner 均通过，分别留存于
  `target/abi-legacy-multi/` 与 `target/abi-legacy-resources/`。

## 已发现的源码边界

本轮首次差分揭示源码字面量 `"a\ud800b"` 在现有字符串常量链中成为替代字符。
ABI 编码器需要实际孤立代理项，因此验收使用 `"a" + chr(0xd800) + "b"`，得到预期
UnicodeEncodeError；C# 单测直接覆盖孤立 UTF16 输入。源码字面量缺口已登记 semantics §4.10，
保留在原语义链的后续修复范围。本轮未扩展 handle、可写借用、回调、native-owned 输出或算法库。
