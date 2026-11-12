# 动态单基定义合同

前三组已通过 CPython 差分与默认/PIC=0 产品验证并去 pending；后三组继续 pending，CPython 有效不等于产品通过。

- source_paths：实际 import / reexport / alias / 属性 / 重绑定与下标求值顺序。
- publication：类体/装饰器成功前保持旧绑定，失败不发布，无旧名不泄漏半成品。
- single_layouts：已有动态 list / dict / str 单基布局能力不得被新合同收窄。
- pending_class_cell：Legacy 显式类 cell 独立待验，不以其它例成功隐瞒。
- pending_dynamic_metaclass：非类型基值在 CP 中涉及动态元类选择及类体副作用，独立保留待办。
- pending_creation_hooks：域外类创建钩子独立待办；CP 记录真实成功行为，产品不得以预期 NI 假装兼容。

首切不加入调用表达式基类、自定义元类、动态异常布局、循环导入或一般 GenericAlias 长尾；
这些不是为了让本组通过而放宽的条件。旧运行时拒绝守卫仍由对应 Runtime/driver 负测保护。
