# sqlite3：成熟 provider、静态适配与可搬迁部署

- 日期：2026-11-13
- 状态：本分支实现并验证；交主代理整合
- 编号：独立命名，留待主代理分配；不占用 ADR0175
- 配套说明：[sqlite3-implementation.md](../sqlite3-implementation.md)

## 决策

1. 使用 `Microsoft.Data.Sqlite 10.0.15` 和 `SQLitePCLRaw.bundle_e_sqlite3 2.1.13`，版本集中在 `runtime/RyuPy.Runtime/RuntimeDependencies.props`。复用 SQLite 引擎及官方 provider，不实现数据库底层、不引入 OpenSSL 或 SQLCipher。
2. SQLite 对象沿用 `PyNativeObject`、固定委托和显式类型判断。实现类保持 `internal`，Python 层提供 `sqlite3.Connection` / `Cursor` 的类型身份；不扩展公开 CLR 桥接 ABI，不使用反射发射、动态表达式编译或动态 provider。
3. 参数化 SQL 继续交由 SQLite 绑定。薄词法适配只把裸 `?` 改成内部命名参数，跳过字符串、引号标识符和注释；引擎 prepare 校验参数身份及单语句 tail，绝不把参数值插进 SQL。重复批量操作复用同一个 `SqliteCommand` 的 prepared cache。
4. 采用 CPython 3.12 legacy 事务模型：DML 按 `isolation_level` 隐式 BEGIN；提交、回滚、with 退出操作真实 SQLite 事务；with 不关闭连接，失败提交继续尝试回滚。禁用连接池以保证显式关闭后的文件锁和未提交事务得到释放。
5. provider 的超时单位是整数秒且零为无限等待。因此只接受正整数秒；对分数、零、负数、非有限数和溢出值明确抛 `NotImplementedError`。先保证时间合同准确，未来再独立设计毫秒超时；不四舍五入或悄悄忽略参数。
6. 运行时依赖是发行闭包。JIT 应用目录携带四个 managed DLL、版本 props、当前宿主 RID 的 SQLite native 和第三方许可证；AOT 使用相同 props 恢复 NuGet，由 SDK/ILC 选择发布 RID，只保留 native 和许可证作为外部依赖。禁止打包所有平台 `runtimes`。
7. 依赖资产使用四个明确角色：`runtime_managed_dependency`、`runtime_native_dependency`、`runtime_dependency_contract`、`runtime_dependency_license`。只接受固定文件名；许可证作为文本资产，不参与原生镜像格式检查。沿用既有清单机制，不新增普通文件摘要流程。

## 依据

- [官方 NuGet：Microsoft.Data.Sqlite 10.0.15](https://www.nuget.org/packages/Microsoft.Data.Sqlite/10.0.15)
- [官方 NuGet：SQLitePCLRaw.bundle_e_sqlite3 2.1.13](https://www.nuget.org/packages/SQLitePCLRaw.bundle_e_sqlite3/2.1.13)
- [Microsoft：SQLitePCLRaw bundles 与 providers](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/custom-versions)
- [Microsoft：参数化 API](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/parameters)
- [Microsoft.Data.Sqlite release/10.0 源码](https://github.com/dotnet/efcore/tree/release/10.0/src/Microsoft.Data.Sqlite.Core)
- [SQLitePCLRaw 上游源码与初始化说明](https://github.com/ericsink/SQLitePCL.raw)

调研核对 `SqliteConnection`、`SqliteCommand`、`SqliteParameter`、`Batteries_V2.Init()`、`sqlite3_prepare_v2`、bind 参数元数据、changes/last_insert_rowid/get_autocommit。依赖采用本次实际验证的锁定版本，升级需重跑差分、许可证核对及真实 NativeAOT 搬迁。

## 后果与验收范围

- 用户可以将既有文件数据库随应用搬走，继续由 CPython 和本应用读写，无自定义数据库格式。
- JIT 运行仍需 .NET 10；NativeAOT 运行无需 SDK/CLR，同 RID 的 SQLite 动态库必须随包。
- 缺失托管/native/props/许可证在构建阶段报清晰错误；产物 native 被删除后，首次 connect 抛 `ImportError`。无系统库降级或自动下载。
- 单独的模块导入、C# 单测或 publish 成功均不足以完成验收；本分支验证 CPython 差分、实际文件读写、搬迁、原路径移走、应用目录加载优先和缺库失败。
- 本次实机平台为 macOS arm64，Linux/Windows 仍需对应 RID 的实机执行。详细逐 API 范围与限制见配套说明；未实现选项一律明确拒绝。
