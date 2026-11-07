# sys.modules 新模块别名挂接首切

## 合同与范围

本切只准入精确非空、无点str的新键到真实PyModule的原引用。键不可已有缓存/注册，
不可成为既有缓存或注册点分名的包前缀。State1拒绝，包括失败后逃逸的普通初始化对象；
State0允许，包括未执行和显式exec后仍为0的独立module_from_spec实例。
State不是“是否曾执行”的统一标记，也不据此判断是否有直接exec活动。

写入不执行正文、不补builtins、不改name/spec/loader、唯一globals、函数owner、状态或
父包属性，不覆盖原来源名缓存。新名字的普通import仍返回同一对象；已有属性from与
find_spec沿原真实对象读取。实现不含Markdown名称判断或输出特判。

检查只读内部状态与Ordinal字符串，不为拒绝调用hash/eq/repr/str、getter或loader。
五处注册写路及声明预检均有逆向冲突守卫，后续宿主不能覆盖alias同名或后代。
无关正常注册及其原覆盖规则保留。alias未知子名导入、缺属性from回退与finder发现
明确NI；finder在父__path__读取前拒绝，from的NI保missing→外层异常链。

__setitem__仅接受位置参数。直接、保存setter、容器内setter都走同一入口；关键字
先TypeError且不发布或占名。已存在键即使写同一对象仍NI，删除、None、任意对象、
点分键、sys.modules整体替换以及其它dict改写接口不开放。

## 明确未完成

typed Cache与受控view保留，不是真dict；旧TypeName及keys/values/items列表快照
偏差没有伪装成CP活view。全替换/删除、None阻断、初始化自替换与失败清理、父包/fromlist
时序、缓存顺序、非str字典键和动态finder仍完整pending。

CP缓存import会观察当前spec._initializing并可能调用property/bool，旧RyuPy Import
直接返回缓存。本切未改普通命中，不承诺自定义或后改spec的导入等价。CP参考还证明
默认__import__会吞掉该检测中的部分异常，而直接属性或bool操作会传播；这些完整
副作用场景保留pending，不能用一次挂接前检查冒充后续元数据安全。

kwargs现在拒绝，但通用binder的posonly错误措辞仍不同于CP的
wrapper __setitem__() takes no keyword arguments。精确type/str/args单独完整pending；
基本绑定正例只验TypeError、无写入及随后同名合法挂接，没有冒领精确消息。

## 验收

正常make check：2519 C#、900非e2e Rust、5清单自检、578 CP与578框架自检、
370编译端到端通过，212 ignored。门禁前后所有源码hash/mode相同。
最终Release runtime SHA256：a7e567d86c914854f3475685041cc4b9d9584a7d5cfdb4377ee08ecac67f5003。
CLI仍1757a3f128983be7a39f8a3efee698f1f8a8cf749d2c0ce1546076bca030f4db，Rust/Import ABI未改。

新增29个完整CP合同，最新参考均固定CPython3.12.13成功、stderr空。五例逐字节真实PE
通过并转正：existing instance、executed clone、unexecuted clone、saved setter binding、
static unknown alias。其余24完整保留，未删断言或改oracle。最后一例的“需要未来模块图”
前期注释已被真实PE证伪，集成时仅纠正注释；所有执行AST不变，原冻结源仍保留。

正常CLI/PE的两项RyuPy限域专项另行通过：未导入注册io不可被覆盖，后续真实原生
UnsupportedOperation typed handler仍正确；尚未执行的已注册源名不可被覆盖，拒绝不
执行其正文，随后正常导入与新别名保持身份。它们不是CP正例，不补进普通e2e分子。

## 保留的失败与修正

隔离新增专项首轮38过/1失败是测试先构造旧不支持的str.__hash__子类；合法str子类与
独立普通对象回调组修正后39/39过，不删除无回调边界。全C#首轮2512过/1失败因最小
隔离树缺原textwrap JSON，复制同SHA夹具后2513/2513过。

父审随后发现v1默认binder允许关键字实际写入。原候选、runtime和4轮日志完整保留，
不以此前绿测掩盖漏洞。v2仅新setter改posonly并加6例，45/45专项及2519/2519全C#过；
独立源码复审核对全部11文件。孤立验证关闭网络NuGet审计，最终canonical采用正常命令。

spec异常参考首版误以为默认导入会传播property错误，完整失败源/双流保留在
supplement-v1；修订完整场景首次supplement-v2锁定CP实际吞错及事件顺序。没有覆盖
旧oracle。设计18探针与29测试参考的用途分别记录，不混计原生支持。

一次Rust清单命令误选unit test二进制，只列7个自身测试；正确e2e二进制列29新例，
但单独--list尝试写默认结果遇只读警告。这两次不作完整验收，最终正常门禁真实运行
并接受全部29清单，无修改测试框架、无不支持的include_module清单字段。

## 原项目与成本

最终Release下原docopt入口16字节golden一致；默认Markdown仍find_spec metadata NI，
375字节stderr不变。原四源窄配置已越过sys.modules挂接，下一阻塞为缺xml模块，
43字节stderr。输入与历史CP来源前后hash核验，复用CP而非新20项目全矩阵；不计默认
Markdown或整库通过，AOT/跨平台未验。

普通Import缓存命中段、PyModule.GetAttr和普通容器分派逐字未改，不等于整程序零成本。
首次别名懒建来源HashSet；每个新键增加强Cache引用与名称标记、终生保留无逐出，
冲突检查线性扫描Registry/Cache，注册与发现增加冷守卫。未新做时间/内存性能采样，
S1/B2与所有更早不利样本仍开放。
