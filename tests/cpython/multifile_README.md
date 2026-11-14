# 普通多文件项目直接发布

常用命令只表达入口与交付形态：

```sh
ryupy main.py --onefile
ryupy main.py --mode standalone
ryupy main.py --native
```

省略发布选项时默认onefile；默认输出为`./dist/main.onefile/main`，Windows为`main.exe`。
项目`.venv`、已激活环境和PATH按顺序探测；本地静态导入自动进入编译图，已安装的第三方依赖、
metadata和包资源自动收集。构建工具在RyuPy私有缓存准备，用户不用安装指定版本的packaging。
`-o`指定输出根，`-I`补充源码搜索根，`--include-module`补充动态导入的本地模块。
旧build/run/probe/dump继续保留开发入口，实验参数通过`--help-advanced`查询。

手写输入位于`fixtures/multifile_app`，入口与四个包内文件形成真实调用链；计价模块独立导入tomli，
标签模块调用第三方包读取中文资源。驱动以普通CLI构建，检查清单中的五个CIL模块，随后搬走
唯一二进制，物理删除整个构建项目后运行。三流、退出码、中文文件副作用与同次CPython逐字节比较，
并检查临时解包目录清理。项目venv构建前后均不含packaging。

```sh
python3 -B tests/cpython/multifile_run.py --python /path/to/python
# Linux额外执行断网干净目标
python3 -B tests/cpython/multifile_run.py --linux-container mcr.microsoft.com/dotnet/runtime-deps:10.0-noble
```

macOS arm64本机完整通过，三平台CI执行同一驱动。普通源码模块的编译范围和Python语义仍按
[兼容性清单](../../docs/compatibility.md)登记。CLI决策见[ADR-0190](../../docs/adr/0190-project-cli.md)。
