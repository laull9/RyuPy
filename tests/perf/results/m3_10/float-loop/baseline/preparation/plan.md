# FloatLoop 单次预设配对（未执行）

- baseline：已测 short-chain Release CLI（2b64e382…），源码来自上一候选完整patch；runtime统一当前Release 0ccd1c…，旧d967…只作历史，不混用。
- baseline/candidate同时编新float_loop.py：外层range(1)避免entry-only holder，11场景，正式count30000；--validate用24，--diagnostic用完整count但不计时。
- 固定13短/完整语义先行，随后两方逐字比对semantics输出；结果/别名/调用次数/慢路prefix值都由原check验证，不只看总数。
- 唯一正式组：pure 3进程×7样本，两tier轮转；原matmul 100×100连续3次同协议，输入重建/输出核验计时外。独立资源3样本，JIT另进程；不追加重复。
- nbody不是此切目标，只保两方MIR/CIL/hash与原输出对照，不重跑四core/80进程。
- empty时间极小只作正确性/开销参考，不用相对百分比冒称有意义收益。bool与首/中/末custom/shared及混合恢复负例不能删。
- 计时期间不构建；所有原始输出、失败、代码体积与不利时间保留。是否默认按实证由父决定。
