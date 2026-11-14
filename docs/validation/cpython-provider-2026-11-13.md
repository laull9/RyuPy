# CPython provider 人工验收登记（2026-11-13）

本页保存人工结论、实际范围和重跑入口。原始三流、数据库、发布目录与机器报告在仓库外
临时目录执行并清理。输入为[CP1/CP3驱动](../../tests/cpython/README.md)、
[原联合应用](../../tests/aot/stdlib_service/README.md)与[C ABI专项](../../tests/aot/c_library/README.md)。

## 固定输入与执行域

- 平台为当前macOS arm64开发机；CPython 3.12.13常规GIL，python-build-standalone
  20260623 shared、PGO/LTO。install_only基础发行加同release full归档原文PYTHON.json及全部
  第三方licenses；分发从显式基础发行重建。
- NumPy 2.3.5采用cp312-cp312-macosx_14_0_arm64官方wheel与完整dist-info许可；实际原生边
  为系统Accelerate、libc++、libSystem。官方来源和可重建命令见验收驱动。
- 入口由RyuPy编译，固定发行标准库库存整体归CPython，额外第三方按声明与随包库存冻结。
  json.loads被编译callback替换，CP fixture修改math.sqrt而编译侧看到同一对象，证明共享导入状态。
- 主矩阵显式选择`container_transfer="synchronized"`，精确list/dict在调用边界物化写回，
  保别名、共享环、回调及异常前副作用。默认proxy专项独立验收。

## 冻结快照与复验范围

完整CLI、Runtime依赖闭包、Bridge依赖闭包和shim各复制到仓库外私有根；每份65文件。
实际编译器、RYUPY_RUNTIME_DIR、声明bridge_directory/shim均指向私有根；门前后大小和mtime
清单一致。仓库正常make构建使用独立输出，未改动私有产品输入。

v3完整运行全部21个Python程序：同次CPython、JIT、NativeAOT原位、删源搬迁及负例。
v3之后产品源码只有PyNativeBindingLibrary三处失败诊断字符串增加库名/实际路径/底层原因，
保留原加载逻辑、路径安全、异常类型与Inner；另有PyForeignObject一行XML接口说明。
21个应用均无direct binding声明，生成程序未引用该加载模块。按依赖影响审查，v4复验完整21项
CP/JIT、六项生命周期、CP2全链，以及C ABI的正常和拒绝路径完整专项。

上述范围分别记录实际快照，v3的NativeAOT结果与v4的短扫和受影响专项共同构成此次结论。
第二、第三WIP仅保存源码实施阶段，正式结果以本页的已执行门为准。

## 已通过范围

| 执行路径 | 行为/API | 实测结果 |
| --- | --- | --- |
| v3 CP/JIT/NativeAOT/搬迁 | host、protocols、identity、hash_keys、provider_state、import_cycles、exceptions、callbacks、container_transfer、shutdown、stream_sharing、exception_laziness、exit_none/int/text/tuple、numpy_arrays、numpy_callbacks、mixed_traceback、exceptions_state、exception_handled_state | 21项完整exit0，各臂独立cwd；三流逐字节比较，SystemExit保真实code，混合栈按明确双域合同检查 |
| v4 CP/JIT | 上述全部21项 | 完整exit0；关闭报告handles/managed_roots/pending_releases/active_leases全零 |
| v4 C#生命周期 | steady-state、exception-release、numpy-lifetime、disposal、late-release、mixed-limit | 六种独立进程模式全部exit0；256批跨堆断环、128批异常与NumPy回调严格稳态，无强制GC |
| CP2 HTTP/编码 | urllib.parse.urlencode、urllib.request.urlopen/read/status/context、base64.b64decode(validate)、gzip.decompress、json.loads | v4同次CP/JIT/NativeAOT/搬迁完整三流通过，loopback HTTP每臂恰好一个请求 |
| CP2资源/存储 | importlib.resources.files/joinpath/as_file、path.read_text/is_file、sqlite3.connect/execute/事务/fetchall/fetchone/close | 原app.py/appdata保持原样；独立数据库全部值、SQL形状输入、异常回滚、integrity_check均通过 |
| NumPy真实扩展 | ndarray/dtype/shape、二维slice及shares_memory、删除owner后视图、arange/reshape/dot/array_equal、frompyfunc | v3完整JIT/NativeAOT/搬迁与v4 JIT通过，真实编译callback成功/原异常实例往返及生命周期通过 |
| handled与异常链 | compiled except→CP sys.exception/裸raise、CP except→compiled callback裸raise、嵌套正常/异常恢复、同handler CP改链/args后再次读和重抛 | v3完整21门与v4 JIT通过，context/cause/suppress/args保持真实最新值 |
| 混合traceback | 原类名/消息/链、编译域与CP域帧顺序、outer无已退出callback帧、总limit0/1/-1 | v3 JIT/NativeAOT/搬迁与v4 JIT/生命周期通过 |
| ABI与元数据 | 50条手写provider发布ABI签名、缺失/改签/重复反例、PE扫描、handled token恢复 | host专项通过 |
| 直接C与CP组合 | json+zlib联合、反向导入绑定模块、13种精确标量、借用UTF8/bytes、ctypes、默认proxy、纯继承异常类 | run.py/run_combined.py/run_scalars.py/run_ctypes.py四驱动完整exit0，含JIT/NativeAOT/搬迁、缺库与同名冲突，详见专项README |

