# ADR-0237：完整源码模块的权威globals

- 日期：2026-11-17。
- 状态：显式候选实施中，完整原项目与默认晋升分别验收。
- 接续：[ADR-0236](0236-compiled-native-call-carrier.md)、ADR-0208/0223。

## 真实触发与选择

ExplorerPro原入口编译通过后，真实Cocoa业务在`QApplication([])`进入原生扩展时崩溃。
扩展调用通用C API `PyEval_GetGlobals()`并读取`__file__`，旧顶层CIL执行没有CPython
frame。模块级carrier需要实际权威globals，Legacy静态槽尚不能提供该字典。

复用现有InstanceGlobals存储和LEGB语义。显式
`--experimental-stock-module-namespaces`选择冻结的本地编译图；独立
`provider_namespace`能力和`RegisterStockModuleSourceLocated` ABI声明真实CP字典需求。
旧入口首切、普通Instance与薄函数授权保持独立。编译器中没有第三方名称选择分支。

采用同一真实模块字典，避免第二份快照与双向同步。CIL读写、公开`__dict__`、外部原生
写入和重绑定都进入同一后端，任意新增名字和builtin遮蔽按LEGB实际读取。实例module
和函数定义来源沿原context传递，跨模块A调用B的原生边界读取B的实际字典。

## 注册、分配与导入事务

Runtime新增可选`IPyCompiledSourceNamespaceProvider`；普通模块由provider通过公开
`PyModule_NewObject`分配独立模块及md_dict，入口复用规范`__main__`原字典。
工厂创建阶段不执行Python业务，也不提前写sys.modules；实际发布、部分初始化、失败
回滚和重试复用loader事务。Runtime模块与原生模块登记双向弱身份，原生模块不持有
CLR模块代理。现有可变键、metadata和canonical builtins沿真实后端设置。

carrier执行机械层见ADR-0236。Legacy和本地PyDict来源保持原调用能力；只有已具
真实CP字典的候选取得新现场，跨域真实对象继续核验Host。候选资格逐项证明，禁止通过
删除旧首切预检来冒领未实现语义。

## 逐行为与成本门

先验两个独立模块的globals身份、外部改写/删除、builtin遮蔽、重入与异常，再验最小
Cocoa的真实窗口/截图与删除源、venv、构建树后的JIT/NativeAOT/OneFile搬迁。
完整原入口随后按首阻塞推进。急切推导式继承当前执行环境；普通def在定义点捕获，
AnnAssign、SETUP_ANNOTATIONS和复杂except各自保即时CPython差分及结构门。

同步生成器与惰性生成器表达式沿既有GeneratorFrame保存真实context，工厂与恢复体各保
模块来源。直接globals的源码callee快照仍只求值一次；验证器只豁免精确私有帧/槽位的
机械spill/restore，邻槽、任意重写与额外读都拒绝。运行产品同时检查send/throw/close、
finally、yield-from终值、再次耗尽与跨暂停的同名callee及返回对象身份。

真实产品发现provider的tp_iternext旧桥调用布尔MoveNext而丢失生成器终值。精确
PyGenerator转发到既有Next协议，保留本次StopIteration.value及后续空args；其余迭代器
继续原协议，不另存终值镜像。生成器源码handler按下节合同扩围；dataclass及其余缺口继续逐竖切；
默认provider和原生模式分别回归。

规范globals/import/build_class身份在解释器初始化后、应用执行前捕获原对象；活builtins
字典内容仍共享。用户提前替换不能被重新登记为canonical。无显式基类使用目标解释器
规范PyBaseObject_Type，避免可覆盖导入和builtins.object改变语言默认基类。旧legacy
类桥只在启动时owned保留必要wrapper，描述符继续lazy；业务阶段不额外调用导入钩子。

权威字典访问的CPU/GIL成本、模块初始化carrier成本与函数热调用成本分别测量，保留
旧编译器/Runtime控制与普通路径结果，默认晋升另过固定项目和完整性能门。

## 生成器源码异常链的逻辑路由证明

决定仅在`provider_namespace`能力下扩围同步生成器handler。普通Instance源码链继续
要求一条对应的真实CatchAll；新GeneratorFrame保留同一SourceHandlerContract，并携带
共享逻辑路由旁证。源码target表达式和as清理由既有动态链降级，未增加业务名称分支。

验证器复用GeneratorLayout/FrameCatchRoute和逐槽Cursor，重走最终CFG：唯一物理
catch接原异常，实际active的Ge/Lt窗口按内层优先首次匹配，typed合成链读取真实类型
并逐项匹配，选择handler旧PC后经统一Retry保存局部、frame[0]和frame[3]。恢复入口
逐槽加载，再由实际PC比较跳到重排后的handler；逻辑Catch须读取并清空同一异常槽。
同时核验挂起/完成出口、私有角色互异及frame不外泄。source链0在物理catch留合同锚，
删除全部旁证仍会触发孤儿ID诊断。跨模块链接只重写真实typed目标GlobalId，保持每份
共享证据身份，绝不把不同旁证合并。窗口整数先try_from再checked_add，坏IR走错误诊断。

CFG优化可以清空真正不可达块；豁免依据是最终入口、实际EH入口及恢复PC分支形成的
真实可达图。仍被异常边或PC边到达的handler被改成Unreachable会拒绝。结构门包含
15组实际路由/能力/私有帧破坏，以及usize::MAX、i64::MAX、负起点、零长和越界五组
窗口破坏；两套输入还完整运行默认pass序列。

`inlet_handlers`与`quarry_handlers`采用独立模块/数据布局，同次固定CPython验证
暂停后target重绑、throw同对象、as局部清理、getter失败越过同级handler、for/with
continue，以及send/close时当前异常上下文。双版本JIT/NativeAOT八产品均删除输入后
搬迁通过：3.12 JIT `121559.974900Z`、AOT `121625.617702Z`，3.13四产品
`121625.617720Z`，完整日志位于仓外`validation/stock-module-namespaces`。

保持原ExplorerPro源码复验后，完整原入口与完整隔离业务probe共同推进至
`archive_service.py`的ArchiveEntry/ArchiveSummary实例dataclass资格守卫。其原始
@dataclass、字段注解/默认值与property全部保留；该装饰器与真实类/模块字典和生成
方法的来源合同进入下一竖切。完整原项目和默认晋升仍按各自产品门推进。
