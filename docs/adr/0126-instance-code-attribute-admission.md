# ADR0126：实例模块的代码同名属性准入

- 日期：2026-11-06。
- 状态：小切片与正常完整门禁通过；保留完整边界失败，最终原源码复验另记。
- 基线：cd6933263e266fde7c18fc43b85bec1adcbd1b77。

## 新鲜原源码证据

模块删除已使固定 CPython 3.12.13 未改四文件闭包越过 _markupbase.py:20 的 del re。
原闭包现有 21 条 E0100：19 条来自 re.compile 属性名被环境名字预检误拦，另有
HTMLParser.__init__/reset 中两处真实零参 super 的边界。原默认 Markdown 的
find_spec('html.parser') 仍明确 NI；本切不是 loader 或解析器实现。

instance_globals 的 AST visitor 原来把 Expr::Attribute.attr 与 Expr::Name 都送入
同一个名字拒绝表。属性标识符不读取同名环境变量，不能因其叫 compile 就把任意
对象上的属性误当 builtin compile；同时 receiver 和实参中的真实裸名仍须检查。

## 最小决定

只放行任意 receiver 上的 compile、exec、eval 三个属性标识符，继续使用现有通用
属性读取、写入与调用路径。没有 re、html、builtins 等 receiver 名称特判。walk_expr
仍递归扫描 receiver、实参和嵌套表达式，keyword 标签、注释与字符串不冒环境读取。

真实裸名的读写、遮蔽、保存别名、global/nonlocal、导入源与导入别名守卫保持不变。
其它环境属性（globals/locals/vars/__import__/super/__class__）、属性 del、except、
急切推导式和其余实例限制未开放。这些是当前未验收的能力边界，不是声称属性与裸名
具有相同语义。默认非实例模块不执行此预检。

不修改 runtime、MIR、CIL 或函数/类存储。实现只改变准入；此前被拒绝的程序可以
到达原有执行路径，但不代表该路径的所有行为均已与 CP 对齐。

## 完整源和失败必须保留

新 HIR 专项 17 项通过，其中新增 5 项覆盖 receiver/实参扫描、精确错误 span、
不可达代码、裸名/别名/遮蔽及其它协议边界。固定 CP3.12.13 两份完整程序均通过，
旧冻结 CLI 对 ordinary_attributes 的同一完整源报告 25 条 E0100。

候选实际编译两份完整程序均成功，随后：

- ordinary_attributes 在 entry.py:18 的 property/实参求值顺序断言真实失败。
  静态独审定位到既有通用点调用仅对受限 receiver 提前捕获目标，普通 PyInstance
  的 property 查找晚于实参。后面的精确 list 缺失属性顺序断言尚未执行，不能称
  它也已在本窗实际失败。完整程序保持 pending，不删减任何断言。
- pending_builtin_module 实际先报 ModuleNotFoundError: No module named 'builtins'。
  目前没有已注册的 builtins 模块；后续动态代码与局部 frame 断言均未到达，不能
  称 eval/compile 的运行时 NI 已验证，更不能造同名模块使其冒通过。

通用点调用顺序需另切设计。直接扩大 CaptureModuleTarget 会把相关动态调用从
PIC 方法缓存改为 captured CallValue，可能增加绑定对象与数组分配；只加普通实例
也不能覆盖精确集合。应保留完整失败并设计兼顾正确顺序与既有快路的通用捕获，
不能为了让本切用例转绿仓促改动所有方法热路。

独立 module_and_saved_attributes 完整程序的固定 CP 与候选真实 PE/JIT 均 exit0，
stdout 48 字节逐字一致、stderr 为空；旧冻结 CLI 对同源实际报告 32 条 E0100。
该程序验收真实模块别名、保存属性函数、独立 property 读取和常量实参调用，
但不替代上述完整 pending 程序，也不宣称点调用副作用顺序已经实现。

## 性能与后续前置

此次没有新增运行时 helper 或存储。应对原已准入程序做真实 PE 结构对照，仍不得
由源码或 CIL 相同推断时间无回退；ADR0125、B2 等历史不利继续开放，不重复同一
性能矩阵来挑选更好样本。

真实 super 不能仅删预检。现有 TLS owner/self 来自属性查找，既不是定义类的词法
__class__ cell，也不是当前函数首个位置形参；显式 super 位置实参还有旧未校验边界。
下一最小切应先实现每次类定义的真实 cell、类体期间空值及装饰器前绑定原类，再
实现基于当前调用数据的零参 super。模块 owner、可写 __module__、查找 owner 必须
与定义类分离；固定 CP 探针只是设计证据，不是 RyuPy 支持声明。

## 正常门禁与最终工具

首次正常门禁只在 clippy collapsible_match 停止，原日志保留；将同一条件等价移到
match guard 后，第二次正常 make check 全通过：2377 C#、866 Rust、5项清单自测、
479 CP 基线、479 cases selftest、335 编译 e2e，148 ignored。11553份源码在门禁
前后逐项一致。最终三个完整程序重新编译/JIT，窄例通过、两完整边界按原首错失败。

最终 CLI SHA256：279b59cdcee9e524767a8df0a046c0b06c4e85191c884217c5240984c84c1a8d。
最终 Release runtime SHA256：27029d9c2dbeef1f68d00d4b023e8fe6bd04441d2db72ad7fa9a2f16857c42f7。
runtime 源码未改，但正常重建嵌入新 Git 信息后字节不同，不能继续称旧 DLL 字节相同；
独立比较确认 728 个反射类型、1921 字段、5169 方法 ABI 与 5116 完整方法体逐项
相同，包括 476170 字节原始 IL、locals、EH、maxstack 和 initlocals。全 PE 的 148 字节
差异精确归入 MVID、提交号 InformationalVersion、PE/调试标识与资源版本七处；
只替换这七个已定位区段后全 DLL 逐字相同，零未归因差异。版本元数据仍是可观察变化。
不能把这些结构相同表述成运行时间实测或关闭既有性能不利。

## 干净提交后的最终原源码复验

在功能提交 4e0bf969e0cd1d1483b0c8dc95151b783a3f3f40 上，只执行固定 9 子进程：
5 次 CLI compile 和 4 次 JIT，无 CP 重跑、无额外构建或计时。旧 CP 结果在原完整
源树、固定工具及原流身份核验后复用；docopt 原 CP SyntaxWarnings 也完整保留。

- 原 docopt compile/JIT 均 exit0，stdout 16 字节严格等 golden，stderr 为空。
- 原 Markdown compile 通过，JIT exit1，stdout 为空、stderr 375 字节逐字重现原
  htmlparser.py:39 的 find_spec('html.parser') native/legacy metadata NI。
- 原四文件窄闭包 compile exit1，stdout 为空、完整 stderr 653 字节。19 条属性
  compile 误报已消失，仅剩 html/parser.py:128:9 与 :141:9 两处 super 的 E0100。
  无 PE、无 JIT，既不计默认 Markdown 通过，也不计窄闭包通过。
- 01_module_binding 与 08_instance_dictionary 两份原已准入完整源各一次 compile/JIT
  通过，输出严格匹配旧 CP/golden。解析实际 PE header 后比较全部字节，各仅 COFF
  timestamp 的偏移136、137两字节不同，其余逐字节相同；未改物理源路径。

输入、工具、相关完整源树与干净 Git 前后一致，没有失败重跑或临时换根。证据为
/workspace/shared/ryupy-instance-env-validation/cli-rechecks-001/result.json，SHA256：
fa792e4896a112970af4748cb60897025562f707990d4a1ce844cabc27c4eced。
