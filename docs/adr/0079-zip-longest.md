# ADR 0079：zip_longest 的真实惰性迭代状态

状态：内部阶段 A 已接线并首次14项C#通过；后续吞耗尽异常现场尾修也已14项复验通过。B真实类型/导出已接线，累计25项通过；首应用窗口Z1过、Z2旧ABI首败后必要窄修，Z2复验和Z3首次均通过；正常完整门禁通过，未公开发布；不称项目通过。

## 语义与结构

固定tabulate0.9.0在候选2a7实际因缺itertools.zip_longest导入失败，见ADR0078。本切实现通用API，不按项目输入形状特判。构造先验证唯一可选关键字fillvalue，再从左到右取得输入迭代器，不读取首项、不调用len/hint或物化输入。

每次next按输入槽从左到右推进，成功立即保存值，再推进下一槽；同一个iterator重复输入不能统一在末尾读Current。某槽Stop后永久退休，以后引用原fill对象；最后活跃槽耗尽直接终止，不返回全fill行。非Stop用户错误原样传播并把整个对象永久关闭，已消费的前槽不回滚。无输入直接结束。

每次推进使用独立局部数组，只有成功后发布新tuple，不修改曾交给用户的结果。保一次有界安全重入，不新增busy拒绝；共享slots/active按真实状态机转移。危险内层耗尽/错误递归未执行参考，不宣称全部重入边界已测。

## 局部协议与成本

仅本API的普通实例获取/推进边界支持类型槽、property及缺iter时getitem序列；iter=None或property返回None不可迭代，真正iter正文错误不回退。CP槽取得/descriptor绑定错误先保原异常现场，再清错尝试getitem；getter副作用不回滚。任意自定义descriptor首切明确NI，守门不在可吞用户异常catch内。

推进共享用户iterator绕既有适配器finished缓存，退休属于zip槽。序列getitem从0开始，IndexError/Stop结束，其它错误传播；不读取lengthhint。原生输入沿PyIter入口保已有关闭校验，不改全PyIter/map/unpack或数字热路。吞耗尽/查找错误后恢复调用者帧深度和ambient，用户可保留的原异常需保已发生的现场，不重包。

状态O(k)：输入槽、active、fill引用和Current；每次尝试一份O(k)数组，成功再新tuple。不仿CP基于引用计数的结果tuple复用，可能多分配；不全量缓存元素、不捕OOM重试、未做性能测量。

## 参考与阶段验收

固定CP3.12.13唯一400行参考，29秒TERM+1秒KILL上限：exit0、0.047840秒、39JSON、空stderr。source SHA cb1766e86699326ac783e1accfe858a8b96527ce149d5cb73b52d67b9d9c6895；stdout SHA13e68ad15e055696948e61f2da4fe605beae4bdd82f2d90acaab37a76aa7ea5c，独立核准。覆盖构造顺序、共享引用、逐槽终止、方法与getter失败区别、sequence及一次安全重入；CP结果不算Ryu通过。

阶段 A 仅新增内部载体/协议helper与14项C#，首次编译与14项全过。随后独立复核要求对raw native callable抛出的Stop/Index在吞掉前也Prepare原异常并锁保存对象现场；该窄尾修另行14项复验通过，不以先前14项冒称覆盖。该阶段真实Type/导出和3应用、完整门禁仍待，不运行新的固定项目、AOT或性能组。

## B 真实公开类型接线

Lazy保存真实原生类型，accepts按专用载体，不以可改TypeName猜资格；实例__class__返回同一类型，模块重绑不改变已创建对象身份。沿PyToolIterator/IPyIterator原next/iter/list/tuple/for入口，无编译器或全局ABI新分支。实例fillvalue未暴露为假可写属性；读写删按实际限定错误，reduce/setstate等pickle反射明确NI，用户派生正例保pending。

原core14阶段出口断言迁为真实导出，B新增5项公共测试。已审3应用/49标签接线，3版本探针另计；这些新增测试随后已累计19项通过。另已确认PyMap.MoveNext会隐藏raw callback Stop对象，已在本API局部调用其已有Next再按原catch准备现场/退休，不修改全map；A14不能冒覆盖该尾项。

