# 通用 `__class__` 只读测试

- `identity`：普通实例、单继承、重复调用点、默认读取不触发
  `__getattr__`、普通类与已列内建类的真实元类；通过普通属性写入验证类版本失效。
- `duplicate_name_pending`：保留首次冻结的完整 identity 源和输出；顶层重复 Same
  定义编译报 E0102，函数内局部类也未支持。同名不同类身份仍由 C# 独立覆盖。
- `lookup`：类体普通值/None/property/函数/staticmethod/classmethod 及继承。
  类自身读取不能执行或返回其类字典同名项，`type(obj)` 不调用 getter。
- `errors`：每次 property 读取可观察、ValueError 原消息、AttributeError 的
  getattr 默认值/hasattr、无 getter。没有把一般 hook 回退补全混进本切。
- `property_constructor_pending`：保留原 errors 完整源及输出，零参数 `property()`
  仍被旧构造器拒绝；主用例以 CP 等价的 `property(None)` 验证无 getter 读取。
- `dict_setup_pending`：保留独立 CP 合同原样的 `obj.__dict__["__class__"]` 字段
  布置；反射仍未实现，不替换为 `obj.__class__ = ...`。只读字段优先级另由
  `PyClassAttributeTests.cs` 直接内部槽位夹具验证，不能把夹具说成 Python 反射支持。

每个目录的 `golden.stdout` 先用固定 CPython 3.12.13 运行冻结并再次逐字核验。
专项 driver 在启动构建/JIT 之前核验 CP 版本、源码、输出标签和冻结字节，独占保存
原始双流、退出码、工具/运行时/产物身份。`RYUPY_CLASS_ATTRIBUTE_CP_ONLY=1`
只自证 CP，不启动构建/JIT；`RYUPY_CLASS_ATTRIBUTE_EVIDENCE_ROOT` 指定证据根目录。
目录 `pending = true` 只在实际差分通过后转正；字典反射 pending 独立保留。

## 不扩大支持范围

此处不验收 `__class__` 赋值/删除、动态改类或布局兼容，不用非法给普通类的
`__class__` 动态赋值构造所谓 CP 参考。类体设置是普通类项，和类对象自身的
元类赋值是不同操作。PIC 失效用正常 `Plain.changed = i` 触发。

一般 `__getattribute__`、任意 descriptor、getter 抛 AttributeError 后进入
`__getattr__`、自定义元类与元类 descriptor、`object.__dict__` 根 descriptor
反射仍属独立合同。str/list/dict 派生同名用户覆盖仍由既有守卫限定，本切不撤销。
少量 PyType 固定点不是所有原生载体的协议验收。没有性能结论，既有性能回退仍未关闭。
