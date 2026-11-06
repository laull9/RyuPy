# 显式模块实例的函数与类归属（B2）

本切继续使用B1的显式--instance-module选择与JIT-only边界，实际验收状态见
[ADR0124](adr/0124-module-definition-owners.md)。不开放Python loader。

## 真实归属

实例只有一个globals字典。函数的__globals__与该字典是同一对象，__builtins__是定义时
捕获的真实字典；这两个属性只读，不能通过普通函数__dict__遮蔽。把函数赋给另一个模块
或改其__module__不转移归属。未选旧静态函数尚无真实字典，读取环境属性明确NI，不造镜像。

参数、局部和自由变量先按词法作用域解析，global才进入实例字典。nonlocal共享真实cell，
不同factory调用的cell分离。默认参数在每次def执行时求值，同一函数共享其可变默认值，
新def取得新默认值。Env仅保存cell，不偷偷塞入模块状态。

每个已进入frame使用自己的builtins引用。就地改变该字典立刻可见；重绑globals中的
__builtins__不改旧函数/旧frame，但后续def从最新globals重新选择。缺键时新def继承
当前frame且不插键；模块Execute的缺键补默认行为仍与此不同。已有非PyDict值提前NI，
并未实现CP允许module/custom mapping或延迟到名字读取才报错的完整域。

生成器factory与resume保原owner，暂停后跨实例调用、重绑字典、同模块重新执行均不能
使其转向其它环境。旧函数仍读原globals里的最新值；新旧函数的captured builtins可不同。
异常或失败import后逃逸函数保旧globals，重试import的新实例不能覆写它。

## 普通类的有限域

支持模块级普通类、有限单继承、普通方法、staticmethod/classmethod/property以及不依赖
super/__class__的生成器方法。类对象、类可变成员和方法均按真实定义创建；类内名字用于
类体及方法默认值求值，方法正文仍按自身词法作用域解析，不能把类namespace当closure。

class的decorator表达式先执行，再检查外frame的__build_class__，然后捕获类体环境，
随后求基类。类__module__在基类之后、用户body之前按globals及captured builtins查找；
方法def再次捕获当前globals的builtins。普通类体与decorator完成后才发布公开名称。
缺hook抛NameError，自定义hook在调用前明确NI，globals同名hook不替代builtins。
NameError消息与相位单独验；现有运行时尚未开放其name属性，不宣称异常反射完整。

直接调用__build_class__、metaclass/class关键字、多继承、函数内/类内class、实例
@dataclass及特殊native基类布局没有开放。普通object或普通用户类之外的基类明确失败。
__qualname__仍是真实词法来源；当前可用class仅模块级，不能据此宣称嵌套类支持。

## 仍拒绝的源码

急切list/set/dict推导式、async、注解赋值/类型别名、del、except handler、相对import、
__debug__以及globals/locals/vars/eval/exec/compile/__import__/super/__class__保守
前置E0100，包含同名属性与导入别名及不可达代码。普通函数参数/返回注解不在此禁域。
普通lambda、闭包、函数/模块域genexpr、yield/yield-from与try/finally采用已有语法范围；
类体推导式包括genexpr仍明确E0100。genexpr在外层iterable求值前捕获新环境，元素延迟执行。

上述能力只针对明确选中实例模式；默认静态模块别名限制不因此全局消失。内部CLR创建/
执行与只读缓存验收不等于SourceFileLoader协议、可写sys.modules或Markdown通过。

## 验收与性能边界

完整门禁及最终77+64项真实生成PE通过，见ADR0124。默认CIL/函数布局不变不等于
性能无回退；正式384暖样本保留明显method/module不利，一次同版本/JIT诊断未能可靠
归因或关闭风险。全部结果与停止条件见[性能报告](module-owners-performance.md)。
