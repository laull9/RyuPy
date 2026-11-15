# ADR-0200：受守卫的精确列表布尔写入循环

状态：已接受并默认启用，2026-11-15在macOS arm64完成验收。

## 动机与决定

原始sieve的内层`while j <= bound: flags[j] = False; j += step`每轮动态加法生成整数对象，
下标和比较再读取这些对象。采用一个四Dynamic参数的Runtime ABI，封装精确类型、下标范围和
存储守卫，成功执行原生long循环并只返回最终整数；失败返回CLR null且无写入，进入完整原CFG。
MIR负责闭合控制流和观察证明，codegen仅机械发射调用。

## 静态准入

只接受普通函数局部、两块自然循环及唯一预头。条件严格是DynRichCompareValue(Le,j,bound)
与DynTruthy；正文严格是装箱False、SetItem(list,j,false)、DynBinary(Add,j,step)、写回j，
另允许Line。四个输入局部必须不同，临时槽必须独立且所有环外语句/终结符均无读取或重定义。
不凭临时变量名字证明活性。所有读集合穷尽MIR变体。

整函数排除env、cell、module_state、EH、source handler、direct globals、locals反射、
Suspend、MakeGenerator和产品trace ABI。LoadChecked、回调、resize、额外赋值或侧出口均不在
循环白名单中。预头新增调用只读取原始局部，不提前执行Python协议。原条件与正文保留为慢路。

## 运行时合同

list必须是sealed PyList，j/bound/step必须精确long；拒绝bool、int、BigInteger和协议对象。
要求0<=j<=bound<Count和step>0，零轮沿原CFG保留j引用身份。只接受object或紧凑bool背板；
数值背板沿原路，避免物化副作用。每次合法写入False后原生步进；正溢出时精确构造BigInteger
终值，因终值大于long bound而退出。保留原容器与未改元素身份，最终正常整数使用BoxInt缓存。

## 取舍

快路移除循环内整数分配，最终BoxInt/BigInteger仍可能分配。极端资源失败时，j尚未发布，
trace行号保留while预头；原路径可能在某次步进失败并已发布更早j。这一资源耗尽时点和诊断
差异接受为低影响默认优化边界；正常异常、协议回调及越界全部由失败守卫交回原循环。
不引入新Python语法、CLI开关、动态IL或公开存储字段。新增一个人工维护public编译器ABI。

## 验收

真实原始sieve管线结构断言、观察/活性负例、Runtime背板与溢出测试、同次CPython差分，
真实JIT/NativeAOT源码删除搬迁、NumPy2.3.5回退、冻结旧新26项独占性能对照及make check均通过。
sieve重点140.26→26.43ms，分配减少104334096字节；全量重复收益及全部不利样本见
[验收报告](../aot-guarded-list-loop-optimization.md)。其他平台保留后续验收。
