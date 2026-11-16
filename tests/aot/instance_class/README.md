# 显式普通类 NativeAOT 验收接线（实施中，尚未产品运行）

本目录在环境重置后重建，旧检查器原件及其10项通过原流未恢复。当前纯测须以新窗口原流为准。

## 单一权威与阶段

1. 来源采用作者重新冻结的 CP fixtures/expected；两副本 owner/reexec/default/builtins 及失败发布分别留证。动态 hook 为产品明确 NI，不用 CP 的 published 结果冒充成功。Class-body uncaught 帧红项独立保留。
2. 源码锁全集生成逐模块 `files=[]` 虚拟来源锚，包括真正 Package 祖先，不部署 `.py`。请求不等于实际图，必须再与本轮实际 AOT module plan / manifest 逐项核对；不使用旧 JIT 图假称 AOT 选择。
3. 最小 ignored driver 测试桥输出同次 MIR FnId/schema2/PE。复用 codegen 实际 PE 门验证 root/thunk/body；补真实 `CreateOwnedFunction` 调用及26方法全集关系，不能按 MemberRef 存在或 suffix 猜。
4. 轻量 Host 构建、检查器纯反例先于 Native。原77B、HTML102B两业务都需真实 Native 原位/完整dist搬移，并保存实际ELF、manifest及前后身份。不把纯测、方法存在、单构造或CP通过记成产品通过。
5. 原无资源 E0100 仍有独立严格负例；新有效配置是不同路径。旧 metadata 门当前不改期望。新的 reusable CI job 仅依 selector，原20/ABI/S1/fullcheck/既有AOT不因其失败而跳过。

## 当前文件

- contract.py：有限来源/选择/原流校验。
- test_contract.py：纯结构及崩溃/重复JSON/缺祖先/回退反例。
- support.py：既有捕获器与dist工具的薄适配，不自行触发运行。

正式runner、fixture引用、PE适配和独立CI已接线，尚待最终独审与实际hosted执行；不能将此目录存在当作已完成产品验收。新门先普通CLI的独立JIT正控，再核CLI AOT所发PE与结构桥仅合法COFF四字节时间戳外逐字相同、对CLI实际PE重新解码及Rust门后才执行Native。aot=true的ExecutableRelative来源PE由Runtime明确拒普通dotnet执行，绝不伪造运行环境；新hook守卫仅由Native验证。原始CP hooks成功与产品NI观察分账。

`instance_class_actual_pe_gate`复用原Rust签名/静态边验证；Python补AST来源全集和CreateOwnedFunction边。两者任何一个未执行/失败都不准进入Native。CLI publish失败仍保存实际Host/managed/日志；成功ELF先归档再执行。搬移阶段临时隔离本门输入副本并完整恢复，不更改仓库源码。
