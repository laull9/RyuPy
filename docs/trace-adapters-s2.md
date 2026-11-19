# S2a测试专用来源/接收纵切

本候选仍未运行.NET/Rust构建；以下是源码合同，不是实际通过声明。生产路径关闭，原generator严格帧失败未修复。accepted不改变。S1内核不改，NativeFresh、Bare、Injection、RestoreHistory、完整host生命周期均未实现。

## 真实路径

四份同源Python经driver parse/HIR/types/typed MIR/CIL编译。隔离host静态调用生成的`RyuPy.Generated.Module.Main`，检查真实导出的result与最后一次Handle对象同一身份，再读取owner.H和Context。H完全来自真实Push/SetLine数组的scene，不取旧Frames裁剪，不打印预设期望。

`TraceAdapterExplicit`只从无cause显式raise发射。归一化后、旧Attach之前拒绝未绑定的legacy历史；active scope+真实activation是源入口的前提，Begin成功才发布binding。构造不访问scope。catch无绑定/错误scope直接拒绝；已有绑定跨scope拒绝，不清字段后重绑，不隐式Rotate。

静态catch从CatchException局部取对象，在原PopTo前Inclusive Handle，再执行旧HandlerEnter。自动handler cleanup在原PopTo前Exclusive Forward同Q。原异常在generate内部Handle时，不提交尚未遍历module；replacement则在module接收并保留generate帧。旧Frames继续保有旧行为，oracle只检查独立H。

按HIR结构拒绝bare、from、generator、dynamic except、with、用户finally，以及handler体内（包括if/loop）嵌套try。最后一项避免同一activation的连续cleanup发重复不前进Forward。未知native来源不自动Fresh，测试失败不能回退猜测来源。测试scope Close检查所有已记录源owner终结。

## 默认构建与测试构建

默认Cargo不启用`trace-adapter-test`；默认MSBuild没有`TraceAdapterTest=true`。所有新增runtime类型/字段/activation采样均条件编译；默认正常Push无新增计数或分配。唯一默认实现重构是PrepareRaise抽出内部NormalizeRaise helper，仍按归一化→Attach→Prepare顺序；不声称默认源码或IL逐字不变、异常冷路零额外成本。专门测试旧身份/Frames/context与无效参数，旧完整门仍保留。

probe使用独立Cargo target与MSBuild artifacts目录。不能用于默认20项目、旧Native或默认/pilot make。实际默认runtime/生成模块PE另查无adapter类型/引用；既有S1门继续检查旧→新核心引用和有限ABI。

## 验证入口及证据

`tests/aot/wcwidth_ci/trace_adapter_s2.py`由既有授权workflow独立job执行，保留每步argv/stdout/stderr/退出码、真实DLL/模块、typedCFG/ABI测试、默认focused TRX、host实际事件/H/哈希。失败即停，不改oracle、不重试。四正例先有固定CP3.12.13一次运行原流及输入SHA，见fixture目录cp-reference.json；CI只核这些冻结字节，不再次运行CP。

host定向边界测试允许手工建立单个帧，仅用于scope外、首次失败、catch不能绑定、legacy历史、unfinished、跨scope拒绝。它们不冒充真实Python纵切；四个正例必须各自实际生成并执行。单元1只JIT，不宣称新核心Native已root。已有项目/Native通过不能替代这个缺口。

后续原失败需要真实generator逻辑handler/Retry-before-PopTo、public Throw与内部转发、PEP479和contextmanager历史恢复。更广的Bare/dynamic/root/CLR边界与原子协议切换仍阻断S3。此单元只提交可运行验证候选，不改变生产异常历史或兼容性分数。
