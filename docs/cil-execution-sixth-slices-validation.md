# 第六轮验收：权威模块环境与真实原生调用

日期：2026-11-17–18，平台macOS arm64。新profile通过显式
`--experimental-stock-module-namespaces`选择完整本地源码图，底层复用原版CPython。
生产选择规则基于冻结来源与实际能力，第三方名称只出现在测试输入、原项目和验收说明。
通用C ABI、pyi及原生组件分发沿第五轮已提交合同继续使用。

## 同一字典与真实执行

Runtime复用InstanceGlobals；provider为普通源码模块分配真实CP模块及其唯一D，入口
复用规范__main__。模块、CIL读写和外部CP写入共同使用D；loader保部分初始化、失败
回滚与重试。模块载体一次进入整个CIL正文，必要原生边界按可信来源切换现场。
资源、code来源与跨堆生命周期见ADR-0236/0237。

第一组四应用的双版本JIT/NativeAOT共16个产品通过：eager三模块、annotations四模块、
harbor_pair和frost_pair各三模块。后两套名称和数据布局独立。每次先生成固定CPython
即时预期，实际构建后删除源码、venv和build，中文空格路径搬迁、空PATH及失效Python
环境运行，核对完整退出码/stdout/stderr、所有源码的编译归属以及真实关闭计数。
3.12日志`20261117T114637.828735Z.json`、3.13日志`20261117T114810.414056Z.json`，
均位于`~/.cache/ryupy/validation/stock-module-namespaces/`。

急切推导式继承当前执行环境，普通def与惰性genexpr在定义点捕获；AnnAssign保留精确
目标域dict、右值/目标/注解/实际mapping写入顺序，回调重绑后活读。规范builtin身份
在用户代码执行前捕获，默认基类使用规范object；类桥启动只保留必要legacy wrapper，
其它描述符继续lazy。实际替换__import__后，无显式import的类创建没有隐藏导入。

两模块生成器产品另通过双版本JIT/NativeAOT四格：send/throw/close、finally、惰性读取、
yield-from的真实终值与再次耗尽，以及同名callee一次求值和对象跨暂停身份。实际测试
发现旧tp_iternext的MoveNext吞掉终值，修复为真实PyGenerator的既有Next协议。
验证器继续核验源码callee快照，只排除精确私有帧槽的机械保存/恢复；四个MIR测试含
context来源、保存邻槽及恢复邻槽的破坏拒绝。产品日志分别为
`20261117T115007.910822Z.json`与`20261117T115742.698468Z.json`。

生成器源码except链随后补齐最终逻辑路由证明：逐项核对实际active窗口、typed匹配、
handler PC、Retry的私有帧/待接异常写入、恢复分派和Catch槽清理，保原for/with合成
StopIteration路由。可达根包含真实EH入口；只豁免最终CFG确不可达的已清空块。
二十项实际IR变异涵盖错误区间、异常/PC仍可达块、整数溢出及恢复槽，均明确拒绝。
两独立多模块输入双版JIT/AOT八产品通过：`20261117T121559.974900Z.json`、
`20261117T121625.617702Z.json`、`20261117T121625.617720Z.json`。

## 导入钩子与原生窗口

实际导入读取本次捕获builtins中的hook并仅调用一次，保留五实参、真实globals、普通
函数locals=None、fromlist/level和任意返回对象。IMPORT_FROM继续真实属性/缓存回退
与ImportError字段；class的prepared namespace仍按其独立合同推进。
两个无关命名/布局的hook输入，双版本JIT/NativeAOT共八产品搬迁通过；原属性删除
输入保留，foreign模块删除转发同一真实对象的既有DelAttr协议。中性Runtime删除门
52项通过，含缺属性、错误身份及失败前副作用；旧静态槽守卫继续保留。
最终hook日志为`20261117T115755.428136Z.json`、`20261117T115851.073239Z.json`。

真实最小Cocoa输入保留`QApplication([])`，JIT、NativeAOT和OneFile均删除源码、venv、
组件输入与构建目录后搬迁通过，实际cocoa窗口visible/exposed，截图已视觉核验，完整
三流与同次CPython一致，本地业务委派为零。证据位于
`~/.cache/ryupy/validation/explorerpro-products/`下
`20261117T115636.399118Z`、`20261117T115851.074664Z`、
`20261117T115851.074451Z`。完整原入口及原GUI业务按其实际后续阻塞分别登记。

首轮窗口探针通过QTimer在`app.exec()`约300ms后截图并退出。用户随后实际观察到鼠标
持续加载，验收已追加持续心跳、真实鼠标/键盘输入及原生关闭事件。CPython、JIT、目录
NativeAOT均完成点击、输入Return、原生关闭，193/391/410次心跳的最大间隔分别为
0.351/0.138/0.274秒。OneFile的这四类回调也执行成功，但两次交互出现7.200/8.208秒
心跳间隔，超过3秒门槛，仍需定位。CUA查询/截图与停顿的因果尚未确定。交互证据在
`~/.cache/ryupy/validation/explorerpro-interactive/`，测试用.app包装尚未成为编译器产品功能。

