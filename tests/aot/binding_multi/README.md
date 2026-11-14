# 单库多导出 C ABI 验收

此目录覆盖公开 `--native-binding` 的版本 2 声明：同一库、同一 Python 模块、1–64 个
固定 `cdecl_i32_i32_i64` 函数。设计与成本见 [ADR-0169](../../../docs/adr/0169-cabi-multiple-exports.md)。

```json
{
  "schema_version": 2,
  "module": "native_math",
  "library": "multi_c",
  "exports": [
    {"name": "add", "symbol": "demo_add", "signature": "cdecl_i32_i32_i64"},
    {"name": "subtract", "symbol": "demo_subtract", "signature": "cdecl_i32_i32_i64"},
    {"name": "add_alias", "symbol": "demo_add", "signature": "cdecl_i32_i32_i64"}
  ],
  "target": {"rid": "osx-arm64", "shared": "libmulti_c.dylib", "archive": "multi.a"}
}
```

v1 单导出声明保持可用。v2 逐项拒绝未知字段、重复公开名、保留元数据名、错误签名、
缺少必需项、空导出及 65 项以上配置；与 v1 单导出字段混用也会失败。
原生输入的路径与格式合同沿用[构建合同](../cabi_probe/PYTHON_BINDING_BUILD.md)。

## 运行

```sh
export CARGO_TARGET_DIR="$PWD/target"
export RUSTC_WRAPPER=
mkdir -p target/cabi-multi-evidence/tmp
export TMPDIR="$PWD/target/cabi-multi-evidence/tmp"
export RYUPY_FOREACH_PYTHON="$(uv python find "$(cat tests/pinned-python.txt)")"
make runtime
cargo build -p ryupy-cli
cargo test -p ryupy-driver --lib binding
cargo test -p ryupy-codegen-cil fixed_binding
PYTHON_BIN=$(uv python find "$(cat tests/pinned-python.txt)")
"$PYTHON_BIN" -B tests/aot/binding_multi/run.py \
  --ryupy target/debug/ryupy --out target/cabi-multi-evidence
make check
```

runner 在 `--out` 内创建独占工作目录，保留 C 输入、两次 SDK 构建现场、原始 stdout/stderr、
`result.json` 及搬迁产物。成功时 `status` 为 `passed`；预期错误步骤的退出 1 也记录在内。
不需要浏览器或常驻服务，不对普通文件做额外 hash 核验。

C 源为自有小库，参考进程通过 ctypes 调用同一份 shared，执行原样应用。
参考 wrapper 显式实现 RyuPy 的严格实际整数边界，Python 调用/身份/求值顺序由 CPython 执行。
该对照的行为覆盖范围如下：

- 三个公开函数、两个实际符号；signed32 极值计算得到超出 signed32 的正确 i64 结果。
- 一等函数、保存别名、不同名字的同符号函数身份、模块属性重绑与求参期间重绑。
- 每个导出的类型、范围、参数数量和位置专用参数错误；末尾 TypeError 保留调用帧。
- v2 schema 4 的三个独立 wrapper/全局槽、单份 shared 输入和产物、两个精确静态符号、单份 archive。
- 原输入和 SDK/build 原路径失效后，JIT 整包及 AOT dist 在另一 cwd 正常运行；AOT 运行目录无 shared/archive/托管 DLL。
- 第二个符号缺失时，首函数和同符号别名仍可调用；整库缺失、坏映像均抛 ImportError。
- 缺库时导入保持惰性；设置指向正确 shared 的搜索变量、在 runtimeconfig 中关闭动态代码能力，包内路径缺失仍失败。macOS 的 DYLD 变量可见性没有另行探测。
- 真实 AOT 链接器拒绝缺少第二个符号的 archive；失败 attempt 保留诊断且无成功清单，先前成功 dist 继续运行。

## 本次记录（2026-11-13）

实现前新增的声明测试在旧 parser 上确实失败（`multi_export_declaration_accepts_one_library_and_distinct_functions`）。
应用先以 `# ryupy: pending` 建立，真实双模式成功后移除标记。
首轮真实 JIT 构建因模块报告按名排序、声明顺序未排序而失败；修复预期集合排序后，
下一轮 JIT / AOT、搬迁、错误隔离及真实静态链接失败全部通过。

实机：macOS 27.0 arm64，.NET SDK 10.0.403 / runtime 10.0.14，CPython 3.12.13。
C fixture 使用 `-mmacosx-version-min=12.0`，跟随已有自有 fixture 的部署目标。
当前新增功能的 Linux/Windows runner 分支尚待对应实机执行，macOS 最低运行版本也待单独验证。

最终真实验收：`target/cabi-multi-evidence/multi-9qrczlu0/result.json` 为 passed，
19 个主流程步骤完成（包含预期失败步骤）；双模式成功输出直接与参考原字节比较。
`real-run-final.log` 保存主流程终态，原始双流位于该独占目录的 `logs/`。
复核发现最初动态代码环境变量未命中 .NET 的配置合同，已改为 runtimeconfig 的
`System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported=false`，仅重跑相关
惰性导入/缺库两步骤（`jit-dynamicfalse-lazy/load`）并追加原流，结果保持预期。
最终 `result.json` 共 21 步，包含此修正的配置来源，后续 runner 直接使用正确配置。

完整 `make check` 首轮在旧 driver 测试停止：18 项受宿主 `/var` 祖先链接影响，
另 1 项旧 ForEach 专项默认启动的 `python3` 未命中固定 3.12.13。
上方命令用本 worktree 的普通临时目录和既有 `RYUPY_FOREACH_PYTHON` 配置恢复测试环境，
保留首轮 `make-check-1.log`，没有修改这些测试或断言。
最终 `make-check-2.log` 的完整门禁 exit 0：fmt/clippy/Rust workspace 全通过，
C# 3000 passed / 2 skipped、固定 CPython 基线 716 passed，
Python-only 目录自检 719 passed，编译端到端 441 passed / 282 ignored。
C ABI driver 17 项、codegen PE 10 项专项通过；`make runtime` Release 零警告、零错误。
资源组合、一般 ABI 和库依赖闭包仍待后续竖切。
