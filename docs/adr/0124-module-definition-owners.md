# ADR0124：显式模块实例的定义归属

- 日期：2026-11-06。
- 状态：B2功能、完整门禁、最终真实PE与CIL独审通过；正式测量和一次有界归因完成，默认性能风险未关闭，未推送。
- 开发基线：1b6364fa538f077f6132b0600cd07a13eb3b4788。

## 动机

B1已经使模块属性和生成正文共享唯一真实字典，但拒绝新函数和类。仅把函数放进新模块
字典并不会改变函数归属；换注册名、改__module__、交换静态全局槽都不能保持闭包、
跨实例重入和暂停生成器。因此本切绑定实际定义环境，不提前开放Python loader。

## 选择与分层

1. 选中模块中的函数始终是动态真实对象。MIR以独立context参数、实际PyFunction self
   和纯cell Env表达三个不同身份，代码后端不依赖HIR，不靠函数名推测来源。
2. 默认PyFunction布局、PyCall及Invoke入口保持不变。只有选中路径创建不可变owner
   载体和绑定委托；共享代码使用静态PyOwnedCall缓存，不强留任何已执行实例。
   普通调用不查弱表、不切换线程模块，也不根据可写__module__寻找globals。
3. 定义点在默认值及注解求值后、装饰器应用前捕获当前globals的__builtins__。
   缺键继承caller已捕获字典且不插键；引用相同直接复用不可变context，重绑才创建。
   globals字典本身始终是原实例字典，不能因为把函数移动到另一模块而改变。
4. 生成器factory保存context到明确帧槽，resume恢复它，不能在每次next/send/close
   时重新选择环境。MIR记录参数/生成器帧等可信来源，并验证恢复和使用的顺序。
5. 普通模块级class使用局部真实类对象，不发可变静态类身份槽。装饰表达式后检查外层
   已捕获builtins中的__build_class__，随后捕获类体环境，再求基类，最后进入类体。
   方法定义是新的定义事件，从当时globals重新选择builtins。类体及装饰成功后才发布。
6. 共享类构造抽出MakeClassCore，显式路径跳过ambient元数据捕获。未选路径仍使用
   原公开MakeClass ABI，但多一层调用及条件分支，属于必须测量的默认成本。

## CP事实与边界

固定CPython3.12.13原探针001在旧函数新建嵌套函数的builtins身份断言上失败，原流保留。
探针002修正已证错误假设后，同一完整核心源的双实例、默认参数、cell、类容器、重入、
重复执行、旧暂停对象及失败import逃逸对象两轮全过。003/004进一步锁定def/class相位。
这些是CP事实，不是RyuPy执行证据；真实生成PE另验。005两轮进一步证明急切推导式
沿用旧frame，而genexpr在outer iterable求值前捕获新环境，元素到恢复时才执行。

class.__module__是类体LOAD_NAME：先globals，再类体捕获builtins；两边都缺则在基类
求值后、用户类体前NameError。function.__module__只取globals，缺键为None。
缺__build_class__在基类求值前NameError，不能用globals同名值补位。默认hook保真实
稳定callable身份，但公开直接调用、自定义hook及metaclass协议仍明确NI/Diagnostic，
不把有限class intrinsic伪装成完整__build_class__。

CP3.12急切list/set/dict推导式内联于当前frame，不能与genexpr一样重新捕获builtins。
本切保守拒绝急切推导式，保完整pending源，不以现有synthetic lambda模拟CP内联环境。
普通genexpr是真实新函数，按定义时刻捕获。完整支持域见[合同](../module-owners.md)。

## 成本与验收约束

新的owner载体、绑定委托、部分context和生成器帧槽会增加选中路径的分配及传参。
共享builtin表增加__name__与__build_class__，类构造公共入口也改变，不能由默认对象
布局未变而断言零成本。默认旧新编译器CIL、同PE交换运行时、新路径绝对成本分开验。

独审首次发现恢复前使用、覆写frame、伪造resume为MakeFunction目标和直接调用伪context
四项校验漏洞；续审又补owner帧槽写入、伪生成器factory和异常边支配三项精确红测。
这些是编译器内部MIR合同漏洞，不等同已经证明正常Python源会误编译；必须以原红测
和修复后的负测闭环，不仅补正常源成功样例。
七组内部MIR负测均先真实红灯再修复，最终MIR158项通过；没有静默删除原失败。

