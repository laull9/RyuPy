# importlib.resources.as_file 文件系统竖切

`as_file(files(pkg).joinpath(...))` 现在返回原生生成器上下文，进入得到原 Path
对象，退出保留资源并传播原异常。目录及不存在路径同样可进入，实际读取仍走 IO。

实现集中在 `PyImportlibResources.cs`、独立 `PyResourcePathContext.cs`；公开
上下文状态/方法放在 `PyGeneratorContextAttributes.cs`，生命周期继续使用
`PyContextManager.cs`。架构取舍见
[ADR-0170](../adr/0170-filesystem-as-file-context.md)。driver/binding/resources
组合构建实现及公共文档由主代理后续整合；下文记录初始子任务结果。
评审修订的最终验收见[上下文协议证据](../validation/aot-context-protocol-review/README.md)。

## 复现

仓库根执行，Cargo 使用当前 worktree 的 `target`：

```bash
export CARGO_TARGET_DIR="$PWD/target"
export RUSTC_WRAPPER=
export PYTHON="$(uv python find 3.12.13)"
make runtime
dotnet test runtime/RyuPy.Runtime.Tests -nologo -v q --filter 'FullyQualifiedName~PyResourcesAsFileTests|FullyQualifiedName~PyContextManagerTests|FullyQualifiedName~PyImportlibResourcesTests'
E2E_PYTHON_ONLY=1 cargo test -p ryupy-e2e -- resources_as_file --include-ignored --test-threads=1
cargo test -p ryupy-e2e -- resources_as_file --test-threads=1
cargo build -p ryupy-cli
"$PYTHON" -B tests/aot/resources_as_file.py --ryupy target/debug/ryupy --out target/aot-as-file
mkdir -p target/check-tmp
TMPDIR="$PWD/target/check-tmp" RYUPY_FOREACH_PYTHON="$PYTHON" make check
```

Native runner 原流、退出码与结果保存在打印的唯一目录下 `logs/` 和 `result.json`。
不进行普通文件摘要校验。runner 删除其独占的源码、JIT 及 AOT 原目录后换 cwd
执行搬迁二进制，保留 bundle 和原流；删除数据负例会使保留 bundle 缺少 data.txt。
重跑请调用 runner 产生新目录。

## 初始子任务结果

- `make runtime`：通过。
- C#：新增 7 项行为及已有资源/上下文 19 项，26/26 通过；另增两项精确 ABI 合同。
- CPython 3.12.13：4/4 自检通过，包含两个尚未实现域的 CP 正期望。
- JIT：Path 与真实包资源两个差分通过并转正；Traversable 与 zip 两项 pending。
- NativeAOT：macOS arm64 真实发布与搬迁通过；源码、托管产物和原发布不可用，
  新 cwd 下 CP/JIT/Native 双流及退出码相等，缺资源直接读取负例 exit 1 / FileNotFoundError。
  原流归档：[Native 证据](../validation/resources-as-file-native/README.md)。
  最终证据：`target/aot-as-file/resources-as-file-53063lqr/`。
- 完整 `make check`：exit 0；fmt/严格 clippy/Rust workspace 通过，C# 3009 通过、2 跳过，
  固定 CP 基线 720、自检 723、编译 e2e 443 通过、284 ignored。
  macOS 使用本 worktree 普通临时目录，旧 ForEach 专用 Python 显式固定 3.12.13；
  原有四项 frame 证明实验沿 Makefile 保留在独立门。

本轮在已安装 SDK 的宿主机执行，无 SDK 目标环境验收仍待完成。

## 已由主代理整合的公共条目

1. TODO：新增已完成的 `importlib.resources.as_file` 文件系统 Path 竖切；范围含
   身份、目录/缺失路径、单次状态、异常传播、公开状态/绑定、真实显式资源 Native 搬迁。
   zip/custom Traversable 继续列为待办。
2. roadmap：在 AOT 显式包资源闭环下登记此 API 已实现及 macOS arm64 宿主搬迁证据；
   C ABI、binding/resources 组合部署、多平台和无 SDK 目标环境沿主任务各自验收。
3. stdlib：在 importlib.resources 的 API 清单增加
   `as_file(Path) → contextlib 生成器上下文`，Path/注册包两项 CP/JIT 通过；
   自定义 Traversable、zip 解包及 singledispatch 注册/反射保留未实现。
4. semantics §4：登记 Path 分支无存在性检查，目录/缺失路径原样进入，退出不删除、
   不抑制异常；重复进入 AttributeError，正常/重复退出 False；公开 args/kwds/func
   首次进入删除，gen/实例字典为实际可写状态，方法保存 self/共享函数身份，相等/hash及字典键语义也按这两项身份。
   Path 之外具有 is_dir 的资源载体明确 NotImplementedError，int/str/None 保留
   缺少 is_dir 的 AttributeError。此项是有界实现合同，无新增已接受的 CP 偏差。

不改固定项目通过数；本轮未运行第三方项目完整验收、跨平台或性能门。

评审后补齐上下文协议：with 的 HIR ContextMethod 与 ContextDecorator 共用
PyOps.ContextMethod，按类型绑定并固定 enter/exit。实例覆盖仅影响显式属性调用。
装饰器每次读取公开 `_recreate_cm`，保留替换函数的副作用、异常及返回上下文。
新增 `tests/cases/resources_as_file/protocol` 差分，详见 ADR0170 的评审修订。
