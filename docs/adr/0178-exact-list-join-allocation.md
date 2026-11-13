# ADR-0178：精确内建list的join去掉重复引用快照

状态：候选，真实Native A/B与部署回归待验，未合入dev/main。

## 原因与单一变量

Native pyfiglet采样CI43335125457显示拼接/拷贝/子串分配成为主要热点。锁定上游pyfiglet 1.0.2的633–639行先`list(addLeft)`、修改后`''.join(l)`；字体加载和最终缓冲合并也使用列表。当前join先由ToSequence复制object数组，再建立string数组，随后BCL再次扫描并拼接。

只对sealed内建PyList，以CollectionsMarshal.AsSpan借用`list.Items`已经完整物化的背板，省掉第一个临时引用数组；保留string数组、全部类型校验及BCL.string.Join。其它iterable不变，不更改PyText.Items或字符串身份，不同时重写拷贝算法。

## 安全边界

先访问Items，保留数值背板完整物化和缓存盒身份。Span只在同步函数内用于类型检查及读取readonly字符串子类Value，不调用用户方法、不修改或交出底层列表，不跨await。CLR分配/GC不会使托管Span失效；不承诺外部.NET线程同时修改容器的并发行为。

PyListInstance子类不走快路；普通iterator仍完整耗尽后才检查元素，因此后续迭代异常优先于早期非字符串。单项/空项继续交给同一BCL，错误不调用用户str转换。已有list子类__iter__覆盖和str子类__len__覆盖准入缺口保持拒绝/独立pending，不能当作本次新兼容性。

## 验证与取舍

专项锁定Unicode/孤立代理原值、单项身份、源列表不变、子类载体、非法元素、iterator异常顺序及数值背板失败路径；独立线程分配计数只证明减少临时数组，不作为Native运行提速。

复用既有同机五负载A/B，基线b6d92e95，runtime源码只允许PyTextMethods.cs变化。同一当前默认Release CLI分别链接两份runtime、真实Native公开项目、逐字对照CP、各负载40交替样本、启动/产物大小/独立RSS及不利结果全部保留。Mac/Windows和Linux资源/metadata/搬迁容器继续实际验证。若真实收益不成立或成本明显退化，不因局部数组减少而宣称成功。
