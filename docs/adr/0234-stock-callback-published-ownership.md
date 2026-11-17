# ADR-0234：stock回调发布结果的所有权

- 日期：2026-11-17。
- 状态：实施中，原生生命周期与实际CIL产品分别验收。
- 接续：[ADR-0228](0228-stock-compiled-program-lifetime.md)、[ADR-0230](0230-stock-thin-local-identity.md)。

## 问题

通用值调用扩围后的真实Python产品发现：同步容器结果在短callback作用域结束时被退休，
而transfer索引仍持有对应包装。外层同步读取因此使用已释放PythonObject。异常前容器
写入与正常返回都需要保持已发布对象的实际使用权。

此外，闭域编译入口可以把managed proxy合法保留在原模块字典直至退出。旧预检要求所有
proxy Roots在终态前为零，导致没有外部引用的正常程序也不能完成关闭。

## 决定与准入

只转移当前受信内部作用域实际拥有的token：运行期发布的容器owner及其opaque项转到
入口弱登记；外部Pause期间创建的owner不收编。容器退休同时撤销对应transfer身份索引。
终态嵌套回调的资源按实际父作用域或所属proxy管理，所有释放仍走真实Release/Drain。

内部proxy按实际root ID登记，原生dealloc真实退账，预检比较Roots与内部登记的精确集合。
入口外和外部Pause的root继续保留外部使用权，关闭可恢复拒绝。终态不清业务字典，
不调用Roots.Clear，也不在解释器终止后补做DECREF。

内部proxy留D、两次atexit、异常和GC、外部proxy拒绝及重试、构造失败回滚分别验收。
proxy与opaque容器形成的跨堆回边及大量临时对象存储另列实际结果，不能靠退出清账掩盖。
普通嵌入Host与专用封闭程序权限继续分开；只登记实际验证过的发布/关闭能力。
