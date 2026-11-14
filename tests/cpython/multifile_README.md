# 普通多文件项目直接发布

RyuPy 发布为单一文件 CLI 工具，C# 运行时程序集、CPython 托管桥、原生 SQLite 依赖、第三方许可证及 shim 源码全部内嵌。普通多文件项目仅需提供入口文件与交付形态：

```sh
ryupy main.py --onefile
ryupy main.py --mode standalone
ryupy main.py --native
```

省略发布选项时默认 onefile；默认输出为 `./dist/main.onefile/main`（Windows 为 `main.exe`）。
项目 `.venv`、已激活环境和 PATH 按优先级自动探测；本地静态导入自动进入编译图，已安装的第三方依赖、metadata 和包资源自动收集。构建所需的辅助工具在私有缓存准备，不修改项目 venv，不注入额外依赖。`-o` 指定输出根，`-I` 补充源码搜索根，`--include-module` 补充动态导入的本地模块。旧 build/run/probe/dump 继续保留开发入口，实验参数通过 `--help-advanced` 查询。

## 单文件 CLI 与多文件验收

驱动文件 `tests/cpython/multifile_run.py` 严格遵守 ADR-0182 与 ADR-0190 规范：

1. **唯一 CLI 隔离构建**：仅将 `ryupy` 单文件本身复制到含中文、空格与特殊字符的仓库外目录，不复制任何 runtime、shim、源码或旁置 DLL。
2. **私有全新冷缓存与免环境探测**：将 CLI 自身运行时解包缓存通过 `XDG_CACHE_HOME`（Windows 为 `LOCALAPPDATA`）重定向至 `inputs` 内部全新目录，证明不复用开发机既有缓存。在空 PATH 且无缓存路径下验证 `--version`、`--help` 与 `--licenses` 均可直接运行，不依赖缓存、.NET、Python 或源码仓库。
3. **四路并发 JIT 冷缓存互斥锁**：在空 PATH 与冷缓存下同时启动四个独立输出的 `--native --mode jit` 构建进程，验证基于文件锁的并发解包与按需部署；各输出目录均包含 `RyuPy.Runtime.dll` 及 SQLite 原生依赖，且明确不包含 `RyuPy.CPython.dll` 桥。
4. **真实多文件与第三方依赖**：使用 `fixtures/multifile_app` 五个手写源码文件（入口、包初始化、业务服务、计价和标签模块），形成跨层相对导入调用链；计价模块引入 `tomli`，标签模块调用第三方包读取打包中文资源。
5. **用户环境纯净性**：测试仅安装业务依赖，不预装 `packaging`；构建前后断言用户项目 venv 均无 `packaging` 注入。
6. **同次 CPython Oracle 比较**：同次同解释器先行运行基准，捕获 stdout、stderr、退出码及生成的 `收据.txt` 字节流。
7. **产物精确审计**：验证 `.onefile` 目录有且仅有唯一可执行文件；清单审计确认 5 个本地模块完整进入 CIL，第三方包及资源随包打包。
8. **删除全部输入（含缓存）与搬迁运行**：将唯一 AOT 可执行档搬迁至独立目录后，物理删除包含复制 CLI、CLI 私有缓存、项目源码、venv、wheel 及全部构建中间产物的输入根目录。
9. **三流与副作用逐字节一致**：在空 PATH、污染 Python 环境变量及独立 cwd 下执行搬迁后的唯一二进制，退出码、stdout、stderr 与 `收据.txt` 与 CPython Oracle 逐字节一致，并验证独占临时解包目录在退出后自动清空。
10. **Linux 断网干净容器**：在 Linux 下额外调用无 Python、无 .NET、无 SDK 的 `runtime-deps:10.0-noble` 容器进行断网离线验证。

## 运行命令

```sh
python3 -B tests/cpython/multifile_run.py --python /path/to/python
# Linux 额外执行断网干净目标容器
python3 -B tests/cpython/multifile_run.py --linux-container mcr.microsoft.com/dotnet/runtime-deps:10.0-noble
```

macOS arm64 本机完整通过，三平台 CI 执行同一驱动。普通源码模块的编译范围和 Python 语义按 [兼容性清单](../../docs/compatibility.md) 登记，CLI 设计参见 [ADR-0190](../../docs/adr/0190-project-cli.md)。
