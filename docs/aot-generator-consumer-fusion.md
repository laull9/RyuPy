# 生成器消费融合：准入与分臂验收

状态：2026-11-15，基于dev `81758bb477729aee4636f2ca6d6b0815b949849a`。
本切片仅增加手写语义输入、同源分臂基准与自检；**没有启用消费者融合，也没有性能收益结论**。
字段驻留首切已有结果不归入本切片。总合同见[架构路线](aot-architecture-performance-plan.md)。

## 当前流水线的实际边界

- `generator_state::build_program`先以`DeferredGenerators`保留高层`Suspend`，随即选择字段
  或旧数组lowering。真正融合需在该选择前分析生产者和消费者，不能事后按`$state_resume`
  名字或状态机PC猜测源结构，也不能让常规pass任意穿过暂停边。
- 普通函数名的动态查找、实际函数owner/defaults与捕获仍须保留。仅知道静态函数名不能
  证明调用目标；生成器创建以后，修改该名字不能改变已创建实例。
- HIR记录生成器表达式来源，但MIR的`is_comprehension`目前仅指急切推导式。不能复用
  该bool把genexpr当作list推导式；候选计划须显式保存生产者来源与消费种类。
- `PyGenerator.MoveNextCore`包办running、异常上下文、真实traceback、PEP479与关闭。
  绕过逐项委托前须为这些行为建立等价边界，不是只把循环体拼接起来。

## 高层计划的最小合同

先只分析有限的无EH、无yield-from、无locals观察的字段状态候选，保留其他路径不变。
计划至少携带生产者身份、创建点及首次启动点、yield/resume映射、消费者种类、逃逸证明、
每条提前退出边与物化映射。分析预算耗尽或任何未知操作应拒绝，不作乐观猜测。

1. 入口：先按原顺序求值实参和最左iterable/取迭代器；尚未消费时不能执行生产者正文。
   真实函数身份守卫在任何生产者副作用之前失败才可回到原工厂。
2. 单项：生产者逐项计算、过滤和yield顺序不变。消费者回调会观察共享cell和全局变化；
   未证明无观察的调用是区边界，不能把读取移到循环外。
3. 已有对象与逃逸：同一盒、列表别名、保存的生成器和跨桥引用原样物化。证明为未逃逸
   才可消除生成器实体；若保留实体，其暂停PC、局部、current与关闭状态须同步。
4. 提前结束：普通for的break或消费者异常不自动close仍可达的生成器。不能为了资源清理
   插入原程序没有的close，也不能为了吞掉错误继续多消费一项。
5. 异常：生产者StopIteration沿PEP479；消费者加法/循环体抛出的异常不属于生产者。
   consumer帧与producer帧的所有权保持独立，禁止把两者放入同一个异常转换catch。
6. 失效：若区内守卫失败，只能从精确当前操作物化后继续，不从入口重放副作用。
7. yield-from单独实现：send、throw、close、返回值及委托暂停都有合同；不能以普通for
   或sum成功替代。首个无委托候选通过也不勾掉整个G。

## 本切片输入与可重复命令

`tests/e2e/phase3/aot_generator_consumers.py`覆盖普通for、sum、yield-from，最左iter立即求值、
过滤惰性、函数重绑定、共享cell、break/消费者错误后的别名恢复、显式close、sum加法失败
停止点、对象身份和PEP479。产品差分尚未运行，因此保留pending；pending不计通过数。

`tests/perf/generator_consumers/`的三个手写文件分别复制原始generators的一个消费臂，
保持函数、次数、表达式与模块级累加形态。每个进程只额外输出自己的总数。
`test_generator_consumers.py`以AST核对其与原源的精确关系，并核对三结果之和。

```bash
python3 -B tests/perf/test_generator_consumers.py
python3 -B tests/perf/aot_hotspot_pair.py \
  --old /absolute/frozen-old/ryupy --new /absolute/frozen-new/ryupy \
  --python /absolute/python3.12.13 --suite generator-consumers \
  --runs 7 --output /external/generator-consumers.json
```

配对脚本默认仍为原始26项，不把三个归因项混入原套件或几何均值；报告显式登记suite。
配对前同次核对CP完整双流和退出，CP/A/B与CP/B/A交错，每臂独立新进程包含启动成本，
分配/GC另轮采样。诊断臂彼此不可加总推断原程序速度或RSS；最终仍须运行未修改的
`tests/benchmarks/generators.py`和原始26项，独占计时、NumPy与删除输入搬迁另行验收。

## 当前验证与未完成项

