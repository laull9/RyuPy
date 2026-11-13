# sqlite3 竖切实现与主代理整合说明

## 范围与入口

本说明登记已整合的 SQLite API、部署接口及验证。默认 CLI 的来源报告、schema9 联合分发与认证 catalog 已共同接通；HTTP→编码解压→包资源→SQLite 的真实跨模块应用见 [联合验收](../tests/aot/stdlib_service/README.md)。

模块由 `PyNativeModules` 注册；`PySqlite` 提供固定函数签名和异常类别，`PySqliteConnection` 管理事务/线程/关闭，`PySqliteStatement` 处理参数词法与单语句检查，`PySqliteCursor` 管理流式结果。实现类型全部内部可见，沿用统一 native 对象分派，不新增编译器运行时函数或公开 CIL 桥接入口。

依赖与架构理由见 [独立 ADR](adr/0180-sqlite3-provider.md)。版本唯一来源为 `runtime/RyuPy.Runtime/RuntimeDependencies.props`：`Microsoft.Data.Sqlite 10.0.15`、`SQLitePCLRaw.bundle_e_sqlite3 2.1.13`。许可证在 `ThirdPartyNotices.sqlite3.txt`，包括 Microsoft MIT、SQLitePCLRaw Apache-2.0、上游 NOTICE 及 SQLite public domain 说明；法律文本保留原文。

## 已实现 API

| API / 行为 | 本次验收 |
| --- | --- |
| `connect(database, *, timeout=5.0, isolation_level='', check_same_thread=True)` | database 为普通 str 路径，支持 `:memory:`、中文路径和实际文件库；三个选项仅 keyword-only |
| `Connection.cursor()` | 创建独立游标，游标持有连接 |
| 连接和游标 `execute(sql, parameters=())` | 返回游标；单条 SQL；`?`、`:name`、`@name`、`$name`；tuple/list/dict 参数；命名重复参数和额外 dict 键 |
| 连接和游标 `executemany(sql, parameters)` | 外层参数迭代器逐项消费，仅 INSERT/UPDATE/DELETE/REPLACE 且无 RETURNING；错误保留成功前缀和事务，`rowcount=-1`，可 rollback |
| `fetchone()` / `fetchall()` / `fetchmany(size)` | tuple 行、耗尽返回 None、剩余行列表；fetchmany 默认 arraysize，按 CPython 3.12 对非正 size 取全部 |
| 游标迭代 | `iter` / `next` 与普通 fetch 共用结果流，耗尽关闭 reader |
| `description` / `rowcount` / `lastrowid` / `arraysize` | 七项列描述；DML 行数；插入 rowid；批量操作不更新 lastrowid；SELECT 的 rowcount 为 -1 |
| `Cursor.connection` | 返回所属连接 |
| `commit()` / `rollback()` / `in_transaction` | 读取真实 SQLite autocommit 状态；事务外提交/回滚为空操作；允许 SQL 显式 BEGIN |
| `isolation_level` | None、空串、DEFERRED、IMMEDIATE、EXCLUSIVE；大小写规范化；设为 None 时先提交当前事务 |
| `total_changes` | 原生连接累计变更数 |
| `with connection` | 正常退出提交；异常退出回滚并传播原异常；提交失败再回滚；连接继续可用 |
| `close()` | 释放 reader/command/连接；连接关闭回滚未提交事务；重复 close；关闭后操作 ProgrammingError |
| DBAPI 基础值 | None ↔ NULL；bool/int ↔ 64位 INTEGER；float ↔ REAL；str ↔ TEXT；bytes/bytearray → BLOB，BLOB 返回 bytes；空值和空 BLOB；大整数溢出抛 OverflowError |
| 模块常量与类型 | apilevel=2.0、paramstyle=qmark、threadsafety=1；Connection/Cursor 支持 isinstance |
| 异常 | Warning、Error、InterfaceError、DatabaseError、DataError、OperationalError、IntegrityError、InternalError、ProgrammingError、NotSupportedError；限定类型身份、继承链、动态捕获及构造 |

## 明确边界

