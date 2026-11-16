# ADR-0220：编译模块原生 finder/loader 首切

- 日期：2026-11-16
- 状态：pending，待实现与真实产品验收
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

## 待验

先登记 pending 输入，覆盖真实包/空包、父包顺序、循环、重复身份、失败重试、None/替换及
关闭时首次反向导入。三版本真实 shim 与类桥、固定 CPython 同次 JIT/AOT、删除 input/build
后的搬迁、生命周期及帧审计分别记录。已删除 class/member 的内部 `<string>` 帧必须为零，
默认 wrap/method 帧仍活跃；审计 complete/startup_complete 保持现有 false 合同。
