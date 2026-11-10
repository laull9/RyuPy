# ADR-0152：有限 re.Scanner（源码候选，待产品验收）

- 日期：2026-11-10。
- 状态：生产及 43 个 C# Fact 源码已准备；C# 编译、JIT、NativeAOT 和完整门禁尚未执行。

## 背景与决定

Markdown 3.5.2 的三个原样 attr_list 标准调用需要通用 Scanner，已有 CP 参考为 202B、
3 次 scan、11 次 callback。本切实现真实多 token 扫描，不按模块名、模式文本或业务输入特判。
完整 extra 仍有 TreeBuilder 等后继；固定 20 项的 11/8/1 不变，不推断另外八个项目直接受益。

Scanner 使用普通 PyClass，沿现有参数绑定、属性、单继承和 property 协议。
复合计划是内部 sealed 的 PyRegexScannerPattern，派生真实 PyRegexPattern；结果复用现有
sealed PyRegexMatch，保持公开类型、m.re 和原 subject 身份。七处 Pattern partial 去 sealed，
TypeName 保 sealed override；不引入通用执行器接口、第二个 re 字典或另一套 Match 表示。
生产触点共 12 个物理文件，其中新增两个文件，不新增 RuntimeFn 或改变 codegen ABI。

每个 phrase 独立进入现有 parser，再用 wrapper 捕获按原词条顺序组合为单个锚定 Regex。
每次 scan 固定一次计划并创建独立具体 cursor，完整输入只建立一次码点映射或 Latin1 编码。
不逐词条轮询 Regex、不切剩余字符串后匹配、不用 finditer 的零长重试。首个匹配优先，
不是最长词优先；失败或零长立即停止，返回实际结果及尚未消费的尾片。

## 可观察状态与错误顺序

构造先写原 lexicon，再获取一次迭代器，逐项 next、恰二项解包、解析当前 phrase；
最后才编译并写 scanner。不预读 len、不快照 action、不回滚失败 reinit，也不提前创建 match。
每个非空 token 都重新读取当前 lexicon 及两层下标；callable 分支先写 match，再读取 group(0)，
最后真实调用 action。常量与返回对象不测 bool/len，只有 None 被跳过。用户异常保原身份及
已有 cause/context/traceback；嵌套 scan 使用独立 cursor，不恢复外层公开 match。

已有固定 CPython 3.12.13 实观纠正了“空 lexicon 可构造永不匹配计划”的旧假定：
根 flags 为 0 或 False 时，在迭代结束后的最终构造阶段抛 RuntimeError('invalid SRE code')。
失败 reinit 保新 lexicon、旧 scanner 和旧 match，不触发最终 scanner setter。早期 S29 仅
观察 L|U，根 0 来自后续真实失败及修订验证，False 也已单独覆盖。其它空 flags 组合仍为
NI/pending。非空 lexicon 中的空 phrase、空 subject 与零长停止不受此勘误影响。

## 有限准入与拒绝边界

支持 exact str/immutable bytes 模式源及其混合词条，同一计划可分别扫描 text/bytes。
无内部捕获的字面量、字符类/类别、分支、greedy/lazy 重复、非捕获组、锚点/边界和
capture-free lookahead 沿现有语法实现。parser 的冷 Scanner 模式区分 phrase 解析 flags
与不可变根执行 flags：global inline 的 X 管词法，执行 I/M/S/U 看根 flags；根 0 的类别及
大小写折叠为 ASCII，但宽 Unicode 字面量、dot 与否定集合仍可匹配。普通 parser 模式不变。

内部捕获、backref、条件组、全部 lookbehind、scoped flags、atomic/possessive、mutable/view、
字符串或字节子类、任意 flags 算术、私有 backend 与高级 descriptor 仍明确 NI/pending。
这些包含 CP 支持的正例，不能将产品拒绝改写为 CP 不支持。

复合 Pattern 的普通匹配、sub/split、私有 scanner API、groupindex、repr/str、copy/pickle，
以及 Scanner Match.expand 暂不开放。已绑定方法先处理参数绑定，再在消费输入、count、模板
之前明确拒绝；re.compile(plan) 保原对象，非零 flags 保原 ValueError，purge 不销毁已有计划。
模块两处入口、SplitInput、expand 和子型属性白名单共同封闭旧消费者，普通 re/dedent 原路保留。

资源上限为 1024 个词条、单源及总源 16384（text 按 UTF16 单元、bytes 按字节）、完整译文
200000 字符、嵌套 256 层和每次后端匹配 2 秒。bytes 在编码前检查长度；每个 phrase 译后、
加入列表前累计 wrapper 与边界的总译文长度，越界不读取下一项。单 phrase 的临时解析分配
仍有既有成本，不宣称整个构造峰值仅为译文上限。

## 成本与验收

普通 Pattern/Match 实例字段集合不增加，旧匹配循环仍是 nonvirtual，RuntimeFn 不变。
去 sealed 仍可能改变 GetAttr/PythonRepr 去虚化、类型检查、JIT/PGO/NativeAOT 选择；
模块/split 增加入口 guard，普通 parser 多冷字段与分支。Scanner 有映射、cursor、Match、
回调参数分配与 wrapper 线性选择成本。尚无测量，不称零成本、无回退或提速。

有限 CP 参考与纯控制测试已分账记录，43 个 C# Fact 尚未执行。下一步须实际验证独立
Scanner 合同的 CLI→PE→JIT 和 NativeAOT，覆盖多 token、callback、失败路径及普通 re、
bytes/mutable、dedent 回归，再跑完整 make check。原样 attr_list 至少须取得 JIT 202B；
AOT 先检查已有合法配置的图和资源，可承载才执行，否则保留 publish 前负界，不扩全模块 AOT。
局部 API 或业务调用通过不能替代完整 extra、默认项目或 main 验收。

完整范围见[验收计划](../validation/re-scanner/PLAN.zh.md)，当前证据与未执行门见
[验收记录](../validation/re-scanner/RESULTS.zh.md)。
