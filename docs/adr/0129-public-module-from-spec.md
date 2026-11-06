# ADR0129：公开的有限编译模块实例执行

- 日期：2026-11-06。
- 状态：限域功能接受；性能与复杂协议继续开放。

## 选择

复用真实实例globals及B2/S0/S1编译入口，公开module_from_spec/create_module/exec_module。
创建与执行分开：CreateEmpty只有一个字典与5个默认键，不插builtins、不执行、不缓存。
输入spec、loader和包path保原对象身份；显式instanceGlobals标志决定属性存储能力，不造
假Metadata取得资格。普通import和旧宿主Create/Execute的默认代码行为保留。

loader以ConditionalWeakTable捕获不可变编译代码，公开name/path不能使旧loader偷换注册
后的新代码。exec明确用loader代码+目标globals；匹配名字的不同代码目标及loader=None
创建的非包空目标可用。名称错误先于builtins，None名保留元数据；重执行/失败不清字典、
不动原缓存。定义、闭包、生成器和类仍沿真实owner/cell，没有解释器式源码执行后备。

## 有限域

只接原生完整未改ModuleSpec、自有未改编译loader和已显式选择instance模式的源码。
自定义spec/loader协议、namespace、静态执行、任意ModuleType、动态磁盘源码、资源clone
继续明确NI，不用伪造错误或改CP预期代替。保存的bound method与后续新属性查找分别保义。
未知回调前检查协议；这些边界拒绝不声称复现完整CP副作用次序。

局部单参绑定器保关键字错误优先级，不改其它函数热绑定。模块GetAttr新增标志分支与
冷能力分配尚未量化，禁止称零成本、总体无回退或全DLL等价。

## 证据

完整门禁2423 C#/900 Rust/533 CP/358 e2e（179 ignored）；6新正例、9新完整pending。
首次manifest失败与后续最小配置修正保留。原docopt过，默认Markdown仍find_spec NI；
未改四源窄配置已过公开loader，下一阻塞为html.parser第42行lookbehind。
全部范围、命令归属和未测成本见[验收记录](../module-from-spec-slice.md)。
