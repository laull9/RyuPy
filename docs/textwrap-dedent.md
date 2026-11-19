# textwrap.dedent 有限候选与验证入口

本页记录新增API源码合同，不宣称整个textwrap、Markdown extra、JIT或NativeAOT已验收。
架构取舍、资源边界及偏差见[ADR-0151](adr/0151-textwrap-dedent.md)。

## 生产接点

- PyNativeModules：在实际re字典创建时捕获compile见证，再登记textwrap惰性初始化。
- PyTextWrap：保留旧共享字典、CreateAttributes与无参Build；新四名只写owner。
- internal PyTextDedentCompile：首次native来源/compile/真实Signature数组/flags/backend有限门。
- internal PyTextDedent：两真实Pattern、owner保存、ASCII/LF归一/共同前缀与当前re.sub调用。

没有修改PyModules/通用注册架构、字符串热路、编译器、上游源或固定项目golden。

## 验证分组

1. 普通常规：tests/fixtures/textwrap_dedent/core.py（冻结24输入）、raw_subclass.py（4 raw子类/6错误）、
   binding.py（参数与新旧导出重绑）、sub_dynamic.py（3pos/归一正文/返回异常身份/property sub无margin零lookup/大margin自定义sub）。
   本地固定CP3.12.13首批均exit0、stderr空；sub_dynamic的002改稿及两个新增pending经新身份单批CP自检均exit0、stderr空，不沿用首批通过。
   这些不是产品测试通过。源码保pending直到产品验收。
2. Ryu明确NI负门：negative_private.py（两private阶段/re重绑/小margin正门及原pattern与译后预算两个NI门）、
   negative_preimport.py（每个argv fresh，compile/flags/八backend）、source_success与source_retry目录。
   后两者显式把re/state纳入静态图，核来源标记、异常身份、失败逃逸owner和retry隔离。
3. CP正期待pending：pending/preimport_compile.py坚持五compile及改变结果的private代理；
   pending/source_re.py坚持有限代理完整dedent，保sys.modules机制域外标记。
   pending/getattribute.py保原完整trap的CP零lookup；pending/stock_large_margin.py保16379/16380的CP成功正期待。
   两者不因普通fixture更换可编译写法或NI负门而计为支持。
   preimport初稿误用raw-string对比导致IndexError，首轮失败保留；纠正为实际TAB/LF后唯一另轮复验exit0、stderr空，保留首批失败。
4. C#常规：PyTextDedentTests共10个Fact，全部经真实Import取得新API；另保护旧字典双向共享、
   public Pattern.pattern/flags、原pattern/译后体积预算及10万空白行防退化。
5. C#首次状态：PyTextDedentInitializationTests.FreshProcessContract由环境场景选择，
   普通make check明确skip，不把串行集合当fresh。专用门逐场景独立dotnet test进程，核唯一Passed无skip。
6. 旧保护：原PyTextWrap/PyTextWrapper类算法专项、原四driver应用、正常make check及固定项目保护。

本地没有.NET SDK/dotnet/Roslyn，未执行C#编译、上述C#测试、JIT、ILC、Native或正常完整门禁。
专用CI须冻结同候选HEAD/tree/fixture哈希，依次保存命令、工具身份、双流、退出码、TRX与真实ILC产物，
分别报告CP/NI/pending/JIT/Native及旧保护，不能用静态检查或CP结果代签。

## 不在本切支持域

源码re或导入前改compile/flags/backend后成功import，只说明旧API仍可用，新dedent会NI。
普通private键写入不拒绝，调用wrap/fill不受新守卫影响；dedent消费替换键才NI。
同一初始re的当前sub重绑支持；换成另一re对象不支持。完整source-re dedent与preimport代理仍pending。
导入后公开compile/MULTILINE改写不影响已有Pattern，私有backend整体替换并未因此支持。

只有Python新字段setter/真实模块getter是新API合同；直接CLR CreateAttributes仍仅服务旧共享字段。
本切不改变固定20项目计数、main/CURRENT，也不预测后续re.Scanner/TreeBuilder何时完成。

stock re保原pattern16384和译后主体200000字符预算；前者不保证16379空格margin可用，后者可能先NI。
本切不宣称实际最大margin，保守小正例仅验证该输入；自定义sub不提前套用这些stock预算。
