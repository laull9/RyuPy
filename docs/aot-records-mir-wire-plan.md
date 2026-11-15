# 实体key的MIR接线准入设计

状态：源码诊断切片，尚无rewrite。Runtime候选22条CP事件相等，不是编译器接线证明。
先保存真实源码经build_program和完整默认pass后的MIR，再决定匹配窗口；不能猜临时槽号。

## 必须保留的顺序

1. Python先求值get接收者并捕获属性目标，再求值get实参。
2. 调用get后执行加法；只有加法成功才求值赋值目标接收者/下标并set。
3. 已物化tuple必须原样进入候选；miss插入同一key，hit不替换原存储key。
4. 原MIR DynCallMethod.captured代表预先捕获的callable或原接收者；Runtime测试的
   CallMethod不能替代编译器CallCapturedMethod。非精确dict必须按原捕获目标调用一次。

## 第一版保守边界

- 优化时点为原get调用处，保留此前全部捕获及实参求值。
- 只有get返回值及add结果均无其他use、加数确为整数1、默认值为整数0才可候选。
- 从get到set的整个切片必须证明无用户代码、可观察Line/locals同步、目标重绑定、
  异常处理边界或可能抛出读取。否则保持原MIR，不尝试移动这些行为。
- set接收者/key必须由同一原始值构成；不能因同名local而忽略中间定义或闭包修改。
- runtime守卫须核sealed精确PyDict、已物化两个exact string的PyTuple、键域和整数值。
- guarded快路false后必须回原始get/add/set CFG，不在新helper内擅自重放未验证的捕获。
- accessor生命周期/跨迭代缓存必须单独证明。逐次新建accessor会重扫表，不能默认当收益。
- 禁止改动用户native/C ABI文件；任何新托管RuntimeFn只在独立实验开关接线。

当前真实diagnostic包括records、返回可观察key、非字面加数回调、locals观察；保存默认流水线
前后完整函数。诊断成功不等于matcher命中，不能将4个成功解析样本算作4个优化。

## 后续接线与证据门

1. 对真实MIR实现有界matcher和负例；审查捕获目标、别名、定义支配、EH及逐语句顺序。
2. 实验臂结构证据必须有新守卫调用和原始fallback CFG；控制臂无守卫。逐pass validate。
3. 同次提交、固定CP3.12.13、SDK10.0.403，执行CP/JIT/NativeAOT输出+异常+身份关系差分。
   原15+11Runtime合同和22事件/独立known门不能替代编译后源码差分。
4. 活动迭代、溢出、用户hash/eq/add异常、外部删插、key逃逸与非精确dict均需源码覆盖。
   dict子类tuple既有不兼容保持独立报告；不能计入相等通过。
5. 同runner随机或交替顺序控制/实验，至少6对独立进程，固定输入和输出校验，报告原始样本、
   中位数、分配和启动成本；分开records完整程序与聚合循环，不把冷进程当暖态。
6. 新实验未通过真实差分、性能及独审前不进入默认dev流水线。

## 只读matcher原型

`records_increment_plan`仅以cfg(test)编译，不注册pass，不改CFG。它从实际记录的连续八句
(snapshot/is_module/capture/box0/get/box1/add/set)要求精确操作数相等，七个临时量唯一、
无命名、非参数、每个仅一次定义且无窗口外使用。保留原receiver/key，不依赖名字匹配。
EH、locals观察、cell、生成器及预算外函数拒绝。匹配表示可进入下一层guard设计，
不是接线成功或收益证明。结构正例和篡改负例首先在隔离CI验证，再实施CFG拆分。

## CFG实验切片（未验证）

现已新增显式feature `records-increment-experiment` 暴露的实验函数；默认流水线、CLI均不调用。
测试构建可直接运行它。只接受已通过计划器且从entry经普通Goto/Branch可达的块。
逆序拆分每个窗口：原capture/box0前缀不动；新bool守卫true直接进入原后续块，false进入
逐字保留的get/box1/add/set四语句，再跳入同一后续块。原terminator保持，第二次运行不再
重写慢路。EH/locals等拒绝仍在；原key从未删除或替换。Runtime只用sealed PyDict门，失败不
执行用户协议；子类原captured callable在失败CFG继续调用，异常不在helper内catch/重放。

Runtime入口 `PyRecordsIncrement.TryMaterialized` 使用ConditionalWeakTable保存原已验证的
StringPairAccess，避免每次new后重扫表；弱键不延长dict生命周期，仍按Version而非Count
处理外部改动。缓存查找/首次分配的成本必须测量，不能继承此前host基准收益结论。
新的托管ABI为bool(object,object)，不改native ABI。默认程序集可能携带未调用的引用，
但默认MIR不会产生守卫调用。当前没有CLI选择入口，故还不是端到端产品接线可用版本。

新增结构测试核原前缀/慢路/后续/终结器逐字保持、重复运行不重复接线、不可达拒绝及ABI
错参/错结果类型。C#入口合同核身份/装箱、拒绝不执行协议、一次普通回退和缓存预算。
CIL测试实发射并解析PE，核方法签名和调用。它们仍不能替代后续CP/JIT/AOT真实源码差分。

## 真实历史ABI门发现的补录（待独立dev DLL证实）

run43698461290的历史1659门没有删除项，但候选1809签名比手写1799预期多10项。
其中9项属于records开始前固定dev81758bb的生成器驻留状态/枚举/列表守卫实现；
另外1项为d9044214 records探针引入的PyKeyComparer泛型alternate接口，不是dev项。
不改旧provider历史判定。新增单列9条dev增量及其独立DLL合同，再单列records接口1条，
最终才是当前守卫2条。CI必须独立checkout精确devSHA/tree并构建DLL，验证无records项，
不能以当前候选差集替代来源证明。与generator分支合并时须组合各自已审增量，不能覆盖。
