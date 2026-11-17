# 固定 C ABI 的真正 Python 接口导出

固定绑定产品从同次已验证 `BindingPlan` 自动生成两项产物：

- `_internal/interfaces/v1/<模块>.pyi`：`int/float/bool/str/bytes/None`，所有参数均为必填位置专用参数。保留字导出通过模块 `__getattr__` 的 `Literal`/`Callable` 签名提供；保留字参数使用无冲突位置别名。
- `_internal/interfaces/v1/<模块>.abi.json`：声明版本、目标 RID、绑定 ID、原始导出/参数名称、原始机器声明、同符号别名、真实 PInvoke 报告引用、整数范围、严格转换、缓冲展开和 `borrowed_call` 生命周期。

类型检查器搜索根为**搬迁后的产品根目录下 `_internal/interfaces/v1`**，例如 `MYPYPATH=/absolute/relocated-product/_internal/interfaces/v1 mypy consumer.py`；Pyright 可将同一绝对目录加入 `extraPaths`。机器合同继续约束实际调用；Python 注解可参与静态分析、ABI/布局/所有权推导，运行时仍执行精确范围和协议检查。

本门有两套独立声明：旧版 v2 的 `harvest_math` 复用 `binding_multi/library.c`；v3 的 `river_codec` 复用 `binding_abi/library.c`。两者均有同符号独立函数与 `class` 导出；v3 还有保留字参数、UTF-8/bytes 借用、标量和 void。v1 同形状导出由 Rust 结构测试覆盖。生产生成器按签名工作，不识别这些库名或符号。

```bash
cargo test -p ryupy-driver --lib native_binding
cargo test -p ryupy-driver --lib binding_build::tests
cargo build -p ryupy-cli
PYTHONDONTWRITEBYTECODE=1 /absolute/cpython3.12 run.py \
  --python /absolute/cpython3.12 --ryupy ../../../target/debug/ryupy \
  --out /outside/repository/binding-stubs
```

Runner 每次即时用固定 CPython 与严格 ctypes 适配器运行原样应用，比较实际 JIT/NativeAOT 的退出码、stdout、stderr。四个产品全部搬迁，并先删除声明、C 源码、原始动态库/静态库和构建树；NativeAOT 使用空工具 PATH 和失效 PYTHONHOME。生成 `.pyi` 以目标 Python 3.12 语法解析；每个导出的机器签名、原参数名、别名关系与同次产物清单交叉核对，错误调用覆盖 kwargs、数量、类型、范围、UTF-8 和非法 bool8 返回。

2026-11-17 在 macOS arm64、固定 CPython 3.12.13 上，以上两声明 × JIT/NativeAOT 四格通过。UTF-8 非法码元使用既有 ABI 专项相同的 `chr(0xd800)` 构造；源码孤立代理码元字面量的前端转换另有已观察差异，首轮失败日志保留在仓库外。

发布记录通过 `binding_interfaces` 的精确路径及 `artifacts` 的 `native_binding_python_stub` / `native_binding_machine_contract` 角色关联；同次发布树若已有目标文件，生成器拒绝覆盖并由既有发布事务回滚。无新增文件 hash。生成接口、二进制及日志只留在仓库外测试产品目录，仓库只保留手写输入与 runner。
