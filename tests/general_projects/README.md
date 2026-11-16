# 通用项目行为验收

本目录对应 `docs/general-project-compatibility-gaps.md` 的两个真实项目。
`tests/fixtures/projects/` 保留原业务输入；驱动复制到仓库外，排除已有数据库、
虚拟环境与字节码，每个执行臂使用独立数据，再对照同次 CPython 结果。
项目输入需预先准备；也可通过 `--fixtures /absolute/path/to/projects` 指定仓库外目录，
其中分别包含 `fastapi-restful/` 与 `SanBudget/`。本次既有项目输入保持原样，独立于实现提交。

## CPython 前置合同

FastAPI 使用 CPython 3.13.3，依赖冻结在 `requirements.txt`。用仓库外 venv 安装后运行：

```bash
uv venv --python 3.13.3 /tmp/ryupy-general-venv
uv pip install --python /tmp/ryupy-general-venv/bin/python -r tests/general_projects/requirements.txt
python3 -B tests/general_projects/oracle.py \
  --fastapi-python /tmp/ryupy-general-venv/bin/python \
  --tk-python /absolute/path/to/python3.12
```

SanBudget 要求真实桌面会话及 Tk；在 macOS arm64 使用 CPython 3.12.13 验证。
`oracle.py` 退出时删除项目副本和数据库，原项目源码及已有数据库保持原样。

- FastAPI：原应用路由、OpenAPI、Pydantic 校验、Depends 注入、异步上下文会话、
  SQLite 新增/读取/修改/删除、缓存 MISS/HIT 和错误状态码；通过 ASGI TestClient 发请求。
- SanBudget：真实 Tk 窗口与平台事件处理、原按钮命令回调、表单保存/编辑、
  Treeview 与 SQLite 数据同步，最后关闭连接并销毁窗口。

## 本轮状态（2026-11-16）

上述两份 CPython 合同已在 macOS arm64 执行通过。FastAPI 保留上游
Starlette 对 httpx TestClient 的弃用警告，后续产品差分须检查该 stderr。
项目原 venv 缺少 greenlet，独立验收环境显式安装固定版本 3.5.6。

当前 Release 编译器重现 FastAPI 的 `async with` E0100，以及 SanBudget 的
CPython 基类继承 NotImplementedError。JIT 基线的 net10.0 程序在本机
.NET 11 下以 `DOTNET_ROLL_FORWARD=Major` 执行。

产品验收已在 macOS arm64 完成：FastAPI 的 JIT/standalone/onefile，以及 SanBudget
原业务编译路径的 JIT/standalone/onefile 均通过。每个模式在删除私有源码、venv 和
构建目录后，从中文/空格搬迁目录执行；运行 PATH 为空，Python 环境变量被污染。
SanBudget 使用真实 macOS 窗口；FastAPI 通过 ASGI TestClient 验证应用行为。
Uvicorn TCP 部署、GUI 文件对话框/CSV 导出及其它平台尚未在本轮运行。
onefile 的 `__file__` 指向解包树，邻接数据库随解包目录清理；本轮验收单进程行为。
需要跨次启动保存的业务数据应由应用选择用户数据目录，目录发布可保留邻接数据库。

通用回归还覆盖混合编译/委派双向导入、源码内省、atexit、退出码/异常、缺失入口、
动态依赖、资源/metadata/NumPy 搬迁与缺失依赖、版本冲突、RECORD 越界、wheel ABI
负例。`str.title` 以固定 CPython 验证 Unicode、保存方法、根描述符和错误参数。
布局/旁件、路径解析与代码生成测试进入完整 make check。

`run.py` 在实现合流后运行三种产品，并删除私有源码、venv 和构建目录再搬迁执行：

```bash
python3 -B tests/general_projects/run.py --ryupy /absolute/path/to/ryupy \
  --python /tmp/ryupy-general-venv/bin/python --project fastapi
python3 -B tests/general_projects/run.py --ryupy /absolute/path/to/ryupy \
  --python /absolute/path/to/python3.12 --project sanbudget --compiled-app
```

默认覆盖 JIT、standalone、onefile；可用重复 `--mode` 限定本次验证。
SanBudget 的 `--compiled-app` 保持原业务类和探针走 CIL，验证真实跨域类桥；
省略该参数则验证显式源码委派交付。FastAPI 显式委派六个本地模块族，
并声明 aiosqlite/httpx/greenlet 动态依赖。stderr 仅归一化已知上游警告的安装根，
警告正文与其它全部输出均参加同次差分。


## 最终门禁

2026-11-16 在 .NET SDK/Runtime 11.0.100/11.0.0、Rust 当前工具链及固定
CPython 3.12.13 下执行 `make check` 成功：3349 项 C# 通过，3 项既有跳过；
编译 e2e 482 通过、291 ignored；Python-only 用例自检 769 通过，Rust 全套和
fmt/clippy 均通过。CPython 类桥程序集另行运行 5 项真实解释器测试，全部通过。
两真实项目的六种产品在最终 CLI 上再次完成搬迁执行，测试副本按驱动自动清理。
