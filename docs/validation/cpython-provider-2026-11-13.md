# CPython provider 人工验收登记（2026-11-13）

本页只保存手工结论、范围和重跑入口。原始三流、数据库、发布目录与机器报告写仓库外，
调试结束即清理。测试来源为 [CP1/CP3驱动](../../tests/cpython/README.md) 与
[原联合应用](../../tests/aot/stdlib_service/README.md)。

## 固定输入与产品配置

- 平台：macOS arm64，当前开发机；其他平台和无SDK干净机待验。
- CPython：3.12.13，常规GIL，python-build-standalone 20260623，shared、PGO/LTO。
  install_only基础发行加同release full归档原文PYTHON.json/第三方licenses，禁止把venv作为分发。
- NumPy：2.3.5，cp312-cp312-macosx_14_0_arm64官方wheel，完整dist-info许可；
  原生扩展实际链接系统Accelerate、libc++、libSystem。
- 应用由RyuPy编译，标准库库存整体归CPython，额外第三方按声明/载荷库存冻结。
  json.loads被编译callback替换、CP fixture改math.sqrt而编译侧看到同一对象，是执行域状态证据。
- 主验收显式`container_transfer="synchronized"`；精确list/dict调用边界物化写回，
  保别名、共享环、回调和异常前副作用。两域后台线程并发修改同一容器暂缓。

## 已通过范围

| 执行路径 | 行为/API | 当前证据 |
| --- | --- | --- |
| JIT | host、protocols、identity、hash_keys、provider_state、import_cycles、exceptions、callbacks、container_transfer、shutdown、stream_sharing、exception_laziness、四种SystemExit、numpy_arrays、numpy_callbacks | 18项最新同次完整CPython三流差分通过，混合栈加强门待验；独立cwd，宿主关闭后句柄/注册/释放队列/租约全零 |
| NativeAOT | 同上18项 | 分批原位及中文/空格/&搬迁通过；删除应用输入/构建目录、隐藏私有发行输入、清空工具PATH并污染Python环境；实际模块和宿主库在dist内 |
| C#生命周期 | steady-state、exception-release、numpy-lifetime、disposal、late-release、mixed-limit | 六种独立进程模式通过；256批跨堆显式断环、128批异常/NumPy成功和错误回调逐批严格稳态，无强制GC |
| CP2原联合应用 | urllib.parse.urlencode、urllib.request.urlopen/read/status/context、base64.b64decode(validate)、gzip.decompress、json.loads | CP/JIT/NativeAOT/搬迁完整三流通过；HTTP服务只监听loopback，每臂独立请求计数 |
| CP2资源/存储 | importlib.resources.files/joinpath/as_file、path.read_text/is_file、sqlite3.connect/execute/事务/fetchall/fetchone/close | 原app.py/appdata未改；独立数据库逐值、参数化SQL形状输入、异常回滚、integrity_check全过 |
| NumPy真实扩展 | ndarray/dtype/shape、二维slice视图与shares_memory、owner删除后视图、arange/reshape/dot/array_equal、frompyfunc | JIT/NativeAOT/搬迁及真实编译回调成功/原异常实例往返通过，数组为opaque真实ndarray |

生命周期的固定intern消息先完整暖机一次再冻结计数；唯一动态消息每批显式释放。
ReleaseObject(error)不会递归释放外部共享Args对象，确定释放后别名明确拒绝使用，重复释放幂等，
弱缓存重新取得Python仍持有对象时生成活代理。迟到释放不重新进入已关闭解释器。

## 负例、待验与环境阻塞

| 项目 | 当前状态 |
| --- | --- |
| 声明python_version错误 | 已通过；配置阶段拒绝，独立于真实库ABI |
| 真实Mach-O CPU架构错配 | 已通过；load/init阶段指向实际库路径 |
| 缺libpython、缺启动encodings | 已通过；load/init阶段/路径及init_fs_encoding/codec明确诊断 |
| CP2缺schema资源、缺sqlite3源码包 | 已通过；独立恢复突变，原位与其他负例互不遮蔽 |
| NumPy缺_multiarray_umath扩展 | 已通过；真实ImportError与numpy路径，禁止系统补包 |
| 真实同架构libpython3.11.15错版本 | 已通过；初始化前明确requires3.12.13 / loaded3.11.15，未进入shim |
| 未捕获编译callback→CP包装异常的混合traceback | 域标记/消息/链通过，新增outer无已退出callback陈旧帧严格门待修复复验 |
| compiled自定义异常类导出限制 | 纯单继承/无额外成员边界由独立ctypes驱动验收，待其最终结果 |
| 默认proxy完整容器比较 | 独立专项待验；主矩阵采用显式synchronized |
| 单独移走_sqlite3共享库 | 环境阻塞：此PBS把_sqlite3编入libpython，需另一个扩展构建发行 |
| Linux x64、Windows x64、无Python/.NET/SDK干净目标机 | 待验 |
| 固定20默认native保留门 | 首轮保护11无丢失、CP20全过；dateutil显式six依赖漏登记致整门invalid，修复后完整重跑 |
| 完整make check | 正在清理旧生成预期消费者和ABI契约总数；最终稳定构建完整重跑后登记 |

首次exit_int NativeAOT发布曾遇ILC `Invalid handle`；外部日志保留给staging审计，稳定单项
重跑原位和搬迁均通过。错误尚待独立根因结论，未通过删用例绕过。

固定20默认native与上述桥接矩阵分列，13个当前native单项结果须等有效整门才形成正式矩阵。
本页逐API描述只覆盖实际调用输入；扩大输入域和平台需要继续同次差分。
