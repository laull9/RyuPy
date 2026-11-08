# 生成器 trace 产品 V1 门（待执行）

生产合同见 [ADR0142](../../../docs/adr/0142-generator-trace-product-v1.md)。本目录不是 checker-only 交付：正常 CLI 生成的 JIT/NativeAOT 必须完成固定原流比较，完整既有门另行保护。

- `goldens/` 仅含固定A-D/热输入五源、实际CP stdout/stderr/exit和简洁来源manifest。B仍逐字核原 `tests/cases/generator_dynamic/frames/entry.py`。源码/stdout/exit逐字导出；stderr仅把File帧的源路径换为源basename，其余字节原样保留。manifest分别记录完整来源包hash、源相对路径、原raw SHA与导出SHA；不包含报告、工具树、运行脚本或协调材料，不重新执行CP。
- `generator_product_gate.py` 默认只输出计划；只有最终 clean commit/tree、实际 baseline 构建 provenance 和工具 hash 冻结后，加 `--execute` 才执行。模板 `frozen=false`，不能直接用于验收。
- 正向 A/B/热输入每个 JIT 与 Native 各编译一次，PIC1/0 各运行一次；原 C/D 两模式只编译拒绝。默认 A/B 仅编译和 PE 无 V1 控制，不执行。
- `product_contract.py` 核完整流、原源 SHA、实际 PE Main Enter(1)、V1 ABI 和唯一 getter 站点。只允许精确源路径与 CP 源码展示归一化。
- `regression_contract.py` 只读核已有同源 fullcheck/default+pilot、20项目逐 ID、原十通过项、四旧实验精确隔离及 C# 两项 TRX，不重建或重跑回归。
- `build_provenance.py` 核已有 baseline 的实际构建 argv、双流、工具和输出身份。其它 job 按自己的构建配置与 runtime 核验，不强迫跨配置二进制同 hash。

产品总预算3600秒：编译300秒、发布1500秒、单运行30秒、decoder60秒，同时受剩余总预算约束。沿用已有进程组终止、8MiB流限制及128文件/256MiB dist归档，不重试、不补采。

离线自测：`python3 -B tests/validation/generator_product/test_product_gate.py`，及加 `-O` 的同一命令。仅解析原件/合成坏证据，不启动 CP fixture、Rust、.NET 或产品。这两类自测通过不能当成产品通过。

真实冷热口径：热 fixture 覆盖同站点重复执行；另核 `PyTraceProductPicTests.ColdAndHotGetterFailureForwardSameOwnerWithoutLegacyAttach` 的 false/3801 与 true/3802 两项实际 C# 结果。产物无 hit 计数，不能声称观察到命中数量。

实际CI入口为 `bootstrap.py`：只在既有baseline已产出compiler-identity时启动，独立核两次构建实际成功/hash，复用同CLI/runtime；仅构建一次现成只读PeAudit，实际SDK原流生成最终contract。准备上限420秒（decoder300秒），产品仍3600秒，外层job上限同时生效。Legacy失败保持失败，不压住已具工具条件的显式V1窗口。

fixed20只读聚合当前要求原CI checkout路径，因为复用原validate_result的绝对entry身份检查；异地下载产物不能直接冒相同路径通过，不泛删路径验证，也不为此重跑产品。
