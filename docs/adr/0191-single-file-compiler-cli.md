# ADR-0191：单文件编译器内嵌 C# 闭包与环境发现

- 日期：2026-11-14
- 状态：核心实现及本机定向检查通过；独立验收与三平台复验中。

## 用户交付合同

RyuPy编译器本身交付为唯一的 `ryupy` / `ryupy.exe`。移动该文件即可使用，不需要源码仓库、
旁置runtime目录、C#源码或shim脚本。常用入口为 `ryupy main.py --onefile`，默认选择项目
Python环境，自动收齐依赖，生成可以单独搬走运行的AOT文件。

CLI自身是Rust原生程序。帮助、版本和许可证查询无需.NET或Python；纯原生JIT编译只需
内嵌组件。执行托管产物需要.NET Runtime 10，原生发布需要.NET SDK 10及以上和平台C工具链。
按本次操作自动检测所需条件，缺失时在构建前给出具体诊断。

## 同次构建的完整内嵌闭包

Cargo构建脚本在OUT_DIR的独立.NET artifacts目录编译AnyCPU组件，按固定清单内嵌：

- RyuPy.Runtime、RyuPy.CPython及四个SQLite托管依赖；
- RuntimeDependencies.props和完整第三方许可文本；
- 当前目标RID对应的SQLite原生库。

源程序集通过include_bytes进入编译器，C shim及构建脚本通过include_str进入编译器。
不提交生成程序集或索引，原有静态模块注册表生成保留。全新OUT_DIR实跑已经验证两类索引
均会生成。构建机需要.NET SDK；使用已发布的CLI不需要重新构建这些C#组件。

内嵌模块组以一次构建的版本/RID/时间标识区分缓存，普通文件不生成摘要。运行时在用户私有
缓存准备完整候选，以跨平台内核文件锁串行提交，再原子安装到对应模块组目录。中断释放锁，
残缺缓存保留到隔离目录后重建。文件类型、数量及长度来自编译器中的固定表，不载入旁置DLL。
锁采用fs2以保持Rust 1.85的构建基线；不为新标准库API扩大既有源代码改写范围。

目标应用按实际执行域部署组件：纯原生应用复制Runtime与SQLite闭包；启用CPython时才复制
互操作桥。shim按实际完整Python版本在独占临时目录构建，计划最后一个所有者释放时清理；
编译失败保留源码和双流诊断。Windows直接调用PowerShell，不经过cmd的元字符解释。

## 环境发现

.NET按显式DOTNET、PATH、架构对应DOTNET_ROOT、用户及系统安装位置发现。
原生发布先查询实际SDK列表；托管运行先查询Runtime列表。真实.NET SDK在空PATH、
仅设置DOTNET_ROOT的子进程验收已经通过。

Python按项目venv、激活环境、PATH和Windows py启动器发现。PATH以目录顺序为先，同目录
再选择命令别名，确保Windows前置venv的python.exe优先于系统python3.exe。完整版本、ABI、
架构和常规GIL检查沿ADR-0189；运行目标不再探测或借用构建机环境。

## 验收

Gemini独立验收已在macOS arm64只复制一个CLI文件到仓库外中文空格路径，使用全新私有缓存，验证无.NET
条件下帮助/版本/许可证与原生JIT编译，再构建五个本地模块的普通项目。删除CLI、私有缓存、
项目源码、venv、wheel和中间目录后，运行唯一AOT，严格比较同次CPython三流及文件副作用。
三平台运行同一驱动，Linux额外使用无Python/.NET/SDK的断网容器。结果以实际完成状态登记。

主代理Release版（约13MiB）重跑同一完整流程退出0，otool仅显示macOS系统库。
完整make check再次exit0：3113项C#通过、2跳过，Rust全套与467项编译e2e通过、283 ignored。
Windows编译器通过crt-static静态链接C运行时；三平台Release产物均由真实验收后上传唯一CLI文件。
