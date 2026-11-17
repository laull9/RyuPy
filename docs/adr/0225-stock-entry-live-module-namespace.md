# ADR-0225：stock 编译入口的真实模块字典首切

- 日期：2026-11-17。
- 状态：无自定义 callable 的入口子合同已实现；函数跨堆所有权与完整入口迁移继续待办。
- 接续：[ADR-0223](0223-stock-libpython-bounded-glue.md)、
  [ADR-0224](0224-cpython-thin-function-core-handles.md)。

## 1. 问题与决定

固定 attrs 23.2.0 原入口 `tests/projects/entries/attrs_sample.py` 的类装饰器在
`attr/_make.py:2172` 执行 `globs.update(sys.modules[cls.__module__].__dict__)`。
旧入口为 LegacySingleton 静态槽，缺少权威字典；复制槽值会丢失后续写删、身份及别名。

为有 provider 的编译入口增加由 driver 冻结的 `entry_namespace` 能力，复用 InstanceGlobals
显式名称读写 ABI，所有模块名字直达 stock 模块原 `md_dict`。普通 native/typed 路径及旧显式
Instance 注册 ABI 保留；新注册单独使用 `RegisterStockEntrySourceLocated`，Runtime 不按公开
模块名猜测资格。没有真实 namespace provider 时明确拒绝。

首切资格由完整 AST 决定：仅在存在类装饰器或无参 globals/locals/vars 调用时自动迁移，
普通数值入口保留 typed globals。排除自定义 def、类方法、lambda、所有显式基类和类关键字，
以及 for/while 内的类声明，包括不可达分支；专用布局和循环建类沿用既有执行域。
保留原实例预检的生成器、推导式、环境协议等边界；允许普通类的急切字段注解和类装饰器。类体由 CIL
执行，普通 object 基类通过既有原生类准备/终结协议创建真实 CP 类型，供装饰器消费。
入口脚本、原业务输入、固定依赖版本和已有 provider 委派集合均保持原值。

## 2. 唯一存储与所有权

`IPyCompiledModuleNamespaceProvider` 只提供模块/字典存储与双向身份登记，不执行应用源码。
stock factory 取得已经存在的真实 `__main__`，同一宿主拒绝第二次实例化入口，避免把同一个
原字典假装成两个独立实例。普通模块重导和旧函数环境属于后续独立合同。

`StockNamespace.ViewObject` 是 canonical PythonObject，拥有真实精确 dict 的 owned 引用。
`globals()`、模块 `__dict__` 和 Runtime 名称读写使用同一存储，没有镜像、批量同步或槽回写。
模块直接导出原生 module 指针，不建立持有 Runtime 模块的 CP managed proxy 根。
适配器弱键缓存保身份；原生查找命中后立即 `Py_NewRef`，owned 值才进入托管转换。
键也以真实 Unicode 对象传递，保留中文与内嵌 NUL；缺键和已绑定 None 分开。
native module 路径绕过用户覆盖的 `__dict__` 属性钩子。

入口无自定义 callable，故本次不导出捕获 `PyFunctionOwner → context → owned dict` 的函数根。
默认入口是本次产品进程的常驻执行环境；跨堆一般对象图、函数/方法/闭包和模块重导的独立
回收继续依赖 ADR-0224 的产品 Core/handle 接线，不能由本首切推导完成。
既有有限 builtin、loader 和 spec 代理沿用原 provider 合同；没有扩充业务解释执行范围。

审查修正了三项默认准入缺口：显式 list/dict/str/异常基类需要旧专用布局；循环建类可经
迭代器形成跨堆强环；无 namespace 需求的数值脚本不应改用动态字典。修正前有 provider 的
5 万次整数循环，三个交替墙钟样本中位数从基线 45.04 ms 增至 20.11 s。该诊断与功能验证
共享机器负载，只用于定位严重退化。守卫产物五次交替墙钟样本中位数为 45.02 ms，
同轮基线 44.82 ms，已消除数量级退化；旧不利样本保留仓外。全量性能预算另验。

## 3. 验收合同

新增手写输入与驱动：

- `tests/cpython/fixtures/live_module_dict_entry.py`：无自定义 callable 的已验入口，覆盖真实
  dict 身份、CP 回调内属性/字典双向写删、CIL 写删立即可见、共享列表别名和缺键异常。
