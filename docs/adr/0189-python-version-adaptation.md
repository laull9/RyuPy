# ADR-0189：按用户完整 Python 版本自动冻结 CPython provider

- 日期：2026-11-14
- 状态：已实现；macOS arm64 三个版本真实验收通过。

## 决定

`--cpython-provider auto` 在构建期选择项目及祖先 `.venv`/`venv`/`env`、
激活环境、PATH。选中的环境损坏或不受支持即报错，保持原诊断。
纯 native 默认入口及编译链不变；auto 使用 synchronized 调用边界物化与写回，
显式 schema 1 默认保留 proxy。应用入口始终编译为 CIL。

探针使用 `-I -S -B`，不执行 site 或任意 `.pth`。3.9–3.13 的 `-S` 会改变
venv prefix，因此从原始选中 exe 的 bin/Scripts 父级读取 pyvenv.cfg 定位环境，
随后统一规范化环境根与依赖源边界。依赖版本、RECORD、ABI 错误直接传播。

接收正式版 CPython 3.9–3.15、当前目标 64 位机器、常规 GIL；核对 implementation、
releaselevel、完整数字版本、Py_DEBUG、Py_GIL_DISABLED、SOABI。拒绝预发行、
开发后缀、debug、free-threaded、未来版本及其他实现。
范围依据 [官方版本表](https://devguide.python.org/versions/)：3.15 已进入 bugfix。

完整 micro 版本冻结到产品，不允许仅主次版本一致就替换。系统/Homebrew/conda
环境只提供构建期版本和依赖源，产品基础发行自动准备为相同完整版本、目标及常规
GIL 的官方 PBS。产品不递归复制系统 /usr 或 Homebrew 前缀。
BUILD 只用于定位待核实 release，必须取得官方资产的真实 PYTHON.json、全部组件
licenses，并核对版本、目标、shared 模式、构建选项及每个 license_paths。
无法获取匹配发行时给出具体构建诊断，禁止换 patch 或编造上游元数据。
已含完整元数据的本地发行仍做相同核验。下载临时文件自动清理，成功缓存位于仓库外。大归档并行取段，核对 HTTP Content-Range 与每段长度后合并；不进行普通文件哈希校验。

C shim 使用对应完整版本的头和库，在版本私有构建目录产生；不复用共享 3.12 shim。
3.9–3.10 handled 异常通过公开 ExcInfo API 保存与恢复；3.9–3.11 raised
异常通过 Fetch/Normalize/Restore 转换，接管引用并保留 traceback。
宿主在初始化前核对实际 libpython 完整版本及 shim 的头文件版本。
运行期只加载产品相对路径，不搜索 Python、不联网、不调用构建工具。

## 验收

macOS arm64：Rust driver 的 CPython 定向测试 22 项通过；driver 全目标 clippy
`-D warnings` 通过，C# Release 构建零警告。

`tests/cpython/version_run.py --python <实际解释器> --python-version <版本线>`：
3.9.25 / NumPy 1.26.4、3.12.13 / NumPy 2.3.5 已通过同次 CPython oracle、
真实 JIT、NativeAOT、完整版本冻结和删除输入后空 PATH 搬迁；3.12 额外
`--priority` 的三个真实 venv 项目/激活/PATH 顺序及产品运行通过。
3.15.2 / NumPy 2.5.4 同样通过上述完整 JIT/NativeAOT 搬迁验收。

旧显式 schema 1：`tests/cpython/run.py --mode jit` 的 21 项探针全部通过，
含 NumPy；`tests/cpython/standalone_run.py` 的 NativeAOT 搬迁、三流、中文
默认 IO、包资源及缺 libpython/shim/资源负例通过。
Linux/Windows 真实产品矩阵由主代理及 CI 验收。

部分 PBS full 元数据引用 `licenses/LICENSE.zlib-ng.txt`，归档遗漏该文件。
构建器读取该 release tag 的 `pythonbuild/downloads.py`，静态解析组件的
精确版本和 cpython-source-deps 源 URL，再取对应 tag 的 LICENSE.md。
补充来源写入产品 `python/license-sources.json`，原始上游 PYTHON.json 保留。
未知遗漏无补充规则时构建报错。固定 3.12.13 旧 shim 保留静态 guard 兼容；
新多版本 shim 必须导出并核对其头文件完整版本。

跨平台首轮发现并修正：GitHub API可使用构建环境的GH_TOKEN/GITHUB_TOKEN，凭据仅通过
curl stdin配置传递且仅用于官方API；匹配缓存先核对精确版本、schema、目标、shared模式和
官方来源记录，再交Rust复验ABI与许可，避免重复构建联网。Windows历史资产的shared文件名
与schema7 optimizations字段按官方20220528的3.9.13真实元数据适配，保留原始JSON。
报告中的archive_url改为本次实际安装归档，本地观察来源无归档时明确留空。

新增8项可重复发行准备合同测试：新旧Windows资产选择、schema7/8优化字段、官方API凭据
隔离与精确缓存离线选择/错误元数据拒绝，入口`python -B tests/cpython/test_portable.py`。
跨平台21项矩阵正在复验；Linux真实zlib专测与目标容器同时作为门禁。

复审与真实CI修复：许可证、来源与关键发行文件在私有候选中全部齐备后，持有内核文件锁原子发布；
中断释放锁，残缺旧目录隔离保存，并发构建复用完整赢家。解包失败保留tar原诊断并提示构建机
需要Zstandard支持；构建机还需要curl、C工具链与.NET SDK，目标程序无需这些工具。
官方API保持5条压缩分页：20条在三平台CI出现截断，100条触发网关504。使用可选凭据并优先
离线缓存，HTTP/1.1与有界重试覆盖HTTP/2取消和连接截断，避免扩大响应造成新的网络故障。
20261009的组件来源迁移为downloads.json，按固定release读取JSON；旧版Python字面量仍静态解析。
真实20261009来源及zlib-ng许可补齐已本机复验。Windows构建脚本使用UTF-8 BOM兼容PowerShell 5，
版本推导先于include参数构造。小归档同样校验HTTP已声明长度，不增加文件摘要。

Windows 3.14.10官方安装归档的48个DLL/pyd在本机完成普通与delay导入静态审计，
系统组件与随包库均有明确归属。Tk9的WINSPOOL.DRV沿Windows系统库处理，依据
[微软Winspool API说明](https://learn.microsoft.com/en-us/windows/win32/printdocs/getdefaultprinter)。
该记录对应静态审计；跨平台原生执行由版本CI逐项登记。

Windows矩阵复验识别的msvcrt.dll与ntdll.dll沿操作系统组件归类；VC可再分发运行库仍要求随包。
来源说明：[系统msvcrt.dll](https://devblogs.microsoft.com/oldnewthing/20140411-00/?p=1273%2F)、
[NT进程API](https://learn.microsoft.com/en-us/windows/win32/api/winternl/nf-winternl-ntqueryinformationprocess)。