- `timeout` 只接受 1..Int32.MaxValue 的整数秒（允许浮点形式如 5.0）；0、负数、小数、NaN/Infinity 明确 `NotImplementedError`。provider 的 0=无限等待，当前没有伪装成 CPython 的零超时。
- database 仅 str，NUL 为 ValueError，`file:` URI 为 NotSupportedError。不支持 PathLike、bytes 文件名、URI 选项。
- 仅 tuple/list/dict 作为单次参数集合；不支持 `?NNN`、qmark/命名混合、扩展 Tcl 参数名。值始终经 provider 绑定；多语句在执行前拒绝。
- DML 识别沿用 legacy 首关键字规则，WITH 开头的语句不触发隐式 BEGIN。批量 RETURNING 当前明确拒绝，execute 可流式读取 RETURNING。
- 未开放 adapters/converters、detect_types、row_factory/text_factory、factory、cached_statements、autocommit、executescript、backup、blobopen、extension/callback API。未知 connect 关键字抛 TypeError，缺少的属性/方法抛 AttributeError；Connection/Cursor 不开放直接构造和继承。
- SQLite provider 的原始错误文本与 CPython 不保证逐字一致；异常类别对齐基础路径。`sqlite_errorcode` / `sqlite_errorname` 属性尚未实现。
- 默认检查 .NET managed thread ID。`check_same_thread=False` 允许跨线程顺序访问；并发使用由调用方同步，当前未提供自动序列化。
- .NET GC 的资源释放时间由运行时决定。文件锁和事务依赖显式 close/finally；with 仅管理事务，不关闭连接。
- 当前不提供整个 CPython sqlite3 模块的全面兼容承诺；测试范围由上述 API 和夹具固定。

## 发行与清单接口

### 发行 CLI 到 JIT 应用

`make build` 的发行布局是 `bin/ryupy` + `runtime/`。runtime 内含 RyuPy.Runtime.dll、下列四个 managed DLL、版本 props、许可证，以及唯一当前宿主 `runtimes/<RID>/native/`。RID 取 SDK 的 `NETCoreSdkRuntimeIdentifier`，不复制全部平台资产。

driver 的 `runtime_dir()` 继续按显式环境变量、相对发行 CLI、开发路径顺序定位。`copy_dependencies()` 先检查完整依赖，再把 managed/native/props/许可证复制到应用根；原生路径选择宿主 RID。JIT 无需依赖开发机 NuGet 缓存解析运行时库。运行仍需要 .NET 10，构建 AOT 仍需要 SDK/原生工具链与 NuGet 恢复。

### NativeAOT

`aot.rs` 生成薄宿主时 Import 同一 runtime 旁的 props，保留 XML/MSBuild 路径转义；由 NuGet/ILC 裁剪编译 managed 依赖，并发布目标 RID 的 SQLite native。props 将许可证设为 CopyToOutput/CopyToPublish。AOT 应用由可执行文件、同目录 native 和许可证构成外部运行闭包；其余清单/资源资产按既有联合分发处理。

### 四种固定角色

| 角色 | 确切文件 |
| --- | --- |
| `runtime_managed_dependency` | Microsoft.Data.Sqlite.dll、SQLitePCLRaw.core.dll、SQLitePCLRaw.batteries_v2.dll、SQLitePCLRaw.provider.e_sqlite3.dll |
| `runtime_native_dependency` | 当前 RID 的 libe_sqlite3.dylib、libe_sqlite3.so 或 e_sqlite3.dll |
| `runtime_dependency_contract` | RuntimeDependencies.props |
| `runtime_dependency_license` | ThirdPartyNotices.sqlite3.txt |

`runtime::dependency_role()` 为 driver 收集器提供精确分类；`binding_build.rs` 的 JIT artifact 白名单增加这四类。`manifest_files.rs` 沿用既有清单收集，不增加 hash。验证器在 `tests/aot/runtime_dependencies.py` 共享完整性合同：JIT 必须四 DLL + props + native + 许可证七项；AOT 必须 native + 许可证两项；不接受其它 RID 或错角色。该 helper 当前沿用已有验证器支持的 linux-x64/osx-arm64/win-x64 RID 表。

这些角色已并入 catalog+显式资源联合清单白名单和校验入口，保留 `binding_sources.rs` 抽取、冲突后 source-plan 保存与资源联合发布。上述分类与来源/catalog API 正交，不需要改动来源编译流程。包内 `schema.sql` 仍交给既有 resources API，sqlite3 接收 SQL 字符串。

