# 第五轮验收：通用接口、分发与语言语义

日期：2026-11-17，平台macOS arm64，起点 `178bbdae`。
用户明确第三方库不能按库名特化，同时允许为性能和良好测试加入ABI/布局/所有权推导。
本轮按这一口径推进，WIP备份分支为 `wip/20261117-generic-interfaces`。

## 通用分发与接口目录

共享RECORD归属按规范目标与真实源文件合并，所有owner、metadata与许可保留；安全环境
内链接仍物化。两独立命名/布局组合覆盖源码、资源与真实原生扩展，三种发布形态共六产品
删除输入/venv/构建树后搬迁通过；十项安装破坏负例、四项运行缺失资源/扩展负例通过。
最终证据在 `~/.cache/ryupy/validation/shared-ownership/20261117T084438.309723Z/`。
未选中发行的新增重复校验不污染实际闭包，另有专用回归。

接口目录从实际部署源建立，Python声明经语法层的显式版本/stub API提取；普通共享库
与实际PyInit导出分开，stub-only不授予执行provider。保留机器ABI证据和待推导项，
声明与已验证的机器调用分别登记。默认不遍历解析整套标准库；成员查询采用目标索引。
两个命名/布局分别使用legacy扩展和PEP489/PEP573 heap type扩展，六个实际产品严格
CPython三流差分通过；普通同后缀共享库和stub-only的真实导入失败保持正确。
静态测试覆盖重命名`.o`/可执行文件、MH_BUNDLE和versioned soname。
证据在 `~/.cache/ryupy/validation/interface-exports/20261117T090652.913212Z/`。

固定C ABI绑定现从同一BindingPlan发布`.pyi`和`.abi.json`，保留原参数名、保留字别名、
位置专用形状、符号别名、机器签名与借用约束。两份独立声明的JIT/NativeAOT共四产品
删除输入与构建树后搬迁通过，生成stub按固定3.12解析；32项绑定测试和driver clippy通过。
证据在 `~/.cache/ryupy/validation/binding-stubs/binding-stubs-28ih0s0a/`。

显式原生组件schema冻结来源、许可、真实Mach-O身份和版本，按精确授权的旧加载名
重定位部署副本。两套独立组件布局的四个JIT/NativeAOT产品删除源码、venv、组件prefix
与构建树后搬迁通过，14项缺失/冲突/ABI/许可负例通过。
证据在 `~/.cache/ryupy/validation/native-components/20261117T094403.190006Z/`。

## 通用语言合同

递归解包和动态格式规格见[ADR-0233](adr/0233-recursive-unpack-and-dynamic-format.md)。
三输入各JIT/NativeAOT/OneFile九产品已删除输入搬迁通过。新增旧测试转正，保留类体
链式赋值与单层多星号等真正不支持形状的诊断。全门曾被历史“嵌套解包必须拒绝”断言
阻断，按新合同改成逐层Unpack结构断言；原星号字面量快路数量与ABI断言继续保留。

f-string普通实例协议复用已验__format__调用，原生布局保留已有格式路径，另加Enum
回归。普通字典保存__getitem__方法、参数错误、空表哈希异常与重入已实际差分通过，
PyDict/PyHash相关153项通过；空表修正增加Count分支，尚未测量吞吐影响。

`oct`和字典`**`按通用整数/映射协议实现。`**`保留普通成对字面量原快路，连续成对项
按BUILD_MAP顺序准备，映射keys/getitem与覆盖按CPython时序执行。已有存储尚未暴露
缓存hash，涉及自定义键的完整输入保留pending，精确拒绝前不得重复调用已存键的hash。
四份输入的JIT/NativeAOT八产品已删除构建输入搬迁通过；foreign精确标量键与本地键
复用统一内部hash/equality域，保留实际原key对象、first-key身份和存活检查。真实原生
映射差分覆盖两方向覆盖、20000bit整数、bool/float/None/NaN及不同Unicode码点键。

## 薄函数真实产品发现的生命周期缺口

普通运算和值调用扩围的结构检查与Python oracle先通过，真实产品再发现容器发布后
PythonObject过早退休以及内部proxy无法关闭。原失败日志保留，按
[ADR-0234](adr/0234-stock-callback-published-ownership.md)继续闭合owned发布、内部root
归属与关闭。正例严格三流差分；frame能力负合同单独计量，包装后的反射场景保留实际结果。
通用值表达式四输入×两个Python版本×三发布形态24产品已通过，共观察528次静态调用、
222个实际owner；frame负合同六产品独立记录。早期wrapped反射失败已推动exact标准库
partial有效调用形状守卫和顶层诊断短scope，最终生命周期与真实产品均已复验。
最终CIL产品复验为24正、直接frame六项NI和wrapped frame六项NI；包装后的显式环境
vars/eval/compile与普通str保持可用，隐式frame依赖按精确调用形状阻断，所有终态计数
归零。最新手写输入为`wrapped_frame_boundaries.py`，历史错误结果和漏账红证继续保留。
最终生命周期四组已通过76正与4个真实跨堆环负例，其中68个授权进程、8个未启动/
未授权对照。1000次终态临时回调峰值记录为3；所有正例owned/native实际退账归零，
跨堆环负例保留ledger/Roots并精确拒绝关闭。日志及合同详见ADR-0234。

## 原大型项目

ExplorerPro完整原入口已依次越过共享RECORD、递归解包、动态格式、oct和字典解包，
进入实际原生依赖闭包审计。iODBC私有官方源码构建和PostgreSQL客户端的OpenSSL/
Kerberos完整闭包已通过严格审计，libpq私有loopback集群实际TLS握手通过。
完整Qt插件闭包当前缺Mimer客户端；保留完整入口失败证据，另按通用显式部署声明验证
Cocoa/SQLite产品所需的组件范围。
显式部署的完整原入口JIT构建已通过；实际Cocoa业务首次运行在原生扩展读取调用现场
时SIGSEGV，保留系统崩溃栈并继续修通用C API调用现场合同。窗口功能与运行搬迁继续
按真实结果更新。
FBA按实际冻结的3.12解析后PEP695版本误诊消失，当前到达模块级globals合同缺口。
当前完整原入口构建、隔离原生GUI业务、NativeAOT搬迁与CIL执行归属分别登记。

## 集成状态

完整检查日志位于 `~/.cache/ryupy/validation/generic-interfaces-20261117/`。
定向通过、产品通过与最终完整门分别登记；本轮没有宣称全26项性能晋升。
原有未跟踪项目输入保持原状，生成产物和原始报告不提交。

基准驱动修复了文档声称交替、实际每轮固定CPython先执行的问题。新协议
`direct-wall-v3`在正式轮循环轮换CPython、主实现与各变体的起点，预热单独计数；
每轮已实际启动的实现顺序进入结果，失败轮也保留执行到的位置。正式墙钟与RSS继续
使用无统计注入的新进程，内部统计独立采集；旧v2结果继续保存，比较前需要重新采样。
21项基准工具测试通过，含三实现三轮各占一次首中末位置、诊断隔离及失败分类。

本轮checkpoint `make-check-v6.log`完整exit0：3446项Runtime通过、3跳过；803项CPython
输入基线、514项编译e2e通过，299项ignored保留；Rust、fmt和clippy全部通过。v5在
并行dotnet构建时无诊断exit1，单独禁用build server重建成功；v6固定仓外TMPDIR、
CPython3.12.13并禁用server复用后整门通过。后续C4权威globals与carrier单独验收。
