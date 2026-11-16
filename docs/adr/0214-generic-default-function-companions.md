# ADR-0214：typed 函数缺省实参的通用伴生核心

- 日期：2026-11-16
- 状态：首切定向语义/JIT/NativeAOT通过；最终集成与性能门待验
- 接续：[ADR-0208](0208-cil-execution-libpython-kernel.md)、[ADR-0212](0212-live-function-defaults.md)

## 问题与决定

跨模块取得原无默认值的typed函数后，设置字符串等默认对象可成功绑定，但旧thunk
仍向原typed核心拆箱，造成Python标注之外的参数限制。当前跨模块pending保留该原始见证。

每个准入的可发布typed定义生成一份通用CIL伴生核心，canonical仍持唯一PyFunction、
签名、默认值、注解和定义身份。原typed核心、静态直调与完整位置thunk的ABI和热路径
保持；正常Binder成功且由缺省值补齐参数时才进入通用核心。当前已收敛Dynamic的定义
保持单核心；单模块封闭数值helper保持原发射。

跨模块图中，参数非空、当前Static、无env/context/生成器/协程的顶层定义为首切候选。
通用体从同一HIR按当前函数Dynamic视图重新降级，复用同一模块槽、bound flags和类身份。
伴生FnId拥有独立局部/CFG，关联同一canonical及CodeSource；禁止二次def求值或发布。

通用体的内部符号采用Python标识符不可含的`$generic$FnId`后缀，避免与用户定义重名。

通用体内部采用显式通用调用分派。编译器在真实def创建后以实际PyFunction弱键登记
静态生成的通用thunk；分派使用该实际对象，不按名字或签名猜测函数体。这样通用值
进入其它typed函数时继续走对应通用体，避免下游再次拆箱。注册和分派属于冷路径，
普通PyFunction实例字段与调用ABI保持原状。

## 验证合同

- 原跨模块pending以及两模块同名函数、默认tuple/dict、可变对象/大数身份、递归与传递调用。
- 实参和callee各求值一次，默认值实时读取，异常/清理/帧只记录实际一次执行。
- canonical/companion一对一、同unit来源、FnId重定位及非法发布/伪配对负例。
- 原typed参数/返回ABI与CIL、闭包/实例/状态机的既有单核心路径维持。
- JIT、NativeAOT、搬迁和固定版本CPython分别验收；性能配对后决定准入。

完整通用调用与typed特化恢复分别推进。代码体积、冷登记和构建成本单列实测，首切
不切换已有带默认值函数的Dynamic正确性选择，也不移除既有缺省直调守卫。

## 首切边界与代价

只在driver已冻结的多源码模块图中启用；单模块dump仍按其既有单模块合同输出。
没有默认值的可发布Static定义按上述准入条件复制一次；已有Dynamic定义、零参数定义、
实例模块/闭包/类方法/状态机不增加核心。每个准入定义增加一份通用方法、一份冷thunk、
一次弱键登记；signature、defaults、env和def求值仍各一份。冷表在独立嵌套类型中延迟初始化，
未用伴生能力的产品无需分配该表。通用内部调用使用对象数组及Binder，成本仅落在冷通用域。

公开完整供参的既有typed thunk边界继续保留：本切选择条件为“Binder成功且实际补齐缺省值”，
没有为完整供参增加精确类型守卫，也没有扩大其任意对象调用承诺。单源码图经未知外部扩展
取得函数、完整供参的typed边界，以及对已有Dynamic定义恢复typed特化分别另切。

通用体的partial/staticmethod/classmethod/绑定方法包装显式透传通用调用域；无TLS、
动态环境变量或二次解释执行。外部用户回调进入自己的普通函数入口时继续遵循其既有模式，
本切没有宣称全生态动态调用闭包证明。`inspect.signature`原生类与真实消费者仍待后续完整竖切。

## 验收记录

固定CPython 3.9.25/3.12.13/3.13.3同次生成oracle，JIT及macOS arm64 NativeAOT逐字一致。
NativeAOT搬迁后删除原输入与build再运行通过。原跨模块pending已转正；新增MIR三项、
Runtime七项、真实源码CIL/PE结构门通过。typed方法体及完整位置thunk前缀逐段一致；
codegen全库70通过、3个旧忽略。构建期类型模式不变，伴生体另在MIR显式计数。
最终全门及Release调用/分配配对仍由主代理冻结后完成。

同编译器显式启用/关闭的两typed定义结构样本：MIR核心3→5、实际CIL指令467→580、
PE字节10752→11264（+512B）；这是代码体积样本，运行性能另由冻结Release配对。
最终审计将未登记绑定方法的冷fallback归还原InvokeBound，保原super上下文；新增Runtime
方法上下文/真实owner正例通过。该修正在产品差分完成后发生，最终产品重验由集成门承接。

## 异构审查的普通dataclass场景反证

审查提出`@dataclass class Box: x:int`及`make_box(x:int)->Box:return Box(x)`可能
被误准入typed伴生体。真实源码的`class.rs`将普通dataclass固定为`runtime_dataclass=true`，
类型检查对该类构造收敛Dynamic；结构门进一步检查make_box只有Ordinary核心、Dynamic参数/
owner、无Unbox/NewObject且运行时类调用正向存在。因此本场景不进入typed布局分支，
严格Unbox门及当前准入实现保持。

两模块源码`c2_function_companion_dataclass.py`及helper保留原`make_box(1).x`输出1，
并追加可变默认对象、跨调用别名及同一类身份正例。固定CPython3.12.13与最新源码CLI的
JIT、macOS arm64 NativeAOT搬迁结果一致；NativeAOT搬迁后删除原输入/build再运行通过。
新增MIR定向结构门通过。原`c2_function_defaults_cross_module_pending.py`文件名及源码
保留，仅移除pending标记，以延续原用例ID。

## 完整门发现的range通用降级修正

全driver门的planner_static真实触发C2错误：伴生体采用旧Dynamic range的i64计数
优化，在边界插入两个Unbox，被严格通用验证拒绝。修正仅让伴生体先按源序冻结range
边界、构造已有原生range值，再复用ForEach的iterator/next/value CFG。typed原计数
核心与普通Dynamic的原计数快路保持；严格Unbox门保持。现有range值域与用户__index__
等边界沿用既有API合同，见[ADR-0135](0135-foreach-local-accumulator.md)。

新增MIR门同时锁typed I64 ABI/计数运算、generic无Unbox且range构造及三个迭代入口
各一处、普通Dynamic仍走原计数降级。新增两模块正例覆盖原完整实参、缺省对象身份、
bool边界、正负步长、break/continue/else、步长0错误与实参修改默认值。固定CPython
3.9.25/3.12.13/3.13.3、JIT、macOS arm64 NativeAOT搬迁删除输入/build后均逐字一致。
driver foreach整组6项通过，包含原失败与off/on语义矩阵；known_traceback_locals由主代理
按新的真实traceback边界单独修正，实验ledger与结构保护保持。此前冻结Release为预修复产物，
最终性能须从此修正后的源码重建并配对。