- `tests/cpython/fixtures/live_module_dict.py`：完整函数 `__globals__` 与 CP 回调重入合同，
  保留 pending，待 Core/handle 产品接线；不能列为本轮通过项。
- `tests/cpython/test_live_module_dict.py`：固定 CPython 3.12.13 同次 oracle；真实 JIT、AOT、
  删除私有输入/基础发行/构建目录后，中文空格目录搬迁。证据仓外保存，临时产品自动清理。
- `CPythonStockNamespaceTests`：真实解释器的 md_dict 属性钩子隔离、中文/NUL 键、None、
  身份、双向写删、列表别名，以及未向 CP 发布的 Runtime 函数绑定同一字典。
- HIR 资格与 lower 结构测试、既有 codegen Instance/Legacy ABI 测试保护分层及旧快路。

macOS arm64、stock CPython 3.12.13 本轮固定 attrs 原入口 JIT/NativeAOT/删除输入搬迁均与
同次 oracle 的 stdout、stderr、退出码一致；无自定义 callable 的活字典入口同样三臂通过。
stdout 分别为原 attrs 的 Point/asdict 输出及 `live entry dict 45`，stderr 为空。
基线来源明确为 `9d9a96b2 + WIP2d78`（含既有未验 C4 候选），三臂均在缺少 `__dict__` 处失败。

该结果归属现有 provider 兼容产品；attrs 依赖的 Python 业务体仍按原计划运行于 CP 域。
完整函数合同、attrs onefile、其它平台及隔离性能预算继续待办。

## 4. 纯 stock 切片的集成门

最终发布切片以 `9d9a96b2` 为基底，只叠加冷 namespace 协议、stock 后端、编译器资格
与发射接线。严格 Host、严格 shim、内核及 ProcessNativeBinding 研究候选全部保留在
独立 WIP；compat Host 沿用基底，shim 仅新增 module_namespace.h 的公开 C API 导出。
普通 PyDict 与 PyFunction 不新增字段，旧 Instance 注册和 Import 返回签名保留。

纯切片完整 `make check` 退出 0：3418 项 Runtime C# 通过、3 项既有跳过，503 项编译 e2e
通过、295 ignored，Rust、fmt/clippy 与 Python 门全部通过。另以各自真实头文件重建 shim，
CPython 3.9.25、3.10.21、3.11.15、3.12.13、3.13.3、3.14.7 各 36 项真实类桥通过，合计 216 项。
list/dict/str/异常专用布局与循环建类保留路径的同次 CPython/JIT/NativeAOT 差分通过。
从真实 `5d36db1e` 重新构建 Runtime 后按 PE 元数据逐签名比较：历史 1843 项全部保留，
本切片 Runtime 共 2009 项公开签名；新增为显式冷 namespace/事务合同，未删除旧入口。

同一纯切片 CLI 的活字典三臂，以及 FastAPI/SanBudget 各 JIT、standalone、onefile 全部
通过删除输入后的中文空格搬迁。FastAPI 仍为原显式六族业务委派与 ASGI TestClient，
SanBudget 保持原业务 CIL 与真实 macOS 窗口。固定二十项目的纯切片三臂矩阵为 **60/60**：
JIT、NativeAOT、删除输入搬迁各 20/20。60 格均先运行固定 CPython 3.12.13 同次 oracle，
三流、退出码与文件副作用匹配；全部临时产品清理。包资源实测覆盖原入口消费的 pyfiglet
standard 字体、Markdown extra/egg-info 与 binaryornot 文件读写；任意资源、所有字体/插件、
源码原文、可写根分离、20 项目的 onefile 及其它平台仍有各自验收门。

前两份完整候选矩阵独立保留：v2 为 60/60；含研究 WIP 的 guarded-v3 为 59/60，其中
toml/reloc 在 dotnet publish 的 Generating native code 阶段退出 1，内层 stderr 为空，
原因未确定。该失败未重跑覆盖，日志和首错保留；最终纯切片的独立 60/60 为本提交验收来源。
本切片未修改严格 kernel，不生成 golden 或普通文件摘要。所有日志和构建身份记录保存在
仓库外 `/Users/laull/.cache/ryupy/validation/adr0208-20261117/`；生成产品由驱动清理。
