# ADR-0183：显式 CPython provider 与封闭分发计划

- 日期：2026-11-13
- 状态：已接受；macOS arm64 首条编译入口的 JIT / NativeAOT / 搬迁通过，完整协议、联合应用与 NumPy 由独立验收驱动继续执行。

## 决定

`--cpython-provider provider.json` 是显式试点入口。声明 schema 1、CPython 3.12.13、基础发行根、
固定来源、额外顶层模块族与包载荷。`stdlib_provider="cpython"` 冻结全部标准库：driver 静态读取
上游 PYTHON.json 的扩展模块键、Lib 顶层源码/普通包及固定 CP312 扩展后缀，排除 site-packages；
打包的第三方导入族单独记录。每个顶层模块族整体交给 CPython，避免在共享模块状态、异常
身份和原生扩展状态之间拆分归属。入口 `__main__` 始终经过 RyuPy syntax → HIR → ty → MIR → CIL；
依赖源码由 CPython 导入器执行，编译进程不加载 Python 或用户扩展。应用源码若遮蔽固定
标准库名称，当前试点以 E0100 和冲突路径拒绝，避免初始化期间双份权威；原生配置保留原解析。

已有导入降级使用 `ImportModule` / `ImportAttr` 和动态调用；driver 对 CPython 域取消原生异常
导出身份快捷证明。来源报告将 CPython 模块标为 `cpython`，该域内部依赖保留由完整基础发行与
显式包载荷承接，避免编译期扫描和解释依赖的语法。

MIR 只增加中性 `external_provider` 部署元数据，codegen 根据明确合同静态组合
`RyuPy.CPython.Bootstrap.Start(string, string[])` 与 `Finish(int)`；后端不发现包、不读取 HIR，
也不选择 provider。Start 位于系统参数初始化后的异常保护区，Finish 在 finally 中执行宿主关闭，
所有应用退出路径共用该合同。Runtime 定义中性对象与模块 provider 合同，独立 CPython 项目单向依赖它。
显式provider从同一Bridge构建输出根取得Runtime、Bridge、版本props和托管/原生依赖，
Bridge显式复制传递NuGet资产；普通模式仍使用原runtime定位规则。Runtime/Bridge使用
embedded PortablePDB，使符号与DLL一起部署。构建宿主组件与产品打包按顺序执行；
跨进程并发覆盖整个Bridge输出根的事务与同步锁仍需独立设计，当前不承诺跨文件同代快照。

## 固定发行

首个平台冻结为 macOS arm64、常规 GIL CPython 3.12.13、python-build-standalone 20260623：