主审发现IMPORT_FROM的name_from布局只存在于3.12+，无版本守卫会破坏旧provider。
保留七版preflight中3.9–3.11失败记录，补真实头文件版本条件后3.9–3.15严格native
编译全部通过；日志`generic-interfaces-20261117/native-versions-guarded.json`。
函数现场后续变更还需同样复验，平台运行与完整frame能力逐项记录。

## 性能与集成门

六输入独占AOT对照发现普通foreign键识别使原字典/集合热路回退30–48%，随后只对
精确本地布局恢复原hash/equality入口。相同冻结CLI的单变量对照，字典、集合、容器、
推导式墙钟分别下降约28%、22%、44%、32%；推导式RSS增加2.219MiB，hello增加
0.121ms，字符串近乎持平。原不利数据、CPU/RSS分列与六输入范围完整保留于ADR-0238。
修复在独立worktree完整make check通过后以`207ebd6c`提交并推送。

启动专项直接启动实际产物，到首个真实100ms Qt心跳停止计时，关闭report/audit/stats，
每轮使用新私有profile和证据目录，平衡顺序；首次观察另列，未清除系统缓存。固定3.12.13
的三轮中位数为CPython 0.338秒、目录AOT 0.481秒、OneFile 8.848秒。首观察为
0.425/0.637/11.161秒，完整逐样本证据为仓外
`generic-interfaces-20261117/gui-startup-20261117T205119/result.json`。

同一1,391,205,918字节原始载荷含6715项，Brotli Optimal与Fastest的启动中位数分别
10.133/10.502秒，外壳体积434,338,704/540,609,936字节；Fastest未获得收益，继续保留
Optimal。这组到ready后的两进程RSS和为584.125/686.531MiB，仅是同一时点的RSS和，
共享页可能重复，完整运行峰值须另测。去掉压缩的提取诊断为0.73–1.23秒，Brotli为
2.81–3.14秒；清理另需0.43–0.48秒。这些是诊断分段，启动主表保留完整进程墙钟。

带trace的同载荷诊断首轮进入Main前3.237秒、CPU仅0.014秒；系统AMFI至syspolicyd
同路径评估为3.220秒。首轮内层同类评估另为3.114秒；重复外壳进入Main为6.8/8.5ms，
提取2.759/2.888秒，内层到ready为1.295/1.278秒。后续同一外壳交叉临时目录六轮，
`/tmp`为10.950/8.064/7.557秒，系统临时目录为8.555/8.711/8.667秒，pre-Main均
8–26ms；系统临时目录未稳定复现4秒启动。目录AOT首次观察也存在逐原生镜像
评估波动，记录于仓外`onefile-compression-20261117/startup-os-diagnostic/FINDINGS.md`。
测试未修改系统安全设置。

另一个仅延迟默认builtins初始化的Runtime候选在六轮平衡采样中为旧0.405秒、新0.408秒，
到ready后的RSS时点值151.461/153.133MiB。该候选没有收益，撤回该优化；负结果保留在
`generic-interfaces-20261117/startup-runtime-pair-20261117T214759/result.json`。

最终采用同一源码快照`3a47968a`、同一冻结Runtime组，分别构建严格全架构、严格目标
架构和最小改写目标架构的目录/OneFile共六产品。构建输入和原输出全部删除后，首观察
另列，每种形态四路轮转八轮；主计时不启用report/audit/stats，到真实Qt首心跳为止。

| 形态 | 全架构严格 | 目标架构严格（默认） | 目标架构最小改写（实验） |
| --- | ---: | ---: | ---: |
| 目录AOT启动中位数，秒 | 0.3980 | 0.3981 | 0.4050 |
| OneFile启动中位数，秒 | 10.5868 | 9.4762 | 9.6530 |
| OneFile首心跳时点RSS和，MiB | 590.508 | 421.227 | 419.813 |
| OneFile文件大小，字节 | 434388240 | 255315600 | 255299088 |

架构收敛使OneFile体积下降41.22%、ready时点RSS和下降28.67%；本机该输入的启动
中位数下降10.49%，八轮中六轮更快。时点RSS包含外壳/内层，共享页可能重复，峰值另验。
最小改写的分组中位数略慢，OneFile成对统计偏快，数秒波动使估计依赖统计口径；本轮
不确认其启动收益，保留为默认关闭的Cargo实验。完整授权、identifier及runtime标志恢复
独立保留在默认严格路径。默认目录启动约0.40秒，OneFile仍有明显提取和系统评估成本。
证据为仓外`startup-products-20261118T000504/measurement-20261118T002204/result.json`。

## 内部依赖目录与完整发布组合

