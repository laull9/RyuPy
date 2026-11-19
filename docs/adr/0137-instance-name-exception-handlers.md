# ADR-0137：显式普通实例的Name/bare异常链（候选）

## 范围

沿ADR-0136结构化能力，ordinary_name_handlers只授显式普通ModuleFile、JIT、非fixed-binding。
旧automatic P1/default集合不扩域。支持模块main和普通函数（含方法/闭包）的源码Name/bare
handler；类体、generator自身handler、async、except*及非Name目标AST仍明确拒绝。
Name值可为合法异常类tuple，复用现ExceptionMatch的全成员先验证合同；不开放tuple目标AST。

## 分派与来源

HIR HandlerSemantics区分旧source static/dynamic、新InstanceNameDynamic及SynthesizedWith。
新source整链保存真实目标表达式，包含未绑定builtin名；复用现lower_name词法顺序，不能
统一改成global或让静态exception_type_names绕过真实实例字典。仅实际异常分派时读取目标，
失败退出整链并保context；无异常不读取。pure bare通过显式dispatch而非any(type_expr)接线。

MIR Body携带source_handler_contracts，实际外层CatchAll带本函数chain ID，逐项验证唯一对应。
缺能力/不可信state/实际GeneratorFrame/source区域改typed或丢标记均拒绝。旧with合成typed
handler及内部finally guard不要求新位。generator可继承模块能力，但不能带新source合同。
codegen仍只生成原语与EH，不解释源码AST或加入异常匹配语义。

## 名称与清理

globals唯一真实PyDict；fallback使用函数定义捕获context的builtins，旧函数不随重执行换owner。
局部/cell/free/global遵循原绑定与未绑定错误分类。as只在命中后赋值；实例ModuleVar离开时先
赋None再DeleteName，正文已自行删除仍可清理，不能向公开dict写Unbound。其他词法槽/cell沿
原Unbound清理。return/break/continue/raise/finally共用已有dynamic cleanup与ambient恢复。

不新增显式local/cell/free del准入。三者在实例预检继续拒绝，不从验收列表消失；旧Legacy
ordinary local删除已经支持但不是此切新增能力。module/global自删为正例，所有词法层隐式
as清理仍独立验收。无异常属性setter/descriptor、全量反射、AOT、whole-wcwidth或性能扩展。

## 当前状态

实现候选与focused完整CP/PE合同已打通，完整make check、固定20保护及独审尚待完成。
固定CP与首切冻结旧工具的依赖核查证明：ordinary local del旧路径通过，cell/free del在HIR
拒绝；Name值合法/空/含非法后项/nested tuple/异常实例匹配相位与CP一致。失败原流保全。