### 缺库和解析顺序

- 安装 runtime 中 props、managed DLL、当前 RID native 或许可证缺失：CLI 构建立即报包含确切依赖名的错误；provider 文件存在但 props 消失同样报错。
- JIT/AOT 应用 native 被删除：首次 connect 抛 ImportError，并指出要求将匹配的 e_sqlite3 库放在应用旁。首次失败由 Lazy 初始化保留，修复部署后应重新启动进程。
- JIT managed DLL 被人为删除：.NET 加载器报告依赖加载错误；无动态下载或隐藏替代路径。
- 实际搬迁测试在工作目录放置损坏的同名 SQLite 库，应用根正常库仍成功加载；删去应用根库后稳定失败。发行依赖解析优先应用目录。

## 验证与复现

固定解释器为 CPython 3.12.13，实机 macOS arm64，.NET SDK 10.0.403。所有命令在此 worktree，设置独立 TMPDIR/CARGO_TARGET_DIR 和 RUST_TEST_THREADS=1，runtime、e2e、AOT 串行。新增三个 phase3 用例与既有 phase4 sqlite3 用例经 CPython 差分后解除 pending。

```sh
export TMPDIR="$PWD/target/sqlite3/tmp"
export CARGO_TARGET_DIR="$PWD/target/sqlite3/cargo"
export RUST_TEST_THREADS=1
export PYTHON="$(uv python find 3.12.13)"
export RYUPY_FOREACH_PYTHON="$PYTHON"
mkdir -p "$TMPDIR"
dotnet test runtime/RyuPy.Runtime.Tests --filter FullyQualifiedName~PySqliteTests -nologo -v q
make runtime
cargo test -p ryupy-e2e -- sqlite3 --include-ignored
cargo build -p ryupy-cli
make build
"$PYTHON" -B tests/aot/sqlite3/run_portability.py --cli "$CARGO_TARGET_DIR/release/ryupy" --output "$PWD/target/sqlite3/portable-accepted"
"$PYTHON" -B -m unittest discover -s tests/aot -p 'test_*manifest*.py'
"$PYTHON" -B -m unittest discover -s tests/aot -p 'test_runtime_dependencies.py'
make check
```

- C# sqlite3 测试：10项，含真实一秒锁等待、跨线程检查、文件关闭回滚、deferred foreign-key 提交失败回滚、参数注入/多语句拒绝、批量中断和 DBAPI 值映射。
- 完整 `make check` 通过：fmt/clippy、C# 3035项通过且2项跳过、Rust workspace测试、727项夹具自检、e2e 448项通过且283项忽略。SQLite四项全部参与差分；空 SQL 保留 lastrowid，与固定 CPython 3.12.13 一致。
- sqlite3 差分：4项；另固定 CPython 的三个新增夹具自检通过。
- 清单测试：37项含1项跳过，另新增运行时依赖合同2项。
- 搬迁脚本实际构建 JIT 和严格 NativeAOT，模拟发行 CLI 布局；复制应用、移走原产物，把既有文件库移到另一工作目录；隐藏 SDK/PATH，读写中文和 BLOB，并用 CPython 读取同库核对。验证应用目录优先、两种产物缺 native 的 ImportError，以及发行 runtime 缺 native 的构建失败。脚本不计算普通文件摘要。
- `make build` 实际产出 `releases/ryupy-0.0.1-darwin-arm64.tar.gz` 并通过发行 CLI 自检；检查压缩包只有 `runtimes/osx-arm64/native/libe_sqlite3.dylib`，四个 managed DLL、props和许可证齐全。最新 Release CLI 的 JIT/NativeAOT 搬迁脚本再次通过。
- Linux/Windows 尚未实机验收。本分支未替主代理运行跨模块 stdlib_service；整合后由主代理串行执行该应用与最终完整门禁。

实际日志位于 ignored 的 `target/sqlite3/make-check-accepted.log`、`make-build-accepted.log`、`portable-accepted.log` 和 `portable-accepted/portability.log`；可按上述命令重建，搬迁输出需使用尚不存在的新目录。本次本地提交交付后，由主代理评审并整合最终跨模块门禁。