本环境仅运行了可用CPython的语义输入、自检2项和`git diff --check`；这些不是固定
3.12.13产品差分，也不是Rust/C#、JIT或NativeAOT通过。无本地cargo/dotnet时交由CI验证，
在实际产品通过前不移除pending，不默认开启新路径。

后续按次序：真实高层候选/拒绝结构测试；同次CP/JIT差分；融合候选的原始AOT三臂与
完整原程序配对；异常/身份/桥边界与搬迁；完整make check。只有这些证据闭合后才能
登记具体命中的消费者与收益，不将测试设施完成写成优化完成。

## 首个真实融合切片：精确整数sum的批量恢复

以下为待实现设计，不是已存在ABI。先从原程序的genexpr_sum臂切入，普通for和委托仍保留。
它复用现有`BuiltinSumFast`的long/dirty合同，不重写浮点求和、不新增用户协议快路。

### 为什么不直接把循环内联到消费者

把生成器正文直接拼到sum所在函数会抹掉真实producer帧，并把生产者StopIteration与
消费者加法错误混在一个异常域。把生成器改成list再求和则破坏惰性、内存和错误停止点。
首切选择在生成的恢复入口内消费连续精确long，再直接跳到原resume块，绕过每项委托
往返；生命周期仍由PyGenerator持有，sum只在准确暂停边接回原实现。

### 高层计划与窄准入

计划在deferred lowering前生成，禁止根据后端名字判断来源。其最小数据形状为：

```text
SumConsumerPlan {
  consumer_function, consumer_block, consumer_statement,
  producer_function, created_generator_local,
  captured_callable, iterator_creation_statement,
  suspend_edges: [(yield_block, resume_block, input_local)],
  disallowed_observations, materialization_map
}
```

只接受直接单次消费、显式真实内建sum调用和单位置生成器输入；start未给或已确认精确long。
先拒绝star/kwargs、未知callable、消费前别名/容器保存、EH、yield-from、locals观察及trace V1。
生产者沿字段状态准入；保留生成器实体和owner，首切不做对象消除。闭包需要共享cell
物化合同，不能为了命中原基准擅自快照，首切未证明则拒绝。

参数绑定与`PyIter.Create`仍在`BuiltinSumFast`原位置执行；不能让新批量协议提前消费，
也不能绕开真实sum被重绑定的调用路径。计划中的消费者身份必须由调用构建器已有真实
callee证明，而不是按字符串"sum"搜索；输入表达式已有副作用不能为守卫重复求值。

### 批量边界的状态合同

候选控制载体增设私有批量累计状态（仅计划命中的恢复入口读取），保存原生total、dirty
及已消费项数。入口用当前sum累加状态初始化，不重新装箱原start。控制状态不能进入Python
对象图，不用于替换用户值。

每个yield的发射顺序必须是：

1. 像原实现一样先计算完整yield值，发布下一个恢复PC与所有跨暂停局部。
2. 若批量模式未激活，直接沿原yield返回。这不是全体生成器新增的通用慢分支，首切只
   对计划命中者生成对应入口；普通恢复入口仍保留。
3. 若value是精确CLR long且与当前total相加无溢出，更新total/dirty/消费数，发送值置None，
   直接跳转原resume块。原`GeneratorInput`仍在该块消费None并清控制槽。
4. 若非long或溢出，**不执行该项加法**，沿原yield返回该对象。此时生成器已精确暂停于
   本项之后；外层sum物化左值，再用原Binary处理这一个current，不重新next、不重放正文。
5. 正常Return把PC置-1，终值仍由原MoveNext协议丢弃。累计结果与完成状态分开，不能把
   生成器return值加入sum；Finish清引用后仍须能取得累计状态的值副本。

批量出口至少区分：`completed`（原生成器耗尽）、`yielded-current`（该项尚未相加）和
`unavailable`（完全未恢复、调用方应走原MoveNext）。不能只用bool混淆后两者，否则容易
重复消费。累计状态必须finally解除，以免后续next/send/throw/close意外继续批量模式。

### 异常所有权

批量resume由原MoveNextCore的running/context/frame/PEP479边界包住。精确long无溢出
加法不调用Python代码；每项之间没有消费者回调，允许把这些连续恢复合成一次进入。
producer自身的调用仍保留自己的Line及调用帧，发生异常立即沿原PEP479/Finish流程退出。

非long/溢出的Binary严格在MoveNextCore返回以后执行；用户__add__/__radd__失败不应
关闭已经暂停的生成器，也不应被包装为generator raised StopIteration。此错误路径是
首切最重要的负例，不能靠成功sum样本代替。

