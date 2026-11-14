> 当前身份：环境丢失后重构候选。runner 只用 fixtures/REBUILT-ORACLE.json 绑定当前源及本轮 CP；fixtures/ORACLE.json 与下文 Oracle 窗口描述属于旧源历史，不用于验证十个重构微源。详见 REBUILD.md。

# 默认来源 NativeAOT 联合门（候选，未产品验收）

命令只有 build / entry / --out-dir / 普通 -I / --aot；不传 stdlib、Instance、include、catalog、metadata、resources。
3 个 ELF：原 Markdown77B、HTML102B、通用包单ELF双argv。每执行原位/搬移，合8 Native进程；普通独立JIT合4进程。严格publish、唯一源码身份、schema4直接检查及v3九字段、完整dist/原流保留，失败不重采。

## Oracle边界

fixtures/semantic.stdout=新CP3.12.13真实288B（旧196B加92B副本观测）；boundary-cp.stdout=真实148B。boundary产品预期四处NI与CP不同，固定在checking.py，不伪称全部CP等价。原77/102引用已有冻结黄金并已本地本轮CP再次证实。
本地001首次semantic因函数dotted-import局部绑定出UnboundLocalError并停止；003只加两个global，002两小源各一次成功。原失败保留于私有原件，不能说第一次四项全过。随后获准新004只追加ModuleFile副本，003窗口新版semantic单次288B成功；旧196B原件保留，不称复制父Package授权通过。

所有CP小源完整AST已冻结。新产品未运行，fixture是否进入真正Deferred/Outside由发射计划明确检查；不符即失败而非调整黄金。默认closed_unplanned权限不得由测试增授。

## 身份和保护

发射前MIR oracle独立保存；真实冷ldftn与ModuleDef slot、init FnId、完整冷ABI签名核对。CLI对桥仅容许合法PE COFF时间戳4字节，除PE身份外不改schema2 owner期望，原schema2 Rust门再检查实际CLI PE。不是只看MemberRef，亦不声称Native全部trim retention。
候选metadata必须实际PE Configure载荷/原始分发目录/报告一致后直接验artifact4；不把artifact4转成2。新manifest不承诺数据resources_complete。
旧metadata红门未改；旧显式类域只添加新标题入口仍执行原完整门。父workflow原保护jobs结构不改，新job没有成为旧20项目/全检/S1/ABI的前置。

## 预算

job85分钟，固定工具10分钟、新门step65分钟、前置纯测2分钟，至少8分钟供上传/尾项。单publish最多600秒、子运行30秒；结构/编译上限服从整step超时，并不保证每子上限能全部耗尽。失败always上传原件，不以预算停机冒通过。
