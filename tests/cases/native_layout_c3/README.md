# 一个 native 布局与 C3 的待验合同

全部清单保持 pending。CPython 参考有效不等于产品通过，不修改固定项目分母或黄金。

拟纳入最小通用核心：

- layout_orders：dict 加普通 mixin 及反序，真实 MRO 与映射存储。
- shared_owner：多路径回到同一 dict 祖先的菱形与再派生。
- protocols：已有方法/属性/staticmethod/classmethod、super 和 dict 根描述符；属性与键独立。
- nested_factory：函数内当次 self._dict + marker 创建，dict 与用户 dict 子类，重复调用不缓存类身份。
- evaluation_publication：属性基值恰好一次，局部旧名在类体/装饰器成功前保持，失败不发布。
- layout_conflicts：dict/list、dict/str 真实布局冲突及相同布局上的 C3 冲突，拒绝不能污染既有类。

单列待定边界：pending_independent_dict_roots。CPython 两个独立 dict 子类仍能共享 dict 物理布局；
不能据两个用户类对象就认定布局冲突。初始核心是否覆盖由布局权威设计决定，完整 CP 合同保留。

本组不要求同时开放 list/str 的成功多继承、新的非空 slots/custom descriptor/metaclass、
动态异常布局或一般 __mro_entries__。这些域外合同不能通过假成功占位或删除旧守卫实现。
只比较实际 Python 可观察行为，不按任何第三方项目名特判。
