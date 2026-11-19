# ADR-0230：薄函数局部身份分支与入口弱登记

- 日期：2026-11-17。
- 状态：已验收显式子合同；两版三形态18/18产品与完整门禁通过，默认关闭。
- 接续：[ADR-0229](0229-stock-thin-compiler-cold-abi.md)、[ADR-0228](0228-stock-compiled-program-lifetime.md)。

## 决定

保持显式 `--experimental-stock-thin-functions` 与中性冷 ABI，扩大同步顶层
`def f(x)` 的静态 CIL 正文：单个局部名称赋值、名称与 `None` 值、单个 `is/is not`
比较、`if/elif/else`、条件表达式、空返回和隐式 `None`。资格检查递归检查全部分支，
包括不可达正文。调用、算术、属性、循环、闭包、装饰器、注解和复杂参数继续明确诊断。

词法局部判定沿用 HIR，动态局部的未绑定状态沿用已有运行时合同；赋值前读取及
未走初始化分支均须产生 `UnboundLocalError`。身份比较按原求值顺序执行，禁止触发
用户真值回调。全局读取经实际活动 lease 查原字典，保持晚绑定和名称异常。
MIR 显式列出该正文允许的运行时操作，lease 只能借给全局读取，禁止别名和逃逸。
后端继续只消费 MIR，NativeAOT 无运行期 IL 生成。

长入口改用 token 对应的弱 owner/holder 登记，短回调继续使用确定释放的强 scope。
ledger 的退账仍由真实 `Release` 完成。入口结束先在无 GIL、无 gate 时等待托管
finalizer，再冻结存活 owner 的强快照与 token 集合；终态预检拒绝外部根与未完成退账。
既有普通 Host 拒绝关闭、正常 atexit、析构重入及原生 owner 回收合同继续保持。

## 验收

结构门、同次 CPython、真实 JIT/NativeAOT/OneFile 删除输入搬迁分别记录。
长循环内存采样与正式墙钟测量分开，旧 HEAD 和当前候选使用相同输入与工具链。
运行期峰值、终态归零和吞吐三个结论各自需要实测证据。

两版各三个手写输入 × JIT/NativeAOT/OneFile 共18/18，通过同次CPython三流差分、
删除输入搬迁及精确120次静态调用/54次owner析构核验。真实NameError.name的Unicode
查询名与UnboundLocalError的type/name/args/str均通过。入口生命周期另有56/56门。
完整检查3424 Runtime、503编译e2e通过。RSS与正式耗时详见
[第四轮验收](../cil-execution-fourth-slices-validation.md)。完整函数体和模块闭包继续实施。