### 发射与回归清单

- MIR：显式批量状态/offer操作和两恢复入口，不复制函数身份；原yield/resume映射逐条断言。
- RuntimeFn/验证器/CIL引用表同步新增ABI，codegen只发射显式MIR，不推测Python内建。
- Runtime：复用BuiltinSumFast累计逻辑；增精确暂停出口处理，保持dirty=false原start身份。
- C#合同：空输入原start身份、long连续累计、正负溢出、异构第一个/中间元素、恢复后送None、
  首次非None send拒绝、失败后next位置、返回值丢弃、累计控制finally清除。
- MIR负例：未知sum、别名逃逸、闭包/观察、EH、yield-from、共享resume块与预算耗尽均不命中。
- 性能证明必须同时检查生成入口里连续yield边已变为回边，以及运行计数显示每元素恢复
  调用确实减少；只看墙钟或字段读写数量不能证明消费融合。

若基于此合同的首切不能在原始genexpr_sum命中，则先记录具体拒绝原因，不扩大守卫或
放松语义换取图表改善。普通for的任意循环体拥有消费者副作用，需要另外的双帧执行区；
yield-from还必须保持完整控制协议，不能复用“精确long无消费者回调”的证明。

## 独审后Runtime协议原型（未接编译器）

实际入口修订为真实`BuiltinSumFast`内部对真实`PyGenerator`实例能力检查，不做静态
sum名字认定、不再次查找/调用工厂。保留原六参字段构造器，新增七参附带`PyResumeSum`
的构造器。编译器目前完全不发射该重载，生产生成器均无能力；这不是默认启用声明。

原型让批量入口经过同一个MoveNextCore生命周期。`PyGeneratorSumState`只存在于本次
调用栈，不挂到可恢复实例，因此异常或消费者回调后没有残余批量模式可泄漏。返回值记录
Completed/Total/ConsumedAny；返回false是完全未执行的unavailable，成功且非Completed
时Current是唯一待相加项。真实sum在外层处理该项并维持dirty，空输入和用户回调返回的
long原盒不丢失；trace V1运行时直接拒绝批量，不提前消费。

C#手写恢复器覆盖一委托消费多项、零增量dirty、空start身份、正负溢出、消费者
StopIteration不触发PEP479、不关闭且继续next、已暂停next/send、生产者PEP479、回调
long身份、生产者重入拒绝/消费者回调恢复和trace拒绝。这里只是已编写测试，尚未编译。
后续须由独审及CI验证，然后才接显式MIR生成入口；不把手写恢复器算产品融合完成。

高层准入原型另在`generator_sum_plan.rs`以test-only模块实现：读取显式Suspend和
residents，拒绝共享resume、字段资格失败、未知语句、动态协议与通用迭代，接受有限原生
标量/局部移动/装箱。该白名单尚不证明原始generators中的range与动态操作，因此有明确
负断言要求原程序各生成器全部拒绝，不宣称已有原基准命中。待结构测试实际编译通过、
精确range来源与观察证明补齐后才接产品lowering；现阶段没有产品开关可误启用。

## 精确range候选的闭环与失败回退（尚未产品接线）

原始三源拒绝点由构建器源码及新增高层结构断言登记，仍须CI运行确认：

| 原源 | 高层MIR阻断 | 本切片能否覆盖 |
| --- | --- | --- |
| count_up | DynRichCompareValue/DynTruthy的动态比较及DynBinary(Add,inplace) | 否，普通for另需消费者执行区 |
| chained | GeneratorDelegateStep与delegated Suspend，未通过字段资格 | 否，完整委托协议单独证明 |
| genexpr | 隐式`.0`上的IterStart/IterNextOk/IterValue与DynBinary(Mul) | 只有新增真实游标guard及精确整数事实后可能覆盖 |

test-only `DeferredGenerators.comprehension_inputs`从HIR的`is_generator && is_comprehension`
和现有FnId映射登记隐式输入LocalId，不通过`<genexpr>`或`.0`名字授予资格。
`generator_sum_range_plan.rs`从此证据建立显式单游标候选：guard_input、迭代器定义、乘法
定义、yield块及resume块。当前检查单静态定义、CFG支配/块内定义先于使用及事实收敛，但**运行时guard和后端
发射尚未接入，因此只是关闭态候选，不是产品证书**；编译器默认不含此模块。

