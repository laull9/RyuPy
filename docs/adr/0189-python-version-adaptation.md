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
