# ADR-0145：实际代码来源与同帧冷消费者

- 状态：有限源码候选，产品未验收；不提高项目通过率
- 日期：2026-11-09

## 背景

原warnings与功能式TypedDict按诊断文件basename扫描模块注册表。仅把未初始化空pkg.data加入显式图，就可能让本来成功的pkg/__init__.py来源变成歧义。图的改变不应重定向已执行代码；不能针对pyfiglet特判，也不能把function.__module__误作实际代码或warning globals。

CP3.12.13参考已区分两个消费者：warning取stacklevel所选同帧活globals；TypedDict经typing._caller读取真实函数Module或模块执行入口快照，再执行真值与AttributeError fallback。旧“TD一律取globals”推论撤回。ADR0076既有TypeVar/内部GetCallerModule边界保留，不为本切引入全Static owner或每次调用隐藏owner。

## 决定

MIR为每个真实源码unit保存一个私有CodeSource及完整signature归属。link按真实ID重映射，CIL冷初始化不可变CompiledCodeSource，关联既有signature。Runtime对象只含代码初名、诊断文件与真实Legacy getter；Instance代码不反持实例。source身份不是文件名或注册名查找键。

三新增ABI：CreateCodeSource、FunctionDefinitionWithSource、CreateModuleExecutionSourceWithCode。旧ABI保原值语义，旧手工MIR emit出口保持兼容。真实源码driver在普通/Trace模块返回、link后、Mir dump提前返回、CIL dump及JIT/AOT/fixed-binding出口强制完整来源校验。required是外部准入要求，不能因可删MIR表为空而关闭验证；它本身不是完整证明。fixed-binding仅对已验证实际新增initializer豁免，保留原模块/init/source关联。

Legacy来源能力要求真实__name__导出槽、生成getter及file_backed_by_global；有限前言必须reset→marker→带origin压帧→原name值及bound发布，无用户回调。模块入口TypedDict用已证明的原初名，不读尚未发布槽；warning执行时活读getter。Instance入口记录真实目标globals的当时值；Execute(X,targetY)持X代码来源与Y字典，绝不从target.State.Code补推X。

Dynamic/generator帧保真实函数，Static帧加载自己的既有signature。Static角色由MIR/PE producer证明，Runtime signature没有CallingConvention自证能力。新signature缺source、文件不符或不一致owner明确失败，不能降为旧Unknown。

## 两消费者的有限合同

- 新warning统一使用同帧代码/环境，包括Main、Dynamic、Static。先类别校验，再读活__name__，None在构造或str消息前抑制；精确str及现PyStringInstance payload参与有限过滤，缺键/其它对象为`<string>`。不能以PyDict普通精确str键测试推导任意碰撞键查找无回调。
- 新环境registry使用弱环境身份，不含可变__name__。default按text/category/line；module与once共用text/category二元altkey；重设过滤器换epoch。同Legacy环境三帧类及同Instance字典的不同实际代码共桶。公开__warningregistry__协议、任意hash/eq重入未扩域。
- Main/Dynamic功能式TypedDict读入口快照/真实function.Module。raw真值一次，false转__main__；AttributeError及Python子类仅在真值阶段被捕获，随后活读同一环境。缺键转main，其余非None原身份返回，不再求真值；RuntimeError等正常传播，清理回调帧前Attach原生错误。
- 二级fallback None涉及元类自身globals默认填充，本切明确NI；最终旧/CP/新同组若发现旧正确而新拒绝必须停止，不能以域外遮盖。已核具体Dynamic旧控制本来返回subject且不执行bool，CP返回typing并执行一次bool；模块入口宿主仍待三方实测，不外推。
- TypedDict非str模块值保原对象，直接repr及typing alias显示共同明确NI，禁止CLR字符串化。
- Static TypedDict保原ModuleForFile算法、歧义与可写Module重绑旧债。已实测旧默认Static路径正确，不能整类NI；不声称新signature提供当前真实函数Module。旧无source warning保原扫描/顺序/旧表。

## 成本与边界

源码闭包为38生产文件；其中一文件仅有限前言验证职责拆分。新增冷source/签名关联与weak registry；Static热前言多一次既有signature字段加载和带origin压帧，无每调用新对象，数字参数/返回/运算与thunk调用ABI不变。前言IL当然会改变，不能声称整个core字节不变或零成本。未做性能测量。

不扩默认模块图、自动发现、完整fromlist、kind2路径、公开sys frame API、完整warning registry、TypedDict元类或全Static owner。本切不是约80%项目覆盖已经达到，也不是AOT/性能优化完成。

## 验收与状态

已完成有限Rust结构/类型门：原CIL窗口3通过1误匹配失败；仅测试改真实Call/MethodRef后单例通过，再9 MIR与3精确Trace通过，driver lib check通过。两窗口分别留原流，不拼成一次全绿。Runtime具名13Fact仍待实际C#编译运行。

必须完成：原J1/J2 two/three图、固定fixture实际PE关键链与CP/JIT、双Instance/mixed八组去重、Static旧正确路径、原生bool错误现场恢复、真实Execute(X,targetY)旧/新host、实际新消费者Native一次publish及原位/搬移/源码隔离。固定PE表不等于通用全部IR证明，与现MIR/CIL测试和真实输出联合使用。

同HEAD default/pilot完整make check、固定20逐ID、全部旧Legacy/Instance/资源/Trace/pyfiglet门仍必需，不能用新专项遮蔽旧门。测试入口见tests/validation/source_provenance/，e2e来源例保持pending直到实际产品门通过。