运行时guard必须作用于工厂已求值/绑定的真实输入对象，精确类型为密封`PyRangeIter`。
`PyRangeIter`自身实现IPyIterator，PyIter.Create返回同一实例；不得创建新range或新游标。
不看range函数名，不重新执行工厂/iter，不执行任何MoveNext；guard失败就不附能力，
原普通恢复入口完整接管。不能先进入MoveNextCore再用pending-current假冒guard失败，
否则首次启动时点和当前元素会出错。

该guard只证明迭代协议封闭、不调用用户代码，不证明数值范围。IterValue仍读同一个
原游标，结果是精确long；boxed常量2也是精确long，故乘法可以保留原内建整数路径，
却不能标记其结果恒为long。原`PyInt.MultiplyI64`的BigInteger升级必须保留；乘法升级后
正常发布当前yield/resident再回sum外域，既不能丢弃本项，也不能把它重做成long乘法。
sum自身的加法溢出另走同一pending-current边，由外层原Binary精确加一次。

首切不改`PyRangeIter.MoveNext`，其已有long步进极值边界不因sealed类型而消失，不能宣称
完整CP范围兼容；如果以后做原生计数或闭式预求和，必须另有Int128边界与整数升级证明。

生产者纯区不得有用户回调、cell/global观察或未知协议。消费者回调仍在暂停态执行，
可以通过别名推进原游标或控制生成器；再次进入批量必须读取当前PC和原游标真实位置，
不能使用上次预算/复制的counter。本轮补充手写pending：range替换及零预消费、创建后
重绑、部分消费/外部消费原游标、用户乘法回调、乘法与累计各自正负溢出、消费者回调推进
游标后恢复。当前只运行CP输入，不算RyuPy兼容或融合通过。

候选额外加入全CFG支配收敛，包含Suspend的resume边；预算不收敛即拒绝。每个非参数
局部的定义必须支配全部读取，同块还须严格在读取之前。测试故意把乘法移到IterValue
之前，证明“只有一个静态定义”不等于安全事实。此检查仍未在本环境编译运行，不记通过。

独审补正：高层返回epilogue仍可能有Eval(TracePopTo)。range候选只准许单参数且该参数
已被证明为真实TracePush/TracePushWithOrigin结果、并通过定义支配检查的TracePopTo。
任意Eval、常量伪深度及换成其他RuntimeFn均拒绝；正例与两种变异负例独立保留，未运行。

来源验证入口要求FnId和输入LocalId同时匹配同一DeferredGenerators见证；非法或不匹配ID
拒绝，不索引非法arena。另加先用后定义、替代控制流分支才定义、共享resume以及伪帧
深度负例。全部仍为test-only，不能用跨块事实汇总替代这些支配与来源检查。

进一步收紧trace匹配：只接受唯一且位于函数入口块的TracePush/TracePushWithOrigin定义，
TracePopTo必须直接使用该唯一LocalId，而非仅凭TraceDepth类型。重复入口、其他深度
以及任意Eval均拒绝。新增循环首次读（定义故意移到Suspend以后）和跨暂停字段映射丢失
原游标的负例；候选持有residents，发射映射必须覆盖全体活值和输入/原游标，不重建游标。

## 尚阻断产品发射的两项证明缺口

独审确认test-only原型不是证书，以下不可用“结构测试通过”代替：

1. `resident_set_requirements_hold`只自检候选要求的集合。它未核对实际lowering生成的
   LocalId→字段号/字段类型映射，未证明每条yield前的写回或resume后的真实读取。
   发射层必须给出实际映射，并按显式yield边逐项核查原对象引用和字段覆盖；不能把
   `plan.residents`拿来与自身比较就宣布物化正确。当前测试名称明确改成集合要求。
2. arena的FnId/LocalId只是数值，同样源码两次构建可以相同。当前`analyze_source`
   参数可自由组合Program和deferred，无法证明来自同次构建。新增反证测试刻意展示此
   碰撞并允许当前test-only候选识别，**该测试通过说明缺口存在，不说明资格安全**。

建议产品接口沿既有PreparedHigh的拥有式模式：私有PreparedSum同时拥有Program、显式
来源元数据和计划；分析后不暴露可变Program，lower消费该对象内部同一版本。link或
其他结构改写失效后重做分析，不接受外来ID/元数据组合。不要简单保存Program指针：
Program可以移动，指针既不是稳定构建身份，也不是未改写证明。此封装与实际字段映射
验证尚未实现，故仍无产品发射、无默认启用、无原基准融合命中结论。

## Runtime首次CI失败及修正范围

