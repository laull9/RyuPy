# 常用标准库默认发行联合验收

2026-11-13，macOS arm64；主代理评审并整合压缩、网络、编码认证、SQLite 和默认 catalog 策略。
普通 `cargo build -p ryupy-cli`，未开启 pilot feature。

## 两条真实分发合同

1. `tests/aot/stdlib_distribution/run.py`：colorsys 原源+C ABI v3 bytes+包资源，11 步通过。
   `catalog-binding-result.json` 保存全部结果，含预期失败步骤。
2. `tests/aot/stdlib_service/run.py`：HTTP→Base64→gzip→JSON→包资源→SQLite，7 步通过。
   `service-result.json` 保存原始退出码；CPython、JIT、原位与搬迁后 Native 输出一致。
   随包许可证、managed DLL、RID native 及清单角色通过；缺 native 时明确报 ImportError。

两者均搬迁至中文/空格/&路径，删除源码和构建目录，清理加载环境，空工具 PATH、新 cwd 运行。
SQLite 输出含两条有序原始文本、回滚后计数 `(2,)` 和资源存在 `True`。
数据库由独立 CPython sqlite3 重新打开，核对行记录与 SQLite 引擎完整性。

## 默认策略与门禁

driver 单元测试 208 通过、4 个既有忽略；默认 colorsys 六 API 的 CP/JIT 差分通过。
完整门禁结果在最终提交前登记。
按用户 AGENTS.md 要求跳过四项旧 PE 普通产物摘要流程，保留其它 ABI 和语义验证；未提供历史 baseline DLL 的两项既有跳过继续保留。

Linux/Windows 与无 SDK 安装目标机继续待验。64 MiB 解压上限、有限网络和 SQLite API
见各模块逐 API 文档。本轮未实现 libpython、任意 CPython 扩展或完整标准库。
