# ADR-0205：跨域类继承中性桥与真实 CPython 派生对象模型（路线 B）

- 状态：已采纳（2026-11-16）
- 关联：`docs/general-project-compatibility-gaps.md`（路线 B）、`docs/cpython-provider.md`、ADR-0181

## 背景

通用 Python 应用（如 Tkinter 桌面应用 `SanBudget` 中的 `class ExpenseTracker(tk.Tk):`）经常需要定义继承自 CPython 模块或 C 扩展导出的类型的编译类。此前 `PyRuntimeBases.Validate`、`PyOps.MakeClassCore`、`PyLexicalClassFactory` 及 `PyModuleInstances` 仅接受纯用户 `PyClass` 与少数内部特化布局，遇到来自 CPython provider 的 `IPyForeignObject` 类型基类即抛出 `NotImplementedError`。

同时，`tk.Tk` 本身是 Python 类，而其内部持有的 `self.tk`（`_tkinter.tkapp`）才是 C 扩展对象；若在托管堆伪造 C 结构体布局或仅移除 `PyRuntimeBases` 守卫，会导致：

1. 属性分裂（CPython `Tk.__init__` 写入 `PyObject*` 的 `self.tk`/`self._w` 与托管 `_slots` 割裂）；
2. 将 `self` 传回 CPython（如 `ttk.Style(self)`、`tk.Frame(self)`、`self.protocol(...)`）时被包装成 `ryupy.ManagedObject`，失去真实类型布局与继承链；
3. C 扩展或 Python 基类通过 `self.method(...)` 回调子类方法时无法经过统一描述符绑定与 `super()` 上下文。

## 决策

1. **保持 Runtime 的 Provider 中性合同**
   - 在 `RyuPy.Runtime` 新增独立接口 `IPyForeignClassBridge` 与组合点 `PyForeignClassBridge`（不改动 `PyProviderAbiContractTests` 锁定的 `IPyForeignObject`、`IPyModuleProvider`、`PyProvider` 公开成员签名）。
   - `PyProvider.Install` / `Remove` 自动同步注册/注销实现 `IPyForeignClassBridge` 的 provider（即 `CPythonHost`），单元测试也可独立安装 mock 实现。

2. **由 CPython 真实构造派生类与实例，不伪造 C 对象布局**
   - `PyClass` 在类体执行期间保留用户定义成员，并在 `FinalizeOrdinaryConstruction` 时通过 `IPyForeignClassBridge.CreateDerivedClass` 在 CPython 侧调用真实基类元类（默认 `type` 或基类真实元类）创建真实 `PyTypeObject*`，存入 `PyClass.ForeignClass`。
   - 类体内的 `PyFunction`、`PyStaticMethod`、`PyClassMethod`、`PyProperty` 在导出到 CPython 类字典时包装为真正的 Python 描述符（普通函数包装为带 `tp_descr_get` 的 Python function，绑定 `ForeignMethodTarget` 在回调进入时为 `UsesSuper` 方法恢复 `PyMethodContext`；`staticmethod` / `classmethod` / `property` 使用 CPython 原生描述符）。
   - 实例化 `App(*args, **kwargs)` 调用 `ForeignClass.Invoke(...)` 由 CPython 执行 `type_call`（真实 `__new__` 与 `__init__`）。

3. **双域统一身份与属性权威**
   - 真实 CPython 实例 `PyObject*` 与托管 `PyInstance(cls, foreignInstance)` 一一对应：`CPythonHost.ToPython` 将 `PyInstance` 解包为真实 `PyObject*`（不包成 `ManagedObject`）；`FromOwned` 通过 `objects` 弱缓存与 `Py_TYPE` 对应的已导出 `PyClass` 恢复同一 `PyInstance`（并保持 `PyClass` 在 `ForeignClass` 与 `PyClass` 之间的双向同一身份）。
   - 跨域实例的属性读写/删除、魔术方法、比较、哈希、真值、字符串化、下标、迭代、成员检测、`isinstance` / `issubclass` 统一委托给真实 CPython 对象与 `IPyForeignClassBridge`；`PyPic` 快速路径显式绕过 `HasForeignHierarchy` 类与实例，避免错误读取空的 `_slots`。
   - 零参 `super()` 与显式 `super(cls, self)` 在遍历完用户 `PyClass` 祖先或遇到 `ForeignClass` 时，委托到 CPython 真实 `super(owner_foreign_type, receiver)`。

4. **生命周期与关闭**
   - `PyInstance.ForeignInstance` 持有 `PythonObject`（受 `CPythonHost` 句柄追踪与引用释放队列管理）。
   - 若托管方法闭包（如 Tkinter `lambda` 回调）捕获了 `self`，会形成正常的跨堆引用；由窗口销毁/回调注销或宿主关闭时 `Py_FinalizeEx` 触发 `managed_dealloc` 清除 `Roots`，关机后迟到释放只记 `LateReleases`、不重入解释器。

## 后果与边界

- **正向收益**：支持单继承（及 JIT 多继承）CPython 纯 Python 类与 C 扩展类（如 `tk.Tk`、`collections.UserDict`、`collections.deque`），实例构造、属性读写、`super()`、回调、MRO 与类型反射均与 CPython 一致，适用于 JIT 与 NativeAOT 单继承路径。
- **保留边界**：
  - 原生模式（未安装 `IPyForeignClassBridge`）仍明确拒绝外部基类，不静默退化。
  - `super()` 的反射属性（`__thisclass__` 等）沿用既有 `PyLexicalSuper` 守卫边界；NativeAOT 多继承仍受编译期既有守卫限制。
  - 跨域异常子类若无编译期 CLR 异常工厂仍受既有限制。
