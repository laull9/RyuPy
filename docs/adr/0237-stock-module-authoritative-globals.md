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

生成器表达式、dataclass及其余真实缺口继续逐竖切；默认provider和原生模式分别回归。
权威字典访问的CPU/GIL成本、模块初始化carrier成本与函数热调用成本分别测量，保留
旧编译器/Runtime控制与普通路径结果，默认晋升另过固定项目和完整性能门。
