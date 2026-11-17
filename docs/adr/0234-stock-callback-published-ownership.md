# ADR-0234：stock回调发布结果的所有权

- 日期：2026-11-17。
- 状态：发布与关闭的生命周期子合同已验；完整CIL产品和调用现场继续独立验收。
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

终态proxy采用入口弱登记和原生dealloc回调。某个root退出时，仍可能有另一个proxy持有
同一CLR对象图；只有内部proxy/core roots全部退出且没有活动owned scope后才退休共享
owned池。1000次atexit临时回调的实际峰值为3，避免临时owner随回调次数线性保留。

异常打印与SystemExitCode在Runtime铸造的一次性诊断请求中取得独立短scope，只退休
本次诊断新建的metadata和流对象。请求绑定实际provider、线程和可接受阶段；普通
嵌入Host继续沿原授权路径，公开调用者无法通过正常API构造或复用诊断请求。

薄体frame守卫按规范内建对象的实际身份和有效调用形状工作。标准库exact partial经
公开func/args/keywords合并参数；显式globals/locals与dont_inherit=True沿真实内建执行。
用户buffer协议由真实内建取得一次，守卫只检查能力，避免额外__buffer__与release回调。
隐式frame依赖且尚未物化的参数协议明确诊断；合法显式环境的参数协议保持原能力。

最终macOS arm64的3.12.13/3.13.3×JIT/NativeAOT四组，每组19正与1个真实跨堆环负例
通过：68个授权stock进程与8个未启动/未授权对照共76正，4个不可见环保留真实ledger/
Roots。所有正例的native/core/H/ledger/queue/late/post/entry/internal-proxy/terminal-pool
终态为零；双atexit、共享alias、构造失败、真实Report与SystemExit字符串均已覆盖。
日志位于 `~/.cache/ryupy/validation/adr0228/`，最终四标识从
`20261117T095727.580504Z-59948`到`20261117T095727.638628Z-59951`。
