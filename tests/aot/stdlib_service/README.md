# 常用标准库跨模块应用验收

本应用将真实 HTTP 请求、URL 编码、Base64 严格解码、gzip 解压、JSON、包数据、SQLite 参数化 SQL 与事务放在同一个程序中。数据库接入复用成熟 SQLite 实现，网络、编码与压缩复用 .NET 能力。

2026-11-13 在 macOS arm64 完成主代理整合验收，7 个执行步骤全部符合预期。CPython 3.12.13、JIT、原位与搬迁后 NativeAOT 输出完全一致；缺失 SQLite 原生库负例退出 1，并给出含 e_sqlite3 的 ImportError。

```sh
make runtime
cargo build -p ryupy-cli
uv run --no-project --python 3.12.13 python tests/aot/stdlib_service/run.py \
  --ryupy target/debug/ryupy --python "$(uv python find 3.12.13)" \
  --out target/stdlib-service
```

验收器启动只监听 loopback 的本地 HTTP 服务，返回包含中文和 SQL 形状文本的数据。每次应用运行均落盘新数据库，由独立 CPython 连接检查记录与数据库完整性。搬迁阶段删除源码、JIT 输出和 NativeAOT 构建目录，在中文、空格及 `&` 路径、空工具 PATH 和不同 cwd 下运行原生程序。

成功合同要求 CPython、JIT、原位 NativeAOT、搬迁后 NativeAOT 四次输出相同；事务异常回滚后仅保留两条记录，包资源仍存在。日志与 `result.json` 保存在命令输出的独立目录。

验收同时检查 JIT 托管依赖、版本合同、NativeAOT 当前 RID 原生库及许可证的随包部署和清单角色。移走原生库后单独运行失败，验证没有从原构建位置补库。

本轮日志：`target/stdlib-service/stdlib-service-wiqxzvn4/result.json`。输出为两条有序记录、计数 `(2,)` 与资源存在 `True`，5 次真实 HTTP 请求（含缺库负例）。Linux/Windows 和无 SDK 安装的干净目标机继续待验。