run43679743022实际提交`66c2db9e94b45b03774c70f59ac08c60f1829dcc`：定向15通过/1失败；
全Runtime3190通过/2跳过/8失败。结果不可写成通过。

- 负溢出测试最终`long.MinValue - 1 + 7`重新落入long范围，原断言错误要求BigInteger盒；
  原PyInt.Normalize明确允许收敛。改成独立BigInteger数学预期，再显式断言正确long或
  BigInteger类型和值，不改变产品数值逻辑。此修正仍待重跑验证。
- TextWrap两项失败在TextWrapOracle.Read，CI未提供固定3.12.13的PYTHON绝对路径。
  属于工作流配置，不能删除预言机或修改测试版本要求充作产品修复。
- 五项真实PE审计准确抓到4参MoveNextCore和TrySumBatch新trace读取不在批准合同。
  经独审选择保留显式调用栈参数，精确迁移私有MoveNextCore的四参数签名与指令/EH检查；
  TrySumBatch只获get_Enabled读取许可，旧三参签名/其他调用/opcode仍拒绝。没有通配扩权。
  新增实际PE变异：TrySumBatch读开关改成Exit必须拒绝；交换已允许的Enter/Leave两个
  同签名目标也必须由finally顺序检查拒绝。变异PE从不加载/执行。

原型delegate、状态类型及七参构造器改为internal，六参公开ABI保留；新增公开形状测试。
没有历史DLL专项通过声明。将来CIL跨程序集接线需要正式公开ABI手写增量和历史DLL专项，
不能直接把本内部构造器作为产品ABI。所有修正仍待真实CI与trace专项验证。

## 拥有式准备与实际字段核查候选

新增test-only `PreparedSum`，唯一入口从source内部同次构建Program、Deferred来源和range
计划；字段私有，没有from_parts、外来ID入口或可变Program getter。finish消费所有权，
调用真实普通generator_state lowering，再核查原高层快照与实际产物；其余生成器仍走
原有字段/旧数组后备。此候选没有生成批量委托，不触及默认编译流程。

`generator_sum_fields`不读取`slotN`名字或假定字段顺序。关系来自真实工厂参数与恢复体
SetField的原LocalId；随后逐原语句核查读字段、实际操作数重写、赋值后立即写回，同一个
payload类/实例、每个yield前发布PC、resume首条GeneratorInput、有效PC派发，以及原
函数owner引用。不能只汇总“某处写过字段”放行错误路径或延迟写回。

现有lowering可能先把trace深度列为跨暂停活值，再删除合成TracePush/Pop，留下无用字段。
验证器只允许对应被删除trace定义、无名字/参数身份且无其他读取/赋值的局部成为这种
死字段，要求默认常量初始化和零实际读写；不按名字猜该字段所属局部，不借此放宽用户
resident物化。实际读写的字段必须一对一关联，类型和构造参数一致。

手写结构测试调用真实lowering后变异：错读/错写/遗漏读写、写回移到另一位置、错PC和
错派发、工厂参数及owner错配均应拒绝；完整同步重排字段/构造参数/读写编号仍应通过，
任意修改字段名也应通过，以防验证器依赖命名或原布局自证。独立两次Prepared构建各自
完成，不存在把另一Program数值ID插入准备对象的公开入口。

这些为已实现但未编译的候选检查，仍须Rust CI及独审。核查使用统一local_access操作数
读写定义，不宣称独立形式化验证；若以后改变状态机形状，必须更新显式证明或保守拒绝，
不能因为同属字段生成器自动接受。实际批量入口/runtime guard/CIL ABI仍未接入。

## 字段核查器的第二轮独审修正

独审指出两个先前漏检路径：新增Goto派发块可夹带语句；两个GetField可共用目标临时并
同步改写后续运算，让原映射核对自洽而错误。新增手写mutant从真实lowering出发：插入
带print调用的Goto块，以及`yield a+b`的两次字段读取合并目标并改写DynBinary；均先验证
原始产物、确认变异实际发生，再要求拒绝。它们尚未在本环境执行，不称实测通过。

修正后除已逐项核验的恢复入口外，新增Goto派发块必须为空；每个合法PC完整遍历派发，
所有新增块必须归入已访问集合。新增空Goto桥仍为明确正例，不能为了拒绝副作用误杀
正常跳转。新增局部定义必须全函数唯一且不能覆盖恢复参数，GetField临时另有独立
全局唯一集合；这也防止跨语句复用或覆盖控制槽。额外未覆盖块单独拒绝。

这些加强不把候选提升成产品证书；实际Rust运行、更多异常/物化独审及批量入口接线仍待完成。
