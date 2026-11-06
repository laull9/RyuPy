# ADR-0071：list.pop 宽索引与一次整数协议

状态：根因与唯一CP参考已独立审核，三运行时文件已接线且Release构建通过；三个新CP/JIT应用首次通过，结构尾修1项通过，新C#7项＋原24项通过；首次正常完整门禁通过，未发布。

## 已实证的破坏性旧行为

ADR0070 的原基线里 `pop(4294967296)` 错误删除首元素，合法用户 `__index__` 被拒。
前切只统一参数绑定，两个边界独立保留。本切处理原因而非修改期望：
PyList.Pop 在范围判断前把 long 窄化为 int，使正负宽值回绕到有效位置；
pop 绑定使用的共享 ToIndex 也不执行整数协议。

## 最小合同

只在 pop 使用专属转换，保无参数直接 Pop() 和 long/int/bool 热路；范围内 BigInteger 转机器整数，
超 i64 使用实证 OverflowError。普通对象只查类型级 __index__ 一次，不查实例伪槽、不走 __int__、不递归转换结果。
既有关键字/arity 拒绝仍先于转换，转换先于空列表判定。回调可以改列表，必须按回调后当前长度执行。

Pop 以 Count 检查空表，在 long 域规范负索引及判界，成功后才转 int 并访问 Items。
失败不额外物化数值背板、不删除元素；用户回调自身修改不回滚。成功仍沿原物化/移除/返回同对象路径。
本层自造转换和边界错误准备当前帧/context；不 catch 或重写用户协议正文异常。
其它 get/set/del/slice、共享 ToIndex、CallValue/Flatten、编译器及运行时 ABI 不变。

## 边界与成本

__index__ 返回 bool 在 CP3.12.13 中会发 DeprecationWarning，warning=error 时必须阻止删除；
本切明确 NotImplementedError，保真实合法正例 pending，不默默省略警告。
真实 int 子类布局及其返回值警告仍未实现，不能用普通类冒充。不可调用 __index__ 槽沿原基础设施，
不借此扩大通用错误帧修复。没有完整索引协议、项目/AOT或性能通过声明。

常用机器整数不制造 BigInteger、闭包、数组或 EH。仅普通对象走冷协议；Count 后 long 判界修复了
错误截断，成功仍有原 Items 物化成本。结构/回归不能替代性能测量。

## 唯一参考

固定CPython3.12.13/64位，一次103例（stdout共105行含版本和结束标记），28秒TERM＋2秒KILL上限。
退出0、stderr空、未超时，0.047978秒仅作预算记录。
脚本SHA256 `8d3267db022dfdc74c432c1bbdd0892c83759484909288216151af794b09fcca`；
stdout SHA256 `b5e6e5043a72f649f46f2256cab5614f886fd694f2213731b83e8f1dde471268`。
原流核实空表转换优先、宽long越界/超i64溢出、回调变异、原异常身份及context、keyword/arity先验和两种warning策略。

## 首次反馈与证据边界

运行时首次Release构建通过，零warning/error。旧四方法3应用＋1结构回归通过。
新结构首次失败于把单参暂停恢复调用误期待为RuntimeFn::CallValue，实际MIR为DynCallValue；
生产编译器未改，尾修只按原实体锁定同callee、frame spill/reload和恢复输入身份，不删除守门。
三个新应用随后首次3/3通过：宽索引/协议、变异和暂停顺序、完整10项用户异常链。
各应用先验固定CP版本/退出码；错误链在构建前核固定期望，再正常编译及JIT比对。
不可调用的 __index__ 槽沿现基础设施，本切未实测其未捕获帧全域，不把潜在旧债说成新修复。
当时C#新专项与完整门禁尚待，不外推全项目或全部索引协议。

## 专项收口

首次C#31项中29通过、2失败，原详细日志定位为测试向 numeric 专用ReadF64传了C#int字面量，
该编译器可信ABI只接受装箱long。仅三处0/1改为0L/1L，未改生产代码或弱化断言；
随后新7项＋原四方法14项＋数值列表10项共31/31通过。原首失败和诊断复核均保留。
结构只修实际MIR的调用形态并增强同frame/恢复实参断言，第二次1/1通过。当时完整门禁尚待。

## 完整门禁终态

首次正常 `make check` 退出0：fmt、strict clippy、workspace、Release零warning/error，
839项C#零失败/跳过，固定CP基线228项通过，e2e202项通过及原30项ignored保持。
新3应用/1结构及前切四方法回归在workspace中通过。无compiler生产、快照、工作流或ignored改动。
仅本地提交并保存可恢复候选，不公开发布；未新增项目/AOT/性能执行，也不宣称warning/int子类已闭。
