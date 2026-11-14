# 固定fixture实际PE关键链门

只读`ci/read_pe.py PE old|new CASE`，复用固定ECMA解码底座，不加载程序集。每编译或publish一次，只对本次实际PE生成自动关键链记录，然后在同一hosted stage继续运行该SHA产物；不拆成人工等待或额外CI。

## 预期与证据边界

预期由固定完整fixture源码、已核词法关系与实际方法ABI独立列出，不从待验Source表生成。不新增production expected侧车/IR/通用证明pass。生成器内部保存恢复、Trace证书、link remap及源码准入的反伪造由已有MIR/CIL具名测试保护，本PE门不再次证明全Runtime。

每例必须核实际MethodDef、真实IL call/ldftn/field操作数，而非unused MemberRef。函数同名用真实签名/词法关系消歧；unknown opcode/截断等明确失败，不猜。

1. 每个固定源码单元的factory按角色精确选择：新Legacy为CreateLegacyCodeSourceV1，Instance仍CreateCodeSource；其模块literal、诊断文件、Legacy本unit实际getter或Instance null与实际存储字段一致。
2. 每个固定函数signature关联own source字段，source初始化先于signature消费。原J2无函数/signature不能为凑检查改输入，仍核所有module source/入口。
3. ModuleMain source/context→marker→PushFrameWithOrigin的实际操作数。Dynamic传真实self，Static加载自己signature。Static参数/返回/运算与thunk ABI保护，允许来源加载/压帧前言差异。
4. 固定mixed八组及同epoch组帧角色来自实际方法；显式include不改变pkg主消费者所属source；ADR0146后warning_origin默认也因unused提示登记data，两臂实际3/3。
5. X/Y真实图两个Instance及主入口的实际字段/载体；State.Code仍Y、真实old/new alias和__bool__帧恢复由托管宿主观察，不能由字段存在替代。
6. 新Native的包/subject ModuleMain与真正Dynamic消费者来自该次publish的managed PE。新消费者输出须原位/搬移/源码隔离三次对CP，不能拿旧Native绿代替。

## JIT与固定Native位置域

默认四参数接口仍逐条要求实际注册Kind1、绝对源码文件存在及固定输入SHA；不存在的路径不能触发降级。
仅`new native_consumers --native-manifest PATH`显式选择固定AOT审核，PATH必须是这次publish的真实`ryupy-artifact.json`。
审核绑定其compiled_managed_input SHA/字节数与实际PE、三个固定模块的ID/名字/环境、sources固定SHA/逻辑来源，
并核真实IL注册的主入口Kind0空路径、包及subject两个Kind2的精确资源相对位置，与module_locations逐项相等。
模块、sources或location的缺失、额外、重名、错kind/path/hash均拒绝。空resources不等于源码资源已携带。
所有source/getter/signature/thunk/core及ModuleMain/Dynamic/Static帧检查继续执行，不因AOT位置域跳过。

read_pe输出实际manifest原字节SHA及`location_mode=native_consumers_aot`，bootstrap原样转交，
Native三次真实运行之前再次比对同次publish保存的PE与manifest SHA。旧角色及其它JIT case禁止使用该参数。
合成元数据与传递负控只验证检查器拒绝边界；真实PE只读通过也不等于Host/JIT/Native或完整make通过。

## 记录与准入

完整解码保留fields/methods/IL。只有固定表全部关键链通过才输出admitted=true、实际pe_sha256/role/case_id及检查版本/预期表身份。历史schema名unit_link_mapping_verified仅表示上述固定映射核对，不声称全IR/link通用证明；frame_carriers_verified只表示列明实际帧操作数链。

bootstrap拒绝只有finite_checks_passed的输出，也拒绝错角色/错PE/缺任一链。Runtime13白盒、真实CP/JIT/host/Native与已有结构门联合验收，任何一项不能相互冒替。旧Static TD默认与重绑债、TD二级None三方停止门独立保留，不能把NI或exit0当CP兼容。

旧host PE只接受old_locator_verified=true且admitted=false的独立旧角色，不能借此给新source准入。真实getter按SameName/BranchFalsy的有限命中区核ValueSentinel或BoundFlag；typed发布顺序为marker→push→name→bound。typed-name尚无真实新PE正例，首个实际不符必须停报。标准discover合成控制不读取或修改argv，不要求外部fixture参数。
