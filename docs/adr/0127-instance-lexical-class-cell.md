# ADR0127：实例模块的真实词法类 cell（S0）

- 日期：2026-11-06。
- 状态：功能验收通过；唯一限定性能观测完成，不利与审计覆盖边界保留。
- 基线：c138b6c279e3a704d527e585c8fa0c24df70b38a。

## 为什么先做 cell

原未改 html.parser 在属性名字守卫修复后，只剩 :128 和 :141 两处零参 super 的
编译诊断。不能直接移除门禁：当前方法调用 TLS 保存的是属性查找 owner 和进入时的
接收者，不是 CP 的词法定义类与当前函数首个位置参数；旧显式 super 位置实参也有
未校验边界。S0 只建立真实定义类 cell，super 仍拒绝，不宣称原 parser 已能编译。

## 支持目标与相位

仅显式实例模式的普通 class 语句建立特殊词法作用域。它只提供隐式 __class__ cell，
不把类 namespace 的其它属性变成方法的 enclosing locals。参数、普通局部或 global
遮蔽依原词法规则处理，不能见到同名文本就捕获类。类体直接裸名仍是 class LOAD_NAME；
类 namespace 的 __class__ 普通值不初始化、不修改方法捕获的隐式 cell。

类体 lambda，以及方法默认值/方法装饰表达式内的 lambda，可以捕获这份 cell。
class decorator/base 表达式仍在外部作用域，不能仅按文本位于 class 附近误捕。
嵌套函数与生成器用已有真实 Env 转发 cell，模块执行 context 与 Env 保持分离。

每次实际类定义都新建空 cell，包括循环重复执行同一 ClassId、另一模块实例、同一
模块字典再次 Execute。当前有限域内，类体与普通终结成功后、应用 class decorator
之前，向 cell 填入原始类。装饰器替换公开类值或抛错，不重定向该 cell。类体/终结
失败后逃逸函数仍持空 cell，实际读取应由 CellGetFree 抛 NameError。

旧方法移植、类名重绑、可写 __module__ 改变，都不改变定义类 cell。旧闭包/暂停生成器
跨模块重执行仍持旧类，而 B2 的真实 globals 可以读同一字典的新值；二者不是同一身份。

## 实现与来源约束

复用 CellNew/CellSet/CellGetFree 和 MakeFunction.env，不增加 PyFunction 布局或
Invoke 热路状态，不使用 TLS、模块注册名或反射模拟定义类。默认模式与不需要 cell
的类不应新增 cell、环境字段或生成调用。codegen 只消费 MIR，不依赖 HIR/ty。

HIR 记录实际 ClassId 捕获来源，MIR 使用冷来源计划验证定义点、捕获槽与 generator
帧恢复。要求原类创建和 CellNew 对应同一次动态定义，不能把 CellNew 外提至循环外；
允许空 cell 先捕获，但成功填充必须在正确 CFG 相位且只填原类。错 cell、跨类串接、
提前/重复填充、原始数组写旁路和错误 Env/帧来源都不能由元数据声明蒙混过关。

## 明确保留的边界

- super、obj.__class__ 属性协议、nonlocal __class__、显式 __classcell__ 创建协议、
  嵌套 class、任意 metaclass、多继承和特殊 native base 不随 S0 开放。
- PEP695 注解作用域不能把类型形参 __class__ 误当定义类。生产 parser 目标仍是3.11，
  相应完整源先到 E0002；HIR 在解析返回的真实 AST 上另有 type_params 防御诊断。
  不能把直接 HIR 检查写成默认 CLI 已越过3.11语法边界。
- 类创建 hooks 尚未实现。CP 的 __set_name__ 先于父 __init_subclass__，两者都能看到
  已填原类。S0 不执行这些 hooks：只对需要 cell 的实例类在冷终结边界检查真实父 hook
  和属性值真实类型链的 __set_name__，命中明确 NI；已有 property/static/classmethod
  不因“都是 descriptor”被一概拒绝。遇此能力拒绝时 cell 保持空，不承诺模拟 CP 的
  hook 失败状态或副作用。类完成后的普通 __classcell__ 属性不是创建协议，不能混淆。
- B2 实例函数的 __closure__ 反射未开放。冷元数据入口依据已有真实 owner 标记拒绝
  读取，写/删保持 readonly；普通 __dict__ 的伪同名值不能替换真实 Env。默认函数的
  旧反射边界不借此整体改写，也不暴露内部 object[] 为假的公开 cell 对象。

