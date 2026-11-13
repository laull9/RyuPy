# ADR-0178：精确内建list的join去掉重复引用快照

状态：分配优化候选，正确性/三平台回归已验；真实Native A/B两轮只有一轮达到预设3%可靠收益门，未合入dev/main。

## 原因与单一变量

Native pyfiglet采样CI43335125457显示拼接/拷贝/子串分配成为主要热点。锁定上游pyfiglet 1.0.2的633–639行先`list(addLeft)`、修改后`''.join(l)`；字体加载和最终缓冲合并也使用列表。当前join先由ToSequence复制object数组，再建立string数组，随后BCL再次扫描并拼接。

只对sealed内建PyList，以CollectionsMarshal.AsSpan借用`list.Items`已经完整物化的背板，省掉第一个临时引用数组；保留string数组、全部类型校验及BCL.string.Join。其它iterable不变，不更改PyText.Items或字符串身份，不同时重写拷贝算法。

## 安全边界

先访问Items，保留数值背板完整物化和缓存盒身份。Span只在同步函数内用于类型检查及读取readonly字符串子类Value，不调用用户方法、不修改或交出底层列表，不跨await。CLR分配/GC不会使托管Span失效；不承诺外部.NET线程同时修改容器的并发行为。

PyListInstance子类不走快路；普通iterator仍完整耗尽后才检查元素，因此后续迭代异常优先于早期非字符串。单项/空项继续交给同一BCL，错误不调用用户str转换。已有list子类__iter__覆盖和str子类__len__覆盖准入缺口保持拒绝/独立pending，不能当作本次新兼容性。

## 验证与取舍

专项锁定Unicode/孤立代理原值、单项身份、源列表不变、子类载体、非法元素、iterator异常顺序及数值背板失败路径；独立线程分配计数只证明减少临时数组，不作为Native运行提速。

复用既有同机五负载A/B，基线b6d92e95，runtime源码只允许PyTextMethods.cs变化。同一当前默认Release CLI分别链接两份runtime、真实Native公开项目、逐字对照CP、各负载40交替样本、启动/产物大小/独立RSS及不利结果全部保留。Mac/Windows和Linux资源/metadata/搬迁容器继续实际验证。若真实收益不成立或成本明显退化，不因局部数组减少而宣称成功。


## 实际结果（2026-11-13）

`312bae71` / `5def7978`同树完整check通过，C#3058/5skip、e2e455/284ignored。CI43343119605两轮各440计时/预热及10 RSS原流、源码冻结、唯一runtime差异与统计独立复核通过。极长hi800中位下降2.57%/5.75%，配对比95%区间为[1.02703,1.03544]/[1.06904,1.11791]；仅第二轮越过既定下界1.03。其余负载无可靠3%收益，startup第二轮略慢、hi首轮略慢的结果保留。

两台host分别AMD EPYC7763/9V45，不能混池；native/dist各+8B。长负载单次RSS略增，第二轮startup单次RSS增约3.80MB；采样不足以认定稳定峰值内存变化，不宣称RSS降低。三平台pyfiglet及binaryornot全部原流对齐CP，只有Linux有禁网无运行时容器证明。完整数字、CI链接及ZIP摘要见[真实Native对照记录](../../tests/aot/native_text_index_perf/README.md)。

本次保留为已验正确性的减分配候选，不把第二轮更有利结果替代首轮，也不以临时数组减少推导一般应用/Nuitka优势或自动晋升到dev。
