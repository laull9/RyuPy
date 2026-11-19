# ADR-0204：显式本地整模块与入口 CPython 委派（路线 A）

- 日期：2026-11-16
- 状态：已实施（macOS arm64 完整 `make check`、双向委派合同及真实 FastAPI 三种产品搬迁验收通过）
- 关联：`docs/general-project-compatibility-gaps.md` 路线 A、`docs/cpython-provider.md`、[ADR-0181](0181-cpython-provider-first.md)、[ADR-0190](0190-project-cli.md)

## 背景

现代异步框架（如 FastAPI、SQLAlchemy AsyncSession、Uvicorn、aiocache）以及依赖源码级反射与依赖注入的框架（`inspect.signature` / `inspect.getsource` / `sys._getframe`）在 RyuPy 当前原生前端与状态机上仍受阻于 `async/await`、`async with` 与框架元编程协议。`docs/general-project-compatibility-gaps.md` 提出了短期高 ROI 的路线 A：**显式本地整模块/入口 CPython 委派**，让含异步或反射的本地模块与入口脚本直接随包进入分发产物，由随包 libpython 宿主解释执行，同时保持其余编译模块继续走 CIL / NativeAOT。

## 决定

### 1. 显式 CLI 委派开关（禁止静默回退）

新增三个全局 CLI 参数（均与 `--native` 互斥）：

1. `--cpython-module <MODULE>`（可重复）：显式将本地顶层模块或整包（例如 `databases`、`routes`、`main`）委派给随包 CPython 执行。
   - 仅接受合法 ASCII 顶层标识符，拒绝 `__main__`（应使用 `--cpython-entry`）与带点子模块名（整包委派，避免同包共享状态拆分）。
   - 必须在入口目录或 `-I` 搜索路径中真实存在；缺失即构建失败。
   - 禁止与 `--include-module`、`--instance-module`、标准库或第三方包冲突。
2. `--cpython-entry`（布尔开关）：显式将应用入口 `__main__` 委派给随包 CPython 执行。
   - 入口文件不经过 HIR/ty/MIR 降级，避免 `async/await` 等语法阻断；入口文件依然参与依赖闭包扫描与静态导入发现，其未列入 `--cpython-module` 的本地依赖仍按原规则由 RyuPy 编译（若含未支持语法则报诊断，不静默回退）。
3. `--cpython-dynamic-module <MODULE>`（可重复）：显式补充运行期按字符串或条件导入的第三方 CPython 依赖（例如 `sqlite+aiosqlite://` 驱动 `aiosqlite` 或 `TestClient` 依赖的 `httpx`），进入依赖闭包收集，不猜全量 `site-packages`。

默认构建与未声明委派的模块/入口严格维持原有行为，绝不因前端诊断失败而自动静默切换为 CPython 执行；在旧 `build`/`run` 子命令中若未启用 `--cpython-provider`，传入委派参数将直接报错拒绝。

### 2. 分发布局与配置契约

- 显式委派的本地模块/包复制到分发目录 `python/ryupy_delegated/`（跳过 `__pycache__`、`.pyc`/`.pyo`、`.DS_Store`、`Thumbs.db`，解引用包内安全符号链接并拒绝越界链接与目录环），并在 `ryupy-cpython.json` 的 `search_paths` 首位写入 `"python/ryupy_delegated"`。
- 显式委派的入口源码复制到独立目录 `python/ryupy_entry/<入口文件名>`（不放入 `sys.path`，防止误遮蔽同名模块），并在 `ryupy-cpython.json` 写入可选字段 `"entry_script": "python/ryupy_entry/<入口文件名>"`。
- `cpython-provider.json` 明确区分执行域：
  - 普通编译入口：`"execution": "ryupy_compiled_entry"`、`"entry_provider": "ryupy_compiled_source"`；
  - 委派入口：`"execution": "cpython_delegated_entry"`、`"entry_provider": "cpython_delegated_source"`，并记录 `"delegated_local_modules"`、`"dynamic_modules"` 与 `"entry_script"`；被委派本地模块在 `"modules"` 中的 reason 标为 `"explicit_local_delegation"`。

### 3. 独立 CPython 入口执行接口

- `RyuPy.CPython` 读取 `ryupy-cpython.json` 的 `entry_script`（通过 `ResolveValue` 校验相对路径与符号链接边界）。
- `CPythonHost.Owns("__main__")` 在 `entryScript != null` 时返回 `true`，`PyModules.Register("__main__", ...)` 不会将其加入 `_RyuPyFinder` 的编译模块列表。
- 入口包装器仍按既定顺序调用 `PySystem.Initialize`、`Bootstrap.Start`、`__init_functions`、`PyModules.RunMain()` 与 `Bootstrap.Finish`；当 `PyModules.RunMain()` 导入 `__main__` 时，`CPythonHost.Import("__main__")` 首次触发 C shim `rp_run_entry(path, source)`：
  - 在真实 `sys.modules["__main__"]` 字典中绑定 `__file__`（随包真实源码路径，支持 `inspect`/`linecache`/traceback）、`__cached__ = None`、`__package__ = None`、`__builtins__`；
  - 使用 `Py_CompileStringObject` + `PyEval_EvalCode` 执行源码，保留同线程重入 `import __main__` 不重复执行语义。
  - `SystemExit` 与未捕获异常通过 `ThrowError()` 转换为 `PySystemExit` / `PyBaseException`，由统一入口包装器渲染 `[CPython 解释栈]` 并按相同退出码与 `atexit` 关闭流程退出。

## 取舍与边界

1. 入口仍保留瘦原生启动器（`RyuPy.Generated.Module.Main`），使 JIT、`standalone`、`--onefile`、manifest 清单、流共享、`atexit` 关闭与双向跨模块导入（委派入口/模块导入已编译 RyuPy 模块，或编译模块导入委派本地模块）复用同一套运行时与发布管线，无需改造 `ryupy-mir` 或 `ryupy-codegen-cil`。
2. 当前 `--cpython-module` 限定顶层模块/包名（与 `ryupy-cpython.json` 的 `modules` 合同一致），防止同一包内 `__init__.py` 与子模块分属不同运行时导致 `__path__` 与包状态分裂。
3. 动态第三方插件（如 SQLAlchemy 方言驱动 `aiosqlite` 或可选测试客户端 `httpx`）需通过 `--cpython-dynamic-module` 或显式 provider 清单声明，不做全环境无边界搬运。
