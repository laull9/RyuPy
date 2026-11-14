# 从真实项目首错推进通用正则 split

## 为什么选择此切片

真实dict派生之后，docopt原入口已编译成功，但在printable_usage调用re.split时报缺属性。另一次应用户请求进行的稳定e066238完整矩阵确认默认9/20入口通过，docopt仍是同一首错；pyfiglet字体模块选项单列后可覆盖10/20。矩阵保固定版本、源、入口与golden，不把各历史候选成功简单相加。

## 学到的语义

不能直接使用.NET Regex.Split。Python会把所有捕获插入结果，未参与组是None；空匹配之后允许同位置非空分支重试。maxsplit计分隔匹配数，达到限制必须立即停止，以免尾部继续执行昂贵匹配。两输入域都接受ssize_t，text不能沿旧Number缩成C int。

模块函数与Pattern方法来自不同绑定实现，其联合错误的优先级和文本并不相同。本次只给split局部绑定，不为一个API改动全部函数调用。用户index可能resize或release原subject，因此先执行一次index再验证并取得租约，正文异常不广泛catch重包。

## 实现与成本

新raw循环复用后端和非空重试辅助模式，不创建公开Match或全匹配列表。text直接切UTF16范围，零宽前进只跳真实代理对；bytes/mutable一次Latin1适配，mutable直接复制最终片段。一次lease由using覆盖所有返回和异常路径；受控executor安全验证第二次后端失败后的两级释放，不用灾难回溯当测试。

旧消费者不迁移，不改变旧对象字段，仍不能断言CPU零成本。每操作列表/绑定、宿主匹配与每组ryuN名字查询有真实成本；分配和吞吐以同host/CIL测量为准。新split无旧实现，不能杜撰旧新提速百分比。

## 原失败不能改口

CP-first源先冻结。旧runtime三主全缺API，新runtime首轮只有tokens通过；buffers依赖匿名父view即时回收，binding先创建非连续view即旧NI。保原v2源/golden/失败后，v3明确release父view并将旧边界另列pending，再按原CP语义验证3/3，不将v3归为v2成功。

独立17观察最初也只有14一致。两项是源字面量孤立代理被ruff替换，一项是旧view构造限制。另立支持域源与chr动态代理源，先CP再JIT严格对齐，既确认split本身行为，也保留前端/对象生命周期欠账。C#内部固定常量期望独立于生产text结果，不用实现生成自己的答案。

## 尚未闭合

宽或多维buffer、非连续view、任意exporter和原生子类、高阶index、bool返回warning、既有flags和非法pattern长尾仍有明确边界。只验固定Linux JIT入口，不外推上游全测试、完整re或跨平台/AOT。最终门禁、成本和新docopt窗口见ADR0107的完成记录。
