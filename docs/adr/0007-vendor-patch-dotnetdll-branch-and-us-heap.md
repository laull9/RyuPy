# ADR-0007：Vendor Patch dotnetdll 分支偏移与 #US 堆缺陷

- 状态：已接受
- 日期：2026-10-31

## 背景

在推进 M1.3 与 M1.4（特别是 Mandelbrot 多重嵌套循环与字符串字面量）时，`dotnetdll 0.3.0` 暴露出两个严重的底层生成缺陷：

1. **分支偏移预测压缩缺陷**：
   `dotnetdll/src/resolution/write.rs` 在将 `Instruction::Branch(idx)` 转换为 CIL 二进制时，试图将长跳转优化缩短为 `br.s`。但在第一遍扫描时，代码直接对**指令数组索引**判断 `i8::try_from(*o).is_ok()`，而非实际的相对字节偏移。当方法体稍大（如 Mandelbrot 中含有长浮点指令与常量）、前向/后向跳转跨界时，累加的 `deltas` 严重污染了各指令的绝对 `offsets`，导致生成的跳转指令目标偏移错位（跳到指令操作数中间），CLR 加载即报 `System.InvalidProgramException`。
2. **`#US` 堆空字符串映射缺陷**：
   `dotnetdll/src/binary/heap.rs` 的 `UserStringWriter` 将空字符串 `""` 预先缓存为 offset 0：`HashMap::from([(hash(&[]), 0)])`。但在 ECMA-335 规范（II.22.4 与 II.24.2.4）中，`#US` 堆的 offset 0 是占位符，`0x70000000` 是未定义/空 token。任何空字符串必须在非 0 偏移处分配 2 字节（`[0x01, 0x00]`）。生成 `ldstr 0x70000000` 直接导致 CLR 抛出 `System.BadImageFormatException: No string associated with token`。

## 方案

### 方案 A：在 MIR 和 Codegen 前端做规避
在 MIR 层拆分函数、折叠指令，避免生成空字符串或长分支。
- **缺点**：治标不治本，任何稍微复杂的 Python 脚本都会随机触发崩溃，严重阻碍后续里程碑推进。

### 方案 B：Vendor Patch 并通过 Cargo 覆盖（选用）
将 `dotnetdll 0.3.0` vendor 到 `vendor/dotnetdll`，在根目录 `Cargo.toml` 声明 `[patch.crates-io]`：
1. 修复 `write.rs`：移除脆弱且有误的 short branch 压缩预测，直接基于精确的 `offsets` 发射标准的 32 位相对跳转（JIT 会在机器码阶段自行处理跳转尺度，IL 层面 32 位跳转 100% 健壮，永不越界）；
2. 修复 `heap.rs`：将 `UserStringWriter` 的初始缓存由 `[(hash(&[]), 0)]` 改为空 `HashMap::new()`，使空字符串正常在 `#US` 堆非零偏移处写入合法编码（`0x01, 0x00`）。

## 结果与影响

- `tests/e2e/phase1/m1_4_mandelbrot.py` 等长方法与含空字符串用例彻底稳定运行；
- 架构分层完全保持：依赖方向未受任何破坏（仅 `ryupy-codegen-cil` 依赖 `dotnetdll`）；
- 全套测试通过。
