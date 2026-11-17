# ADR-0227：stock 薄函数的 closure、GC owner 与有限元数据合同

- 日期：2026-11-17。
- 状态：独立探针两版及 JIT/NativeAOT 回收通过；Host 候选两版通过，Host 两版 JIT/NativeAOT 与完整检查已验；编译器资格、产品搬迁和性能继续待办。
- 接续：[ADR-0223](0223-stock-libpython-bounded-glue.md)、
  [ADR-0224](0224-cpython-thin-function-core-handles.md)、
  [ADR-0226](0226-stock-thin-function-product-contract.md)。
- 重跑：[元数据探针](../../tests/libpython_embedding/thin_guard/README.md)、
  [双 GC 探针](../../tests/libpython_embedding/core_handles/README.md)。

## 1. 最终决定

采用 **A closure + 直接 GC owner**：真实 `def f(x): return p(x)` 由 CP binder 绑定，
P 是静态 native dispatch，P 的 self 是参与 CP GC 的 owner。owner 强持 F，直接持纯
native context/core token；不使用 capsule、raw-F 生命期见证或内部 Function watcher。

```text
F -> globals D                 CLR H -> 独立 owned F
F -> closure tuple -> cell -> P -> GC owner -> F
                                  owner -> 强 core K -> weak H
```

owner 的 `tp_traverse` 精确访问 owned F 与 heap type；`tp_clear` 幂等清 F，先置 NULL 再
DECREF；`tp_dealloc` 在任何 DECREF 前摘除 borrowed 登记链，再清字段、单点释放 core、
释放实例及 heap type 引用。K 不持 F、D、Host、owner、lease 或业务参数/结果强引用。
P、P.__self__ 或 closure cell 单独外逃会保持 F/D/K，最后一端释放后由 CP GC 收集整环。
直接 P 调用按实际 caller frame 拒绝，禁止根据外逃 P 猜测 F 已死亡。

正常 Python `__del__` 在 GC 内立即调用 F、保存并复活 F，必须保留身份与调用能力。
这一结构由正常强引用保证环境有效，无需读取 weakref 已清空后的函数地址。

## 2. 淘汰路线及实证