生命周期中固定intern消息先完整暖机，再冻结逐批计数；动态唯一消息每批显式释放。
ReleaseObject(error)保持外部共享Args对象有效。重复释放幂等、释放后别名拒绝使用、弱缓存重新
获取Python仍持有对象时生成活代理；迟到释放增加统计且不再进入关闭后的解释器。

## 负例与剩余平台门

| 项目 | 实测状态 |
| --- | --- |
| 声明python_version错误 | 配置阶段拒绝，通过 |
| 真实同架构libpython 3.11.15 | 初始化前报告requires3.12.13 / loaded3.11.15，通过，尚未加载shim |
| 真实Mach-O CPU错配 | load/init阶段拒绝并指出实际库路径，通过 |
| 缺libpython、启动encodings | load/init路径与init_fs_encoding/codec明确诊断，通过 |
| CP2缺schema资源、缺sqlite3源码包 | 独立恢复突变，FileNotFoundError/ModuleNotFoundError及具体资源/包名通过 |
| NumPy缺_multiarray_umath | 真实ImportError及核心扩展路径，通过 |
| 默认proxy及异常类型边界 | 纯单继承异常链先预检；成功导出后类型不可变；额外字段/方法拒绝；set/frozenset混合比较在两模式明确NI；专项通过 |
| 单独移走_sqlite3共享库 | 环境阻塞：该PBS把_sqlite3编入libpython，需要独立扩展构建发行补验 |
| Linux x64、Windows x64 | 待验 |
| 无Python/.NET/SDK的干净目标机、HTTPS | 待验；本次HTTP采用loopback，搬迁运行采用清PATH/污染Python环境及加载路径审计 |
| 两域后台线程同时修改同一容器 | 当前合同域外；synchronized采用边界复制与写回 |

首次exit_int NativeAOT发布出现过ILC `Invalid handle`，该次根因未知；稳定单项重跑与最终
21全量均通过。之后embedded PDB与完整依赖闭包分发改善经PE读取验证，两项事实分别记录。

## 默认native项目与完整检查

第三WIP `f3f8e1bbb74275cb383aad2f0523a15e60f8a3b3` 在全repo无写窗口运行
`make projects-preserve`完整20入口，源码、工具、候选前后一致，结果valid且exit0。
当次CP20全过，默认JIT为13 native、6 unsupported、1 failed，原保护11逐ID全部保留。
native为sortedcontainers、toml、tomli、docopt、pyfiglet、tabulate、texttable、chardet、wcwidth、
colorama、termcolor、binaryornot、progress；pyfiglet和sortedcontainers为保护集以外新增通过。
unsupported为six、pyparsing、attrs、networkx、python-dateutil、click；Markdown仍因find_spec
缺source metadata失败。固定20默认项目NativeAOT与CP provider20应用矩阵待验。

2026-11-14，固定CPython3.12.13的完整make check退出0：全workspace fmt/clippy通过，
CPython基线732通过，C# 3088通过/2原跳过，全部Rust套件通过；e2e共742发现，
459通过/283原忽略/0失败。driver lib为217通过/4原忽略，动态except四项和资源发现三项均通过。
当前主门消费者已迁移到同次CPython与结构断言，原ID/输入及已有差异合同保留；
TextWrap的1200矩阵与87360穷举逐项生成当次CP预期。历史独立门迁移继续登记于TODO。
