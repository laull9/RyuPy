# 快速上手与部署边界

当前实现为 Phase 1 M1.0–M1.5 的静态子集。动态核心、容器、普通类、异常与模块安排在 Phase 2；高级语法和标准库移植安排在 Phase 3。支持现状见 [semantics.md](semantics.md)，愿景与排期见 [roadmap.md](roadmap.md)。

## 开发环境

| 依赖 | 版本 | 用途 |
| --- | --- | --- |
| Rust | stable，2024 edition | 构建编译器 |
| .NET SDK | 10.x | 构建 C# 运行时与执行测试；后续 AOT 发布 |
| Python | 3.12+ | 当前 CPython 差分测试；M2.0 将固定具体基准版本 |

```bash
make runtime
make check
```

这两条命令构建运行时并执行 fmt、clippy、Rust / C# 测试和已有 e2e。九个 Phase 1 e2e 当前已实现；项目级覆盖率尚未测量。

## 编译、运行与查看 IR

```bash
make run FILE=tests/e2e/phase1/m1_5_dataclass.py
cargo run -q -p ryupy-cli -- build tests/e2e/phase1/m1_0_print_int.py -o out/
dotnet out/m1_0_print_int.dll

make dump FILE=tests/e2e/phase1/m1_0_print_int.py EMIT=ast
make dump FILE=tests/e2e/phase1/m1_0_print_int.py EMIT=hir
make dump FILE=tests/e2e/phase1/m1_0_print_int.py EMIT=types
make dump FILE=tests/e2e/phase1/m1_0_print_int.py EMIT=mir
make dump FILE=tests/e2e/phase1/m1_0_print_int.py EMIT=cil

make e2e
make e2e-all   # 包括未来尚未实现的 pending 用例
```

当前 `import` 仅支持特殊识别的 `from dataclasses import dataclass`；任意第三方库、标准库与多文件项目尚不可直接运行。不要据未来路线图推断现有 CLI 已具备这些能力。

## 当前产物

`build` 默认把产物写在源文件旁的 `__pycache__/<文件名>/`（与 CPython 存放 `foo.cpython-312.pyc`
的 `__pycache__` 同位），也可以用 `-o` 指定目录：

```text
out/
├── m1_0_print_int.dll
├── m1_0_print_int.runtimeconfig.json
└── RyuPy.Runtime.dll
```

构建当前仓库需要 SDK 来生成 `RyuPy.Runtime.dll`。生成的普通程序集通过 .NET Runtime 运行，运行目标机不需要编译器或 Python。编译器自身不调用 CPython 做编译；Python 目前用于差分测试。

### 发行包

`make build` 构建 Release 版 CLI 与 C# 运行时，并在 `releases/` 下打包宿主平台产物：

```text
releases/
├── ryupy-0.0.1-darwin-arm64/
│   ├── bin/ryupy                # Rust 编译器 CLI
│   └── runtime/RyuPy.Runtime.dll
└── ryupy-0.0.1-darwin-arm64.tar.gz
```

CLI 查找运行时的顺序是：`RYUPY_RUNTIME_DIR` → 可执行文件旁边的 `runtime/`（`bin/../runtime`
或同目录）→ 开发期的 `runtime/…/bin/Release/net10.0/`。所以解压后无需设置环境变量即可使用；
目标机仍需要 .NET Runtime（当前目标框架 net10.0），但不需要 Rust 工具链、.NET SDK 或 Python。
包名只反映构建机器的平台，`make build` 不做交叉编译。

## 计划中的分发与执行形态

以下内容尚待实现，特别是 `--aot` 虽已有参数入口，`publish_aot` 仍为占位实现。

| 形态 | 目标 | 构建与运行依赖 | 排期 |
| --- | --- | --- | --- |
| 原生语义的 JIT 程序 | 项目编译成 CIL，通过 RyuJIT 运行 | 目标机 .NET Runtime；不依赖 libpython，但只能调用已原生实现的 API | 现有静态子集可用，项目支持在 Phase 2 / 3 扩展 |
| 桥接 JIT 程序 | 必要模块通过 libpython 调用 | .NET Runtime、兼容的 libpython、Python 标准库与扩展依赖 | M3.8，Phase 4 扩展到 NumPy 等非纯 Python 模块 |
| 原生 AOT 程序 | `ryupy build --aot` 生成目标平台可执行文件 | 构建需 .NET SDK 与目标工具链；运行免装 .NET / Python，仍服从平台系统库依赖 | M5.2，承接旧 M1.6 |
| 桥接 AOT 程序 | AOT 编译托管部分，保留解释器边界 | 构建需 SDK 与目标工具链；运行仍需 libpython / 标准库 / 扩展，单独验收裁剪与原生加载 | M5.2 独立验证 |

CLI 内嵌预构建运行时、全局缓存与跨平台发行仍属于后续分发设计；当前 `make build` 只产出宿主平台的
CLI + 运行时压缩包，没有单文件 CLI 发布。产物可跨哪些系统 / 架构运行，以实测支持矩阵为准。

原生通过、桥接通过和已接受偏差的区别见 [compatibility.md](compatibility.md)。Native AOT 的目标平台、构建与加载限制参考 [Microsoft 文档](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)。

## 后续阶段的 Python 用例

Phase 2–4 用例已经写入 `tests/e2e/` 并标记 pending。`python3 tests/run_python_baseline.py` 自检 Phase 1–3 的 CPython 行为；安装 NumPy 后加 `--phase phase4` 检查扩展基线。辅助包与预期错误退出的运行方式见 [测试说明](../tests/README.md)。这些基线通过不表示 RyuPy 已支持相应功能。
