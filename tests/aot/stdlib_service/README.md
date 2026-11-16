# 常用标准库跨模块应用验收

本应用将真实 HTTP 请求、URL 编码、Base64 严格解码、gzip 解压、JSON、包数据、SQLite 参数化 SQL 与事务放在同一个程序中。数据库接入复用成熟 SQLite 实现，网络、编码与压缩复用 .NET 能力。

2026-11-13 在 macOS arm64 完成主代理整合验收，7 个执行步骤全部符合预期。CPython 3.12.13、JIT、原位与搬迁后 NativeAOT 输出完全一致；缺失 SQLite 原生库负例退出 1，并给出含 e_sqlite3 的 ImportError。

```sh
make runtime
cargo build -p ryupy-cli
uv run --no-project --python 3.12.13 python tests/aot/stdlib_service/run.py \
  --ryupy target/debug/ryupy --python "$(uv python find 3.12.13)" \
  --out /tmp/ryupy-stdlib-service
```

验收器启动只监听 loopback 的本地 HTTP 服务，返回包含中文和 SQL 形状文本的数据。每次应用运行均落盘新数据库，由独立 CPython 连接检查记录与数据库完整性。搬迁阶段删除源码、JIT 输出和 NativeAOT 构建目录，在中文、空格及 `&` 路径、空工具 PATH 和不同 cwd 下运行原生程序。

成功合同要求固定 CPython 3.12.13、JIT、原位 NativeAOT、搬迁后 NativeAOT 四次输出相同；
事务异常回滚后仅保留两条完整记录，包资源仍存在。日志、数据库和 `result.json` 默认在
仓库外临时目录执行后清理，只有显式 `--keep` 才保留失败调试材料。

验收同时检查 JIT 托管依赖、版本合同、NativeAOT 当前 RID 原生库及许可证的随包部署和清单角色。移走原生库后单独运行失败，验证没有从原构建位置补库。

历史原始产物已按 ADR-0182 清理。该次输出为两条有序记录、计数 `(2,)` 与资源存在 `True`，
5 次真实 HTTP 请求（含缺库负例）。Linux/Windows 和无 SDK 安装的干净目标机继续待验。

## CPython provider 同程序迁移

`run_provider.py` 使用同一份 `app.py`、`appdata/__init__.py` 与 `schema.sql`；只调整构建
声明和验收驱动。`urllib`、`base64`、`gzip`、`json`、`sqlite3`、`importlib`、`sys`、
`appdata` 整族归 CPython，入口继续由 RyuPy 编译。每个执行臂使用独立 cwd 和数据库，
独立检查两条完整记录、回滚、完整性与一次真实 HTTP 请求，并比较 stdout/stderr/退出码。
搬迁前删除私有发行输入、原业务输入与构建目录，运行路径含中文、空格、`&`；验证实际加载
路径、原生链接闭包、缺资源与缺sqlite3包。完整合同及固定发行见
[CPython provider 验收](../../cpython/README.md)。

```sh
python3.12 -B tests/aot/stdlib_service/run_provider.py --baseline-only
python3.12 -B tests/aot/stdlib_service/run_provider.py --ryupy target/debug/ryupy \
  --distribution "$(dirname "$(dirname "$(uv python find 3.12.13)")")" \
  --distribution-metadata "$HOME/.cache/ryupy/cpython-provider/pbs-20260623-metadata/python"
```

2026-11-13 macOS arm64：固定CPython3.12.13/PBS20260623，显式synchronized模式下，
CP/JIT/NativeAOT原位与搬迁全通过；四次完整三流一致，数据库逐值/回滚/完整性及请求数通过。
搬迁后缺资源与缺sqlite3包均按预期独立失败。其他平台及干净目标机待验收。
