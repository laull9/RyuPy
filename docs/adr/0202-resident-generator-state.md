# ADR-0202：普通生成器的字段驻留状态

状态：已接受；默认启用，完整门禁、原始26项和独立NativeAOT搬迁通过。

## 目标与分层

依据四项架构路线，先替换普通生成器每次挂起/恢复的全局部object[]搬运。
MIR生成私有引用类型并显式发射GetField/SetField；codegen只处理声明好的字段、恢复函数与
构造ABI。PyGenerator继续作为Python可见对象，拥有全部send/throw/close、running、完成值、
PEP479、异常上下文和traceback生命周期。无Reflection.Emit或动态程序集依赖。

控制状态采用内部PyGeneratorState：原生long PC、发送值、注入异常、字段载体引用。
编译器产生的具体载体保留原MIR字段类型，长期局部直接驻留；单次恢复只取得载体和PC，
不再逐yield全量保存和加载。完成时释放载体与控制引用。

## 首切准入与后备

只接受无EH、无source handler/direct globals合同、无env/cell、无yield from及product ABI的
普通生成器；locals观察沿原路径。生成器调用、装饰器、默认值和真实owner绑定继续使用原工厂合同。
复杂生成器仍由原数组降级处理，新旧生成器可通过正常迭代或yield from相互消费。
普通延迟降级与trace V1构建模式显式分离，推迟lowering本身不会启用V1动作。

默认build_program先构建高层CFG，选择字段或旧数组降级；带来源审计的旧入口及V1流程保留
既有数组合同。旧只读形参测试明确检查数组后备，字段默认路径另做真实源结构断言。

## 读写、活跃性和物化

local_access统一提供穷尽的MIR操作数读集合及重写，DeadValues复用同一读取器。
暂停边活跃性处理Branch/Goto/Suspend/Return/Raise以及恢复输入定义；预算为最多512块、
512局部和64轮固定点，不收敛则选择原降级。

全部用户命名绑定与参数保留强引用；匿名临时仅在跨暂停实际活跃时成为字段。
同一值的字段加载保留原对象身份，赋值完成后才发布字段，异常不重放之前的操作。
本切保持动态算术的原对象语义，没有把Python整数强制窄化；后续原生计数需独立的
范围/溢出与同一盒物化证明。原生PC与现有静态类型局部直接保持原类型。

短命编译器临时可更早释放，沿既有GC/refcount时机合同；用户命名绑定仍保留。
载体分配从数组改为控制对象和生成类型，资源耗尽时点可能不同；不承诺逐分配对应CPython。

## 验收

手写主e2e、真实generators.py结构命中、匿名表达式跨yield、EH/cell/委托/locals后备，
运行时返回值一次消费、输入覆盖、首次throw/close、完成清引用和新ABI验证。
然后同次CP/JIT、实际NativeAOT、删除输入搬迁、NumPy、原始26项独占旧新对照与make check。
当前首轮原始generators独占1warm7：旧284.045、新212.307ms，耗时减少25.3%；
分配145263520→145263392字节，基本持平。首切收益来自状态访问，动态整数物化与消费融合仍待实现。
完整门禁：C#3179、MIR382、cases-selftest761、编译e2e479通过；隔离/忽略项保持原门禁边界。
主用例CP/JIT两种PIC及NativeAOT删除输入搬迁通过；NumPy2.3.5两个生成器确认命中字段工厂，
外来整数身份与数组别名写入、随包NativeAOT删除全部输入搬迁通过。
函数体和注入异常的旧新AOT退出/完整双流一致；CP核对退出、stdout和最后错误行，
源码echo/caret格式沿既有边界。最终26项主表生成器耗时减少22.23%，独立10轮减少26.29%；分配/RSS基本持平。
完整数据与不利结果复测见[验收记录](../aot-generator-state-optimization.md)。
