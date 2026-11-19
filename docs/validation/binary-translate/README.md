# bytes/bytearray.translate验证

范围、算法与边界见[ADR0102](../../adr/0102-binary-translate.md)。
固定解释器CPython3.12.13，独立220项观察不等于220项实现断言通过。
三unique公开应用：table_delete_identity、binding_alias_capture、errors_chain。
首次CP-only、原源JIT与转正后JIT均3/3通过；完整错误链保留cause/context和六个用户帧。
18项C#专项覆盖四调用路线、PIC、表/删除活读、导出生命周期与明确NI；完整门禁另记。

独立审查的duplicate_keyword.py.pending保逐字原源，六入口CP/JIT差异仍在，未移除或伪造期望。
CP重复**关键字在调用展开时报multiple values，当前公共FlattenArgs错误地进入binder报arity。
用户buffer、宽格式、多维、真实子类及全局空bytes单例均不计本切支持。

外部证据根ryupy-binary-translate-reference保存probe/assertions、原双流、初稿与重复关键字失败；
ryupy-binary-translate-validation保存CP-first、两轮JIT、专项、真实生成CIL性能与完整门禁。
恢复集继承历史证据；产物DLL只记录hash，不将构建二进制冒充源码。项目复测仅单独有界窗口。

首正常门禁1857C#过/旧警告测试1败，后续阶段未运行，check-first.log保留。原代码单类3/3
和全量诊断1858/1858通过后，仅补旧警告类的共享帧/警告状态测试隔离，保原断言及生产语义。
首次安静日志不含具体异常消息，根因不能从该次输出精确归定。

真实性能与分配见performance-summary.json：最终Invoke guard两轮各72例、每例7样本，固定
CP结果/身份/输入不变和5组buffer校验通过。同一真实generated.dll/host.dll只换runtime，
FullOpts/初版/两个不利候选与全部原样本留恢复集。1MiB实际空白删除1.693/2.034ms、819848B/op；
None省略删除42.1/46.2ns、0B/op，是不扫描快路，不能外推非空删除/项目整体。共享机器噪声明显，
不宣称全面无回退；bytearray仍fresh。初版也是本轮未发布实现，不冒旧发行版基线。

最终第三次正常make check全绿：1858C#、228CP、202e2e及30原ignored，fmt/clippy/workspace全过。
最终check.log SHA256：743b88fa07500813c9d9cfd6d815fa1cd03a281919f0e62f12e35dc5389c59cc。原项目窗口另记。

原项目唯一窗口：干净f3ba26b，CP/compile/JIT各exit0、原golden精确一致、JITstderr空。
result.json SHA256为383e55be75ea2687b6ffa06d1d6d7f70d9bcef04c2ff2abf0a44d76aa1ea4ab6，
原source76/dependencies501逐hash不变；只计此候选原入口，不扩矩阵/AOT或性能结论。