## 实际功能验收

固定CP3.12.13的设计探针与生命周期oracle先固定完整源；真实RyuPy验收没有裁源或
改断言换绿。最终CLI完成14份完整多文件程序：7个正例编译/JIT输出与CP一致，另外
7个边界保留pending及实际诊断。正例包括词法遮蔽、class namespace与cell分离、
方法移植、循环重复定义、默认值/装饰表达式、类体/终结/装饰器失败和生成器转发。
边界为nonlocal cell、显式__classcell__、嵌套class、super、PEP695、创建hook及
closure反射。泛型完整源先由目标3.11 parser给E0002，不能冒称已进入HIR/runtime。

最终生成PE另完成32项双实例与同字典重执行检查，完整1025字节stdout与CP相同、
stderr为空。旧方法/lambda/闭包/暂停与仅转发生成器持原类cell，读取原字典的新global；
旧middle在重执行后创建的新inner仍持原类cell，但按真实定义事件捕获新builtins。
Host直接引用实际生成PE入口，没有C#被测函数、手工cell或假loader。

正常make check通过：2381 C#、887 Rust、5项清单、493 CP基线、493框架自检、
342 e2e通过，155 ignored；220.393517秒。11611份源码前后SHA一致。最终CLI再次
完整复验14源，7正例通过、7边界明确失败；独立静态复核确认来源校验及新增负测。

保留全部原失败：HIR首轮泛型夹具误要求3.11 parse无诊断；C#首轮3处测试夹具给只读
BaseClass赋值；修夹具后发现delattr旧入口先报NI，已只对真实owned函数__closure__
放行到readonly元数据拒绝。对应修正后HIR5、MIR15、CIL1、runtime4专项均通过。
PE首次父启动漏清单身份环境变量，在0个工作进程的前置检查阶段失败；补齐后唯一
源码编译、Host构建和JIT通过。这不是失败PE的补采。

原docopt最终工具编译/JIT通过；原Markdown native仍在find_spec处产生与旧版完全相同
的375字节NI。四份未改CP stdlib窄源仍仅在html/parser.py:128/141的super上给两条
E0100，尚无该窄源PE。S0完成的是必要前置，不计原Markdown兼容通过。

## 结构与成本边界

默认common、默认class、显式实例无cell三对真实生成PE完整字节相同，包括时间戳，
并非归一化后推断。feature CIL实际显示每次CreateClass后CellNew、两方法捕获同一
raw cell、Finalize→Guard→填原类→发布；新闭包转发cell，不复制当前类值。

新增ClassCellGuard枚举真实namespace，完整成本属于建类/模块Execute。三个函数
metadata方法新增冷分支；删除守卫只在旧函数反射拒绝分支中新增名称比较与owner解析，
但真实CIL还为PyFunction临时值新增stloc/ldloc及一个local，不能称所有普通删除零新增
指令或断言JIT消除了它。PyFunction布局和Invoke没有新增状态/判断。

性能准备24进程与6个正确性smoke已完成。原汇总器因重复展开超过1MiB报告上限，
保留FAIL_REPORT_BUDGET及9份完整audit；一次纯离线整理未重新编译、导出或采样。
原metadata heap、token scope和PE余区仍有未解析范围，不声称全runtime DLL等价。
负责人明确选择限域审计口径，新入口只改变证据准入与结果声明，保原执行协议和所有
原证据。独审通过已证/未证范围后，唯一10进程/144样本完成，无失败、超时、重跑或
补采；这不是将原报告失败签字改成通过，也不是全DLL等价证明。

性能只报告固定观测，不声明显著性或总体无回退；ADR0125、ADR0126及更早不利继续
开放，不以默认PE相同或分配相同抵消时间不利，不补采挑选好值。

正式默认method_calls +310.450%、generator_create_resume +40.126%、resource_files
+31.608%等不利全部保留，对应分配相同不能抵消；单A/B进程配对未可靠归因，不称噪声。
新feature建类、已保存方法调用、新闭包和旧闭包读取分别观测1800/32/256/0B每完整
操作，包括真实调度与必要结果保存，不是纯cell helper成本。全20组配对、8项绝对值和
冷段原流见[完整性能记录](../class-cell-performance.md)，按预定收尾，不补采。
