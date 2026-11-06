# 显式模块实例基础（B1）

本文保留B1已验历史合同。B2函数/类扩展的当前范围与验收状态见[定义归属合同](module-owners.md)。

本切提供真实存储和编译器内部执行能力，不开放 Python 的 `module_from_spec`、
`create_module` 或 `exec_module`。最终验收状态见 [ADR0123](adr/0123-module-instance-foundation.md)。

## 选择与能力边界

`build/run --instance-module NAME` 只选择已经在实际源码依赖图中的模块，可重复使用。
若只需预编译而不在入口导入，另用 `--include-module NAME`；包含本身不执行正文。
未选模块仍是默认静态模块，失败导入旧对象的静态槽别名问题没有因此全局消失。

B1 仅支持 JIT 的模块顶层。选择不存在的目标或与 AOT 组合，在生成产物前报 E0100。
选中模块中的函数/类定义、lambda、推导式、生成器、相对导入、注解赋值/类型别名、
del、except handler、`__debug__` 以及尚未接好的环境 API 同样先报 E0100；
不可达源码也检查。环境名字的限制包括同名属性和导入别名，是明确的保守边界。
不据此宣称动态函数、类、方法、闭包或生成器已经拥有正确模块环境。

## 真正的实例状态

内部不可变代码描述可创建多个真实 PyModule。每个实例只有一个权威 PyDict，
模块属性、`__dict__` 和生成正文的名字读写访问同一字典。没有静态全局镜像、
交换当前模块槽或执行前后拷贝属性的模拟。外部注入未声明名称、改变值的类型、
删除字典键和设置 None 都在下一次真实读取时生效。

生成的实例正文接显式执行环境参数。创建时初始化元数据但不执行正文、不写模块缓存。
再次执行保留字典身份和未覆盖键，按真实源码重新执行赋值；源码 docstring 是正文赋值，
不是每次初始化元数据。直接执行失败不回滚之前写入，也不管理缓存。普通 import
失败只移除仍指向本次对象的缓存项；再次 import 创建新对象，旧对象及其容器不被覆写。
此内部执行协议不冒充 SourceFileLoader 的名称检查或公开 loader 方法。

## 名称与 builtins

名字按本实例 globals、随后本次执行捕获的 builtins 字典查找，赋值只写 globals。
range 等名字不保留静态特殊入口。默认 builtins 是真实共享字典，值复用现有稳定内建
函数/类型对象。缺少 `__builtins__` 键时在正文前插入默认字典；已有非 PyDict 值则
明确 NotImplementedError，并非 CPython 接受 module 对象或延迟报错的完整协议。

一次执行捕获字典引用，不复制内容。替换 globals 中的 `__builtins__` 只影响下一次
执行；就地修改已捕获字典对本次执行可见。显式 context 使重入不切换外层环境。
默认表尚未实现的标准 builtin 明确 NotImplementedError；用户 globals 的覆盖仍先取，
自定义 builtins 中缺名则为 NameError，不偷偷回退默认字典。

每条 import 在执行时检查已捕获的 `__import__`。缺键抛 ImportError，替换为自定义 hook
则在调用 hook 前明确 NotImplementedError；不能悄悄绕过用户替换。默认 `__import__`
是真实稳定函数，支持精确字符串绝对名、整数零 level 和空 list/tuple fromlist 的有限域，
加载完整名称后返回顶层模块。其它导入 hook/相对协议没有因此完成。

## 默认路径及性能

未选模块不发实例名称读写，也不为普通函数增加 owner 参数或实例状态查找。
实例公共全局不发静态字段/getter/setter；后端按 MIR 显式存储标记处理，不根据名字猜测。
注册能力、执行 context、字典访问和冷关联仍有成本，不能由结构隔离推断零回退。
真实 PE、完整门禁和唯一有界性能矩阵分别验收，不以 C# 手写正文替代生成代码。

公开 loader、函数归属、完整 builtins 反射、动态 finder/path、可写 sys.modules、
native/legacy 的发现及完整 html.parser 仍为后续任务；原 Markdown 项目不计通过。
