# 快速上手与部署

## 发布普通Python项目

RyuPy CLI本身为单一文件。把`ryupy`（Windows为`ryupy.exe`）放到PATH，在已安装好业务依赖的项目中运行：

```sh
ryupy main.py
# 同一默认形态的显式拼写
ryupy main.py --onefile
```

编译器自动选择项目venv、激活环境或PATH中的Python，核验完整版本、机器架构及ABI，
再收集静态导入的本地模块、已安装的第三方依赖、metadata、资源与原生库。
当前自动环境范围为正式CPython 3.9–3.15常规GIL。构建工具在私有缓存准备，项目只保留自身依赖。

输出为`./dist/main.onefile/main`，Windows为`main.exe`。只需搬走这个文件即可交付；
启动时使用独占临时目录，正常退出和已覆盖的中断后清理。

```sh
ryupy main.py --mode standalone       # 完整独立目录 ./dist/main.dist
ryupy main.py --mode jit              # 托管程序集
ryupy main.py --native                # 显式纯RyuPy执行域
ryupy main.py -o release              # 选择输出根
ryupy main.py -I src                  # 补充源码搜索根
ryupy main.py --include-module plugin # 补充动态引用的本地模块
```

静态导入会自动跟随本地文件和包，包内相对导入保持原来的初始化顺序。字符串动态导入、
自定义导入器及应用自己的任意外部数据按具体声明和兼容合同处理；包数据随第三方安装RECORD收集。
[多文件验收](../tests/cpython/multifile_README.md)包含五个本地CIL模块、深层第三方依赖和中文资源，
实际使用仓库外的唯一CLI构建，再删除CLI、私有缓存、项目源码、venv和所有中间产物后运行AOT，
严格比较同次CPython三流及业务副作用。

## 自动探测构建环境

CLI为Rust原生程序，内嵌C# Runtime、CPython互操作桥、SQLite依赖和许可。默认从自身字节在私有
缓存准备完整模块组，使用时无需源码仓库、旁置runtime目录或手写provider JSON。

- 版本、帮助和许可证查询直接运行，不需要.NET或Python。
- 纯原生JIT编译由Rust完成，运行托管产物需要.NET Runtime 10。
- NativeAOT发布需要.NET SDK 10及以上和平台C工具链。
- 自动准备可搬迁Python发行需要curl、tar及Zstandard支持。

.NET按显式DOTNET、PATH、架构对应DOTNET_ROOT和系统安装位置发现；构建前查询本次操作所需能力。
Python环境一旦选定，完整版本、依赖和ABI错误直接给出诊断。安装环境的操作不修改用户venv。

```sh
ryupy --help
ryupy --help-advanced
ryupy --licenses
```

`main.build`、`main.dist`和`main.onefile.build`是构建或诊断目录，真正的单文件交付位于`main.onefile`。
工具核对已拥有的发布输出；完整新候选成功后才替换旧版，失败保留旧版和诊断。

## 从源码构建CLI

| 依赖 | 版本 | 用途 |
| --- | --- | --- |
| Rust | stable，2024 edition，MSRV 1.85 | 编译原生CLI |
| .NET SDK | 10.x | 在OUT_DIR隔离构建并内嵌C#闭包 |
| Python | 3.12.13 | 仓库固定差分基准 |

```sh
cargo build -p ryupy-cli
make check
make build
```

Cargo构建脚本保留静态模块注册表生成，同时在独立.NET artifacts目录构建完整运行时闭包。
`make build`生成Release版唯一CLI，分发归档里仅有该文件。可以覆盖`RELEASE_DIR`将交付物写到仓库外。
Windows编译器静态链接C运行时。各平台实际结果见[平台验收](cpython-platform-support.md)和
[版本矩阵](../tests/cpython/version_README.md)。

## 开发入口与IR

旧子命令保留既有默认值，`build/run`默认使用纯原生执行域，适合语义开发和历史性能复验：

```sh
ryupy build main.py -o out
# 运行生成的托管产物
dotnet out/main.dll
ryupy run main.py
ryupy probe main.py
ryupy dump main.py --emit hir

make dump FILE=tests/e2e/phase1/m1_0_print_int.py EMIT=cil
make e2e
make bench
```

原生执行域、CPython对象桥和各标准库API的实际覆盖分别登记在[兼容性清单](compatibility.md)、
[语义规范](semantics.md)和[CPython provider合同](cpython-provider.md)。历史Decimal/tzinfo专项与
固定项目结果保留原入口和日期，最新发布体验见[ADR-0190](adr/0190-project-cli.md)与
[ADR-0191](adr/0191-single-file-compiler-cli.md)。
