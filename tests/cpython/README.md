# CPython provider 的分层验收

本目录保存手写程序、固定依赖和驱动。每次执行先用 CPython 3.12.13 运行同一程序取得
stdout、stderr 和退出码，再运行 RyuPy 编译入口。各执行臂使用独立 cwd；联合应用还各自
创建数据库、重置 loopback HTTP 请求计数。输出默认写仓库外临时目录并清理，`--keep`
仅用于显式失败调试。本次固定21个Python程序及6种独立C#宿主模式；最终冻结版全量结果登记在下表。

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
uv安装输入缺少上游底层组件许可，驱动额外要求同一release的
`cpython-3.12.13+20260623-aarch64-apple-darwin-pgo+lto-full.tar.zst` 中 `PYTHON.json` 与
`licenses/`，按上游原文随产品部署。`prepare.py` 显式准备这份官方材料，支持 `--offline`。

## 手写程序与退出门

| 稳定 ID | 手写合同 | 产品状态 |
| --- | --- | --- |
| host | 固定版本、基础库/扩展、隔离初始化 | macOS arm64：JIT/NativeAOT/搬迁通过 |
| protocols | 属性、位置/关键字调用、下标写入、长度、真值、迭代、上下文 | macOS arm64：JIT/NativeAOT/搬迁通过 |
| identity | 模块身份、list/dict别名、str/bytes/大整数双向身份 | macOS arm64：JIT/NativeAOT/搬迁通过 |
| hash_keys | CP创建的等值str/bytes/大int/tuple，在两域dict/set双向查询与去重 | macOS arm64：JIT/NativeAOT/搬迁通过 |
| provider_state | 重复模块身份、替换模块函数、from绑定、父包属性、argv | macOS arm64：JIT/NativeAOT/搬迁通过 |
| import_cycles | Python反向导入半初始化编译模块、导入失败回滚与重试 | macOS arm64：JIT/NativeAOT/搬迁通过 |
| exceptions | 原异常实例、cause/context/traceback、用户子类、同名异继承负例 | macOS arm64：JIT/NativeAOT/搬迁通过 |
| callbacks | 静态编译callable、同线程重入、受控Python外线程 | macOS arm64：JIT/NativeAOT/搬迁通过 |
| container_transfer | 显式精确list/dict物化、共享环、回调和异常写回 | macOS arm64：JIT/NativeAOT/搬迁通过 |
| shutdown | Python atexit实际调用编译函数 | macOS arm64：JIT/NativeAOT/搬迁通过 |
| stream_sharing | 编译print与CP sys.stdout/stderr重绑共享真实流 | macOS arm64：JIT/NativeAOT/搬迁通过 |
| exception_laziness | 捕获不执行用户异常字符串/属性钩子，显式str才触发 | macOS arm64：JIT/NativeAOT/搬迁通过 |
| exit_none/int/text/tuple | SystemExit真实code、文本错误流与退出码 | macOS arm64：JIT/NativeAOT/搬迁通过 |
| exceptions_state | from None、已有Foreign异常双向改链/args、CP正常返回后重抛刷新 | macOS arm64：JIT/NativeAOT/搬迁通过 |
| exception_handled_state | sys.exception、双向裸raise、嵌套重入/失败恢复、同handler内CP改链后再读和重抛 | macOS arm64：JIT/NativeAOT/搬迁通过 |
| mixed_traceback | 未捕获编译回调→CP包装异常的链、双域标记和帧顺序，outer无陈旧callback帧 | macOS arm64：JIT/NativeAOT/搬迁通过 |
| numpy_arrays | 真实ndarray/dtype、视图owner与别名、向量运算、Python异常 | macOS arm64：JIT/NativeAOT/搬迁通过 |
| numpy_callbacks | 真实frompyfunc调用编译函数、异常实例往返、重复创建 | macOS arm64：JIT/NativeAOT/搬迁通过 |
| lifecycle/steady-state | 256批显式跨堆断环、注册/句柄/释放队列稳态 | macOS arm64：C#宿主探针通过 |
| lifecycle/late-release | 关闭后迟到释放、释放后别名拒绝、重复释放 | macOS arm64：C#宿主探针通过 |
| lifecycle/disposal | 确定释放、重复释放幂等、已释放别名拒绝、弱缓存重建活代理 | macOS arm64：C#宿主探针通过 |
| lifecycle/exception-release | 128批回调异常实例往返、异常路径句柄和注册稳态 | macOS arm64：C#宿主探针通过 |
| lifecycle/numpy-lifetime | 128批真实NumPy ufunc成功/异常回调、确定释放稳态 | macOS arm64：C#宿主探针通过 |
| lifecycle/mixed-limit | Runtime格式化双域总帧limit0/1/-1，chain=False保异常本体 | macOS arm64：C#宿主探针通过 |
| stdlib_service | 原HTTP/编码/压缩/JSON/资源/SQLite应用、数据库副作用 | macOS arm64：JIT/NativeAOT/搬迁通过 |

