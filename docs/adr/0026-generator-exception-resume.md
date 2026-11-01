# ADR-0026：生成器逻辑异常恢复、上下文与委托清理

状态：已接受。完整聚合门禁与只读复审通过，按整M3.2提交，不按小切提交。

## 背景

恢复入口不能跨调用直接跳进原CLR try中部，yield/return也不能在受保护区ret。
现有finally已使用退出原因局部与分派，重复实现一套Python finally会扩大风险。

## 决定

保留build_try及with的HIR去糖。generators把原Region变成逻辑handler表，按原基本块号记录
active_block；原CatchException改读帧待接异常。只有含Region的恢复函数建立单outer catch，
先RaisePrepare保存context与原traceback，再匹配最内handler；保存locals、PC、异常后返回内部Retry。
运行时清除被异常展开的callee帧并重调同一恢复委托，不对用户产出值。保护区内所有返回leave到外部出口。
没有匹配则重新抛出；仅运行时最终未捕获异常才永久关闭生成器。

帧控制槽扩展为PC/send/注入异常/handler异常四个，locals从4起。普通yield恢复调用GeneratorInput；
委托yield恢复由GeneratorDelegateStep消费send/throw，不能预先抛出注入异常绕过委托。
yield-from保存真实iterator与独立current/result，委托StopIteration在内部消费，close错误不能误当终值。

PyErrors当前异常改为节点token。每生成器拥有根节点，恢复期间临时链接caller、暂停后断开；
保存own handler链而不持久借用caller异常。finally逐层独立退出原因；内部cleanup保证except与finally
所有return/break/continue/异常退出均恢复token并清空as绑定。EnterHandler(None)为内部只读快照。

## 代价与边界

有EH生成器每原块增加active整数写入和单CLR catch；异常被处理时增加全locals spill、线性Region匹配
及一次委托重入。普通无EH路径不付这些开销；所有生成器仍全locals保存，尚无liveness优化与加速测量。
委托对象独立保存迭代器，不捕获父frame。新建close/throw不执行body，close非法yield保留暂停位置。
现代单参throw已支持，旧式2/3参形式明确拒绝；GC即时关闭仍受D001限制。零参super方法上下文待做。

## 验证

原M3.2 generators/cleanup及新增异常、嵌套finally、异常上下文和自定义yield-from差分均先pending，
实际unexpected-pass后转正。driver专测检查Retry后重新抛出仍保留原helper行；MIR验证拒绝残留高层委托
及返回对象的控制调用使用Eval（曾导致CIL栈不平衡）。最终以完整聚合门禁与固定项目结果为准。

聚合结果：159项C#、79编译差分通过/19忽略、94基线/框架；固定项目仍native0/20，项目首阻断变化已同步compatibility。
