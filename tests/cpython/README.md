# CPython provider 的分层验收

本目录保存手写程序、固定依赖和驱动。每次执行先用 CPython 3.12.13 运行同一程序取得
stdout、stderr 和退出码，再运行 RyuPy 编译入口。各执行臂使用独立 cwd；联合应用还各自
创建数据库、重置 loopback HTTP 请求计数。输出默认写仓库外临时目录并清理，`--keep`
仅用于显式失败调试。所有新程序保留 `pending`，通过产品门后才逐项转正。

## 发行和依赖锁

| 输入 | 固定选择 | 来源和部署要求 |
| --- | --- | --- |
| CPython | 3.12.13，常规 GIL，macOS arm64 | python-build-standalone `20260623`；共享 libpython、PGO/LTO，保留完整基础发行及许可证 |
| 基础发行资产 | `cpython-3.12.13+20260623-aarch64-apple-darwin-install_only.tar.gz` | [官方资产](https://github.com/astral-sh/python-build-standalone/releases/download/20260623/cpython-3.12.13%2B20260623-aarch64-apple-darwin-install_only.tar.gz)，[固定发行页](https://github.com/astral-sh/python-build-standalone/releases/tag/20260623) |
| NumPy | 2.3.5，CPython `cp312` | [官方 PyPI 发行](https://pypi.org/project/numpy/2.3.5/)，只使用目标架构 wheel，连同 dist-info/许可证部署 |
| 当前本机 wheel | `cp312-cp312-macosx_14_0_arm64` | 实际 WHEEL tag；核心扩展链接系统 Accelerate、libc++、libSystem，本轮没有独立 BLAS 库 |

uv 管理的基础 CPython 来自 python-build-standalone，基础发行不是 venv。驱动在临时工作区
建立私有输入副本，搬迁前隐藏该副本原位置，删除应用与包载荷源码、JIT 和 AOT 构建目录。
运行时采用分发内根路径，清除开发环境并污染 `PYTHONHOME` / `PYTHONPATH`，更换 cwd 和
工具 PATH。macOS 同时逐 Mach-O 审计加载边，并检查宿主报告的实际 libpython、shim 和
模块文件路径，发行外的普通包路径直接失败。

发行重建、组成和许可证以 [上游归档说明](https://docs.astral.sh/python-build-standalone/distributions/)
和 [运行/许可说明](https://github.com/astral-sh/python-build-standalone/blob/main/docs/running.rst)
为准。基础发行保留 `lib/python3.12/LICENSE.txt`，NumPy 保留其完整 dist-info 许可证。
平台 wheel 的原生库逐文件审计，许可证中的潜在组件列表与实际链接闭包分别记录。

## 手写程序与退出门

| 稳定 ID | 手写合同 | 产品状态 |
| --- | --- | --- |
| host | 固定版本、基础库/扩展、隔离初始化 | 待验收 |
| protocols | 属性、位置/关键字调用、下标写入、长度、真值、迭代、上下文 | 待验收 |
| identity | 模块身份、list/dict别名、str/bytes/大整数双向身份 | 待验收 |
| hash_keys | CP创建的等值str/bytes/大int/tuple，在两域dict/set双向查询与去重 | 待验收 |
| provider_state | 重复模块身份、替换模块函数、from绑定、父包属性、argv | 待验收 |
| import_cycles | Python反向导入半初始化编译模块、导入失败回滚与重试 | 待验收 |
| exceptions | 原异常实例、cause/context/traceback、用户子类、同名异继承负例 | 待验收 |
| callbacks | 静态编译callable、同线程重入、受控Python外线程 | 待验收 |
| container_transfer | 显式精确list/dict物化、共享环、回调和异常写回 | 待验收 |
| shutdown | Python atexit实际调用编译函数 | 待验收 |
| numpy_arrays | 真实ndarray/dtype、视图owner与别名、向量运算、Python异常 | 待验收 |
| numpy_callbacks | 真实frompyfunc调用编译函数、异常实例往返、重复创建 | 待验收 |
| lifecycle/steady-state | 256批显式跨堆断环、注册/句柄/释放队列稳态 | 待验收 |
| lifecycle/late-release | 关闭后迟到释放、释放后别名拒绝、重复释放 | 待验收 |
| lifecycle/disposal | 确定释放、重复释放幂等、已释放别名拒绝、弱缓存重建活代理 | 待验收 |
| stdlib_service | 原HTTP/编码/压缩/JSON/资源/SQLite应用、数据库副作用 | 待验收 |

2026-11-13：上述12个 Python 程序的固定 CPython 基线通过，原联合应用基线通过。
首轮JIT的callbacks/protocols业务输出通过，宿主报告门待实现；其余用例真实发现比较递归、
异常身份、跨域hash、精确容器写回、包导入、多维slice、NumPy回调和atexit关闭缺口，
均已分派实现。产品 NativeAOT、搬迁和生命周期结果逐项补录，新程序保留pending。
Linux x64、Windows x64、无Python/.NET/SDK的干净目标机均待验收。

## 重跑

依赖只装入仓库外专用缓存，产品分发从显式基础发行和包载荷重建：

```sh
uv pip install --python "$(uv python find 3.12.13)" \
  --target "$HOME/.cache/ryupy/cpython-provider/numpy-2.3.5-cp312-osx-arm64" \
  --only-binary :all: --no-deps -r tests/cpython/requirements.txt
python3.12 -B tests/cpython/run.py --mode baseline \
  --numpy-root "$HOME/.cache/ryupy/cpython-provider/numpy-2.3.5-cp312-osx-arm64"
make runtime-cpython CPYTHON_HOME="$(dirname "$(dirname "$(uv python find 3.12.13)")")"
cargo build -p ryupy-cli
python3.12 -B tests/cpython/run.py --mode all --ryupy target/debug/ryupy \
  --distribution "$(dirname "$(dirname "$(uv python find 3.12.13)")")" \
  --numpy-root "$HOME/.cache/ryupy/cpython-provider/numpy-2.3.5-cp312-osx-arm64"
python3.12 -B tests/aot/stdlib_service/run_provider.py --ryupy target/debug/ryupy \
  --distribution "$(dirname "$(dirname "$(uv python find 3.12.13)")")"
```

`--case ID` 可重复指定；默认发现全部源程序，每个用例失败后继续发现其他用例。
`--mode jit` 只跑基线与 JIT，`--mode aot` 只跑基线、NativeAOT 与搬迁。
生命周期探针作用于含 `bridge_fixture` 的本次 JIT 分发，独立新进程执行：

```sh
python3.12 -B tests/cpython/lifecycle.py --dist /绝对路径/本次JIT目录
```

`synchronized` 是显式容器传递合同：调用边界物化精确list/dict并写回，保留共享引用、环和
异常前副作用；Python后台线程与编译侧并发修改同一容器暂缓。默认 `proxy` 保持活代理，
扩展要求精确内建布局时明确报类型错误。数组始终作为真实Python ndarray持有。

## 负例和平台边界

host的AOT搬迁后分别验证声明版本错误、真实Mach-O CPU架构错配、缺libpython、缺启动
encodings。联合应用分别验证缺包资源、缺sqlite3源码包。NumPy的核心扩展缺失另立验收门。
本次PBS把 `_sqlite3` / `zlib` 编入libpython，无法移走单独 `_sqlite3` 共享库；需要对应独立
扩展构建发行后补该负例，状态为环境阻塞。空PATH和环境污染已进入驱动；干净目标机单独记录。