- [官方发布](https://github.com/astral-sh/python-build-standalone/releases/tag/20260623)。
- [确切 install_only 归档](https://github.com/astral-sh/python-build-standalone/releases/download/20260623/cpython-3.12.13%2B20260623-aarch64-apple-darwin-install_only.tar.gz)。
- [官方归档与构建元数据说明](https://docs.astral.sh/python-build-standalone/distributions/)。

`install_only` 是完整构建归档的安装前缀；固定发布修订和上游构建工程给出可重建来源。
声明必须匹配 BUILD、版本头以及同发行完整归档中的 PYTHON.json v8（schema字段为字符串）、
目标 triple、pgo+lto 与 shared 构建选项。install_only 自带的 CPython LICENSE 不覆盖全部底层组件；
必须显式补齐上游完整归档的 PYTHON.json 和全部 licenses 文件，并逐项进入 source.licenses。
分发包含 lib、include、share、BUILD、PYTHON.json、组件许可证，以及显式选择的
包、dist-info 和包资源。基础发行中的 libpython 已内建 `_sqlite3`、zlib、`_ssl`；缺少独立
`_sqlite3.so` 的负例须按该发行事实设计，不能声称存在未交付的文件。

开发机 uv 管理的同版本基础发行可以作为显式构建输入，venv 拒绝。运行时不搜索该开发目录，
不读取系统 Python、不下载依赖。`cpython-provider.json` 记录确切上游归档、版本、RID、
模块归属、包载荷、文件相对路径与大小；普通文件不计算摘要。

macOS 发布逐个审计 dylib / 扩展映像；只允许 `/usr/lib/` 和系统 Framework 白名单，
其他原生边必须在同一分发内唯一解析。依赖重写为 `@loader_path` 相对路径，dylib ID 改为
`@rpath/<name>`，改动后重新进行 ad-hoc 签名。发行源内符号链接解引用复制，越过授权根与
目录环直接拒绝。第三方包自身附带的原生库随包载荷统一审计，缺闭包阻断发布。

首个macOS发行的CA政策为 `system_default`：使用该固定PBS/OpenSSL构建的
`/private/etc/ssl/cert.pem`、`/private/etc/ssl/certs`，来源是宿主系统证书数据，随包不复制CA。
`SSL_CERT_FILE` / `SSL_CERT_DIR` 保留CPython/OpenSSL的显式覆盖合同；清单将这项数据依赖与
Mach-O系统库白名单分别登记。固定CPython 3.12.13的 `ssl.get_default_verify_paths()` 已读取
上述路径，方法语义见[官方ssl说明](https://docs.python.org/3.12/library/ssl.html#ssl.get_default_verify_paths)。
应用可用显式SSLContext选择自己的证书；本轮不新增证书下载或TLS配置层。
运行时不会下载编译器、解释器或第三方依赖；应用自己的网络操作保留Python协议。
当前联合HTTP门只验证loopback HTTP，HTTPS握手、证书链和异机系统CA可用性尚待独立验收。

JIT 使用外部候选目录，编译和冻结完成之后替换本工具已有输出；失败保持上一份可运行产物。
AOT 复用既有 `<entry>.build/` / `<entry>.dist/` 事务，发布前加入相同 CPython 配置和依赖树。
`ryupy-cpython.json` 的所有加载路径相对可执行文件旁配置解析，原位和搬迁使用同一计划。

直接C绑定与CPython provider可在同一应用组合：先冻结provider执行域，再编译绑定源码闭图，
生成的绑定模块、托管桥、CPython和直接C动态依赖进入同一个候选目录，JIT/AOT复用各自既有
发布事务。绑定模块名若与固定标准库或CPython依赖族重叠，发布前拒绝并要求选择唯一归属。
执行域计划覆盖整个候选构建，成功与失败均恢复调用者Session，避免后续构建继承临时配置。

## 精确容器布局与身份

默认 `container_transfer="proxy"`。可变托管对象以代理跨域，保留身份和可观察别名。
标准库和扩展中依赖精确 list / dict 布局的消费者需要显式
`container_transfer="synchronized"`：通用身份图物化真实 Python 容器，在每次调用边界同步修改，
包括回调重入和异常退出。不得按包名选择特例，也不得在默认代理模式暗中复制。

同步身份图必须保持共享子对象、容器环和往返对象身份；tuple 的不可变封套仍保持身份及元素别名。
CPython、托管双方对同步容器的改动在跨域边界按明确顺序写回。异步后台线程在边界之外持有并
并发修改同步容器属于域外，必须明确拒绝或先提供长期租约和冲突协议。
同步与跨堆自动 GC 是不同合同；回调和 Python 对象形成的跨堆环仍按 ADR-0181 的登记、注销、
显式断环和关闭清理要求独立验收。

## 富比较与多维切片

动态比较新增独立 `DynRichCompareValue` → `PyOps.CompareValue(int,object,object)->object`，
保留 ndarray 掩码和自定义对象的原始结果；旧 bool 比较 ABI 与精确静态比较继续保留。
操作码沿既有 Eq=1 至 Ge=6，分支位置另行求真。模块全局槽位的比较类型只在精确静态
操作数可证明时固定 bool，其他情况保留 Dynamic。

HIR 的 CompareChain 记录有序操作数，MIR 用基本块短路并冻结中间值。每个操作数最多
求值一次，只有中间比较求真，最终比较对象保持身份。闭包保存、yield 跨暂停和异常顺序
由手写差分输入覆盖。tuple 内 Slice 复用既有 Slice / MakeSlice，按 start、stop、step
从左至右各求值一次，没有按 NumPy 名称加入专用降级。

## 验收边界

新增手写输入和可复现驱动由 `tests/cpython/`、`tests/aot/stdlib_service/run_provider.py`
维护。预期使用固定 CPython 当次生成，所有构建、日志和数据库写到仓库外并在结束时清理。
必须逐项记录 CP1a、CP1b、CP2、CP3 的 JIT、NativeAOT、搬迁、负例与干净机结果。
默认配置晋升仍受 ADR-0181 全部门约束，Linux / Windows 使用独立来源与平台加载合同验收。