非OneFile公开根仅保留实际入口和`internal/`。JIT使用官方SDK apphost，托管程序集、
runtimeconfig/deps、Python/原生依赖、资源、pyi/机器接口和清单都在内部；版本化marker
明确依赖根，旧无marker宿主保持原定位。Unix应用名`INTERNAL.py`自动用`INTERNAL.bin`
入口，中文空格、相对输出及预建空目录的JIT/AOT搬迁均通过。缺SDK时给官方SDK页及
macOS/Linux安装命令；真实无SDK、有Runtime环境中的两个程序已核验中文引导。

初版16个真实产品覆盖Cocoa、共享归属、接口导出、声明原生依赖；两独立C ABI声明
另完成JIT/AOT四格。最终Cocoa JIT/AOT补验真实事件循环、截图及完整加载边/ID/ABI/
架构审计。所有产品删除源码、venv、组件输入与原构建树，整体搬迁后核对同次CP三流。
证据分别位于`internal-layout-cocoa`、`internal-layout-shared`、`internal-layout-native`、
`interface-exports/20261117T143426.916219Z`、`internal-layout-cabi-v2`和`internal-layout-v3-cocoa`。

两独立fat消费者×JIT/AOT/OneFile六格最终全过，实际扩展调用/回调/异常、无扩展名helper
执行、原授权/identifier/runtime标志、声明资源逐字节读取、退出终态清零均参与核验。
fat64测试先签真实thin slice再组容器，平台generic外层签名不充当架构内授权证据；实际
重签发现preserve-metadata会丢授权，改为修改前读取完整plist、重签显式传回。证据
默认严格目标架构的最终v7证据`macho-slices/20261117T164724.828152Z/results.json`，
18项映像实际走保授权严格重签；早期v5实验结果单独保留。合同见ADR-0240/0241。

显式ResourcePlan优先驱动模块定位与真实D.__file__，provider编译源码库存继续登记，
无声明时保持原BundleRelative。两独立AOT资源应用完成相邻/子目录读取、删除所有输入
搬迁、真实薄调用及关闭核验：`stock-resource-locations/20261117T153147.911475Z`。
独立SQLite/resolver八进程覆盖JIT/AOT、旧flat/新internal、默认/既有自定义resolver；
现有自定义resolver未被覆盖。完整CPython.Tests两模式各45/45、0跳过，补修已释放
foreign实例在carrier前的纯托管存活检查，保原ObjectDisposedException与镜像查找顺序。
日志`cpython-tests-full/20261117T154106.749375Z`。

JIT失败来源报告存于公开根之外的独占诊断目录，CLI明确显示位置；失败不会覆盖旧产品。
测试工作树移至仓外独占目录，work与product分离，e2e结果通过RYUPY_E2E_RESULTS显式
保存；原语义、错误码、trace顺序与资源失败路径断言保留。最终联合门计数待完成后补齐。

函数定义点builtins与Python回调来源现已完成有限合同：两套独立模块输入双版JIT/AOT
八产品通过，原pending原样迁入四模块sunrise场景；canonical、外来及字面量builtins
均补验真实CP dict类型，四产品通过。日志为`20261117T121951.009514Z.json`、
`20261117T122126.203157Z.json`、`20261117T122218.500702Z.json`、
`20261117T122351.389323Z.json`。Runtime相关41项及冷边界两项通过。

来源选择区分真正新CIL帧和Python内部协议。Python A调用CIL B时切到B的真实D及
定义点builtins，返回后恢复A；无新增CIL帧的协议回调保持A的globals/locals/builtins及
code/file，反向四产品亦通过。CLR错误展开残留的诊断帧在错误导出期不能取得现场切换权。
运行期载体在业务执行前以对应版本真实PyFunctionObject布局安装owned func_builtins，
业务D保持原样；该布局使用目标头文件构建，七版本严格编译已复验。

完整函数frame仍有明确待做项：原样差分中CP为`(True,17,'read_frame',8)`，候选为
`(True,None,'<module>',1)`，对应globals已正确，locals/name/line继续开发。真实异常
产品另发现CLR入口EH强根延迟释放载体；入口现用NoInlining私有内层结束catch栈，外层
再执行Finish；十二个双版JIT/AOT搬迁产品已通过RuntimeError、字符串SystemExit及原生
KeyError的退出码/stdout/错误末行核验，真实carrier/token/origin均为零。完整frame四项
继续保原语义红测，其关闭计数已为零。产品索引为仓外
`~/.cache/ryupy/validation/ownership-carrier-final-20261117.json`。

完整ExplorerPro新profile已越过生成器except，当前到archive_service.py的实例
dataclass合同。FBA同profile完整原入口重新构建，已越过模块globals，当前E0100拒绝
实例模块async def，日志`github-large-build/fba-20261117T140445.762989Z`。原完整入口、
原业务probe、窗口交互与各发布形态分别登记。最终联合
完整门及必要边界成本在冻结实现后追加；C1–C4完整闭包和默认晋升按各自门槛验收。
