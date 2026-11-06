# 模块裸名删除合同

本切实现模块顶层 `del name` 与函数中显式 `global name; del name`。普通静态模块与
显式 B2 实例共享 Python 结果，但各用真实存储，不把默认静态模块包装成假的字典。
公开 loader、可写 sys.modules 和完整 html.parser 不由本切开放。

## 删除和读取是不同操作

- 删除只看当前模块的绑定，不查 builtin。不存在时抛 NameError，常用消息为
  `name 'name' is not defined`。builtin 存在也不能使删除成功，更不能删掉 builtin。
- 多个独立目标按从左到右执行；中途失败保留前面的删除，后面的目标不执行。
  不先验证整句、不回滚。tuple/list 包裹的嵌套目标仍明确 E0100。
- None 是有效值。删除改变绑定而不修改原对象；保存的函数、类、容器和 import alias
  仍是原对象。之后赋值/定义/导入重新建立同名绑定。
- 删除后的读取才考虑 builtin。只接已有真实 builtin 对象的默认表，未实现 builtin
  继续明确 NotImplementedError，不把缺失的 API 伪造成 None 或普通 NameError。
- 这里不宣称异常对象的完整反射属性（例如原生 NameError.name）已经实现。

## 默认静态模块

编译器在降级定义前按词法作用域收集 may_delete。函数只认自己的 global 声明，
不继承外层函数的声明；类 namespace 与 nonlocal cell 不会被提升成模块删除。
删除目标不是读取，因此不会仅因 `del name` 把整个 main 及所有全局降为 object。

可保持静态类型的普通全局仍有原 typed 字段与独立 bool 绑定位。删除先检查位，再清位，
保留 backing 值，不装箱也不重建模块。动态槽先检查实际值，再写原有 PyUnbound 哨兵。
GC 与引用保留时间沿用既有边界，不承诺 CP 引用计数的即时析构。

可能回退 builtin 的可删名字从首轮类型推导起就是动态绑定。公开函数/类名称同样走
真实绑定，内部 FuncId/ClassId 不能绕过删除；callee 查找先于参数，基类查找先于类体。
可删异常名使 except 使用真实目标表达式，不能保留旧 CLR 类型 catch 身份；目标 tuple
须完整求值。可删 range 禁用只按文本识别的计数循环快路，真实调用后走迭代协议。
没有删除的程序不因此新增这四个 helper 引用或改变原有路径。

模块外部属性读取和新 from import 看到绑定缺失；旧 from import 保存的对象不受影响。
默认模块没有真实可写 __dict__，也没有新增属性删除协议，不能把这些边界当作已完成。

默认模式明确拒绝：

1. 任何可达/不可达、顶层/函数 global 的 __name__、__doc__、__annotations__ 删除。
   定义元数据、初始 docstring/None 与模块注解懒建协议尚未全部统一，不能泄漏哨兵或
   错误声称不存在。已有 B2 模式的真实字典语义不受此默认守卫限制。
2. 用户 __builtins__ 绑定与任何模块删除同时存在。普通自定义 builtin 键也可能影响
   后定义函数，不能只拒绝 len 等已知 builtin 名。仅删除 __builtins__ 同样拒绝。
3. cell/nonlocal、class namespace 与嵌套目标删除不借本切开放。现有普通局部、属性、
   下标删除的既有准入规则不扩大。
4. 同模块可删/重绑的builtin异常名又作用户异常基类时，失去静态基类证明；现有runtime
   特殊基类协议仍明确NI，不能为了捕获异常的用例偷用旧类型身份。

真实源码模块的 __spec__/__loader__/__cached__/__path__ 来自当前执行的元数据初值，
__package__/__file__ 也有真实前言赋值。删除可改变其实际绑定，但未实现的 builtin
元数据回退仍明确 NI；不能据此宣称完整元数据删除协议。

## 显式实例模式

只有明确选择 --instance-module 的模块使用实例字典。生成代码把经过 MIR 来源与支配
验证的 context 传入 DeleteName，直接删除唯一 globals 中的键；不复制字典、不切换
静态槽、不根据可写 __module__ 反查模块。

