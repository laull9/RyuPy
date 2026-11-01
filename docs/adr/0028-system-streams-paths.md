# ADR-0028：受控系统模块、原生流与路径适配

状态：已接受。M3.4 整里程碑聚合门禁与最终只读复审通过。

## 背景

文件、标准流和路径是实际项目的基础依赖，不能把 .NET 同名 API 直接当作 Python 兼容。
模块导入、API 差分与固定项目通过分别统计；本轮不引入 libpython 或宿主反射调用。

## 决定

运行时提供有限的 PyNativeObject / PyNativeType 与绑定签名适配，复用动态调用、
属性、迭代与异常协议。类型对象按模块注册缓存身份；回调的 CLR null 统一为 Python None，
不能流入表示未绑定模块变量的 null 槽。缺失 API 报 AttributeError，已知不支持选项明确报错。
代码生成器只引用有限运行时 ABI，不理解文件系统；HIR/MIR 不依赖宿主文件实现。

sys.argv 使用实际入口路径与 Main 参数；traceback 文件名仍独立保存，不能为快照把真实 argv[0]
改成 basename。sys.modules 是只读已注册模块视图，sys.path 是只读入口目录元组，
不宣称支持运行时搜索路径修改、import hook 或解释器内部对象；编译期模块定位保持原有规则。

无关键字的静态 print 保留类型化写入，不为每个基本值构造参数数组；实参全部求值后
捕获一次目标流，之后 __str__ 重绑定 sys.stdout 不改变本次目标。动态 print 的 flush 真值
转换先于写入及 sep/end 校验，目标为 None 时不写入；input 只移除 readline 返回值最后一个 LF。
退出入口调用 FlushForExit：所有当前输出流均尝试刷新，以引用去重，不 close 用户对象；
任一刷新失败改为退出码 120，仍刷新另一条流。此为受控单次刷新策略，不保证 CPython 多阶段
finalization 的精确 flush 次数。显式 with/close 决定文件清理，GC 关闭时机继续受 D001 限制。

文件以 FileStream 为底层，Python 侧适配模式、编码、换行、错误和定位。文本读取逐码点推进，
避免预读导致 tell 与写入位置错误；文本 seek 仅支持零偏移及本流发出的 cookie。
StringIO 保存码点单元，BytesIO 保存字节。参数验证在破坏性打开前完成，初始化失败释放新句柄。
路径词法明确按 POSIX 验证；Path.absolute 保留 ..，os.path.abspath 才做词法归一化。
目录迭代第一次 next 时获取快照，不保留宿主枚举器句柄。环境映射与 putenv 的直接宿主修改
分别实现，避免错误地让 putenv 回写 os.environ。

## 代价与未支持范围

FileStream 缓冲不等于整个文本层高性能：逐码点 ReadByte/GetString 与 StringIO 的
List<string> 分配成本明显，本轮未测加速收益。后续块式增量解码/紧凑缓冲必须保持换行、
cookie、异常时机与副作用。动态调用有绑定数组成本，静态 print 不承担这一成本。

详细参数与逐 API 状态见 [系统 API 表](../stdlib-system.md)。UTF-8/ASCII/Latin-1 严格模式之外、
任意文件描述符/自定义 opener、完整 io 类层级、buffer/raw/detach/fileno 等接口未支持。
不可读/不可写目前报 OSError，尚非同时继承 ValueError 的 io.UnsupportedOperation。
文件权限错误映射有可注入测试，当前 root 执行环境不能用 chmod 假称实测非特权权限隔离。
Windows 词法路径明确拒绝并有注入测试；实际宿主端到端仅 Linux/POSIX，Windows 文件与
退出整数 C-long 宽度仍需独立验收。bytes 路径、复杂 glob、递归 glob 与自定义权限模式未支持。
受控模块/环境的 keys/items 返回快照，不承诺完整可变 dict 或 live view 协议。

## 验证

先 pending 后固定 CPython 3.12.13 与真实编译差分，覆盖临时目录、文本/二进制、
Unicode/换行、with 及异常清理、环境变量、argv、标准流替换、print/input 和退出异常。
驱动子进程另验落盘、刷新失败 120、双流失败隔离、不隐式关闭及 SystemExit 状态。
完整门禁与固定项目统计见本轮实现笔记；不能用这些语法/API 用例推算 80% 项目覆盖。

完整 make runtime / make check：209 C#、98 编译 e2e / 16 ignored、110 CPython 基线通过。
固定项目仍原生 0/20；binaryornot 首阻断 open → locals，不计项目通过。
