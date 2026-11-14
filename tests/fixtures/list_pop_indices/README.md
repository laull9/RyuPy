# list.pop有限索引协议回归合同

源码准备阶段仅有隔离稿，尚未执行；后续实际验收见末节。
父代理统一执行runtimeowner的参考脚本；新应用只在driver内执行固定CPython 3.12.13，先核冻结完整链再build/JIT。
不得将本文件、源码行号或结构检查称为运行通过。

## 三应用

1. wide_and_protocol.py：direct重复PIC / saved / 展开调用；空与非空列表，普通整数与bool，
   ±2^32、i64端点和超i64，合法/非法普通__index__结果，缺槽/__int__单独存在，
   返回值/剩余列表/别名/副作用全流比较。异常不做额外删除。
2. order_and_yield.py：所有参数先求值，arity/keyword失败不运行__index__；索引一次调用先于空表检查；
   回调clear/append/replace后基于当前列表执行；宽值/坏返回只保留回调自己的修改；
   原错误对象及活动context；prepared跨yield冻结原列表，恢复后才转换。
3. error_chain.py：outer ValueError context + module/outer/middle/__index__全用户链。
   driver仅剥路径/源码展示/caret/空行，未知帧不吞。按源码定位冻结10条projection，首CP先核对。

## 结构与旧回归

list_pop_index_structure.rs沿用上一切真实源码HIR/MIR/CIL检查器，合法单参pop仍是PIC小参数入口，
不新增包装/数组；saved为属性读取与CallValue1，prepared方法暂停前初始化并spill，再恢复调用。
保留数字静态算术和解包结构不退化的断言。结构不计作第四个应用。
父需原样运行旧list_method_bindings三应用与list_method_binding_structure，不覆盖首次bb6证据或宽索引pending归档。
warning / int子类返回尚属边界，必须保留真实CP正例，不能把Ryu拒绝写成CP预期。

## 运行证据

准备阶段尚无应用执行结果；后续结果见首次验收。driver固定版本并保存源码、CP双流/exit、build诊断和结果、JIT双流/exit；
正常默认优化build/JIT，失败保留隔离目录。未涉及public export、AOT、性能或项目矩阵。

## 已读真实参考（2026-11-04）

父执行的参考位于 /workspace/shared/ryupy-list-pop-indices-validation/reference/，
stdout为103case加version/DONE；CPython3.12.13，exit0，空stderr。
确认：空表的普通/wide i64为IndexError: pop from empty list；超i64仍先OverflowError，
坏类型/坏__index__返回仍先TypeError。非空宽i64是pop index out of range，不修改列表。
__index__一次；append空表后成功；clear非空后empty；replace负索引按新长度。
用户回调原对象/context保留；binder错误不调用协议。bool或int子类的__index__返回是
CP成功+DeprecationWarning，warning转error后不删除；实际int子类直接输入成功且不调用override。
这不是上述三份新应用已经执行的证据。新应用只在父driver执行时CP核对；不另开预跑窗口。

## 首次验收

三个CP/JIT应用首次3/3通过，错误应用在build前核固定CP完整10项链。
新结构首次旧CallValue期待失败，按真实DynCallValue修测试后1/1通过。
新7项C#与原24项回归在测试numeric ABI字面量修正后31/31通过；
随后首次正常完整门禁通过（839 C#、228 CP基线、202 e2e＋原30 ignored）。
本文件不将warning/int子类pending记作已实现，也不冒新项目或性能结论。
