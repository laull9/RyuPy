# ADR-0214：typed 函数缺省实参的通用伴生核心

- 日期：2026-11-16
- 状态：实现中；语义、产品与性能门待验
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