2026-11-13，macOS arm64固定PBS/NumPy与显式`synchronized`：21个Python程序在冻结v3
快照完整通过当次CP/JIT/NativeAOT原位与搬迁；SystemExit保留真实退出码，混合栈按双域合同检查。
v4仅修改直接C绑定三处失败诊断字符串，另有一行接口注释；复验21项CP/JIT、六项生命周期、
原联合应用完整链及C ABI专项均通过。两份65文件私有快照的大小/mtime在门前后保持一致。
六项C#宿主先独立暖机，再逐批严格比较句柄/注册/队列，无强制GC；动态唯一消息显式释放。
原联合应用业务输入保持原样，数据库/HTTP副作用、资源与sqlite3包负例全过。
默认native固定20门完整有效：13 native、6 unsupported、1 failed，保护11全部保留。
2026-11-14完整make check已exit0，结论见[人工验收登记](../../docs/validation/cpython-provider-2026-11-13.md)。
Linux x64、Windows x64、无Python/.NET/SDK干净目标机与HTTPS仍待验。
首次exit_int AOT发布曾有ILC `Invalid handle`；稳定单项及最终21全量均通过，该次根因仍未知。
后续embedded PDB和依赖闭包分发改善另有PE读取验证。

## 声明示例

[provider.example.json](provider.example.json) 固定PBS20260623 macOS arm64完整许可清单。
先由`prepare.py`准备官方full归档材料，将其`PYTHON.json`和`licenses/`连同基础发行部署到
同一显式`distribution_root`，然后只替换示例根路径。`modules=[]`表示无额外第三方包；
标准库由`stdlib_provider="cpython"`整体归属。默认`proxy`传递活代理；需要精确list/dict布局的
应用显式改为`synchronized`，本目录主矩阵使用此模式。NumPy还需额外模块声明与包载荷。

## 重跑

依赖只装入仓库外专用缓存，产品分发从显式基础发行和包载荷重建：

```sh
python3.12 -B tests/cpython/prepare.py
python3.12 -B tests/cpython/run.py --mode baseline \
  --numpy-root "$HOME/.cache/ryupy/cpython-provider/numpy-2.3.5-cp312-osx-arm64"
make runtime-cpython CPYTHON_HOME="$(dirname "$(dirname "$(uv python find 3.12.13)")")"
cargo build -p ryupy-cli
python3.12 -B tests/cpython/run.py --mode all --ryupy target/debug/ryupy \
  --distribution "$(dirname "$(dirname "$(uv python find 3.12.13)")")" \
  --distribution-metadata "$HOME/.cache/ryupy/cpython-provider/pbs-20260623-metadata/python" \
  --numpy-root "$HOME/.cache/ryupy/cpython-provider/numpy-2.3.5-cp312-osx-arm64"
python3.12 -B tests/aot/stdlib_service/run_provider.py --ryupy target/debug/ryupy \
  --distribution "$(dirname "$(dirname "$(uv python find 3.12.13)")")" \
  --distribution-metadata "$HOME/.cache/ryupy/cpython-provider/pbs-20260623-metadata/python"
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

host的AOT搬迁后分别验证声明版本错误、真实3.11.15同架构libpython错版本、
真实Mach-O CPU架构错配、缺libpython、缺启动
encodings。联合应用分别验证缺包资源、缺sqlite3源码包。NumPy的核心扩展缺失另立验收门。
本次PBS把 `_sqlite3` / `zlib` 编入libpython，无法移走单独 `_sqlite3` 共享库；需要对应独立
扩展构建发行后补该负例，状态为环境阻塞。空PATH和环境污染已进入驱动；干净目标机单独记录。