## 首次应用窗口的必要前置

累计19项C#包含B5项及map尾差，零失败/跳过。首真实应用窗口按序执行：Z1构造/绑定原CP与JIT通过；Z2 CP通过，但JIT在protocol_state.py第120行构造Done(StopIteration)时失败，原错误为MissingMethod：PyStopIteration缺少`.ctor(string,string,object[])`。Z3因首失败停止未执行；不把runner或参考成功当三应用通过。

独立只读确认生成用户异常固定调用上述protected ABI，而PyStopIteration只有内部终值object构造。当时拟按已批准必要窄前置补实际args/value/延迟message与该基类消息提前格式化排除，原Done子类断言保留，不扩全异常框架，也不把失败移出活跃验收。该阶段修复和后续真实复验尚待；首原源码/流留证。

## StopIteration 必要窄前置与复验

仅PyExceptionTypes/PyErrors两点：补生成子类实际调用的protected(typeName,message,args)，浅拷贝参数数组、保元素和Value首项引用；PyMessage按完整Args在请求时格式化，StopIteration精确加入既有延迟消息分类，构造不提前调用用户str/repr。不改codegen或其它异常基类，旧public终值构造（None表示内部无终值）保持原样。Args当前只读，不冒通用可写异常元数据支持。

新增6项专项与zip19累计25/25通过、零失败跳过。原Z2 Done构造不删，并加0/None/单参/多参、二级派生、Value身份、构造零格式化和显式str才调用原对象的断言；新增两个标签。修后仅重跑Z2、首次运行Z3，两者CP→编译→JIT均通过，完整原流及构造/推进用户错误链保持。Z1沿首轮已过结果，不重复该专项。最终三应用24+16+11=51标签；两窗口实际合计4次应用CP/编译/JIT，4版本探针另计，原首失败保留。每窗口统一600秒含cargo准备、首失败停，不声称每应用独立计时上限。

该阶段正常完整门禁进行中；gate复验另计，当时不预标全绿，不重跑固定tabulate或AOT/性能组。

正常完整门禁首次终态exit0：fmt/strict clippy/workspace、1019项C#零失败跳过、固定CP基线228、e2e202通过与30原ignored，Release零warning/error。完整日志SHA256 `6ed3583569bf02012083678ff5329e49e265e6ed0a49334975c437ff121b068b`。三应用在gate内另复验通过，故本切真实应用执行共7次（首窗口2、修复窗口2、gate3），7个版本探针另计；所有原流留证，不称只跑3次。该门禁阶段未公开发布、未重试固定tabulate，未做AOT或性能组，不更新历史项目分子。

## 后续固定 tabulate 单入口

未发布候选 `105a82644d7a24d85d3b8d7b5a16ad34d5d7b54c`（tree `640cb60ff33390aa8fc51529725223539167143b`）完成一次CLI准备（exit0、0.123883秒），随后只执行一次原锁定tabulate0.9.0 CP→编译→JIT。官方sdist为81090字节，SHA `0095b12bf5966de529c0feb1fa08671671b3368eec77d7ef7ab114be2c068b3c`；32源码与归档逐成员完全一致。

CP exit0且原stdout精确匹配golden、stderr空，编译exit0；JIT exit1、stdout空，原stderr仅 `NotImplementedError: re: bytes patterns and subjects are not supported`，无traceback帧。三个阶段分别0.114738、2.121369、0.164768秒，总2.402213秒，无超时，保30/90/30阶段与180总限、首失败停，不重试。runner自身exit0只表示记录完成。

独立审核源/入口/golden、候选CLI/runtime/解释器、三个产物与六原流SHA全部一致，result SHA `64692667e2854951cc6d49da11aae06da07d0e2645546b3f1c92f8c6b862b3d7`。本轮错误不再是缺zip_longest导出，但无帧错误不能唯一定位具体regex调用，也不能推断项目内工厂、escape或zip消费者已执行。结论仅此候选固定Linux JIT入口仍失败；不改历史5/20，不冒main/全库/AOT/性能。本段只归档结果，不重新运行完整门禁。
