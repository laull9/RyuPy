# ADR-0183：显式 CPython provider 与封闭分发计划

- 日期：2026-11-13
- 状态：已接受；macOS arm64 首条编译入口的 JIT / NativeAOT / 搬迁通过，完整协议、联合应用与 NumPy 由独立验收驱动继续执行。

## 决定

`--cpython-provider provider.json` 是显式试点入口。声明 schema 1、CPython 3.12.13、基础发行根、
固定来源、顶层模块族与额外包载荷。每个顶层模块族整体交给 CPython，避免在共享模块状态、异常
身份和原生扩展状态之间拆分归属。入口 `__main__` 始终经过 RyuPy syntax → HIR → ty → MIR → CIL；
依赖源码由 CPython 导入器执行，编译进程不加载 Python 或用户扩展。

已有导入降级使用 `ImportModule` / `ImportAttr` 和动态调用；driver 对 CPython 域取消原生异常
导出身份快捷证明。来源报告将 CPython 模块标为 `cpython`，该域内部依赖保留由完整基础发行与
显式包载荷承接，避免编译期扫描和解释依赖的语法。

MIR 只增加中性 `external_provider` 部署元数据，codegen 根据明确合同静态组合
`RyuPy.CPython.Bootstrap.Start(string, string[])` 与 `Finish(int)`；后端不发现包、不读取 HIR，
也不选择 provider。Start 位于系统参数初始化后的异常保护区，Finish 在 finally 中执行宿主关闭，
所有应用退出路径共用该合同。Runtime 定义中性对象与模块 provider 合同，独立 CPython 项目单向依赖它。

## 固定发行

首个平台冻结为 macOS arm64、常规 GIL CPython 3.12.13、python-build-standalone 20260623：

- [官方发布](https://github.com/astral-sh/python-build-standalone/releases/tag/20260623)。
- [确切 install_only 归档](https://github.com/astral-sh/python-build-standalone/releases/download/20260623/cpython-3.12.13%2B20260623-aarch64-apple-darwin-install_only.tar.gz)。
- [官方归档与构建元数据说明](https://docs.astral.sh/python-build-standalone/distributions/)。

`install_only` 是完整构建归档的安装前缀；固定发布修订和上游构建工程给出可重建来源。
声明必须匹配 BUILD 及版本头，分发包含 lib、include、share、BUILD、许可证，以及显式选择的
包、dist-info 和包资源。基础发行中的 libpython 已内建 `_sqlite3`、zlib、`_ssl`；缺少独立
`_sqlite3.so` 的负例须按该发行事实设计，不能声称存在未交付的文件。

开发机 uv 管理的同版本基础发行可以作为显式构建输入，venv 拒绝。运行时不搜索该开发目录，
不读取系统 Python、不下载依赖。`cpython-provider.json` 记录确切上游归档、版本、RID、
模块归属、包载荷、文件相对路径与大小；普通文件不计算摘要。

macOS 发布逐个审计 dylib / 扩展映像；只允许 `/usr/lib/` 和系统 Framework 白名单，
其他原生边必须在同一分发内唯一解析。依赖重写为 `@loader_path` 相对路径，dylib ID 改为
`@rpath/<name>`，改动后重新进行 ad-hoc 签名。发行源内符号链接解引用复制，越过授权根与
目录环直接拒绝。第三方包自身附带的原生库随包载荷统一审计，缺闭包阻断发布。

JIT 使用外部候选目录，编译和冻结完成之后替换本工具已有输出；失败保持上一份可运行产物。
AOT 复用既有 `<entry>.build/` / `<entry>.dist/` 事务，发布前加入相同 CPython 配置和依赖树。
`ryupy-cpython.json` 的所有加载路径相对可执行文件旁配置解析，原位和搬迁使用同一计划。

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

## 验收边界

新增手写输入和可复现驱动由 `tests/cpython/`、`tests/aot/stdlib_service/run_provider.py`
维护。预期使用固定 CPython 当次生成，所有构建、日志和数据库写到仓库外并在结束时清理。
必须逐项记录 CP1a、CP1b、CP2、CP3 的 JIT、NativeAOT、搬迁、负例与干净机结果。
默认配置晋升仍受 ADR-0181 全部门约束，Linux / Windows 使用独立来源与平台加载合同验收。
