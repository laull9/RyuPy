# 函数局部字符串累加优化

仅扩展既有 NumericAccumulator holder：精确 System.String 延迟缓冲，保留空串身份、
旧快照不可变及同版本物化缓存；动态对象与字符串子类仍走原协议，成功后才替换状态。
MIR 保守拒绝全函数 FunctionLocals ABI，既有模块、闭包、cell、异常区域、yield、
循环内别名和多出口限制不放宽。资源需求改变，不保证 OOM 时机与旧实现相同。

回归由 PyStringAccumulatorTests、numeric_accumulator_tests 与
phase3/m3_10_string_accumulator.py 承担。普通字符串子类保留严格 CPython 差分。
原生字符串子类自定义 __radd__/__iadd__ 仍有既有 NotImplementedError 缺口，
不能把此优化解释为完整字符串子类兼容；CPython provider 的兼容性另行计量。

历史专项 run43612817769（48e67b3d，非本次新 dev 合入基线）已完成 Linux x64、
SDK10.0.403、CPython3.12.13 的 MIR 12 项、C# accumulator 10 项及两个语义输入的
基线/候选 JIT/NativeAOT 全输出差分。自定义槽单独记录为已知不兼容而非差分通过。
同 runner 10 轮交错测量中，局部追加 AOT 中位约1610.95→10.43ms，JIT约1715.34→85.96ms；
模块追加没有改善。计时包含新进程启动及约200KB输出，使用共享runner，不能推断暖态、
一般应用或新onefile路径的收益。新dev的内嵌运行时和完整检查须由该基线CI独立验收。
