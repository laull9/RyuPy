# io.UnsupportedOperation：双父异常竖切设计

当前状态：运行时、稳定导入身份和静态 catch 展开已集成；固定 CPython 3.12.13 两条差分、18 项相关运行时专测、两项 driver 测试已通过。完整工作树门禁仍由 M3.7 聚合验收执行。

## 类型与捕获

Python 的真实继承顺序是 UnsupportedOperation → OSError → ValueError → Exception → BaseException。CLR 单继承无法直接表达，因此采用 **CLR 主树 + 明确额外父边的静态 catch 展开**：

1. PyUnsupportedOperation 真正继承 PyOSError，用户异常子类仍生成真实 CLR 派生类。
2. Python 异常层次表登记 UnsupportedOperation 的 ValueError 额外父边；供 isinstance/issubclass 和生成器逻辑 handler 匹配。
3. MIR 在每个源 handler 的原位置，将精确内建 ValueError 展开为 ValueError、io.UnsupportedOperation 两项；为每项保留既有独立 trampoline，合流同一 handler 正文。不把补充 catch 放到所有 handler 末尾，不改变 first-match 顺序。
4. 元组类型按发现顺序去重；bare except、Exception、OSError 不作额外展开。生成器已有逻辑类型匹配复用上述层次表。

不能只修改 isinstance 而遗漏 CLR catch，也不能为此放宽任意动态 except 类型表达式。此模式是已登记原生异常的有限适配，不是一般 Python 多重继承支持。

## 静态导入身份

只接受 driver 确认无本地源码遮蔽的稳定 io 导入及 from-import 别名。复用现有原生异常身份保护；对参与 typed handler 的导入重绑定保持诊断，禁止原生 io.UnsupportedOperation 被改写。编译器内部名称使用 io.UnsupportedOperation，公开类型短名仍为 UnsupportedOperation。

用户类仅允许以该已确认身份继承；不按属性拼写承认任意对象基类。类工厂复用现有异常派生/protected 构造 ABI。

## 构造与流协议

首批支持无参和单个消息参数，保留参数身份，构造不执行用户 `__str__`；显示异常时才格式化。OSError 的 errno/strerror 多参数构造尚未实现，超过一个参数明确 NotImplementedError，用户派生类的默认构造同样限制，不静默丢参。

- 文件/标准流的既有 not readable/not writable 能力错误改为该类型。
- 文本文件非零相对 seek 属于该类型；StringIO 的非零相对 seek 仍为普通 OSError，不能一刀切替换。
- StringIO/BytesIO.fileno() 抛 UnsupportedOperation('fileno')，即使已关闭也仍为该能力错误。
- 关闭后的一般 I/O 操作保持 ValueError，参数校验优先级不改。
- 不因注册异常而假装实现标准流 fileno、os.isatty 或真实文件描述符接口。

## 成本与验收

每个包含内建 ValueError 的源 handler 多一个明确 typed catch/trampoline，增加 EH 元数据和生成代码体积（跨模块调用也可能抛此异常，因此不能仅在导入 io 的模块展开），不把普通异常路径全改成动态 catch-all。生成器复用既有逻辑 EH；不声明无成本。类型化 catch 与 Python 层次表必须一起维护。

`m3_7_io_unsupported.py`：导入别名、两父 isinstance/issubclass、互换 handler 顺序、元组重复去重、用户派生类、yield 后抛出和外部 throw、真实内存流 fileno。`m3_7_io_unsupported_shadow.py` 验证用户同名 ValueError 不是内建父类。PyUnsupportedOperationTests：CLR 主链/双父图、构造惰性与参数身份、关闭后的 fileno。driver `io_unsupported` 已覆盖稳定导入/重绑定及本地 io.py 遮蔽；termcolor 固定项目仍须复测下一阻断。

## 跨模块同名异常身份

普通 typed catch 中的用户异常引用在 MIR 链接时改写为本模块的内部限定类型名，CIL 引用表不再用用户裸名覆盖内建异常。Python 可见 `__name__` 和异常消息不改变；本模块用户类遮蔽同名内建类时仍捕获该用户类，而另一个模块的同名类保持独立。

生成器逻辑 handler 从编译期传入的用户类全局槽读取实际 PyClass，不能把用户类名交给内建 TypeObject 工厂。槽 ID 随链接正常重映射；内建和已登记原生异常仍走原类型对象路径。此修复不增加一般动态 except 能力。

三模块 `m3_7_exception_identity` 验证入口内建 ValueError/io 双父、两个模块的同名用户异常各自捕获和互不误捕获，以及生成器 throw 注入两种同名类；固定 CPython 3.12.13 差分已通过。新 driver CIL 快照同时保留内建与各用户类型引用。