**B/C 常量 P + 强 owner 淘汰。** 两个固定版本的常规 GIL `PyCode_Type` 无 GC traversal。
F -> code/co_consts -> P -> owner -> F 中的 code 边无法被 CP GC 完整遍历；本地初测
A/B/C 共九个对象时仅 A 三个析构，B/C 六个保持存活。给 owner 增加虚假 borrowed
traverse 或强行 Track 非 GC code 都不成立。产品退出 code.replace 实例化路线。
上游：[3.12.13 codeobject.c](https://github.com/python/cpython/blob/v3.12.13/Objects/codeobject.c)、
[3.13.3 codeobject.c](https://github.com/python/cpython/blob/v3.13.3/Objects/codeobject.c)。

**raw-live watcher + weak callback 淘汰。** GC 会先清 weakref 再执行 finalizer，弱引用死亡
不能决定 F 是否已销毁。尝试 DESTROY 时重建 weakref 并在 callback 撤销 raw 指针，
虽通过普通复活测试，仍有 P1：后续 CP 内部回调 tuple 分配失败可能跳过 callback，
使 raw 指针保留到 F 已释放后。新结构彻底移除这条依赖；失败方案源码退出工作树。
上游：[3.12.13 weakrefobject.c](https://github.com/python/cpython/blob/v3.12.13/Objects/weakrefobject.c)、
[3.13.3 weakrefobject.c](https://github.com/python/cpython/blob/v3.13.3/Objects/weakrefobject.c)。

初始失败注入还发现未跟踪 F 直接 DECREF 会违反 stock func_dealloc 的跟踪约定，
曾产生 SIGSEGV；修正为失败时主动断开 owner/F，再恢复 GC 跟踪并释放局部 owned 引用。
所有失败日志与先前诊断成绩保留在仓外，正式成绩采用最终 closure/owner 源码。

## 3. 必要胶水与元数据

固定源码为 `factory(p)` 内的 `f(x): return p(x)`，仅编译并提取嵌套 code，创建时通过
PyFunction_New、PyCell_New、PyFunction_SetClosure 组装，**不执行 factory**。
实例 P 只在 closure 中，code 常量不持实例对象；完整初始化后恢复 F 的 GC 跟踪并发布。
元数据探针来源 `<ryupy-thin-guard-A>`，产品来源 `<ryupy-stock-thin-A>`，code/frame/closure
如实属于胶水；业务运算在静态 CIL body，业务来源通过独立元数据登记。

| 操作 | 最终有限合同 |
| --- | --- |
| f(value)、f(x=value)、原生回调 | CP binder；保留输入和结果身份 |
| 缺参、多参、重复 x、未知关键字 | CP TypeError，CIL body 不进入 |
| name/qualname/module/annotations/属性字典 | F 单一权威，H 直接转发，无镜像 |
| defaults/kwdefaults 合法写入 | setter 成功；任何非 None 值使候选调用能力失效，恢复后可用 |
| 已登记 F 的 code setter | 原生 audit 在赋值前拒绝，保留 code 身份；普通函数 setter 对照正常 |
| closure cell 写成 abs | CP 直接 F 按赋值后的 Python 行为返回；候选 H 在调用前明确拒绝，恢复 P 后可用 |
| trace/profile 只观察 | 显示真实 glue call/line/return 与 native c_call/c_return，不伪造业务帧 |
| 不同 globals 的函数复制 | 当前 caller code/globals 核验拒绝；同 code/同 globals 复制与高级 C API 另立消费者合同 |

closure 是真实可写 cell。有限候选只对仍绑定原 P 的函数承诺静态 CIL 执行；用户改变
内部执行绑定后的直接 CP 行为按 CP 语义发生，H 显式报告能力失效。不存在对任意
原生扩展或调试器的全局执行封锁。观察器保持原覆盖口径，完整业务闭包与胶水清单继续开发。

## 4. 两种复活阶段

普通 Python finalizer 发生在 GC clear 前；最终两版已覆盖 finalizer 内即时调用、保存 F、
旧 weakref 已清空、复活后正常调用/首个 code setter/H 回流及随后再次回收。

原生 Function DESTROY watcher 可在 `tp_clear -> DECREF(F) -> func_dealloc` 中复活 F。
同次 stock 普通 closure/D 环与候选的公开 getter 均观察到：
`(code存在, globals存在, closure存在) == (1, 0, 0)`。对象虽被保活，环境已清理。
探针仅观察字段和最终回收，不调用该不完整对象；初次误调用的 SIGSEGV 日志单独保留。
这个高级原生消费者的完整执行恢复尚未准入，不能与普通 `__del__` 复活混用。

## 5. 实测结果

2026-11-17，macOS arm64、主解释器、常规 GIL，各以 stock PBS **3.12.13 / 3.13.3**
自身头文件和共享库构建。元数据 C 探针两版退出 0、完整 stderr 为空，结果相同：

`RESULT failed=0 finalize=0 live=0 destroyed=42 calls=16 blocked=6`

覆盖三处创建失败、三十轮 D/F 环、P 外逃、owner/cell 单独持有、合法 weak callback、
cell 写入/恢复、Python finalizer 复活和上述 clear 期 stock oracle。全部回收在 finalize 前。

独立双 GC 探针两版各 JIT/真实 NativeAOT 通过：每组合原 48 函数、三处失败、一个复活
函数，52 次 owner 析构、172 次 CIL 运算，core 根、owned H、释放队列、native live 全零。
产品 Host 两版各四个独立 JIT 进程及六项托管测试通过：普通矩阵、audit 安装拒绝、
外部 watcher 槽耗尽仍能启用、普通 finalizer 复活；精确状态见 ADR-0226。

这些测试未修改默认资格或 Bootstrap，候选继续默认关闭；产品 Host NativeAOT 与完整
make check 已完成；编译器实际 body、产品搬迁、正式性能与三平台扩围继续独立验收。所有构建产物仓外清理。
