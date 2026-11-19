# 普通函数locals有限验证

参考解释器为固定CPython3.12.13；reference.py / results.txt / stderr.txt保原CP输入与流。
参考含未实现的frame重入/间接调用等，不把全部参考计入RyuPy支持。

当前支持范围与成本见[ADR0101](../../adr/0101-function-locals.md)。
公开应用为state_identity、binding_chain、collision_contract三unique；原CP首次均成立。
首轮仅binding_chain通过，另两份因裸局部del编译E0100；原字节保pending，生产窄补后两份原源定向复验通过，最终三份全部通过。
独立边界/结构测试不加公开CP/JIT应用分母；NI不是CP正常语义通过。

Runtime25项最终专项通过；暖态1000次Validate(empty)+Ensure(existing)+Sync(bound preboxed)
+Sync(missing null)实测0托管字节，仅共享协议，不包含首次dict/调用参数/数值装箱。
另实际生成CIL委托对照：100次调用×每次1000循环，control44,800字节，locals1,855,200字节。
此路线包括每次新dict、循环局部对象化及共有运行时，未做分配剖析，不能把差额归到单个helper。
没有时间阈值、吞吐倍率、AOT、跨平台或完整项目矩阵结论。相同生成源的第二次真实编译/委托测量结果一致，两个原流均保留。

前两次正常完整门禁分别在class_if/getopt_core旧测试的过时拒绝哨兵处停止；原失败保留。
独立CP确认locals值读取现在应成功，局部del源保持并改验真实动态异常目标，另补global del拒绝。
第三次正常完整门禁全绿：1840C#、228CP、202e2e及30原ignored，fmt/clippy/workspace通过。
原binaryornot固定入口已唯一复测：CP/golden一致、编译成功、JIT首错bytes.translate缺属性；
未计项目通过，源/依赖/入口不改，原无帧stderr保留。结果JSON SHA256：485ace5584cdc0a292a6c03574a5aa4b19df7eb1278c3f3d7ed04b32ea77fbc4。
恢复校验在六件包元数据单独记录，不冒恢复后重跑门禁。