## 最终验收

- 正常完整make check通过：2355 C#、836 Rust、5项清单解析自测、461 CP基线、
  461 cases selftest、326编译e2e，139 ignored。11469份源在门禁前后逐份相同。
- 六份完整正例转正：function_globals、closures、generator_owner、class_owner、
  builtins_capture、genexpr_capture_phase。直接__build_class__和急切推导式完整源保pending。
- 最终CLI 6e813e2e…、Release runtime 6df960a2…真实重编两套完整源：77项双实例/
  defaults/cells/类容器/重入/重绑/旧暂停对象/失败import逃逸与重试，64项def/class相位/
  缺hook及name/生成器方法均通过。宿主仅提供输入事件和检查，不手写被测Python正文。
- 默认common及class两对PE的所有CIL、ABI、字段和raw metadata逐字一致，各仅COFF
  时间戳一字节不同。运行时5096→5112正文：7修改、16新增、无删除，另4个无IL的
  委托方法；全部26个实际metadata表及10块27303B FieldRVA数据已核，未解释项0。
  原PyFunction十字段、构造器、Invoke及PyCall保留；不由此推断时间零回退。

## 性能结果与有界归因

正式唯一20进程/384暖样本全部完成，无失败、重跑或补采。之前P2首smoke曾因两个
入口PE共享__main__缓存失败；保留原流后，将class隔离进程，正常Main一次再执行
真实生成模块body，七smoke和新Host独审通过后才计时，不伪称重复Main测类创建。

默认method_calls两配对+237.02%/+129.65%、module_bare +26.96%/+97.46%及其余
全部不利保留；暖分配相同不证明无时间回退。实例def同引用/缺键均224B每完整操作，
重绑256B，包括factory调用和child创建，不是纯helper成本。全部40配对、48实例绝对值、
冷边界、CPU/GC/分配和限制见[完整报告](../module-owners-performance.md)。

针对大幅不利，另做一次33.28秒/16进程/432样本的有界归因（324非插桩、108定向JIT）。
保持原Host/PE/预热及九common路径顺序；同版本和双方向组不能稳定隔离版本效应。
默认插桩实际出现不同Tier1/Tier0层级；禁tier下所选7个FullOpts正文规范地址后相同。
没有原P3逐样本JIT时间线，不把这些现象反推为原回退原因或“只是噪声”。没有可靠可修
结构性证据，停止采样、不盲改非热MakeClass、不宣称已修复。原风险与历史风险均保留。

公开module_from_spec/create_module/exec_module、可写sys.modules、native发现及完整
html.parser不在本切，不能以内部API通过代替。

## 干净功能提交后的原项目复验

功能提交7b4956f92ae60181a8ccbe9693f8afb3ae5561c6，tree
9cbe80e35f4a564c359a4bbedd5ad02e1d39684f。使用门禁冻结的最终CLI及Release runtime，
原docopt0.6.2固定入口CP/compile/JIT均exit0，CP与JIT stdout均16字节等原golden；
保留CP的860字节SyntaxWarnings，JIT stderr空。没有换入口、缩短源或删除原警告。

原Markdown3.5.2的CP/compile exit0，CP stdout77字节等golden；JIT exit1、stdout空，
375字节原stderr仍定位htmlparser.py:39的find_spec('html.parser')：
NotImplementedError: find_spec(): legacy or native module registrations without source metadata are not implemented。
未到module_from_spec/exec_module，不计Markdown通过。整个复验runner因该真实失败exit1，
不是门禁失败，也不能改记为整个项目窗口成功。

前后审计一致：干净HEAD/tree、原63输入、62矩阵输入、20根顺序、两份各3187文件源树、
入口/golden及CLI/runtime/Python/dotnet身份不变。每项目CP/compile/JIT各一次，未重跑旧20项目矩阵。
结果证据projects-final-001/result.json的SHA256为
b22f883b5a8961706c86674ccb52aa7abed2ce2cf976cd13095ebd8b736ac8ce；前后审计相同SHA256为
72569cec2b6f6b97aa3ddca221eedc00821c6ec4a6abcf04d899471b00b4ec0f。
