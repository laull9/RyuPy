# 一个 dict 布局与 C3 的有限合同

九组 freeze3 已通过 CPython 3.12.13 与默认/PIC=0 产品逐字节差分，
包含无 slots 类晚加元数据变体；清单去 pending 后再运行严格双模式门禁。
这只证明以下有限行为，不代表全部 native 多继承或全部 Python 类协议兼容，也不改变固定项目分母与黄金。

- layout_orders：dict 加普通 mixin 及反序，真实 MRO 与映射存储。
- shared_owner：多路径回到同一 dict 祖先的菱形、再派生与 cooperative 初始化链。
- protocols：已有方法/属性/staticmethod/classmethod、super 和 dict 根描述符；属性与键独立。
- nested_factory：函数内当次 self._dict + marker 创建，dict 与用户 dict 子类，重复调用不缓存类身份。
- evaluation_publication：属性基值恰好一次，局部旧名在类体/装饰器成功前保持，失败不发布。
- layout_conflicts：dict/list、dict/str 真实布局冲突及相同布局上的 C3 冲突，拒绝不能污染既有类。
- pending_independent_dict_roots：两个独立用户 dict 子类由同一个 dict 布局提供者支撑；
  已实际验证并转正，目录名前缀仅为历史名称，不代表当前清单状态。
- string_protocols：保存 __str__、super 根描述符与常规 str/repr；保留审查发现的真实回归合同。
- slots_metadata：原空 slots 和原无 slots 两类冻结后的元数据重绑，都不能改变已存在的物理布局；保留审查回归合同。

本组不开放 list/str 的成功多继承、新的非空 slots/custom descriptor/metaclass、
动态异常布局或一般 __mro_entries__。不同 native 布局和域外元类的旧守卫不能删除。
只比较实际 Python 可观察行为，不按第三方项目名特判；Native AOT 仍需独立验收。

真实 toml 的函数内 dict 工厂已由父线程实际推进，但完整项目仍阻塞于 decimal，
不能据本组通过宣称 toml 已兼容或提高项目通过数。
