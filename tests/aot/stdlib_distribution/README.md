# 标准库源码、C ABI 与资源联合分发验收

用同一 Python 程序调用原版 `colorsys`、读取显式包数据并通过 v3 bytes 绑定调用真实 C 函数。
fixture 使用自有小型 C 库，CPython 参考通过 ctypes 调用；RyuPy 生成静态绑定。

```sh
make runtime
cargo build -p ryupy-cli
uv run --no-project --python 3.12.13 python tests/aot/stdlib_distribution/run.py \
  --ryupy target/debug/ryupy --python "$(uv python find 3.12.13)" \
  --out target/stdlib-distribution
```

runner 保存原始 stdout/stderr 和 `result.json`，包含 11 步的实际退出码。
失败构建、缺失运行期资源两步应返回非零，其余步骤为零。

已验收：2026-11-13，macOS arm64，11 步通过。当前已用无 feature 默认 CLI 重新运行通过，见 `target/stdlib-distribution-default/stdlib-dist-qg7r72mz/result.json`。
除对照原位输出，还验证 schema9、provider 闭包、缺资源失败保旧 dist、
搬迁中文路径、删除源码/构建/JIT 目录、清理环境变量、空工具 PATH 和更换 cwd。
Linux/Windows 与无 SDK 安装的目标机仍需独立运行。

具体合同见 [ADR0175](../../../docs/adr/0175-stdlib-binding-resource-distribution.md)。
