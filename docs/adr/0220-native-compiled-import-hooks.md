# ADR-0220：编译模块原生 finder/loader 首切

- 日期：2026-11-16
- 状态：实现完成，三版本桥门通过；等待本轮最终产品冻结产物复验
- 接续：ADR-0208、ADR-0213

## 决策

使用静态 C heap type 替换 `_RyuPyFinder`、`_RyuPyLoader` 及三个 Python 成员函数，
以真实 CPython ModuleSpec 类构造规格；标准库仍归原兼容 provider。
create_module 回调现有 Runtime Import 单点事务并返回同一模块，exec_module 保持无正文操作。
不同时迁移 Runtime 两阶段模块事务，禁止正文二次执行；标准 PEP451 时序另切。

直接缓存真实 property/staticmethod/classmethod C 类型，移除三个 Python 别名。
默认 LegacyMethodWrapper 保留；原生方法候选不晋升。rp_install_imports 旧 ABI 保留，
产品只传默认 wrapper 源码，候选空源码跳过 PyRun；原生钩子通过独立固定 ABI 安装。

包能力优先读取真实注册 is_package 标记，仅旧无标记注册保留原前缀兜底。
该查询不初始化模块、不访问公开属性或用户协议。CPython 自有 sys.modules 缓存、None 与替换
仍由真实 CPython importer 消费；Runtime 维持自己的已注册模块唯一初始化事务。

## 固定接口与所有权

新增固定 Cdecl ABI `rp_install_native_imports(void)` 与 `rp_descriptor_type(int kind)`。
两者成功均返回 owned `PyObject*`，失败返回 NULL 并保留真实 Python 异常；调用方持 GIL。
安装操作重复调用返回 None；描述符编号固定为 1=property、2=staticmethod、3=classmethod，
其他编号报 ValueError。这两个入口没有字符串参数。旧 `rp_install_imports(const char*)`
继续接收调用期间借用、NUL 终止的 UTF-8 源码；空字符串跳过求值，已有非空源码 ABI 保留。
无消费者的 `rp_op(29)` 保留入口与参数布局，固定返回 NotImplementedError。

中性 `PyModules.TryGetRegisteredPackage(name, out isPackage)` 只读已注册 source/InstanceCode
包标记；未知名字与旧无标记注册返回 false，由 CP 桥保留旧前缀兜底。它不创建或导入模块。
finder/loader 通过原有 dispatch 20/21/22 检查登记、初始化和读取包事实，没有新增动态回调签名。

每次 find_spec 返回真实 `_frozen_importlib.ModuleSpec` 与独立 loader；name、parent、包搜索列表、
origin、cached、loader_state、has_location 继续使用该真实类的既有规则。finder 实例持有真实
bootstrap 模块与 loader 类型引用，GC traverse/clear/dealloc 覆盖 heap type 与字段。
关闭先释放静态缓存，sys.meta_path 中仍活动的 finder 自持引用，atexit 首次导入继续可用。

## 执行边界

CPython 的 sys.modules None、任意替换值、父包绑定和导入失败清理继续由真实 importer 消费。
循环导入通过 Runtime 缓存取得同一正在初始化的模块；初始化失败由原事务移除缓存后重试。
create_module 已完成 Runtime 正文，exec_module 重复调用保持空操作。reload、删除 CP 缓存后
重建正文等完整 PEP451 两阶段事务留待独立竖切，避免本轮产生第二次初始化。

默认路径仅剩 LegacyMethodWrapper 的顶层 factory 与转发闭包；两个 Python 类、三个类成员、
三个内建描述符别名从产品安装源码移除。真实 ModuleSpec 构造、兼容 provider 源导入和 CPython
标准库仍产生执行帧，C0 的 complete/startup_complete 继续为 false。
显式 native-methods 候选传空源码；候选策略、精确 FunctionType/code/frame 边界保持 ADR-0211。

## 已验与最终门

先登记 pending 产品输入，旧实现真实类桥规格断言失败后实现。3.9.25、3.12.13、3.13.3
各自重新构建 shim，默认/候选各跑 33 项真实类桥，共 198 项通过；六个源码输入同次 CPython
oracle 全过。每版本独立 C 小宿主还验证了修改 builtins 后的真实描述符身份、旧源码导出 ABI
及 rp_op(29) 拒绝执行。独立共享库宿主避免静态解释器与共享 libpython 的双份类型身份。

产品初验已覆盖包/空包、父包顺序、循环、重复身份、失败重试、sys.modules None/模块与普通对象
替换、关闭时首次导入。默认与候选分别通过固定 3.12.13 同次 oracle、JIT、NativeAOT、删除
input/build 后的中文空格路径搬迁及私有分发隐藏。最终冻结 CLI 后再次执行相同产品门。
帧门只匹配原内部 `<string>` 源码：已删除两个 class 与三个成员帧为零；默认 wrap/method 帧
必须活跃，候选二者为零。三个生命周期帧数均为正且无丢帧，严格审计门保持原样。

本切新增的 inspect profile 接线由 ADR-0219 定义：配置显式启用、受信启动先捕获、关闭清理，
独立 header 与静态 P/Invoke 经公共桥合流；finder/loader 本身不依赖该 profile。
