# GitHub 大型项目补充验收

2026-11-17 固定两份真实上游：后端 **fastapi-best-architecture（fba）**、
桌面 **ExplorerPro 1.0.9**。本目录只保存手写 catalog、分析/重跑脚本与验收合同，
上游代码放 `~/.cache/ryupy/upstream-projects/<owner>--<repo>/`，可跨轮复用。
原二十项目 manifest、lock、入口、成功标准保持原样，新增两项独立登记。

已完成主源 clone、源码/许可证/依赖核查、固定 CPython AST 图分析，并进入原版 CPython
运行基线。fba 原 app startup/OpenAPI/ORM/关机门与 ExplorerPro 真实 Cocoa
窗口/业务链/关闭门均已通过；最新逐项结果见文末。
原 pytest、完整业务矩阵与三种 RyuPy 产品运行门另行验收。
后续驱动调用上游原应用和业务对象；只负责数据隔离、交互、断言和生命周期管理。

## 固定来源与选择

以下均为 2026-11-16 上游提交，使用完整 Git commit 定位；浅 clone 未获取完整 tag 历史，
因此本轮直接固定 commit。未计算普通文件摘要。

- [fba 主源](https://github.com/fastapi-practices/fastapi-best-architecture/tree/dd391ee5df510ec334d4426a5ba8e58b23ddf190)：
  `dd391ee5df510ec334d4426a5ba8e58b23ddf190`，LICENSE/pyproject 为 MIT，Python >=3.12。
  `uv.lock` 随该提交保留；CPython 基线选 3.12.13。
- [ExplorerPro 主源](https://github.com/file-bricks/ExplorerPro/tree/f4f65982128e5d0478556febfef2380b39409e2c)：
  `f4f65982128e5d0478556febfef2380b39409e2c`，LICENSE 为 AGPL v3，元数据标 AGPL-3.0。
  Python >=3.10，PySide6 >=6.5,<7；CPython 基线选 3.12.13，实际安装的 PySide6、
  shiboken6、Qt 版本须在首次环境门固定，基线使用下述精确依赖清单。本轮无上游业务源码进入 RyuPy 仓库。
- 对照 [full-stack-fastapi-template](https://github.com/fastapi/full-stack-fastapi-template/tree/77e550a73bb49dd2967622c5ed11bf7a49603c93)：
  `77e550a73bb49dd2967622c5ed11bf7a49603c93`，MIT，当前 backend 要求 Python >=3.14,<4。
  原 `app/api/routes/items.py` 是同步 SQLModel Session 的 CRUD/commit/refresh；
  含 Alembic 历史迁移、PostgreSQL、JWT、邮件模板、React 前端。后端 Python 规模较小，
  fba 的异步、多数据源、插件、队列与权限组合更适合新增压力门。
- 对照 [pyVideoTrans](https://github.com/jianchang512/pyvideotrans/tree/5e287b1a05de2c47910eaba9ba5ad8d8cc3efd78)：
  `5e287b1a05de2c47910eaba9ba5ad8d8cc3efd78`，GPL v3，版本 4.19；
  Python >=3.10,<3.11，`.python-version=3.10.19`，PySide6/Addons/Essentials=6.9.2。
  `sp.py` 启动真实 MainWindow，加载 qss、编译资源、QSettings；依赖 torch/torchaudio 2.7.1、
  FFmpeg、libsndfile、多个语音/翻译 SDK、模型和资源。`tests/conftest.py` 自动 mock 缺失
  PySide6/torch/服务 SDK；当前优先可离线操作的 ExplorerPro。未下载模型或调用付费 API。

## 源码规模与真实复杂度

`git ls-files '*.py'`，LOC 为物理行（含空行和注释），不包含前端 TS、资源和未跟踪文件。
测试路径、test_ 和 conftest 单列；生成 Qt 资源单列；Designer 残留头或 Ui_* + setupUi
保守列为“UI 布局候选”（含手修可能），从业务源码数剔除；Alembic 迁移属于源码合同。
脚本输出完整逐文件分类，计数可重复生成。

- fba：业务/工具源码 **265 文件 / 19,703 行**；测试 **7 / 69**；生成 UI/资源 0。
- ExplorerPro：业务/工具源码 **70 / 20,337**；测试 **77 / 12,446**；生成 UI/资源 0。
- full-stack：后端/工具源码 **28 / 1,736**；测试 **15 / 1,165**；生成 UI/资源 0。
- pyVideoTrans：剔除布局后源码 **402 / 55,507**；测试 **83 / 10,889**；
  生成资源 **1 / 3,390**；UI 布局候选 **78 / 10,271**。

CPython 3.12.13 解析 fba 全部 272 文件、ExplorerPro 全部 147 文件成功。
静态本地导入去重边分别 **910 / 332**，入口可达文件分别 **161 / 53**。
fba 全树含 501 个 async def、810 个 await、42 个 async with、320 个 class、4 个 type alias；
ExplorerPro 全树含 156 个 class、321 个 lambda、330 个 try；以上特性数包含测试。
图保留文件/行号、相对导入、本地目标、外部/未解析引用及动态调用点。
这是条件和函数内导入也计入的语法超图；运行闭包还需追踪动态导入和资源加载。
对照 full-stack 在 3.12 下 `backend/app/api/deps.py:36` 遇 3.14 的无括号多异常语法，
其基线应使用原要求 3.14，不能套用本目录选中项目的 3.12 解析结果。

## 后端源码链与执行门

真实入口链：`backend/main.py::_prepare_plugins → register_app` →
`core/registrar.py`（中间件、Socket.IO、路由、异常、lifespan）→ 原 admin/task/plugin 服务 →
`database/db.py`（asyncpg、SQLAlchemy AsyncSession、多数据源工厂）和 Redis。
`get_db_transaction` 使用 `async_db_session.begin()`，覆盖成功提交与异常退出回滚。
`app/task/celery.py` 默认 Redis broker，也支持 RabbitMQ；应用覆写 Celery 数据库结果后端。
原 `backend/conftest.py` 导入 app，用 TestClient 登录固定私有测试账户并覆盖 DB 依赖，
`backend/tests/utils/db.py` 另用 `<DATABASE_SCHEMA>_test`；Redis 仍需私有实例。

启动前必须完成：

1. 原缓存只读，复制到仓外临时树，创建固定 3.12.13 venv；按上游 `uv.lock` frozen 安装，
   同时审计并预装每个现有插件的 requirements。原 main 会调用 uv pip 安装缺失插件依赖，
   原配置模块会复制 `.env.example`。禁止让启动阶段修改共享环境或联网补依赖。
2. 新建私有 PostgreSQL 18 cluster（仓外临时 data/socket、随机私有端口、trust 仅私有 socket），
   新建专属主库和 `_test` 库。新建私有 Redis 进程（私有端口、无持久化），所有 DB index
   均属于该实例。每个 oracle/JIT/AOT/onefile 臂分别拥有服务和数据库。
   显式配置 DATABASE_*、DATABASE_SCHEMA、DATABASE_SOURCES、REDIS_*、TOKEN_SECRET_KEY、
   CELERY_BROKER_REDIS_DATABASE；环境变量与复制树 `.env` 保持一致，禁用外部 OAuth/邮件/IP 查询。
3. 原 lifespan 会 create_all、初始化 Redis、启动操作日志消费者，并在退出时排空/关闭。
   先运行原 pytest 认证测试，再通过原 HTTP 登录、错误凭据、RBAC、字典 CRUD、校验错误、
   缓存命中与失效；保留原状态码/JSON/数据库副作用和 stderr，逐臂同次比较。
4. 事务门调用原服务/会话与原模型：提交可重查、唯一约束异常后完整回滚、多数据源隔离。
   Alembic 原 `env.py` 使用 async engine、transaction_per_migration、模型 metadata；
   固定提交未跟踪 `backend/alembic/versions/` 历史。`upgrade head` 空跑不足以完成迁移门。
   必须在临时副本加载原模型，使用原 Alembic autogenerate 生成临时 revision 后做 upgrade、
   inspect、downgrade、再 upgrade；生成迁移和 SQL 输出都留仓外并清理。
5. 私有 Redis worker 调用原已有 Celery 任务，验证完成/失败状态和结果后端；不得 eager 替代 worker。
   uvicorn 仅监听 127.0.0.1 私有端口；关闭后断言 consumer、连接池、worker 均退出。

本机只读预检：Docker CLI 存在，但 `/var/run/docker.sock` 不存在；PostgreSQL 18
和 Redis 127.0.0.1:6379 进程正在运行，5432 接受连接。共享进程与数据库保持原样；新增驱动使用独立 Unix socket PostgreSQL 和随机端口 Redis。

## 桌面源码链与原生门

`src/main.py::main` → 翻译/图标/gui_gc → `ExplorerProApp` → `MainWindow`、
浏览器/侧栏/预览 → `FileIndex`（SQLite FTS5）、PrivacyMonitor、线程和子进程。
原依赖包括 PySide6/shiboken6/Qt 平台与图像插件、PyMuPDF 原生 MuPDF、pandas/NumPy、
openpyxl；SQLite 必须支持 FTS5。资源有 locales/translations.json、assets/icons、Qt 翻译，
`ExplorerPro.spec` 与 src 中加载路径需共同审计。Windows shortcuts 为可选 pywin32。

默认启动会写 `~/.explorerpro/fileindex.db`、QSettings、QStandardPaths，启用剪贴板监控，
800ms 后预加载磁盘根。后续驱动必须先把 Path.home、Qt 配置路径、QSettings IniFormat 路径
和缓存引向同一私有 profile；关闭剪贴板监听，限制浏览/索引根为新造临时输入。
macOS 只改 HOME 变量不足以证明所有 Qt/系统路径隔离，必须在创建业务对象前断言实际配置路径。
不启动普通入口直接扫描真实 home/挂载盘。

- 原 pytest 入口为 `python -m pytest tests`，执行前先审计所有 fixture 与临时路径，
  选出与本合同一致且不计算普通文件摘要的原测试；全套未执行：
  `tests/conftest.py` 默认设置 offscreen，完整 pytest 结果单列为逻辑回归。
  禁止运行普通文件 checksum/duplicate 摘要测试；本轮也没有运行此类计算。
- 原生：当前上游 `src/main.py --release-smoke <私有输出>` 调用 `release_smoke.py`，
  明确拒绝非 `windows` Qt platform。Windows 可复用此原探针；本机 macOS 必须新建薄驱动
  调用原对象并断言 `QApplication.platformName() == 'cocoa'`、原窗口可见/暴露、事件循环实际推进。
  offscreen/minimal 不通过原生门。原 Windows smoke 包含 PDF、XLSX、Unicode、语言切换、
  隐私列表和属性子进程，Cocoa 驱动应覆盖这些原业务调用并补导航/FTS5/编辑保存和退出清理。
- 另一上游 `tests/source_platform_smoke.py` 默认 offscreen，且含 duplicate 扫描和普通窗口
  默认配置访问，当前不运行。薄驱动使用原控件调用并完成隔离，避开普通文件摘要计算。
- 操作只针对新建 Unicode/空格目录；通过原控件信号/点击、文件模型、预览控件、SQLite 查询
  对照输出及副作用，验证 worker 信号、取消与窗口关闭；禁止复制重写文件管理业务。
- 两个平台分别记录原生证据、实际 Qt/Python/扩展版本和资源加载路径；macOS 已由本目录私有隔离驱动完成首组原生业务门；上游 Windows smoke
  保留其平台前置条件，完整跨平台业务矩阵继续待验。

## 有限委派与三种产品计划

catalog 中 `cil_target` 是目标源码范围，provider_candidates 是待冻结的外部运行库归属。
**当前业务委派清单为空**；后续任何本地模块委派必须逐项登记模块、上游来源、原因、版本、
执行阶段、业务覆盖和退出条件。禁止一条 `--cpython-entry` 把整个项目变成已完成的 CIL 结果。
产品兼容门与实际 CIL 执行归属分列；必要 Qt 原生扩展/ORM 驱动使用 provider 需要记录路径证据。

先通过原 CPython 行为门，再用同一上游提交、同次 oracle，依次运行 JIT、NativeAOT
standalone、onefile。各臂独立私有数据库/profile。构建后复制产品到中文/空格目录，删除
**本臂私有源码、venv、构建目录**；保留仓外原缓存供下轮复用。执行时缓存目录必须通过
沙箱/独立用户权限等方式不可读，防止产品偷读原缓存；若暂不能屏蔽，搬迁门保持待验。
清空 PATH，污染 PYTHONHOME/PYTHONPATH，换 cwd；JIT 显式指定 dotnet，原生产品直接执行。
逐臂比较 stdout/stderr/退出码、HTTP 与 ORM 结果、GUI 控件和文件/SQLite 副作用；只归一化
预登记路径/非确定字段。动态导入、dist-info、许可证、Qt 插件、资源、原生库及 worker 子进程
均参与搬迁；缺平台插件/资源/数据库驱动负例须明确失败。服务和 profile 用后只清理本臂私有树。

## 重跑与本轮验证

```sh
uv run --no-project --python 3.12.13 python -B \
  tests/general_projects/github_large/inspect_sources.py --prepare
uv run --no-project --python 3.12.13 python -B \
  tests/general_projects/github_large/inspect_sources.py --all-candidates
uv run --no-project --python 3.12.13 python -B \
  tests/general_projects/github_large/analyze_ryupy.py --ryupy /absolute/path/to/ryupy
```

`inspect_sources.py` 不导入上游，不执行配置、pytest 或应用；支持 `--output /仓外/report.json`
保存完整图，由调用者清理；缓存提交错配/脏工作树直接报错且保留现场。
`analyze_ryupy.py` 对选中项目全部非测试/非 UI 源码逐文件执行 `dump --native --emit ast/hir`，
保留每个诊断并继续发现后续文件，临时 IR 自动清理，失败退出 1；可指定 types/mir 进一步定位。
这是单文件前端压力门，项目链接、依赖闭包、运行语义与原生界面各有上面的独立完成条件。
本轮没有构建 Runtime/Rust，也没有提交、推送或修改原验收入口。

### 本轮现有 Release 前端扫描结果

使用当前 `target/release/ryupy`，未重新构建，二进制与当前脏工作树的精确对应关系待主代理冻结。
335 个非测试源码各跑 AST/HIR 共 670 次。`dump` 即使输出 E0100 等错误也可能退出 0，
脚本同时检查诊断，结果按无错误诊断文件计：

- fba AST：258 清洁 / 7 诊断；HIR：150 清洁 / 115 诊断（其中 7 个退出 1）。
- ExplorerPro AST：70 清洁 / 0 诊断；HIR：47 清洁 / 23 诊断（全部退出 0）。

首项版本诊断为 `backend/common/lifespan.py:9` 的 PEP 695 type alias：
E0002 目标 3.11 拒绝 >=3.12 语法。其余 HIR 诊断包括原生协程 feature 未开启、async with、未支持表达式、globals，
以及单模块上下文的相对导入、嵌套解包、f-string 嵌套格式说明符等；具体错误由重跑 `--output` 输出逐文件保存，运行后清理外部报告。
单模块相对导入诊断需要用完整项目构建再次判断；当前扫描结果用于定位前端压力点。
CPython 3.12.13 AST 图解析两项目全部源码成功，RyuPy 前端诊断继续列为首阻断。

## 原版 CPython 基线驱动（2026-11-17 新切片）

```sh
uv run --no-project --python 3.12.13 python -B \
  tests/general_projects/github_large/baseline.py --project fba
uv run --no-project --python 3.12.13 python -B \
  tests/general_projects/github_large/baseline.py --project explorerpro
```

每次创建新的仓外源码副本、venv、profile，原缓存提交和脏状态先校验，仅修改私有 `.env`
及私有用户配置。依赖 wheel 缓存复用于 `~/.cache/ryupy/upstream-projects/uv-cache/`。
原始 stdout/stderr、freeze、私有服务日志和失败 traceback 按时间戳持久写入
`~/.cache/ryupy/validation/adr0208-20261117/github-large/`，失败记录不会覆盖或删除。
`temporary.json` 记录本臂临时树，`cleanup.json` 记录删除结果；后续新运行另存同次手写
驱动副本以定位实际调用。临时源码/venv/profile/数据库最终清理；截图和运行日志保留。
所有子命令有 timeout；超时/中断回收本命令新进程组，服务在 finally 中仅关闭本臂实例。

### fba 已验合同

成功证据：`fba-20261117T065728.962181Z/`；前一次加强版亦通过于
`fba-20261117T065405.451215Z/`。CPython 3.12.13，根依赖按原 `uv.lock`
frozen export（无普通文件摘要）安装；现有 email/oauth 插件的额外依赖固定为
`aiosmtplib==5.0.0`、`fastapi-oauth20==0.0.2`，其传递依赖复用原锁定环境，freeze 记录实际版本。

- 私有 Pg18：仅本臂 0700 Unix socket，无 TCP listener，initdb 主机认证 reject、本地 trust；
  新建 `ryupy_large` 和 `ryupy_large_test`。私有 Redis：独立随机 loopback 端口，不持久化。
  原应用所有 DATABASE/REDIS/Token/Celery 配置显式覆盖；默认 5432/6379 禁止使用。
- 原 `backend.main.app` 和五个原插件正常导入，TestClient 执行真实 lifespan；
  原 `/openapi` HTTP 200，含 78 条路由。这里只运行 ASGI 请求，TCP 部署另验。
- 原 Role 模型与原 `async_db_session.begin()`：成功提交后重查存在；第二事务 flush 后抛
  异常，重查确认该条记录已回滚。业务模型、Session、路由和生命周期均使用上游原代码。
- lifespan 退出后所有原引擎 checkedout=0；之后在最早登记的 harness atexit 观察点检查
  上游自己的 TaskRunner/loguru 清理钩子，最终 `remaining_threads=[]`，进程退出 0。
  服务日志确认 Redis 子进程已 wait、Pg18 stop 完成，临时树删除。
- fba 无 monkeypatch。harness 只增加审计 hook：阻止运行阶段 subprocess 动态安装，
  socket.connect 只允许本臂 PostgreSQL socket 和 Redis 地址；无付费 API/外部连接。

保留的失败证据包括早期探针文件尚未写入、RabbitMQ 必填字段缺失、以及把上游 atexit
线程误判为 lifespan 后残留的那次失败；最终驱动改为在上游退出钩子之后观察线程，
没有手动替上游关闭这些线程。原认证 pytest/登录/RBAC/完整 CRUD、迁移、Celery worker、
TCP 与 RyuPy 产品门继续待验，本切片通过 startup/OpenAPI/ORM/关机这组原程序门。

### ExplorerPro 隔离合同

依赖固定于 `explorerpro-requirements.txt`，PySide6/shiboken6/Qt=6.9.2；无模型/付费 SDK。
薄驱动保留原 ExplorerProApp、MainWindow、文件浏览器、预览、FileIndex 与线程实现。
业务对象创建前的控制项明确如下：

1. `Path.home()` 指向本臂 profile；`QStandardPaths.writableLocation/standardLocations`、
   `QDir.homePath` 指向私有路径；QSettings 改 IniFormat 并设私有 User/SystemScope 路径，
   创建前断言 SettingsManager 与 QSettings 的实际文件名在 profile 下。
2. `QDir.drives()` 只提供私有 profile，原云发现函数返回空集合；原驱动容量查询、
   800ms 预加载与文件模型仍运行，但输入根均为本臂私有树。
3. 原设置关闭剪贴板监听与自动索引；`QApplication.clipboard` 改为直接报错的访问防线，
   不读写系统剪贴板。`FileBrowser.navigate_to` 仅增加私有根断言后调用原方法。
4. 原 `main` 的翻译/图标/版本/gui_gc 初始化由薄驱动调用；直接创建原 ExplorerProApp，
   显式要求 Cocoa、窗口 visible/exposed、真正 app.exec 定时回调和事件循环。
5. Unicode 导航、原文本预览、原 FileIndex.index_file(calculate_hash=False)/search，
   SQLite 中 hash 字段保持 NULL；窗口截图写持久证据目录。
6. 原窗口 closeEvent 后，沿用原 main 的 shutdown_capacity_executor/gui_gc 清理，
   检查业务 Qt worker 和 Python 线程列表。整个驱动有外部超时与进程组回收。

这是隔离后的原业务对象运行门；默认用户 profile、云盘发现和剪贴板监听保持独立待验。

ExplorerPro 首次成功证据：`explorerpro-20261117T065025.969854Z/`。
`probe.log` 记录 Cocoa、visible/exposed、Qt/PySide6 6.9.2、Unicode 导航/文本预览、SQLite
索引和搜索全部成功，关闭后 `qt_threads=[]`、`python_threads=[]`，退出 0；临时树删除。
截图 `cocoa-unicode-preview.png` 已通过 view_image 查看，显示私有 Unicode 目录、原文件
列表、预览文本和 metadata。保留了原上游的 Consolas 字体替代性能警告，未屏蔽 stderr。
此门尚未执行完整 pytest、PDF/XLSX、编辑保存、其它平台及 RyuPy 编译/搬迁。

最终驱动复跑亦通过：`explorerpro-20261117T070122.463120Z/`，截图已再次查看；
该次同样记录 `temporary_tree_removed=true`，同次驱动副本保存在 `driver/`。

## 完整原入口构建门（2026-11-17）

`build_project.py` 使用预先按上述基线锁定准备的 CPython 3.12.13 venv，校验上游
固定 Git 提交与干净状态，复制原项目到仓外，再通过真实 CLI 构建完整原入口。
所有本地业务保持 CIL 编译请求，未增加业务委派。可重复 `--mode jit|standalone|onefile`；
默认仅 JIT 构建。驱动保存精确命令、依赖 freeze、完整诊断与结果后清理源码/构建副本。
该门只构建，`runtime` 和 `relocation` 明确标为 `not-run`。

```sh
python3 -B tests/general_projects/github_large/build_project.py \
  --project fba --python /absolute/path/to/fba-venv/bin/python \
  --ryupy /absolute/path/to/ryupy
python3 -B tests/general_projects/github_large/build_project.py \
  --project explorerpro --python /absolute/path/to/explorerpro-venv/bin/python \
  --ryupy /absolute/path/to/ryupy
```

最新完整入口结果：fba 的语法目标现已按 provider 精确版本联动，原 PEP 695 语法
已通过；真实首阻塞是 `backend/__init__.py:10/11` 的 `globals` 内建，证据为
`~/.cache/ryupy/validation/github-large-build/fba-20261117T084437.106342Z/`。

ExplorerPro 经通用可选导入、同源多发行 RECORD 共归属、嵌套解包、动态 f-string、
`oct`、mapping `**`、原生组件与 LC_RPATH 解析修复后，完整原入口 JIT 构建已通过：
`~/.cache/ryupy/validation/github-large-build/explorerpro-20261117T095900.504075Z/`。
固定原源码及本地业务 CIL 请求保持，零本地业务委派。

默认完整原生闭包保留实际缺库诊断：iODBC 由官方3.52.16私有构建、PostgreSQL18.3
客户端及OpenSSL/Kerberos完整闭包由现装来源复制仓外，真实ABI通过后继续在Mimer
客户端缺失处停止，证据
`~/.cache/ryupy/validation/github-large-build/explorerpro-20261117T094237.139792Z/`。
客户端官方下载需要邮件链接，本门未提交邮箱、未安装系统、未伪造库。

`explorerpro-native-selection.json` 是明确的Cocoa/SQLite发布能力数据：固定6.9.2
sqldrivers目录完整成员清单，只部署SQLite驱动；未选三插件及其全部加载边仍进入
provider审计。其它插件、源码、资源、metadata与许可证保持部署，默认不启用此选择。

```sh
python3 -B tests/general_projects/github_large/explorerpro_products.py \
  --python /absolute/path/to/explorerpro-venv/bin/python \
  --ryupy /absolute/path/to/ryupy --mode jit \
  --native-components tests/general_projects/github_large/explorerpro-native-selection.json
```

`explorerpro_products.py` 同次运行真正Cocoa CPython oracle，再构建CIL probe，删除全部
源码/venv/组件输入/构建目录后，在中文空格路径、空PATH及污染Python环境执行。完整
stdout/stderr/退出码比较仅归一私有profile和Qt字体别名初始化耗时；双流实时写仓外。
默认使用原隔离业务probe，可通过 `--probe-entry explorerpro_native_smoke.py` 使用
同形空argv的最小Cocoa窗口辅助定位C API现场。

首次真实CIL搬迁运行在 `QApplication([])` 发生SIGSEGV：官方Shiboken6.9.2 helper.cpp
第495行直接把 `PyEval_GetGlobals()` 交给 `PyDict_GetItem`，直接CIL→C扩展调用缺少
Python frame，得到NULL。证据
`~/.cache/ryupy/validation/explorerpro-products/20261117T095900.521068Z/`
包含同次CP成功结果、构建/实际运行命令、完整双流、官方固定源码与原生崩溃栈。
保留空argv和原业务调用形态；通用C API现场修复后继续原业务JIT/AOT/OneFile搬迁门。

### 2026-11-19 固定项目复验

- fba `dd391ee5`：固定 CPython 3.12.13 和原锁定依赖，stock profile 完整原入口 JIT
  仍在 `backend/app/admin/api/v1/auth/auth.py:18` 报 E0100（实例模块异步函数）。
  未执行 RyuPy 业务。构建证据 `github-large-build/fba-20261118T234943.997860Z/`。
- ExplorerPro `f4f65982`：实例 dataclass 已越过。显式 stock profile、原 `src/main.py`、
  明确选择 Cocoa/SQLite 的原生插件闭包，JIT、目录 NativeAOT 和 OneFile 完整构建通过，
  无本地业务委派；构建证据 `github-large-build/explorerpro-20261118T235604.187877Z/`。
  OneFile 证据单独位于 `github-large-build/explorerpro-20261119T000120.321711Z/`。
  这不改变默认闭包缺 Mimer 依赖的旧结论，也不等于运行原入口。
- `--bundle-data` 按 [ADR-0242](../../../docs/adr/0242-declared-provider-application-data.md)
  部署原版本、翻译及图标；两套独立布局的 JIT、目录 AOT、OneFile 资源搬迁 6/6 通过。
  Cocoa 薄驱动另断言原 `applicationVersion`、实际英译和非空 `QIcon`。
- 真 Cocoa CPython 隔离业务通过；stock JIT 和目录 AOT 隔离探针删除源码、venv、构建目录后启动，
  原缓存和原 venv 通过沙箱拒绝读取。产品已加载资源并进入 `window.show()`，但 150 秒未返回；
  样本主线程位于 Qt 显示事件 → 原应用事件过滤器 → CPython/CIL 回调链。尚未确定根因，
  避免绕过原过滤器来记作通过。仓外 `~/.cache/ryupy/validation/explorerpro-products/`
  下的 JIT `20261118T235604.188134Z/` 和目录 AOT `20261119T000149.020378Z/`
  保存完整证据。原生方法候选不能直接替换默认方法桥：原 Qt 信号连接在 Shiboken
  `PepCode_Get` 崩溃，未启用候选。
- 原 translator 与原 1185 项目录可以离开 Qt 重现慢速：热态 15 次非命中翻译在
  stock JIT 用时 4.37 秒，同次 CPython 为约 14 微秒。单次诊断采样显示
  `CPythonHost.PushContainer`／`PullContainer` 在同步字典上反复扫描键；先降低该成本，
  再复验整个窗口，不能把非配对的诊断时点当成发布性能数据。

同步字典现按引用线性比较（[ADR-0243](../../../docs/adr/0243-linear-synchronized-dictionary-deltas.md)）。
四轮配对的 JIT 热态中位数 2.945→0.145 秒。优化后原业务 JIT 两次、目录 NativeAOT
一次在窗口构造报
`AttributeError: 'PySide6.QtWidgets.QWidget' object has no attribute 'addSeparator'`
（`explorerpro-products/` 下的 `20261119T001648.245885Z/`、`20261119T002100.889303Z/`、
`20261119T002341.044731Z/`），不能记为通过。最小
`explorerpro_qt_wrapper_probe.py` 保留待验：只安装事件过滤器然后构造原 Qt 菜单；
同次 CPython 通过，搬迁 JIT 在 `isinstance(bar, QMenuBar)` 明确失败
（`explorerpro-products/20261119T002738.391743Z/`）。独立 CPython 对照中不持有构造回调参数时
五轮均返回 `QMenuBar`，显式保留该参数时
五轮均返回 `QWidget`。后续按通用回调临时引用/逃逸合同定位，不按 Qt 方法名绕过。

本资源与桥接切片 `make check` 通过：3484 项 Runtime、353 项 driver 单测及 514 项
编译端到端通过；既有跳过保留。