旧函数移入 B、改 __module__ 后仍删除 A 的 globals。嵌套函数、方法与生成器继承其
真实 owner；生成器跨暂停与同 dict 重执行仍访问原字典里当下的值。外部注入的键与
源码写入的键没有区别；保存的旧容器、函数和类不会因为删除/重执行而变成新对象。

globals 的 __builtins__ 键与 frame 已捕获的 builtins 引用是两件事。删除键不改变旧
frame/函数的捕获；下次 Execute 补默认环境也不会重定向旧函数。自定义 builtin 中有
某名字而 globals 没有时，读可以回退，删除必须失败，两个字典不能相互替代。

实例现有的 annotation、super/__class__、特殊 base、metaclass、急切推导式等守卫保留。
内部 Create/Execute 是真实 PE 验证入口，不是假冒 Python module_from_spec/exec_module。

## 验证原则

完整 CP 源和 golden 永不为了通过而裁剪。普通用例的通过需要实际新编译 PE/JIT 的
stdout、stderr 和退出码一致；双实例与暂停重执行另用真实 Python PE 的 CLR 宿主。
宿主只提供输入事件和检查，不手写被测函数正文。完整边界保留原失败，单独的新窄例
不能代替原源成功。CIL 与性能分别验收，暖分配不变或引用惰性不能证明没有时间回退。

## 原项目与未改标准库闭包复验

干净功能提交 6502e10612c9f09142dbbafcbbf73bb3add03d3c，tree
b4b6d6d69ecd0b3368266895e153a73a2cea909b，使用最终 CLI/Release runtime。
原 docopt 0.6.2 与 Markdown 3.5.2 的入口、golden、源树和原搜索根顺序均未改。
两个项目的 CP 与 compile 各执行一次，均 exit0；docopt CP stdout 16 字节等 golden，
保留原 860 字节 SyntaxWarnings。原 Markdown 的 CP 结果也严格匹配 golden。

首次两次 JIT 均 SIGABRT(-6)：docopt stdout 为空、stderr 60 字节 Exception*，
Markdown stdout 为空、stderr 15 字节 Out of memory。核对发现新 runner 额外给子进程
设置 RLIMIT_FSIZE=4MiB，旧 runner 无此限制。不能把这两次中止直接判为源码回归，
也不能隐藏失败或把它写成原来的应用结果。

另获授权后只做一次校正窗口：保留同一已生成 PE、runtime、命令、cwd、env，
仅移除新增 FSIZE 限制；仍有父进程输出/空间监测及原 30 秒时限。每个 PE 各 JIT
一次，不重复 CP 或 compile。docopt exit0，stdout 16 字节等 golden，stderr 为空；
Markdown exit1，stdout 为空、stderr 375 字节，实际首错仍为 htmlparser.py:39 的
find_spec('html.parser') 因 native/legacy 无 source metadata 明确 NI。
两份输出逐字重现旧 B2。只移除限制后结果改变是观测事实，未证明底层 OOM 因果。
原失败窗口逐文件未改，全部原流、输入及前后干净 Git 审计保留，不计 Markdown 通过。

固定 CPython 3.12.13 的四文件窄闭包也仅编译一次。原 _markupbase.py:20 的 del re
诊断消失；新的实际首条为 html/parser.py:21:25 的 re.compile，被实例模块环境名字
`compile` 守卫拒绝，共 19 条 compile 与 2 条 super 的 E0100。exit1，stdout 为空，stderr
7991 字节，无 PE、无 JIT；这不是默认原项目配置，也不能称 parser 已兼容。
下一前置是准确区分属性访问和环境裸名，并按真实语义处理实例中的 super 等边界。

证据根为 /workspace/shared/ryupy-module-delete-validation：

- projects-plan-001/result.json：
  a2e6c004c71ec7e618fc1c3ed98a8ef0f4f236e075e586412ecccd4e97e7541e。
- jit-correction-001/result.json：
  564d1385fa389813f9713531da0bc8794e864f547cacee108a223e7c9a0e501a。
- source-reprobe-plan-001/result.json：
  d40e621b21b14699a460b65474b213d97180f231aa4379c9592ad3444303bb9a。
